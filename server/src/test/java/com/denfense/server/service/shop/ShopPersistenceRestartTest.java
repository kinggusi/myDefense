package com.denfense.server.service.shop;
import com.denfense.server.DefenseServerApplication;
import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.dto.shop.ShopDtos.*;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.io.TempDir;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.context.ConfigurableApplicationContext;
import java.nio.file.Path;
import java.util.*;
import java.time.Instant;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.jdbc.core.JdbcTemplate;
import static org.assertj.core.api.Assertions.*;

class ShopPersistenceRestartTest {
    @TempDir Path directory;
    @Test void fileDatabasePreservesWalletSlotsLimitsDiscountAdsAndReceipts() {
        String url="jdbc:h2:file:"+directory.resolve("shop").toAbsolutePath().toString().replace('\\','/')+";MODE=MySQL";
        long uid;Catalog before;PurchaseRequest purchase;PurchaseResponse original;List<Map<String,Object>> inventory;
        try(var context=start(url)) {
            var users=context.getBean(UserRepository.class);var user=new User("persist-shop",null);user.setGold(1000000);user.setDiamond(1000000);uid=users.saveAndFlush(user).getId();
            var shop=context.getBean(ShopCatalogService.class);
            purchase=request(shop.catalog(uid),"DNA_PACK");original=shop.purchase(uid,purchase);
            shop.purchase(uid,request(shop.catalog(uid),"GACHA_TEN"));shop.purchase(uid,request(shop.catalog(uid),"DAILY_NORMAL"));
            var c=shop.catalog(uid);var ad=shop.challenge(uid,UUID.randomUUID());shop.completeDevelopmentAd(uid,ad.challengeId());
            shop.reroll(uid,new RerollRequest(UUID.randomUUID(),ad.challengeId(),c.catalogRevision()));before=shop.catalog(uid);
            inventory=context.getBean(JdbcTemplate.class).queryForList("select alien_id,level,pieces from user_aliens where user_id=? order by alien_id",uid);
        }
        assertThat(directory.resolve("shop.mv.db")).exists();
        try(var context=start(url)) {
            var shop=context.getBean(ShopCatalogService.class);var after=shop.catalog(uid);
            assertThat(after.wallet()).isEqualTo(before.wallet());assertThat(after.offers()).isEqualTo(before.offers());
            assertThat(after.catalogRevision()).isEqualTo(before.catalogRevision());assertThat(after.rerollsRemaining()).isEqualTo(2);
            assertThat(shop.purchase(uid,purchase)).isEqualTo(original);assertThat(shop.catalog(uid).wallet()).isEqualTo(before.wallet());
            assertThat(context.getBean(JdbcTemplate.class).queryForList("select alien_id,level,pieces from user_aliens where user_id=? order by alien_id",uid)).isEqualTo(inventory);
        }
    }
    private PurchaseRequest request(Catalog c,String id){var o=c.offers().stream().filter(v->v.offerId().equals(id)).findFirst().orElseThrow();
        return new PurchaseRequest(UUID.randomUUID(),id,o.slotKey(),c.catalogRevision(),o.price(),o.currencyType());}
    @TestConfiguration static class FixedClock {
        @Bean @Primary ShopTimeProvider fixedShopClock(){return new ShopTimeProvider(){@Override public Instant now(){return Instant.parse("2026-09-19T03:00:00Z");}};}
    }
    private ConfigurableApplicationContext start(String url){return new SpringApplicationBuilder(DefenseServerApplication.class,FixedClock.class)
            .web(WebApplicationType.NONE).profiles("local").run("--spring.datasource.url="+url,"--spring.jpa.hibernate.ddl-auto=update",
                    "--spring.jpa.show-sql=false","--spring.sql.init.mode=never","--mydefense.shop.development-ads-enabled=true");}
}
