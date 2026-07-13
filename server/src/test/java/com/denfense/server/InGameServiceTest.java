package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MutationType;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.game.manager.GameSessionManager;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.session.GameSession;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.InGameService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.ArrayList;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest
class InGameServiceTest {

    @Autowired
    private InGameService inGameService;

    @Autowired
    private GameSessionManager sessionManager;

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private UserRepository userRepository;

    private User testUser;
    private AlienSpec normalAlienSpec;
    private AlienSpec epicAlienSpec;
    private AlienSpec legendAlienSpec;
    private AlienSpec mythicAlienSpec;

    @BeforeEach
    void setUp() {
        userRepository.deleteAll();
        alienSpecRepository.deleteAll();

        // 1. 테스트용 AlienSpec DB 저장
        normalAlienSpec = new AlienSpec();
        normalAlienSpec.setId(100L);
        normalAlienSpec.setName("일반왹져");
        normalAlienSpec.setGrade(AlienSpec.Grade.NORMAL);
        normalAlienSpec.setLocked(false);
        alienSpecRepository.save(normalAlienSpec);

        epicAlienSpec = new AlienSpec();
        epicAlienSpec.setId(101L);
        epicAlienSpec.setName("에픽왹져");
        epicAlienSpec.setGrade(AlienSpec.Grade.EPIC);
        epicAlienSpec.setLocked(false);
        alienSpecRepository.save(epicAlienSpec);

        legendAlienSpec = new AlienSpec();
        legendAlienSpec.setId(102L);
        legendAlienSpec.setName("전설왹져");
        legendAlienSpec.setGrade(AlienSpec.Grade.LEGEND);
        legendAlienSpec.setLocked(false);
        alienSpecRepository.save(legendAlienSpec);

        mythicAlienSpec = new AlienSpec();
        mythicAlienSpec.setId(103L);
        mythicAlienSpec.setName("신화왹져");
        mythicAlienSpec.setGrade(AlienSpec.Grade.MYTHIC);
        mythicAlienSpec.setLocked(false);
        alienSpecRepository.save(mythicAlienSpec);

        // 2. 테스트용 유저 DB 저장 및 신화 등급 해금 정보 매핑
        testUser = new User();
        testUser.setUsername("Tester");
        testUser.setGold(1000);
        testUser.setDiamond(100);
        testUser.setHeart(5);
        testUser.setUserAliens(new ArrayList<>());
        
        UserAlien userAlien = new UserAlien();
        userAlien.setUser(testUser);
        userAlien.setAlienSpec(mythicAlienSpec);
        testUser.getUserAliens().add(userAlien);

        userRepository.save(testUser);

        // 3. 테스트용 인게임 세션 강제 초기화 및 설정 (getSession() 호출 시 에러가 나므로 removeSession()을 직접 호출)
        sessionManager.removeSession(testUser.getId());
        sessionManager.createSession(testUser.getId());
    }

    @Test
    @DisplayName("NONE + NONE 합성은 결과 pendingMutationType이 NONE이다")
    void merge_noneAndNone_yieldsNone() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertEquals(MutationType.NONE, result.getPendingMutationType());
    }

    @Test
    @DisplayName("BERSERK + NONE 합성은 결과 pendingMutationType이 BERSERK이다")
    void merge_berserkAndNone_yieldsBerserk() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.BERSERK, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertEquals(MutationType.BERSERK, result.getPendingMutationType());
    }

    @Test
    @DisplayName("NONE + BERSERK 합성은 결과 pendingMutationType이 BERSERK이다")
    void merge_noneAndBerserk_yieldsBerserk() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.BERSERK, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertEquals(MutationType.BERSERK, result.getPendingMutationType());
    }

    @Test
    @DisplayName("SWIFT + SWIFT 합성은 결과 pendingMutationType이 SWIFT이다")
    void merge_swiftAndSwift_yieldsSwift() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.SWIFT, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.SWIFT, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertEquals(MutationType.SWIFT, result.getPendingMutationType());
    }

    @Test
    @DisplayName("BERSERK + SWIFT 합성은 결과 pendingMutationType이 BERSERK 또는 SWIFT 중 하나이다")
    void merge_differentMutations_yieldsOne() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.BERSERK, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.SWIFT, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertTrue(result.getPendingMutationType() == MutationType.BERSERK || result.getPendingMutationType() == MutationType.SWIFT);
    }

    @Test
    @DisplayName("BLANK(꽝) + NONE 합성은 결과 pendingMutationType이 BLANK이다")
    void merge_blankAndNone_yieldsBlank() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.BLANK, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        InGameAlien result = inGameService.processMerge(testUser.getId(), source.getId(), target.getId());

        assertNotNull(result);
        assertEquals(MutationType.BLANK, result.getPendingMutationType());
    }

    @Test
    @DisplayName("서로 다른 DNA라도 동일 종/동일 등급이면 합성이 정상 허용된다")
    void merge_differentDNA_success() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.BERSERK, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.SWIFT, MutationType.NONE, 0, 2, 2);

        // 예외 없이 성공하는지 확인
        assertDoesNotThrow(() -> {
            inGameService.processMerge(testUser.getId(), source.getId(), target.getId());
        });
    }

    @Test
    @DisplayName("합성 완료 시 결과 유닛의 위치는 target 유닛의 위치와 같고, 재료는 삭제되고 active는 NONE, reroll은 0이다")
    void merge_keepsTargetPositionAndRerollCount() {
        GameSession session = sessionManager.getSession(testUser.getId());
        InGameAlien source = session.spawnAlien(normalAlienSpec, MutationType.BERSERK, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(normalAlienSpec, MutationType.SWIFT, MutationType.NONE, 0, 2, 3); // target 위치는 (2, 3)

        Long sourceId = source.getId();
        Long targetId = target.getId();

        InGameAlien result = inGameService.processMerge(testUser.getId(), sourceId, targetId);

        // 결과 위치 검증 (target의 X, Y 좌표 유지)
        assertEquals(2, result.getGridX());
        assertEquals(3, result.getGridY());

        // 재료 삭제 검증
        assertFalse(session.getBoardObjects().containsKey(sourceId));
        assertFalse(session.getBoardObjects().containsKey(targetId));
        assertNull(session.getGrid()[1][1]);

        // activeMutationType NONE 검증 및 rerollCount 0 검증
        assertEquals(MutationType.NONE, result.getActiveMutationType());
        assertEquals(0, result.getMutationRerollCount());
    }
}
