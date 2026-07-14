package com.denfense.server;

import com.denfense.server.domain.User;
import com.denfense.server.dto.request.GameEntryRequestDto;
import com.denfense.server.dto.response.GameEntryResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.GameEntryService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.time.LocalDateTime;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicInteger;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest
class GameEntryServiceTest {

    @Autowired
    private GameEntryService gameEntryService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private GameSessionManager sessionManager;

    private User testUser;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();
        testUser = new User("EntryTester", "pw");
        testUser.setGold(1000);
        testUser.setHeart(100);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(20));
        testUser = userRepository.saveAndFlush(testUser);
        
        if (sessionManager.hasActiveSession(testUser.getId())) {
            sessionManager.removeSession(testUser.getId());
        }
    }

    @Test
    @DisplayName("정상 입장 및 하트 1개 차감 (최대하트일 때)")
    void test_enterGame_success() {
        // User starts with 100 hearts
        GameEntryResponseDto res = gameEntryService.enterGame("EntryTester");

        assertNotNull(res);
        assertFalse(res.isReconnected());
        assertEquals(99, res.getRemainingHeart());
        assertEquals("EntryTester", res.getUsername());
        assertNotNull(res.getSessionId());

        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(99, dbUser.getHeart());
        
        assertTrue(sessionManager.hasActiveSession(dbUser.getId()));
        
        sessionManager.removeSession(dbUser.getId());
    }
    
    @Test
    @DisplayName("기존 세션 존재 시 재접속 및 하트 차감 없음")
    void test_enterGame_reconnect() {
        GameEntryResponseDto res1 = gameEntryService.enterGame("EntryTester");
        assertEquals(99, res1.getRemainingHeart());
        assertFalse(res1.isReconnected());
        
        GameEntryResponseDto res2 = gameEntryService.enterGame("EntryTester");
        assertTrue(res2.isReconnected());
        assertEquals(99, res2.getRemainingHeart());
        
        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(99, dbUser.getHeart());
        
        sessionManager.removeSession(dbUser.getId());
    }

    @Test
    @DisplayName("하트 부족 시 예외")
    void test_enterGame_insufficientHeart() {
        testUser.setHeart(0);
        testUser.setLastHeartUpdateTime(LocalDateTime.now());
        userRepository.saveAndFlush(testUser);

        BusinessException ex = assertThrows(BusinessException.class, () -> gameEntryService.enterGame("EntryTester"));
        assertEquals(ErrorCode.INSUFFICIENT_HEART, ex.getErrorCode());
        assertFalse(sessionManager.hasActiveSession(testUser.getId()));
    }

    @Test
    @DisplayName("자투리 시간 보존 (95하트, 10분 경과)")
    void test_enterGame_preservesTime() {
        testUser.setHeart(95);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(10));
        userRepository.saveAndFlush(testUser);
        
        GameEntryResponseDto res = gameEntryService.enterGame("EntryTester");
        assertEquals(94, res.getRemainingHeart());
        
        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(94, dbUser.getHeart());
        
        // 10분이 경과되었으므로, lastHeartUpdateTime은 10분 전 시점이어야 함
        assertTrue(dbUser.getLastHeartUpdateTime().isBefore(LocalDateTime.now().minusMinutes(9)));
        sessionManager.removeSession(dbUser.getId());
    }

    @Test
    @DisplayName("회복 적용 후 100이 되는 경우 (90하트, 160분 경과)")
    void test_enterGame_recoversToMax() {
        testUser.setHeart(90);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(160)); // 10칸 이상 회복 가능 -> 100
        userRepository.saveAndFlush(testUser);
        
        GameEntryResponseDto res = gameEntryService.enterGame("EntryTester");
        assertEquals(99, res.getRemainingHeart());
        
        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(99, dbUser.getHeart());
        sessionManager.removeSession(dbUser.getId());
    }

    @Test
    @DisplayName("동시 입장 요청 3건 시 세션 1개 생성 및 2건 재접속")
    void test_enterGame_concurrent() throws InterruptedException, java.util.concurrent.ExecutionException, java.util.concurrent.TimeoutException {
        int threadCount = 3;
        ExecutorService executorService = Executors.newFixedThreadPool(threadCount);
        CountDownLatch doneLatch = new CountDownLatch(threadCount);
        java.util.concurrent.CyclicBarrier barrier = new java.util.concurrent.CyclicBarrier(threadCount);

        AtomicInteger reconnectedCount = new AtomicInteger(0);
        AtomicInteger newCount = new AtomicInteger(0);
        
        // 동일 userId 조회 시 동일 ReentrantLock 인스턴스 검증
        java.util.concurrent.locks.ReentrantLock lock1 = sessionManager.getEntryLock(testUser.getId());
        java.util.concurrent.locks.ReentrantLock lock2 = sessionManager.getEntryLock(testUser.getId());
        assertSame(lock1, lock2, "항상 동일한 락 인스턴스를 반환해야 함");

        java.util.List<java.util.concurrent.Future<?>> futures = new java.util.ArrayList<>();

        for (int i = 0; i < threadCount; i++) {
            futures.add(executorService.submit(() -> {
                try {
                    barrier.await(); // 세 스레드 동시 시작 대기
                    GameEntryResponseDto res = gameEntryService.enterGame("EntryTester");
                    if (res.isReconnected()) {
                        reconnectedCount.incrementAndGet();
                    } else {
                        newCount.incrementAndGet();
                    }
                } catch (Exception e) {
                    throw new RuntimeException(e);
                } finally {
                    doneLatch.countDown();
                }
            }));
        }
        
        // Timeout 10 seconds for deadlock prevention
        boolean completed = doneLatch.await(10, java.util.concurrent.TimeUnit.SECONDS);
        assertTrue(completed, "교착 상태 발생: 10초 내에 스레드들이 완료되지 않음");
        
        for (java.util.concurrent.Future<?> f : futures) {
            f.get(1, java.util.concurrent.TimeUnit.SECONDS); // 예외 발생 여부 확인
        }

        assertEquals(1, newCount.get(), "새로운 세션 생성은 1번만 일어남");
        assertEquals(2, reconnectedCount.get(), "나머지 2건은 재접속 처리됨");

        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(99, dbUser.getHeart(), "하트 차감은 단 한 번만 발생");
        
        assertTrue(sessionManager.hasActiveSession(dbUser.getId()));
        
        // 정상 요청 이후 재요청 가능
        GameEntryResponseDto res4 = gameEntryService.enterGame("EntryTester");
        assertTrue(res4.isReconnected());
        
        sessionManager.removeSession(dbUser.getId());
    }

    @Test
    @DisplayName("빈 username 요청 시 예외 발생 및 락 안전 반환")
    void test_enterGame_blankUsername() {
        BusinessException ex = assertThrows(BusinessException.class, () -> gameEntryService.enterGame(" "));
        assertEquals(ErrorCode.INVALID_REQUEST, ex.getErrorCode());
        // 락이 반환되었는지 간접적으로 알기 위해선, removeEntryLock이 호출되어 map에서 사라지거나
        // 정상 동작해야 함. (Mocking하지 않는 이상, 여기선 기능 동작 여부만 확인)
    }

    @Test
    @DisplayName("TransactionSynchronization 없이 바로 종료되어도 락이 풀려야 다음 요청 성공")
    void test_enterGame_lockReleasedOnReconnect() {
        // 첫 번째 입장
        GameEntryResponseDto res1 = gameEntryService.enterGame("EntryTester");
        assertFalse(res1.isReconnected());
        
        // 두 번째 입장 (재접속) -> 이 경우 TransactionSynchronization.afterCommit이 안 탐
        // 그래도 락이 풀려야 세 번째 입장이 가능함
        GameEntryResponseDto res2 = gameEntryService.enterGame("EntryTester");
        assertTrue(res2.isReconnected());

        // 세 번째 입장 (정상 동작)
        GameEntryResponseDto res3 = gameEntryService.enterGame("EntryTester");
        assertTrue(res3.isReconnected());
        
        sessionManager.removeSession(testUser.getId());
    }
}
