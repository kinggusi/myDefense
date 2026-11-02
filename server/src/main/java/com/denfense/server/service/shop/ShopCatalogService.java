package com.denfense.server.service.shop;

import com.denfense.server.balance.ShopOfferBalance;
import com.denfense.server.domain.*;
import com.denfense.server.domain.shop.*;
import com.denfense.server.dto.shop.ShopDtos.*;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.repository.*;
import com.denfense.server.repository.shop.*;
import com.denfense.server.service.balance.*;
import com.denfense.server.service.gacha.GachaPurchaseService;
import com.denfense.server.exception.*;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.*;
import java.time.temporal.TemporalAdjusters;
import java.util.*;

/** All paths take the same User lock, then read receipts, then mutate wallet and shop state atomically. */
@Service @RequiredArgsConstructor
public class ShopCatalogService {
    private final UserRepository users;
    private final UserAlienRepository inventory;
    private final AlienSpecRepository aliens;
    private final ShopStateRepository states;
    private final ShopReceiptRepository receipts;
    private final ShopAdChallengeRepository challenges;
    private final ShopBalanceRegistry shop;
    private final BalanceRegistry balance;
    private final BalanceVersionRegistry version;
    private final ShopTimeProvider clock;
    private final ShopRandomSource random;
    private final ShopAdPolicy ads;
    private final GachaPurchaseService gacha;
    private final ObjectMapper json;
    private static final ZoneId KST=ZoneId.of("Asia/Seoul");
    private record Context(User user,ShopState entity,ShopStateData data,Instant now) {}

    @Transactional public Catalog catalog(long userId) {
        var c=context(lock(userId)); save(c);return catalog(c);
    }

    @Transactional public PurchaseResponse purchase(long userId,PurchaseRequest request) {
        if(request==null || request.requestId()==null || request.offerId()==null || request.expectedCurrency()==null
                || request.expectedRevision()==null) throw fail(ErrorCode.INVALID_REQUEST);
        User user=lock(userId);
        String fingerprint=write(request);
        var old=receipt(userId,request.requestId(),"PURCHASE",fingerprint,PurchaseResponse.class);
        if(old!=null)return old;
        var c=context(user);
        requireRevision(c,request.expectedRevision());
        var offer=shop.offer(request.offerId());
        if(offer==null)throw fail(ErrorCode.SHOP_PRODUCT_NOT_FOUND);
        String key=request.slotKey()==null?"":request.slotKey();
        var slot=key.isEmpty()?null:c.data.slots.stream().filter(s->s.slotKey.equals(key)).findFirst().orElse(null);
        boolean slotted=Set.of("DAILY","LUCKY").contains(offer.category());
        if(slotted ? slot==null||!slot.offerId.equals(offer.offerId()) : slot!=null||!key.isEmpty()) throw fail(ErrorCode.SHOP_QUOTE_CHANGED);
        if((slot!=null&&slot.purchased)||remaining(c,offer)==0)throw fail(ErrorCode.SHOP_LIMIT_REACHED);
        int price=price(c,offer);
        if(price!=request.expectedPrice() || !offer.currencyType().equals(request.expectedCurrency()))throw fail(ErrorCode.SHOP_QUOTE_CHANGED);
        GachaPurchaseResponseDto gachaResult=null;
        long alienId=slot==null?0:slot.alienId;
        if("GACHA".equals(offer.rewardType())) {
            gachaResult=gacha.purchaseQuoted(user.getUsername(),offer.gachaProductId(),request.requestId(),price);
        } else {
            checkGrantCap(user,offer,alienId);
            if("GOLD".equals(offer.currencyType()))user.spendGold(price);else user.spendDiamond(price);
            grant(user,offer,alienId);
        }
        if(slot!=null)slot.purchased=true;
        if(!"NONE".equals(offer.limitPeriod()))usage(c,offer.limitPeriod()).merge(offer.limitGroup(),1,Math::addExact);
        if(offer.offerId().equals(shop.policy().discountOfferId()) && discountRemaining(c)>0)c.data.weeklyUsage.merge("_GACHA_DISCOUNT",1,Math::addExact);
        c.data.revision++;save(c);
        var response=new PurchaseResponse(request.requestId(),offer.offerId(),price,offer.currencyType(),offer.rewardType(),offer.rewardAmount(),alienId,gachaResult,catalog(c));
        store(userId,request.requestId(),"PURCHASE",fingerprint,response);
        return response;
    }

