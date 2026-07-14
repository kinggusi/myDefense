package com.denfense.server.service.gacha;

import com.denfense.server.domain.GachaPurchase;
import com.denfense.server.domain.GachaPurchaseStatus;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.repository.GachaPurchaseRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.boot.test.mock.mockito.SpyBean;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;
import java.util.UUID;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@SpringBootTest
@ActiveProfiles("test")
class GachaPurchaseIdempotencyConcurrencyTest {

    @Autowired
    private GachaPurchaseService gachaPurchaseService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private UserAlienRepository userAlienRepository;

    @Autowired
    private GachaPurchaseRepository gachaPurchaseRepository;

    @MockBean
    private GachaRandomGenerator randomGenerator;

    @SpyBean
    private GachaDrawService gachaDrawService;

    @BeforeEach
    void setUp() {
        User user = new User("concurrentIdempotentUser", "pw");
        user.setDiamond(500);
        userRepository.saveAndFlush(user);
        when(randomGenerator.nextInt(anyInt())).thenReturn(0);
    }

    @AfterEach
    void tearDown() {
        gachaPurchaseRepository.deleteAll();
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
    }

    @Test
    @DisplayName("동일 purchaseRequestId 동시 2건은 완료 응답을 재사용하고 구매 효과를 한 번만 적용한다")
    void concurrentSamePurchaseRequestId_reusesCompletedResponse() throws Exception {
        UUID purchaseRequestId = UUID.randomUUID();
        ExecutorService executor = Executors.newFixedThreadPool(2);
        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);

        try {
            Future<GachaPurchaseResponseDto> first = executor.submit(() ->
                    purchaseAfterStart(ready, start, purchaseRequestId));
            Future<GachaPurchaseResponseDto> second = executor.submit(() ->
                    purchaseAfterStart(ready, start, purchaseRequestId));

            assertThat(ready.await(5, TimeUnit.SECONDS)).isTrue();
            start.countDown();

            GachaPurchaseResponseDto firstResponse = first.get(10, TimeUnit.SECONDS);
            GachaPurchaseResponseDto secondResponse = second.get(10, TimeUnit.SECONDS);

            assertThat(firstResponse).isNotNull();
            assertThat(secondResponse).isEqualTo(firstResponse);

            User updatedUser = userRepository.findByUsername("concurrentIdempotentUser").orElseThrow();
            assertThat(updatedUser.getDiamond()).isZero();

            List<UserAlien> userAliens = userAlienRepository.findAllByUser(updatedUser);
            assertThat(userAliens).singleElement().satisfies(userAlien -> {
                assertThat(userAlien.getAlienSpec().getId()).isEqualTo(22L);
                assertThat(userAlien.getLevel()).isEqualTo(1);
                assertThat(userAlien.getPieces()).isEqualTo(49);
            });

            List<GachaPurchase> purchases = gachaPurchaseRepository.findByUser(updatedUser);
            assertThat(purchases).singleElement().satisfies(purchase -> {
                assertThat(purchase.getStatus()).isEqualTo(GachaPurchaseStatus.COMPLETED);
                assertThat(purchase.getProductId()).isEqualTo("ALIEN_GACHA_SINGLE");
                assertThat(purchase.getPurchaseRequestId()).isEqualTo(purchaseRequestId);
                assertThat(purchase.getResponseJson()).isNotBlank();
            });

            verify(gachaDrawService, times(1)).draw("ALIEN_GACHA_SINGLE");
        } finally {
            start.countDown();
            executor.shutdownNow();
            assertThat(executor.awaitTermination(5, TimeUnit.SECONDS)).isTrue();
        }
    }

    private GachaPurchaseResponseDto purchaseAfterStart(
            CountDownLatch ready,
            CountDownLatch start,
            UUID purchaseRequestId
    ) throws InterruptedException {
        ready.countDown();
        assertThat(start.await(5, TimeUnit.SECONDS)).isTrue();
        return gachaPurchaseService.purchase(
                "concurrentIdempotentUser",
                "ALIEN_GACHA_SINGLE",
                purchaseRequestId
        );
    }
}
