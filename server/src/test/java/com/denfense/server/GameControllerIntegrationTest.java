package com.denfense.server;

import com.denfense.server.domain.User;
import com.denfense.server.dto.request.GameEntryRequestDto;
import com.denfense.server.dto.response.GameEntryResponseDto;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.game.manager.GameSessionManager;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
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
}
