package com.denfense.server;

import com.denfense.server.domain.GameSettlement;
import com.denfense.server.domain.User;
import com.denfense.server.dto.request.GameFinishRequestDto;
import com.denfense.server.dto.response.GameFinishResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.GameSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.GameSettlementService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicInteger;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

@SpringBootTest
class GameSettlementServiceTest {

    @Autowired
    private GameSettlementService gameSettlementService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private GameSettlementRepository gameSettlementRepository;

    @Autowired
    private GameSessionManager sessionManager;

    private User testUser;

    @BeforeEach
    void setUp() {
        gameSettlementRepository.deleteAll();
        userRepository.deleteAll();

        User user = new User("settleTester", "pass");
        user.setGold(0);
        testUser = userRepository.save(user);
    }

    @Test
    void 정상_보상정산_성공() {
        // given
        GameSession session = sessionManager.createSession(testUser.getId());
        session.setGameOver(true);
        session.nextWave(); // currentWave = 2, clearedWave = 1
        String sessionId = session.getSessionId();

        GameFinishRequestDto request = new GameFinishRequestDto();
        request.setUsername(testUser.getUsername());
        request.setSessionId(sessionId);

        // when
        GameFinishResponseDto response = gameSettlementService.finishGame(request);

        // then
        assertThat(response.isAlreadyProcessed()).isFalse();
        assertThat(response.getRewardGold()).isEqualTo(100 + (1 * 10)); // 110
        assertThat(response.getAccountGoldAfter()).isEqualTo(110);
        assertThat(response.getClearedWave()).isEqualTo(1);

        User updatedUser = userRepository.findById(testUser.getId()).get();
        assertThat(updatedUser.getGold()).isEqualTo(110);

        GameSettlement settlement = gameSettlementRepository.findBySessionId(sessionId).get();
        assertThat(settlement.getRewardGold()).isEqualTo(110);

        // 인메모리 세션 제거 검증
        assertThat(sessionManager.hasActiveSession(testUser.getId())).isFalse();
    }

    @Test
    void 비정상_큰_웨이브_최대_보상제한() {
        GameSession session = sessionManager.createSession(testUser.getId());
        session.setGameOver(true);
        for(int i=0; i<=1000; i++) session.nextWave(); // clearedWave = 1000

        GameFinishRequestDto request = new GameFinishRequestDto();
        request.setUsername(testUser.getUsername());
        request.setSessionId(session.getSessionId());

        GameFinishResponseDto response = gameSettlementService.finishGame(request);

        assertThat(response.getRewardGold()).isEqualTo(1000); // MAX 제한
        assertThat(response.getAccountGoldAfter()).isEqualTo(1000);
    }

    @Test
    void 재시도_요청시_보상_중복지급방지_및_기존기록반환() {
        // given
        GameSession session = sessionManager.createSession(testUser.getId());
        session.setGameOver(true);
        session.nextWave();
        String sessionId = session.getSessionId();

        GameFinishRequestDto request = new GameFinishRequestDto();
        request.setUsername(testUser.getUsername());
        request.setSessionId(sessionId);

        GameFinishResponseDto firstResponse = gameSettlementService.finishGame(request);
        assertThat(firstResponse.isAlreadyProcessed()).isFalse();

        // 인메모리 세션은 제거됨
        assertThat(sessionManager.hasActiveSession(testUser.getId())).isFalse();

        // when - 동일 sessionId로 재시도
        GameFinishResponseDto secondResponse = gameSettlementService.finishGame(request);

        // then
        assertThat(secondResponse.isAlreadyProcessed()).isTrue();
        assertThat(secondResponse.getRewardGold()).isEqualTo(110);
        assertThat(secondResponse.getAccountGoldAfter()).isEqualTo(110); // 골드 증가 없음

        User updatedUser = userRepository.findById(testUser.getId()).get();
        assertThat(updatedUser.getGold()).isEqualTo(110); // 1회만 지급됨
    }

    @Test
    void 다른_유저가_정산_시도시_예외발생() {
        User otherUser = userRepository.save(new User("thief", "pw"));
        
        GameSession session = sessionManager.createSession(testUser.getId());
        session.setGameOver(true);
        
        GameFinishRequestDto request = new GameFinishRequestDto();
        request.setUsername(otherUser.getUsername());
        request.setSessionId(session.getSessionId()); // 다른 사람 세션 ID 도용

        BusinessException ex = assertThrows(BusinessException.class, () -> gameSettlementService.finishGame(request));
        assertThat(ex.getErrorCode()).isEqualTo(ErrorCode.GAME_SESSION_NOT_FOUND); // otherUser는 세션이 없으므로
    }

    @Test
    void 동일_sessionId_동시_정산_경합_테스트() throws InterruptedException {
        GameSession session = sessionManager.createSession(testUser.getId());
        session.setGameOver(true);
        session.nextWave();
        String sessionId = session.getSessionId();

        int threadCount = 2;
        ExecutorService executorService = Executors.newFixedThreadPool(threadCount);
        CountDownLatch startLatch = new CountDownLatch(1);
        CountDownLatch doneLatch = new CountDownLatch(threadCount);

        AtomicInteger successCount = new AtomicInteger(0);
        AtomicInteger processedCount = new AtomicInteger(0);

        for (int i = 0; i < threadCount; i++) {
            executorService.execute(() -> {
                try {
                    startLatch.await(); // 모든 스레드가 동시에 시작되도록 대기
                    
                    GameFinishRequestDto request = new GameFinishRequestDto();
                    request.setUsername(testUser.getUsername());
                    request.setSessionId(sessionId);

                    GameFinishResponseDto res = gameSettlementService.finishGame(request);
                    if (res.isAlreadyProcessed()) {
                        processedCount.incrementAndGet();
                    } else {
                        successCount.incrementAndGet();
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                } finally {
                    doneLatch.countDown();
                }
            });
        }

        startLatch.countDown(); // 스레드 동시 시작
        doneLatch.await(); // 완료 대기

        // then
        assertThat(successCount.get()).isEqualTo(1); // 최초 1건만 성공
        assertThat(processedCount.get()).isEqualTo(1); // 1건은 이미 처리됨 반환

        User updatedUser = userRepository.findById(testUser.getId()).get();
        assertThat(updatedUser.getGold()).isEqualTo(110); // 골드는 110만 증가
        
        long count = gameSettlementRepository.count();
        assertThat(count).isEqualTo(1); // DB 기록도 1건만
    }
}
