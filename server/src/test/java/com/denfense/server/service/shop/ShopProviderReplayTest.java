package com.denfense.server.service.shop;
import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.repository.shop.ShopAdChallengeRepository;
import com.denfense.server.dto.shop.ShopDtos.*;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import java.util.*;
import java.util.concurrent.*;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;
import static org.mockito.ArgumentMatchers.*;

@SpringBootTest(properties={"spring.datasource.url=jdbc:h2:mem:shop-provider-test;MODE=MySQL;DB_CLOSE_DELAY=-1","mydefense.shop.development-ads-enabled=false"})
@ActiveProfiles("local")
class ShopProviderReplayTest {
    @Autowired ShopCatalogService shop;
    @Autowired UserRepository users;
    @Autowired ShopAdChallengeRepository challenges;
    @MockBean RewardedAdVerifier verifier;
    @Test void sameProviderTransactionAcrossAccountsCanOnlyBeConsumedOnce() throws Exception {
        when(verifier.available()).thenReturn(true);when(verifier.verifiedTransactionId(any())).thenReturn("PROVIDER:"+UUID.randomUUID());
        long a=users.saveAndFlush(new User("ad-a-"+UUID.randomUUID(),null)).getId();
        long b=users.saveAndFlush(new User("ad-b-"+UUID.randomUUID(),null)).getId();
        var ca=shop.catalog(a);var cb=shop.catalog(b);var aa=shop.challenge(a,UUID.randomUUID());var ab=shop.challenge(b,UUID.randomUUID());
        assertThat(aa.developmentAd()).isFalse();
        var pool=Executors.newFixedThreadPool(2);var start=new CountDownLatch(1);
        try {
            var fa=pool.submit(()->attempt(start,a,new RerollRequest(UUID.randomUUID(),aa.challengeId(),ca.catalogRevision())));
            var fb=pool.submit(()->attempt(start,b,new RerollRequest(UUID.randomUUID(),ab.challengeId(),cb.catalogRevision())));
            start.countDown();boolean wa=fa.get(15,TimeUnit.SECONDS);boolean wb=fb.get(15,TimeUnit.SECONDS);
            assertThat(List.of(wa,wb)).containsExactlyInAnyOrder(true,false);
            assertThat(shop.catalog(a).rerollsRemaining()).isEqualTo(wa?2:3);assertThat(shop.catalog(b).rerollsRemaining()).isEqualTo(wb?2:3);
            assertThat(challenges.findByChallengeIdAndUserId(aa.challengeId(),a).orElseThrow().isConsumed()).isEqualTo(wa);
            assertThat(challenges.findByChallengeIdAndUserId(ab.challengeId(),b).orElseThrow().isConsumed()).isEqualTo(wb);
            if(!wa)assertThat(shop.catalog(a).offers()).isEqualTo(ca.offers());if(!wb)assertThat(shop.catalog(b).offers()).isEqualTo(cb.offers());
        }finally{pool.shutdownNow();}
    }
    private boolean attempt(CountDownLatch start,long id,RerollRequest request)throws Exception{start.await();try{shop.reroll(id,request);return true;}
        catch(BusinessException expected){assertThat(expected.getErrorCode()).isEqualTo(ErrorCode.SHOP_AD_INVALID);return false;}}
}