    @Transactional public Challenge challenge(long userId,UUID requestId) {
        if(requestId==null)throw fail(ErrorCode.INVALID_REQUEST);
        var c=context(lock(userId));
        if(!ads.available())throw fail(ErrorCode.SHOP_AD_UNAVAILABLE);
        var existing=challenges.findByUserIdAndRequestId(userId,requestId).orElse(null);
        if(existing!=null)return new Challenge(existing.getChallengeId(),existing.getExpiresAt().toString(),existing.isDevelopment());
        requireReroll(c);
        var challenge=new ShopAdChallenge(userId,requestId,c.data.dayKey,c.now.plusSeconds(shop.policy().adChallengeTtlSeconds()),ads.development());
        challenges.save(challenge);save(c);
        return new Challenge(challenge.getChallengeId(),challenge.getExpiresAt().toString(),challenge.isDevelopment());
    }

    @Transactional public void completeDevelopmentAd(long userId,UUID id) {
        var c=context(lock(userId));
        if(!ads.development())throw fail(ErrorCode.SHOP_AD_UNAVAILABLE);
        var challenge=validChallenge(c,id);
        if(!challenge.isDevelopment())throw fail(ErrorCode.SHOP_AD_INVALID);
        challenge.verify("DEV:"+challenge.getChallengeId());save(c);
    }

    @Transactional public RerollResponse reroll(long userId,RerollRequest request) {
        if(request==null||request.requestId()==null||request.challengeId()==null||request.expectedRevision()==null)throw fail(ErrorCode.INVALID_REQUEST);
        User user=lock(userId);
        String fingerprint=write(request);
        var old=receipt(userId,request.requestId(),"REROLL",fingerprint,RerollResponse.class);
        if(old!=null)return old;
        var c=context(user);
        requireRevision(c,request.expectedRevision());requireReroll(c);
        var challenge=validChallenge(c,request.challengeId());
        String transaction=ads.verify(challenge);
        if(transaction==null||transaction.isBlank()||transaction.length()>255)throw fail(ErrorCode.SHOP_AD_INVALID);
        for(var slot:c.data.slots) {
            if(slot.purchased)continue;
            var offer=shop.offer(slot.offerId);
            if(offer==null)continue;
            if("DAILY".equals(offer.category()))slot.alienId=pickAlien(offer.grade(),slot.alienId);
            else {
                var candidates=shop.offers().stream().filter(o->"LUCKY".equals(o.category())&&!o.offerId().equals(slot.offerId)).toList();
                slot.offerId=candidates.get(random.nextInt(candidates.size())).offerId();
            }
        }
        challenge.verify(transaction);challenge.consume();
        try {
            // The database unique key is the final arbiter across accounts and simultaneous callbacks.
            challenges.saveAndFlush(challenge);
        } catch (org.springframework.dao.DataIntegrityViolationException duplicateProof) {
            throw fail(ErrorCode.SHOP_AD_INVALID);
        }
        c.data.rerolls++;c.data.revision++;save(c);
        var result=new RerollResponse(request.requestId(),catalog(c));
        store(userId,request.requestId(),"REROLL",fingerprint,result);return result;
    }

