package com.denfense.server;

import com.denfense.server.domain.User;
import com.denfense.server.dto.response.EconomyBalanceResponseDto;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.EconomyService;
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
class LobbyControllerTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private EconomyService economyService;

    private User testUser;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();

        testUser = new User("LobbyTester", "secret-password");
        testUser.setGold(5000);
        testUser.setDiamond(100);
        testUser.setHeart(10);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(16)); // 1구간(10개) 회복
        testUser = userRepository.saveAndFlush(testUser);
    }

    @Test
    @DisplayName("Lobby API 호출 전후 DB 불변 및 Economy API와 값 일치")
    void lobbyApi_noDbMutation_and_matchesEconomyApi() {
        LocalDateTime beforeTime = testUser.getLastHeartUpdateTime();

        // 1. Lobby API 호출 (기존 호환 URL 유지 확인)
        ResponseEntity<String> response = restTemplate.getForEntity("/api/lobby/info/LobbyTester", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());
        
        String json = response.getBody();
        assertNotNull(json);
        
        // 2. 비밀번호 미노출 및 UserAlien 목록 직접 미노출 확인
        assertFalse(json.contains("secret-password"));
        assertFalse(json.contains("\"userAliens\""));
        // 계산된 heart가 20(10+10)인지 확인
        assertTrue(json.contains("\"heart\":20"));

        // 3. Economy API 호출 및 heart 값 일치 확인
        EconomyBalanceResponseDto economyDto = economyService.getBalance("LobbyTester");
        assertEquals(20, economyDto.getHeart());

        // 4. DB 상태 불변 확인
        User dbUser = userRepository.findByUsername("LobbyTester").get();
        assertEquals(10, dbUser.getHeart(), "DB value should not change during read");
        assertEquals(beforeTime.withNano(0), dbUser.getLastHeartUpdateTime().withNano(0));
    }
}
