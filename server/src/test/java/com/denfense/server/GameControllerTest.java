package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MutationType;
import com.denfense.server.domain.User;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.object.InGameInjector;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserRepository;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

import java.util.ArrayList;

import static org.hamcrest.Matchers.is;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureMockMvc
class GameControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private GameSessionManager sessionManager;

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private ObjectMapper objectMapper;

    private User testUser;
    private AlienSpec normalAlienSpec;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();
        alienSpecRepository.deleteAll();

        normalAlienSpec = new AlienSpec();
        normalAlienSpec.setName("일반왹져");
        normalAlienSpec.setGrade(AlienSpec.Grade.NORMAL);
        normalAlienSpec.setLocked(false);
        alienSpecRepository.save(normalAlienSpec);

        testUser = new User();
        testUser.setUsername("Tester");
        testUser.setGold(1000);
        testUser.setDiamond(100);
        testUser.setHeart(5);
        testUser.setUserAliens(new ArrayList<>());
        userRepository.save(testUser);

        sessionManager.removeSession(testUser.getId());
    }

    @Test
    @DisplayName("정상 이동 API는 HTTP 200과 이동 성공 메시지를 반환한다")
    void move_success() throws Exception {
        // Given
        GameSession session = sessionManager.createSession(testUser.getId());
        InGameAlien alien = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);

        String json = String.format(
                "{\"userId\":%d, \"objectId\":%d, \"newX\":2, \"newY\":2}",
                testUser.getId(), alien.getId()
        );

        // When & Then
        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message", is("이동 성공!")))
                .andExpect(jsonPath("$.alien.id", is(alien.getId().intValue())))
                .andExpect(jsonPath("$.alien.gridX", is(2)))
                .andExpect(jsonPath("$.alien.gridY", is(2)));
    }

    @Test
    @DisplayName("없는 세션에 대한 요청 시 HTTP 404와 GAME_SESSION_NOT_FOUND 에러를 반환한다")
    void move_sessionNotFound() throws Exception {
        String json = "{\"userId\":9999, \"objectId\":1, \"newX\":2, \"newY\":2}";

        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code", is("GAME_SESSION_NOT_FOUND")));
    }

    @Test
    @DisplayName("존재하지 않는 BoardObject 이동 시 HTTP 404와 BOARD_OBJECT_NOT_FOUND 에러를 반환한다")
    void move_boardObjectNotFound() throws Exception {
        sessionManager.createSession(testUser.getId());

        String json = String.format(
                "{\"userId\":%d, \"objectId\":9999, \"newX\":2, \"newY\":2}",
                testUser.getId()
        );

        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code", is("BOARD_OBJECT_NOT_FOUND")));
    }

    @Test
    @DisplayName("범위 밖 좌표로 이동 시 HTTP 400과 INVALID_BOARD_POSITION 에러를 반환한다")
    void move_invalidBoardPosition() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        InGameAlien alien = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);

        String json = String.format(
                "{\"userId\":%d, \"objectId\":%d, \"newX\":-1, \"newY\":2}",
                testUser.getId(), alien.getId()
        );

        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code", is("INVALID_BOARD_POSITION")));
    }

    @Test
    @DisplayName("보드 만석일 때 Kidnap 시도 시 HTTP 409와 BOARD_FULL 에러를 반환한다")
    void kidnap_boardFull() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        // 보드 24칸 다 채우기
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 6; j++) {
                session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, i, j);
            }
        }

        mockMvc.perform(post("/api/game/summon")
                .param("userId", testUser.getId().toString()))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.code", is("BOARD_FULL")));
    }

    @Test
    @DisplayName("골드가 부족할 때 Kidnap 시도 시 HTTP 409와 INSUFFICIENT_GOLD 에러를 반환한다")
    void kidnap_insufficientGold() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        session.spendGold(460); // 40 Gold만 남김

        mockMvc.perform(post("/api/game/summon")
                .param("userId", testUser.getId().toString()))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.code", is("INSUFFICIENT_GOLD")));
    }

    @Test
    @DisplayName("등급이 다른 왹져를 합성 시도 시 HTTP 400과 INVALID_MERGE 에러를 반환한다")
    void merge_invalidMerge() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        
        AlienSpec epicAlienSpec = new AlienSpec();
        epicAlienSpec.setName("에픽왹져");
        epicAlienSpec.setGrade(AlienSpec.Grade.EPIC);
        epicAlienSpec.setLocked(false);
        alienSpecRepository.save(epicAlienSpec);

        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(epicAlienSpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        String json = String.format(
                "{\"userId\":%d, \"sourceId\":%d, \"targetId\":%d}",
                testUser.getId(), source.getId(), target.getId()
        );

        mockMvc.perform(post("/api/game/merge")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code", is("INVALID_MERGE")));
    }

    @Test
    @DisplayName("인젝터 주입 시 MutationType.NONE 등 잘못된 인젝터 타입 주입 시 HTTP 400과 INVALID_INJECTOR 에러를 반환한다")
    void useInjector_invalidInjector() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        InGameAlien alien = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        
        // NONE 인젝터는 InGameInjector 생성단계나 사용단계에서 거절됨
        String json = String.format(
                "{\"userId\":%d, \"injectorId\":9999, \"alienId\":%d}",
                testUser.getId(), alien.getId()
        );

        mockMvc.perform(post("/api/game/use-injector")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code", is("INVALID_INJECTOR")));
    }

    @Test
    @DisplayName("잘못된 JSON 형식 전송 시 HTTP 400과 INVALID_REQUEST 에러를 반환한다")
    void badRequest_invalidJson() throws Exception {
        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content("{ bad json }"))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code", is("INVALID_REQUEST")));
    }

    @Test
    @DisplayName("예상치 못한 서버 에러 발생 시 HTTP 500과 INTERNAL_SERVER_ERROR를 반환하고 스택트레이스를 노출하지 않는다")
    void internalServerError_hidesTrace() throws Exception {
        // sessionManager에 userId를 null로 조회하여 강제로 NullPointerException 유발
        mockMvc.perform(post("/api/game/summon")
                .param("userId", ""))
                .andExpect(status().isInternalServerError())
                .andExpect(jsonPath("$.code", is("INTERNAL_SERVER_ERROR")))
                .andExpect(jsonPath("$.message", is("예상치 못한 서버 에러가 발생했습니다.")));
    }

    @Test
    @DisplayName("왹져 성공 응답 직렬화 시 AlienSpec 구체 멤버를 유실하지 않는다")
    void serialize_alienSuccessful() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        InGameAlien alien = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);

        String json = String.format(
                "{\"userId\":%d, \"objectId\":%d, \"newX\":2, \"newY\":2}",
                testUser.getId(), alien.getId()
        );

        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.alien.objectType", is("ALIEN")))
                .andExpect(jsonPath("$.alien.alienSpec.name", is("일반왹져")))
                .andExpect(jsonPath("$.alien.alienSpec.grade", is("NORMAL")))
                .andExpect(jsonPath("$.alien.pendingMutationType", is("NONE")));
    }

    @Test
    @DisplayName("인젝터 성공 응답 직렬화 시 Injector 구체 멤버를 유실하지 않는다")
    void serialize_injectorSuccessful() throws Exception {
        GameSession session = sessionManager.createSession(testUser.getId());
        InGameInjector injector = session.spawnInjector(MutationType.BERSERK, 1, 1);

        String json = String.format(
                "{\"userId\":%d, \"objectId\":%d, \"newX\":2, \"newY\":2}",
                testUser.getId(), injector.getId()
        );

        mockMvc.perform(post("/api/game/move")
                .contentType(MediaType.APPLICATION_JSON)
                .content(json))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.alien.objectType", is("MUTATION_INJECTOR")))
                .andExpect(jsonPath("$.alien.mutationType", is("BERSERK")));
    }
}
