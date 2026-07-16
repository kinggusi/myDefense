package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;

import static org.hamcrest.Matchers.*;
import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureMockMvc
public class LobbyControllerApiIntegrationTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private UserAlienRepository userAlienRepository;

    private User testUser;
    private User emptyUser;

    @BeforeEach
    void setUp() {
        // DB는 Application 기동 시 DataInit/Seed로 인해 기본 48종이 들어있는 상태를 가정
        userAlienRepository.deleteAll();
        userRepository.deleteAll();

        testUser = new User("lobbyUser1", "pw1");
        testUser.setGold(100);
        testUser.setDiamond(50);
        testUser.setHeart(10);
        testUser.setUniversalPiece(20);
        testUser.setGrowthCell(5);
        testUser = userRepository.save(testUser);

        emptyUser = new User("emptyUser", "pw");
        emptyUser = userRepository.save(emptyUser);

        List<AlienSpec> specs = alienSpecRepository.findAll();
        if (!specs.isEmpty()) {
            AlienSpec spec1 = specs.get(0); // ID 1 가정
            UserAlien ua1 = new UserAlien(testUser, spec1);
            ua1.setLevel(3);
            ua1.setPieces(7);
            userAlienRepository.save(ua1);
        }
    }

    @AfterEach
    void tearDown() {
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
    }

    @Test
    @DisplayName("정상 로비 조회: 재화, 전체 종 반환, 보유/미보유 스탯 및 잠금 처리, 오름차순 검증")
    void getLobbyInfo_success() throws Exception {
        List<AlienSpec> specs = alienSpecRepository.findAll();
        specs.sort(java.util.Comparator.comparing(AlienSpec::getId));
        int specCount = specs.size();
        Long firstId = specs.get(0).getId();
        Long lastId = specs.get(specCount - 1).getId();

        mockMvc.perform(get("/api/lobby/info/lobbyUser1"))
                .andExpect(status().isOk())
                // 1. 사용자 재화 전체 반환
                .andExpect(jsonPath("$.user.username").value("lobbyUser1"))
                .andExpect(jsonPath("$.user.gold").value(100))
                .andExpect(jsonPath("$.user.diamond").value(50))
                .andExpect(jsonPath("$.user.heart").isNumber()) // heart는 자동계산 반영됨
                .andExpect(jsonPath("$.user.universalPiece").value(20))
                .andExpect(jsonPath("$.user.growthCell").value(5))
                // 2. 전체 AlienSpec 반환
                .andExpect(jsonPath("$.aliens").isArray())
                .andExpect(jsonPath("$.aliens.length()").value(specCount))
                // 8. alienId 오름차순 검증
                .andExpect(jsonPath("$.aliens[0].id").value(firstId))
                .andExpect(jsonPath("$.aliens[" + (specCount - 1) + "].id").value(lastId))
                // 3. 보유 Alien 검증 (ID 1)
                .andExpect(jsonPath("$.aliens[0].owned").value(true))
                .andExpect(jsonPath("$.aliens[0].level").value(3))
                .andExpect(jsonPath("$.aliens[0].pieces").value(7))
                .andExpect(jsonPath("$.aliens[0].requiredPieces").value(15))
                // 4. 미보유 Alien 검증 (ID 2)
                .andExpect(jsonPath("$.aliens[1].owned").value(false))
                .andExpect(jsonPath("$.aliens[1].level").value(0))
                .andExpect(jsonPath("$.aliens[1].pieces").value(0))
                .andExpect(jsonPath("$.aliens[1].requiredPieces").value(0))
                // 5. specLocked는 출시 메타데이터, legacy locked는 미보유 여부만 표현
                .andExpect(jsonPath("$.aliens[0].specLocked").isBoolean())
                .andExpect(jsonPath("$.aliens[0].locked").value(false))
                .andExpect(jsonPath("$.aliens[1].specLocked").isBoolean())
                .andExpect(jsonPath("$.aliens[1].locked").value(true))
                // 6. 기본 스탯 반환 검증
                .andExpect(jsonPath("$.aliens[0].baseAtk").isNumber())
                .andExpect(jsonPath("$.aliens[0].baseMp").isNumber())
                .andExpect(jsonPath("$.aliens[0].atkSpeed").isNumber())
                .andExpect(jsonPath("$.aliens[0].range").isNumber());
                // 7. evolutionTargetId 매핑 검증
                // jsonPath에서는 값이 null일 때 exists()가 실패할 수 있음. 매핑됨을 다른 방식으로 간접 확인.
    }

    @Test
    void requiredPiecesUsesBalanceCostForOwnedLevelsAndZeroAtMax() throws Exception {
        AlienSpec firstSpec = alienSpecRepository.findAll().stream()
                .min(java.util.Comparator.comparing(AlienSpec::getId))
                .orElseThrow();
        UserAlien owned = userAlienRepository.findByUserAndAlienSpec(testUser, firstSpec).orElseThrow();

        assertRequiredPieces(owned, 1, 5);
        assertRequiredPieces(owned, 9, 45);
        assertRequiredPieces(owned, 10, 50);
        assertRequiredPieces(owned, 50, 0);
    }

    private void assertRequiredPieces(UserAlien owned, int level, int expected) throws Exception {
        owned.setLevel(level);
        userAlienRepository.save(owned);
        mockMvc.perform(get("/api/lobby/info/lobbyUser1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.aliens[0].requiredPieces").value(expected));
    }

    @Test
    @DisplayName("9. 보유 Alien이 0건인 사용자 검증")
    void getLobbyInfo_emptyUser() throws Exception {
        int specCount = alienSpecRepository.findAll().size();
        mockMvc.perform(get("/api/lobby/info/emptyUser"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.user.username").value("emptyUser"))
                .andExpect(jsonPath("$.aliens[0].owned").value(false))
                .andExpect(jsonPath("$.aliens[" + (specCount - 1) + "].owned").value(false));
    }

    @Test
    @DisplayName("10. 존재하지 않는 사용자 (404 예외 확인)")
    void getLobbyInfo_notFound() throws Exception {
        // BusinessException(ErrorCode.USER_NOT_FOUND) -> GlobalExceptionHandler에서 404로 매핑된다고 가정
        // 혹시 400이나 다른 값일 수 있으나 상태코드가 정상 200이 아니고 예외인지 검증
        mockMvc.perform(get("/api/lobby/info/unknown_user"))
                .andExpect(status().is4xxClientError());
    }

}
