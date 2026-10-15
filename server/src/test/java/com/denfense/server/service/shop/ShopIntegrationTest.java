package com.denfense.server.service.shop;

import com.denfense.server.auth.*;
import com.denfense.server.domain.*;
import com.denfense.server.dto.shop.ShopDtos.*;
import com.denfense.server.exception.*;
import com.denfense.server.repository.*;
import com.denfense.server.repository.shop.*;
import com.denfense.server.service.gacha.*;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;
import static org.mockito.ArgumentMatchers.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties={"spring.datasource.url=jdbc:h2:mem:shop-integration;MODE=MySQL;DB_CLOSE_DELAY=-1","mydefense.shop.development-ads-enabled=true"})
@ActiveProfiles("local") @AutoConfigureMockMvc
class ShopIntegrationTest {
    @Autowired ShopCatalogService shop;
    @Autowired UserRepository users;
    @Autowired UserAlienRepository inventory;
    @Autowired AlienSpecRepository aliens;
    @Autowired ShopReceiptRepository receipts;
    @Autowired ShopAdChallengeRepository challenges;
    @Autowired GachaPurchaseService gacha;
    @Autowired GachaPurchaseRepository gachaReceipts;
    @org.springframework.boot.test.mock.mockito.SpyBean com.denfense.server.service.balance.ShopBalanceRegistry shopBalance;
    @Autowired MockMvc mvc;
    @Autowired ObjectMapper json;
    @MockBean ShopTimeProvider clock;
    @MockBean ShopRandomSource random;
    @MockBean GachaRandomGenerator gachaRandom;
    long uid;
    @BeforeEach void setUp() {
        when(clock.now()).thenReturn(Instant.parse("2026-09-19T03:00:00Z"));
        when(random.nextInt(anyInt())).thenReturn(0);when(gachaRandom.nextInt(anyInt())).thenReturn(0);
        User u=new User("shop-"+UUID.randomUUID(),null);u.setGold(1000000);u.setDiamond(1000000);uid=users.saveAndFlush(u).getId();
    }
    PurchaseRequest request(Catalog c,String id){var o=c.offers().stream().filter(v->v.offerId().equals(id)).findFirst().orElseThrow();
        return new PurchaseRequest(UUID.randomUUID(),id,o.slotKey(),c.catalogRevision(),o.price(),o.currencyType());}
    PurchaseResponse buy(String id){return shop.purchase(uid,request(shop.catalog(uid),id));}
    RerollResponse reroll(){var c=shop.catalog(uid);var ad=shop.challenge(uid,UUID.randomUUID());shop.completeDevelopmentAd(uid,ad.challengeId());
        return shop.reroll(uid,new RerollRequest(UUID.randomUUID(),ad.challengeId(),c.catalogRevision()));}
    void error(Runnable call,ErrorCode code){assertThatThrownBy(call::run).isInstanceOfSatisfying(BusinessException.class,e->assertThat(e.getErrorCode()).isEqualTo(code));}
    @Test void catalogAndDailyOffersArePersistedAndDoNotRerollOnGet(){
        var a=shop.catalog(uid);var b=shop.catalog(uid);assertThat(a).isEqualTo(b);assertThat(a.offers()).hasSize(10);
        assertThat(a.dayKey()).isEqualTo("2026-09-19");assertThat(a.weekKey()).isEqualTo("2026-09-14");
        assertThat(a.dailyResetAt()).isEqualTo("2026-09-19T15:00:00Z");assertThat(a.weeklyResetAt()).isEqualTo("2026-09-20T15:00:00Z");
    }
    @Test void singleAndWeeklyTenDrawDiscountPrices(){assertThat(buy("GACHA_SINGLE").chargedPrice()).isEqualTo(300);
        for(int i=0;i<3;i++)assertThat(buy("GACHA_TEN").chargedPrice()).isEqualTo(2700);
        assertThat(buy("GACHA_TEN").chargedPrice()).isEqualTo(3000);}
    @Test void sameRequestIsExactlyOnceAndChangedPayloadConflicts(){var q=request(shop.catalog(uid),"DNA_PACK");var a=shop.purchase(uid,q);
        assertThat(shop.purchase(uid,q)).isEqualTo(a);assertThat(users.findById(uid).orElseThrow().getUniversalPiece()).isEqualTo(20);
        error(()->shop.purchase(uid,new PurchaseRequest(q.requestId(),q.offerId(),q.slotKey(),q.expectedRevision(),1,q.expectedCurrency())),ErrorCode.PURCHASE_REQUEST_CONFLICT);}
    @Test void dailyGoldAndWeeklyMaterialLimits(){for(int i=0;i<3;i++)buy("GOLD_PACK");error(()->buy("GOLD_PACK"),ErrorCode.SHOP_LIMIT_REACHED);
        buy("DNA_PACK");buy("DNA_PACK");error(()->buy("DNA_PACK"),ErrorCode.SHOP_LIMIT_REACHED);
        for(int i=0;i<3;i++)buy("CELL_PACK");error(()->buy("CELL_PACK"),ErrorCode.SHOP_LIMIT_REACHED);}
    @Test void stalePriceOrRevisionNeverCharges(){var c=shop.catalog(uid);var q=request(c,"GOLD_PACK");
        error(()->shop.purchase(uid,new PurchaseRequest(q.requestId(),q.offerId(),q.slotKey(),q.expectedRevision(),1,q.expectedCurrency())),ErrorCode.SHOP_QUOTE_CHANGED);
        buy("CELL_PACK");int before=users.findById(uid).orElseThrow().getDiamond();error(()->shop.purchase(uid,q),ErrorCode.SHOP_QUOTE_CHANGED);
        assertThat(users.findById(uid).orElseThrow().getDiamond()).isEqualTo(before);}
    @Test void insufficientFundsAndCapsRollback(){var u=users.findById(uid).orElseThrow();u.setDiamond(0);users.saveAndFlush(u);
        error(()->buy("DNA_PACK"),ErrorCode.INSUFFICIENT_DIAMOND);u.setDiamond(1000000);u.setUniversalPiece(Integer.MAX_VALUE);users.saveAndFlush(u);
        error(()->buy("DNA_PACK"),ErrorCode.SHOP_BALANCE_CAP);assertThat(users.findById(uid).orElseThrow().getDiamond()).isEqualTo(1000000);}
    @Test void slottedPurchaseCannotBeForgedOrRepeated(){var c=shop.catalog(uid);var q=request(c,"DAILY_NORMAL");
        error(()->shop.purchase(uid,new PurchaseRequest(q.requestId(),q.offerId(),"",q.expectedRevision(),q.expectedPrice(),q.expectedCurrency())),ErrorCode.SHOP_QUOTE_CHANGED);
        shop.purchase(uid,q);error(()->buy("DAILY_NORMAL"),ErrorCode.SHOP_LIMIT_REACHED);}
    @Test void firstPiecePurchaseUnlocksWithOnePieceAndKeepsRemainder(){var c=shop.catalog(uid);var offer=c.offers().stream().filter(o->o.offerId().equals("DAILY_NORMAL")).findFirst().orElseThrow();
        buy("DAILY_NORMAL");var a=inventory.findByUserAndAlienSpec(users.findById(uid).orElseThrow(),aliens.findById(offer.alienId()).orElseThrow()).orElseThrow();
        assertThat(a.getLevel()).isEqualTo(1);assertThat(a.getPieces()).isEqualTo(19);}
    @Test void purchasedSlotStaysAndUnpurchasedSlotsChange(){var before=buy("DAILY_NORMAL").catalog();var after=reroll().catalog();
        for(var o:before.offers())if(o.category().equals("DAILY")){var n=after.offers().stream().filter(x->x.slotKey().equals(o.slotKey())).findFirst().orElseThrow();
            if(o.purchased())assertThat(n).isEqualTo(o);else assertThat(n.alienId()).isNotEqualTo(o.alienId());}
        assertThat(after.offers()).anyMatch(o->o.offerId().equals("LUCKY_CELL"));}
    @Test void absentLuckyNeverAppearsOnReroll(){when(random.nextInt(10000)).thenReturn(2000);assertThat(shop.catalog(uid).offers()).noneMatch(o->o.category().equals("LUCKY"));
        assertThat(reroll().catalog().offers()).noneMatch(o->o.category().equals("LUCKY"));}
    @Test void luckyDailySlotAndSharedWeeklyLimit(){buy("LUCKY_DNA");error(()->buy("LUCKY_DNA"),ErrorCode.SHOP_LIMIT_REACHED);
        when(clock.now()).thenReturn(Instant.parse("2026-09-20T03:00:00Z"));reroll();buy("LUCKY_CELL");
        assertThat(shop.catalog(uid).luckyPurchasesRemaining()).isZero();
        when(clock.now()).thenReturn(Instant.parse("2026-09-20T15:00:00Z"));assertThat(shop.catalog(uid).luckyPurchasesRemaining()).isEqualTo(2);}
    @Test void unverifiedAndForeignAdDoNotConsumeQuota(){var c=shop.catalog(uid);var ad=shop.challenge(uid,UUID.randomUUID());
        error(()->shop.reroll(uid,new RerollRequest(UUID.randomUUID(),ad.challengeId(),c.catalogRevision())),ErrorCode.SHOP_AD_INVALID);
        User other=users.saveAndFlush(new User("other-"+UUID.randomUUID(),null));error(()->shop.completeDevelopmentAd(other.getId(),ad.challengeId()),ErrorCode.SHOP_AD_INVALID);
        assertThat(shop.catalog(uid).rerollsRemaining()).isEqualTo(3);}
    @Test void adReplayAndRerollRequestReplayAreSafe(){var c=shop.catalog(uid);UUID id=UUID.randomUUID();var ad=shop.challenge(uid,id);assertThat(shop.challenge(uid,id)).isEqualTo(ad);
        shop.completeDevelopmentAd(uid,ad.challengeId());var q=new RerollRequest(UUID.randomUUID(),ad.challengeId(),c.catalogRevision());var a=shop.reroll(uid,q);
        assertThat(shop.reroll(uid,q)).isEqualTo(a);error(()->shop.reroll(uid,new RerollRequest(UUID.randomUUID(),ad.challengeId(),a.catalog().catalogRevision())),ErrorCode.SHOP_AD_INVALID);}
    @Test void threeAdsPerDayAndNoCostForCancel(){var c=shop.catalog(uid);shop.challenge(uid,UUID.randomUUID());assertThat(shop.catalog(uid).rerollsRemaining()).isEqualTo(3);
        for(int i=0;i<3;i++)reroll();error(()->shop.challenge(uid,UUID.randomUUID()),ErrorCode.SHOP_LIMIT_REACHED);}
    @Test void expiryAndMidnightRequireFreshQuote(){var c=shop.catalog(uid);var ad=shop.challenge(uid,UUID.randomUUID());var q=request(c,"GOLD_PACK");
        when(clock.now()).thenReturn(Instant.parse("2026-09-19T03:10:00Z"));error(()->shop.completeDevelopmentAd(uid,ad.challengeId()),ErrorCode.SHOP_AD_INVALID);
        when(clock.now()).thenReturn(Instant.parse("2026-09-19T15:00:00Z"));error(()->shop.purchase(uid,q),ErrorCode.SHOP_QUOTE_CHANGED);}
    @Test void mondayResetsDiscountAndWeeklyLimits(){buy("GACHA_TEN");buy("DNA_PACK");buy("DNA_PACK");var q=request(shop.catalog(uid),"GACHA_TEN");
        when(clock.now()).thenReturn(Instant.parse("2026-09-20T15:00:00Z"));error(()->shop.purchase(uid,q),ErrorCode.SHOP_QUOTE_CHANGED);
        assertThat(buy("GACHA_TEN").chargedPrice()).isEqualTo(2700);buy("DNA_PACK");}
    @Test void historicalLegacyUuidCannotConsumeShopDiscount(){UUID id=UUID.randomUUID();gacha.purchase(users.findById(uid).orElseThrow().getUsername(),"ALIEN_GACHA_TEN",id);
        var q=request(shop.catalog(uid),"GACHA_TEN");error(()->shop.purchase(uid,new PurchaseRequest(id,q.offerId(),q.slotKey(),q.expectedRevision(),q.expectedPrice(),q.expectedCurrency())),ErrorCode.PURCHASE_REQUEST_CONFLICT);
        assertThat(shop.catalog(uid).offers().stream().filter(o->o.offerId().equals("GACHA_TEN")).findFirst().orElseThrow().discountRemaining()).isEqualTo(3);}
    @Test void concurrentSameRequestChargesOnce() throws Exception {var q=request(shop.catalog(uid),"GOLD_PACK");var pool=Executors.newFixedThreadPool(2);var start=new CountDownLatch(1);
        try{var a=pool.submit(()->{start.await();return shop.purchase(uid,q);});var b=pool.submit(()->{start.await();return shop.purchase(uid,q);});start.countDown();
            assertThat(a.get(10,TimeUnit.SECONDS)).isEqualTo(b.get(10,TimeUnit.SECONDS));assertThat(users.findById(uid).orElseThrow().getDiamond()).isEqualTo(999500);
        }finally{pool.shutdownNow();}}
    @Test void concurrentLastDiscountDoesNotSilentlyChargeFullPrice() throws Exception {buy("GACHA_TEN");buy("GACHA_TEN");var c=shop.catalog(uid);var pool=Executors.newFixedThreadPool(2);var start=new CountDownLatch(1);
        try{Callable<Boolean> action=()->{start.await();try{shop.purchase(uid,request(c,"GACHA_TEN"));return true;}catch(BusinessException e){assertThat(e.getErrorCode()).isEqualTo(ErrorCode.SHOP_QUOTE_CHANGED);return false;}};
            var a=pool.submit(action);var b=pool.submit(action);start.countDown();assertThat(List.of(a.get(10,TimeUnit.SECONDS),b.get(10,TimeUnit.SECONDS))).containsExactlyInAnyOrder(true,false);
        }finally{pool.shutdownNow();}}
    @Test void apiRequiresPrincipalEvenOnLoopback() throws Exception {mvc.perform(get("/api/shop/catalog")).andExpect(status().isUnauthorized());}
    @Test void developmentCompletionRequiresLoopback() throws Exception {var ad=shop.challenge(uid,UUID.randomUUID());
        mvc.perform(post("/api/shop/development/ad-challenges/"+ad.challengeId()+"/complete").requestAttr(AccountAccess.PRINCIPAL_ATTRIBUTE,new AccountPrincipal(uid,"unused","test"))
                .with(r->{r.setRemoteAddr("198.51.100.8");return r;})).andExpect(status().isForbidden());}
    @Test void oldEndpointsRejectNewPurchases() throws Exception {String name=users.findById(uid).orElseThrow().getUsername();
        mvc.perform(post("/api/shop/gacha").param("username",name)).andExpect(status().isConflict()).andExpect(jsonPath("$.code").value("SHOP_LEGACY_DISABLED"));
        mvc.perform(post("/api/shop/gacha/purchase").param("username",name).param("productId","ALIEN_GACHA_SINGLE").param("purchaseRequestId",UUID.randomUUID().toString()))
                .andExpect(status().isConflict()).andExpect(jsonPath("$.code").value("SHOP_LEGACY_DISABLED"));}
    @Test void malformedChallengeBodiesAreClientErrors() throws Exception {
        for(String body:List.of("null","{}","{broken"))mvc.perform(post("/api/shop/reroll-challenges").contentType("application/json").content(body)
                .requestAttr(AccountAccess.PRINCIPAL_ATTRIBUTE,new AccountPrincipal(uid,"unused","test"))).andExpect(status().isBadRequest());
    }
    @Test void inactiveDailySlotIsSkippedWithoutBlockingOtherRerolls(){var c=shop.catalog(uid);
        var active=shopBalance.offers().stream().filter(o->!o.offerId().equals("DAILY_EPIC")).toList();
        doReturn(active).when(shopBalance).offers();doReturn(null).when(shopBalance).offer("DAILY_EPIC");
        var after=reroll().catalog();assertThat(after.offers()).noneMatch(o->o.offerId().equals("DAILY_EPIC"));assertThat(after.rerollsRemaining()).isEqualTo(2);
    }
    @Test void laterGachaOverflowRollsBackWalletEarlierGrantDiscountAndBothReceipts(){var user=users.findById(uid).orElseThrow();
        var first=new UserAlien(user,aliens.findById(22L).orElseThrow());first.setPieces(11);inventory.saveAndFlush(first);
        var second=new UserAlien(user,aliens.findById(23L).orElseThrow());second.setPieces(Integer.MAX_VALUE);inventory.saveAndFlush(second);
        when(gachaRandom.nextInt(anyInt())).thenReturn(0,0,0,1);var before=shop.catalog(uid);var q=request(before,"GACHA_TEN");
        error(()->shop.purchase(uid,q),ErrorCode.SHOP_BALANCE_CAP);
        assertThat(shop.catalog(uid)).isEqualTo(before);assertThat(inventory.findById(first.getId()).orElseThrow().getPieces()).isEqualTo(11);
        assertThat(inventory.findById(second.getId()).orElseThrow().getPieces()).isEqualTo(Integer.MAX_VALUE);
        assertThat(receipts.findByUserIdAndRequestId(uid,q.requestId())).isEmpty();assertThat(gachaReceipts.findByUserAndPurchaseRequestId(user,q.requestId())).isEmpty();
    }
    @Test void goldAndCellCapDoNotCharge(){var u=users.findById(uid).orElseThrow();u.setGold(Integer.MAX_VALUE);u.setGrowthCell(Integer.MAX_VALUE);users.saveAndFlush(u);
        var before=shop.catalog(uid);error(()->buy("GOLD_PACK"),ErrorCode.SHOP_BALANCE_CAP);error(()->buy("CELL_PACK"),ErrorCode.SHOP_BALANCE_CAP);
        assertThat(shop.catalog(uid).wallet()).isEqualTo(before.wallet());}
}