    private ShopAdChallenge validChallenge(Context c,UUID id) {
        var v=challenges.findByChallengeIdAndUserId(id,c.user.getId()).orElseThrow(()->fail(ErrorCode.SHOP_AD_INVALID));
        if(v.isConsumed()||!v.getDayKey().equals(c.data.dayKey)||!c.now.isBefore(v.getExpiresAt()))throw fail(ErrorCode.SHOP_AD_INVALID);
        return v;
    }
    private void requireReroll(Context c) {
        if(c.data.rerolls>=shop.policy().dailyAdRerolls()||c.data.slots.stream().noneMatch(s->!s.purchased&&shop.offer(s.offerId)!=null))throw fail(ErrorCode.SHOP_LIMIT_REACHED);
    }
    private User lock(long id) { return users.findByIdForUpdate(id).orElseThrow(()->fail(ErrorCode.USER_NOT_FOUND)); }
    private Context context(User user) {
        Instant now=clock.now();String day=now.atZone(KST).toLocalDate().toString();
        String week=now.atZone(KST).toLocalDate().with(TemporalAdjusters.previousOrSame(DayOfWeek.MONDAY)).toString();
        var entity=states.findByUserId(user.getId()).orElse(null);
        var d=entity==null?new ShopStateData():read(entity.getStateJson(),ShopStateData.class);
        if(d.schemaVersion!=1)throw new IllegalStateException("Unsupported persisted shop state.");
        if(!week.equals(d.weekKey)){d.weekKey=week;d.weeklyUsage.clear();d.revision++;}
        if(!day.equals(d.dayKey)) {
            d.dayKey=day;d.dailyUsage.clear();d.rerolls=0;d.slots.clear();d.revision++;
            for(var o:shop.offers())if("DAILY".equals(o.category()))d.slots.add(new ShopStateData.Slot(o.offerId(),o.offerId(),pickAlien(o.grade(),0)));
            if(random.nextInt(shop.policy().luckyTotalWeight())<shop.policy().luckyWeight()) {
                var choices=shop.offers().stream().filter(o->"LUCKY".equals(o.category())).toList();
                d.slots.add(new ShopStateData.Slot("LUCKY",choices.get(random.nextInt(choices.size())).offerId(),0));
            }
        }
        if(entity==null)entity=states.save(new ShopState(user.getId(),write(d)));
        return new Context(user,entity,d,now);
    }
    private long pickAlien(String grade,long exclude) {
        var candidates=balance.getAllAlienSpecs().stream().filter(a->a.grade().equals(grade)&&a.alienId()!=exclude).toList();
        if(candidates.isEmpty())throw new IllegalStateException("Shop grade pool is empty.");
        return candidates.get(random.nextInt(candidates.size())).alienId();
    }
    private String revision(Context c) {return c.data.dayKey+":"+c.data.weekKey+":"+version.getContentHash()+":"+c.data.revision;}
    private void requireRevision(Context c,String quote) {if(!revision(c).equals(quote))throw fail(ErrorCode.SHOP_QUOTE_CHANGED);}
    private Map<String,Integer> usage(Context c,String period) { return "DAY".equals(period)?c.data.dailyUsage:c.data.weeklyUsage; }
    private int remaining(Context c,ShopOfferBalance o) {return "NONE".equals(o.limitPeriod())?-1:Math.max(0,o.limitCount()-usage(c,o.limitPeriod()).getOrDefault(o.limitGroup(),0));}
    private int discountRemaining(Context c) {return Math.max(0,shop.policy().weeklyDiscountCount()-c.data.weeklyUsage.getOrDefault("_GACHA_DISCOUNT",0));}
    private int price(Context c,ShopOfferBalance o) {return o.offerId().equals(shop.policy().discountOfferId())&&discountRemaining(c)>0?shop.policy().discountPrice():o.price();}
    private Catalog catalog(Context c) {
        var offers=new ArrayList<Offer>();
        for(var o:shop.offers()) {
            if(Set.of("DAILY","LUCKY").contains(o.category())) {
                for(var s:c.data.slots)if(s.offerId.equals(o.offerId()))offers.add(view(c,o,s));
            } else offers.add(view(c,o,null));
        }
        var day=LocalDate.parse(c.data.dayKey);
        int lucky=shop.offers().stream().filter(o->"LUCKY".equals(o.category())).mapToInt(o->remaining(c,o)).min().orElse(0);
        return new Catalog(c.now.toString(),c.data.dayKey,c.data.weekKey,day.plusDays(1).atStartOfDay(KST).toInstant().toString(),
                day.with(TemporalAdjusters.next(DayOfWeek.MONDAY)).atStartOfDay(KST).toInstant().toString(),revision(c),
                new Wallet(c.user.getGold(),c.user.getDiamond(),c.user.getUniversalPiece(),c.user.getGrowthCell()),offers,
                Math.max(0,shop.policy().dailyAdRerolls()-c.data.rerolls),ads.available(),ads.development(),lucky);
    }
    private Offer view(Context c,ShopOfferBalance o,ShopStateData.Slot s) { return new Offer(o.offerId(),s==null?"":s.slotKey,o.category(),
            o.currencyType(),price(c,o),o.price(),o.rewardType(),o.rewardAmount(),o.grade(),s==null?0:s.alienId,
            s!=null&&s.purchased?0:remaining(c,o),s!=null&&s.purchased,o.offerId().equals(shop.policy().discountOfferId())?discountRemaining(c):0); }
    private void checkGrantCap(User u,ShopOfferBalance o,long alienId) {
        int existing=switch(o.rewardType()) {
            case "GOLD"->u.getGold();case "DNA"->u.getUniversalPiece();case "GROWTH_CELL"->u.getGrowthCell();
            case "ALIEN_PIECE"->inventory.findByUserAndAlienSpec(u,aliens.findById(alienId).orElseThrow(()->fail(ErrorCode.ALIEN_SPEC_NOT_FOUND))).map(UserAlien::getPieces).orElse(0);
            default->throw fail(ErrorCode.INVALID_REQUEST);
        };
        if((long)existing+o.rewardAmount()>Integer.MAX_VALUE)throw fail(ErrorCode.SHOP_BALANCE_CAP);
    }
    private void grant(User u,ShopOfferBalance o,long alienId) {
        switch(o.rewardType()) {
            case "GOLD"->u.earnGold(o.rewardAmount());case "DNA"->u.earnUniversalPiece(o.rewardAmount());case "GROWTH_CELL"->u.earnGrowthCell(o.rewardAmount());
            case "ALIEN_PIECE"->{var spec=aliens.findById(alienId).orElseThrow(()->fail(ErrorCode.ALIEN_SPEC_NOT_FOUND));
                var ua=inventory.findByUserAndAlienSpec(u,spec).orElse(null);
                if(ua==null){ua=new UserAlien(u,spec);ua.setPieces(o.rewardAmount()-1);inventory.save(ua);}else ua.addPieces(o.rewardAmount());}
            default->throw fail(ErrorCode.INVALID_REQUEST);
        }
    }
    private void save(Context c) {c.entity.update(write(c.data));}
    private <T>T receipt(long uid,UUID id,String operation,String fingerprint,Class<T> type) {
        var r=receipts.findByUserIdAndRequestId(uid,id).orElse(null);if(r==null)return null;
        if(!r.getOperation().equals(operation)||!r.getFingerprint().equals(fingerprint))throw fail(ErrorCode.PURCHASE_REQUEST_CONFLICT);
        return read(r.getResponseJson(),type);
    }
    private void store(long uid,UUID id,String operation,String fingerprint,Object result){receipts.save(new ShopReceipt(uid,id,operation,fingerprint,write(result)));}
    private String write(Object v){try{return json.writeValueAsString(v);}catch(Exception e){throw new IllegalStateException("Shop serialization failed.",e);}}
    private <T>T read(String v,Class<T> type){try{return json.readValue(v,type);}catch(Exception e){throw new IllegalStateException("Shop persistence invalid.",e);}}
    private static BusinessException fail(ErrorCode code){return new BusinessException(code);}
}
