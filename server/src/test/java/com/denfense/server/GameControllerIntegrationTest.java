package com.denfense.server;

import com.denfense.server.domain.User;
import com.denfense.server.dto.request.GameEntryRequestDto;
import com.denfense.server.dto.response.GameEntryResponseDto;
import com.denfense.server.dto.request.GameFinishRequestDto;
import com.denfense.server.dto.response.GameFinishResponseDto;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.session.GameSession;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.time.LocalDateTime;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class GameControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private GameSessionManager sessionManager;

    private User testUser;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();

        testUser = new User("ControllerTester", "pw");
        testUser.setGold(1000);
        testUser.setHeart(100);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(10));
        testUser = userRepository.saveAndFlush(testUser);
        
        if (sessionManager.hasActiveSession(testUser.getId())) {
            sessionManager.removeSession(testUser.getId());
        }
    }

    @Test
    @DisplayName("POST /api/game/enter 정상 작동 검증")
    void test_enterApi_success() {
        GameEntryRequestDto req = new GameEntryRequestDto();
        req.setUsername("ControllerTester");

        ResponseEntity<GameEntryResponseDto> response = restTemplate.postForEntity("/api/game/enter", req, GameEntryResponseDto.class);
        
        assertEquals(HttpStatus.OK, response.getStatusCode());
        GameEntryResponseDto dto = response.getBody();
        assertNotNull(dto);
        assertEquals(99, dto.getRemainingHeart()); // 하트 1개 차감
        assertFalse(dto.isReconnected());
        assertNotNull(dto.getSessionId());

        assertTrue(sessionManager.hasActiveSession(testUser.getId()));
        sessionManager.removeSession(testUser.getId());
    }

    @Test
    @DisplayName("기존 /api/game/start 우회 시 정상적으로 enter 로직 수행 및 하트 차감")
    void test_startApi_compatibility() {
        // HTTP POST with url param
        ResponseEntity<String> response = restTemplate.postForEntity("/api/game/start?userId=" + testUser.getId(), null, String.class);
        
        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertTrue(response.getBody().contains("게임 시작!"));

        User dbUser = userRepository.findById(testUser.getId()).get();
        assertEquals(99, dbUser.getHeart(), "하트 우회 불가 - 1 차감되어야 함");
        assertTrue(sessionManager.hasActiveSession(testUser.getId()));
        
        sessionManager.removeSession(testUser.getId());
    }

    @Test
    @DisplayName("POST /api/game/finish 정상 정산 검증")
    void test_finishApi_success() {
        // 1. Enter
        GameEntryRequestDto enterReq = new GameEntryRequestDto();
        enterReq.setUsername("ControllerTester");
        ResponseEntity<GameEntryResponseDto> enterRes = restTemplate.postForEntity("/api/game/enter", enterReq, GameEntryResponseDto.class);
        String sessionId = enterRes.getBody().getSessionId();

        // 2. GameOver 강제 설정
        sessionManager.getSession(testUser.getId()).setGameOver(true);

        // 3. Finish
        GameFinishRequestDto finishReq = new GameFinishRequestDto();
        finishReq.setUsername("ControllerTester");
        finishReq.setSessionId(sessionId);

        ResponseEntity<GameFinishResponseDto> finishRes = restTemplate.postForEntity("/api/game/finish", finishReq, GameFinishResponseDto.class);
        assertEquals(HttpStatus.OK, finishRes.getStatusCode());
        GameFinishResponseDto finishDto = finishRes.getBody();
        assertNotNull(finishDto);
        assertEquals(100, finishDto.getRewardGold()); // clearedWave=0, so 100
        assertFalse(finishDto.isAlreadyProcessed());

        // 4. 세션 삭제 확인
        assertFalse(sessionManager.hasActiveSession(testUser.getId()));
    }

    @Test
    @DisplayName("POST /api/game/finish 예외 케이스 검증")
    void test_finishApi_exceptions() {
        // 1. username blank
        GameFinishRequestDto reqBlankUser = new GameFinishRequestDto();
        reqBlankUser.setUsername("");
        reqBlankUser.setSessionId("some-uuid");
        ResponseEntity<String> res1 = restTemplate.postForEntity("/api/game/finish", reqBlankUser, String.class);
        assertEquals(HttpStatus.BAD_REQUEST, res1.getStatusCode());

        // 2. sessionId blank
        GameFinishRequestDto reqBlankSession = new GameFinishRequestDto();
        reqBlankSession.setUsername("ControllerTester");
        reqBlankSession.setSessionId("");
        ResponseEntity<String> res2 = restTemplate.postForEntity("/api/game/finish", reqBlankSession, String.class);
        assertEquals(HttpStatus.BAD_REQUEST, res2.getStatusCode());

        // 3. User 없음
        GameFinishRequestDto reqNoUser = new GameFinishRequestDto();
        reqNoUser.setUsername("NoUserExists");
        reqNoUser.setSessionId("some-uuid");
        ResponseEntity<String> res3 = restTemplate.postForEntity("/api/game/finish", reqNoUser, String.class);
        assertEquals(HttpStatus.NOT_FOUND, res3.getStatusCode()); // USER_NOT_FOUND is 404

        // 4. 세션 없음
        GameFinishRequestDto reqNoSession = new GameFinishRequestDto();
        reqNoSession.setUsername("ControllerTester");
        reqNoSession.setSessionId("some-uuid");
        ResponseEntity<String> res4 = restTemplate.postForEntity("/api/game/finish", reqNoSession, String.class);
        assertEquals(HttpStatus.NOT_FOUND, res4.getStatusCode()); // GAME_SESSION_NOT_FOUND is 404

        // 세션 생성
        GameSession session = sessionManager.createSession(testUser.getId());
        String sessionId = session.getSessionId();

        // 5. sessionId 불일치
        GameFinishRequestDto reqMismatch = new GameFinishRequestDto();
        reqMismatch.setUsername("ControllerTester");
        reqMismatch.setSessionId("wrong-uuid");
        ResponseEntity<String> res5 = restTemplate.postForEntity("/api/game/finish", reqMismatch, String.class);
        assertEquals(HttpStatus.FORBIDDEN, res5.getStatusCode()); // GAME_SESSION_OWNERSHIP_MISMATCH is 403

        // 6. 게임 미종료
        GameFinishRequestDto reqNotOver = new GameFinishRequestDto();
        reqNotOver.setUsername("ControllerTester");
        reqNotOver.setSessionId(sessionId);
        ResponseEntity<String> res6 = restTemplate.postForEntity("/api/game/finish", reqNotOver, String.class);
        assertEquals(HttpStatus.BAD_REQUEST, res6.getStatusCode()); // GAME_NOT_FINISHED is 400
    }
}
