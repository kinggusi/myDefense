package com.denfense.server.service.gacha;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.boot.test.mock.mockito.MockBean;
import static org.mockito.Mockito.when;
import org.springframework.transaction.annotation.Transactional;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@ActiveProfiles("test")
class GachaPurchaseServiceConcurrencyTest {

    @Autowired
    private GachaPurchaseService gachaPurchaseService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private com.denfense.server.repository.UserAlienRepository userAlienRepository;

    @Autowired
    private com.denfense.server.repository.GachaPurchaseRepository gachaPurchaseRepository;

    @MockBean
    private com.denfense.server.service.gacha.GachaRandomGenerator randomGenerator;

    private User testUser;

    @BeforeEach
    void setUp() {
        testUser = new User("concurrentUser", "pw");
        testUser.setDiamond(500); // 1회 뽑기 가능 금액
        userRepository.save(testUser);
        
        // SINGLE(500원), POOL1(테스트용 풀), 뽑기 확률 고정
        // NORMAL 등급(가중치 누적 알고리즘 기준), alienId 22 고정
        when(randomGenerator.nextInt(org.mockito.ArgumentMatchers.anyInt())).thenReturn(0);
    }

    @AfterEach
    void tearDown() {
        gachaPurchaseRepository.deleteAll();
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
    }

    @Test
    @DisplayName("31,32,33,34. 동시성 - 다이아 500으로 동시 2건 요청 시 1건만 성공하고 보상(UserAlien)이 1회분만 지급됨")
    void concurrentPurchaseTest() throws InterruptedException {
        int threadCount = 2;
        ExecutorService executorService = Executors.newFixedThreadPool(threadCount);
        CountDownLatch latch = new CountDownLatch(threadCount);
        
        AtomicInteger successCount = new AtomicInteger(0);
        AtomicInteger failCount = new AtomicInteger(0);

        for (int i = 0; i < threadCount; i++) {
            executorService.execute(() -> {
                try {
                    gachaPurchaseService.purchase("concurrentUser", "ALIEN_GACHA_SINGLE", UUID.randomUUID());
                    successCount.incrementAndGet();
                } catch (Exception e) {
                    failCount.incrementAndGet();
                } finally {
                    latch.countDown();
                }
            });
        }

        latch.await();

        // 1건 성공, 1건 실패
        assertThat(successCount.get()).isEqualTo(1);
        assertThat(failCount.get()).isEqualTo(1);

        // 잔액 검증
        User updatedUser = userRepository.findByUsername("concurrentUser").orElseThrow();
        assertThat(updatedUser.getDiamond()).isEqualTo(0);

        // 보상 검증
        java.util.List<com.denfense.server.domain.UserAlien> userAliens = userAlienRepository.findAllByUser(updatedUser);
        assertThat(userAliens).hasSize(1);
        com.denfense.server.domain.UserAlien userAlien = userAliens.get(0);
        assertThat(userAlien.getLevel()).isEqualTo(1);
        assertThat(userAlien.getPieces()).isEqualTo(49);
}

}
