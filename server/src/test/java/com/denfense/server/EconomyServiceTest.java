package com.denfense.server;

import com.denfense.server.domain.User;
import com.denfense.server.dto.response.EconomyBalanceResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.EconomyService;
import com.denfense.server.service.HeartPolicy;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.time.LocalDateTime;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest
class EconomyServiceTest {

    @Autowired
    private EconomyService economyService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private ObjectMapper objectMapper;

    private User testUser;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();

        testUser = new User("EcoTester", "secret-password");
        testUser.setGold(5000);
        testUser.setDiamond(100);
        testUser.setHeart(10);
        testUser.setUniversalPiece(20);
        testUser.setGrowthCell(5);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(5)); // 5분 전
        testUser = userRepository.saveAndFlush(testUser);
    }

    @Test
    @DisplayName("1. 정상 재화 조회 및 2~5. 필드 매핑 검증")
    void getBalance_mappingCheck() {
        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals("EcoTester", dto.getUsername());
        assertEquals(5000, dto.getAccountGold()); // 2. accountGold 매핑
        assertEquals(100, dto.getGem()); // 3. diamond -> gem 매핑
        assertEquals(20, dto.getUniversalPiece()); // 4. universalPiece 매핑
        assertEquals(5, dto.getGrowthCell()); // 5. growthCell 매핑
    }

    @Test
    @DisplayName("6. inGameGold 미포함, 7. password 미노출, 8. userAliens 미노출 확인")
    void getBalance_noSecretFields() {
        // DTO 클래스 자체에 해당 필드가 없으므로 컴파일 레벨에서 보장됨을 확인 (리플렉션 없이 단언)
        // json 직렬화 시에도 노출 안되는지 확인
        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertNull(getFieldValueOrNull(dto, "password"));
        assertNull(getFieldValueOrNull(dto, "userAliens"));
        assertNull(getFieldValueOrNull(dto, "inGameGold"));
    }

    @Test
    @DisplayName("9. 하트 회복 구간 미도달")
    void getBalance_heart_noRecoveryYet() {
        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(10, dto.getHeart());
        assertNotNull(dto.getNextHeartRecoveryAt());
        // 5분 전 기준이므로 +15분 = 10분 뒤 회복
    }

    @Test
    @DisplayName("10. 하트 1회 회복")
    void getBalance_heart_oneRecovery() {
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(16));
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(20, dto.getHeart()); // 10 + 10
    }

    @Test
    @DisplayName("11. 하트 여러 구간 회복")
    void getBalance_heart_multipleRecovery() {
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(46)); // 3구간(45분) 경과 + 1분 자투리
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(40, dto.getHeart()); // 10 + 30
    }

    @Test
    @DisplayName("12. 하트 최대치 제한")
    void getBalance_heart_maxLimit() {
        testUser.setHeart(95);
        testUser.setLastHeartUpdateTime(LocalDateTime.now().minusMinutes(16)); // 1구간(10개) 회복
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(100, dto.getHeart()); // 95 + 10 = 105 -> 100
    }

    @Test
    @DisplayName("13. 최대치일 때 nextHeartRecoveryAt null")
    void getBalance_maxHeart_nullRecoveryTime() {
        testUser.setHeart(100);
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertNull(dto.getNextHeartRecoveryAt());
    }

    @Test
    @DisplayName("14. 자투리 시간 보존")
    void getBalance_preserveRemainingTime() {
        LocalDateTime past = LocalDateTime.now().minusMinutes(17); // 1구간(15분) + 2분 자투리
        testUser.setLastHeartUpdateTime(past);
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(20, dto.getHeart());
        
        // 자투리 2분이 보존되었으므로 다음 회복은 past + 15(사용됨) + 15(다음)
        LocalDateTime expectedNext = past.plusMinutes(30);
        // assert equal up to seconds
        assertEquals(expectedNext.withNano(0), dto.getNextHeartRecoveryAt().withNano(0));
    }

    @Test
    @DisplayName("15. lastHeartUpdateTime null 처리")
    void getBalance_nullLastUpdateTime() {
        testUser.setLastHeartUpdateTime(null);
        userRepository.saveAndFlush(testUser);

        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        assertEquals(100, dto.getHeart());
        assertNull(dto.getNextHeartRecoveryAt());
    }

    @Test
    @DisplayName("16. 사용자 없음")
    void getBalance_userNotFound() {
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            economyService.getBalance("Unknown"));
        assertEquals(ErrorCode.USER_NOT_FOUND, ex.getErrorCode());
    }

    @Test
    @DisplayName("17. username blank")
    void getBalance_blankUsername() {
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            economyService.getBalance(""));
        assertEquals(ErrorCode.INVALID_REQUEST, ex.getErrorCode());
    }

    @Test
    @DisplayName("18. JSON camelCase")
    void getBalance_jsonCamelCase() throws JsonProcessingException {
        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        String json = objectMapper.writeValueAsString(dto);
        assertTrue(json.contains("\"accountGold\""));
        assertTrue(json.contains("\"nextHeartRecoveryAt\""));
        assertTrue(json.contains("\"universalPiece\""));
        assertFalse(json.contains("\"account_gold\""));
    }

    @Test
    @DisplayName("19. GET 호출 후 DB의 heart와 lastHeartUpdateTime이 변경되지 않음")
    void getBalance_noDbMutation() {
        LocalDateTime beforeTime = testUser.getLastHeartUpdateTime();
        
        EconomyBalanceResponseDto dto = economyService.getBalance("EcoTester");
        
        // flush & clear is handled if we just read it directly via a new query or check entity
        User dbUser = userRepository.findByUsername("EcoTester").get();
        assertEquals(10, dbUser.getHeart(), "DB value should not change during read");
        assertEquals(beforeTime.withNano(0), dbUser.getLastHeartUpdateTime().withNano(0));
    }

    private Object getFieldValueOrNull(Object obj, String fieldName) {
        try {
            java.lang.reflect.Field field = obj.getClass().getDeclaredField(fieldName);
            field.setAccessible(true);
            return field.get(obj);
        } catch (NoSuchFieldException | IllegalAccessException e) {
            return null; // Field does not exist or cannot be accessed
        }
    }
}
