package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MutationType;
import com.denfense.server.game.object.BoardObject;
import com.denfense.server.game.object.InGameAlien;
import com.denfense.server.game.object.InGameInjector;
import com.denfense.server.game.session.GameSession;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class GameSessionTest {

    private GameSession session;
    private AlienSpec dummySpec;

    @BeforeEach
    void setUp() {
        session = new GameSession(1L);
        dummySpec = new AlienSpec();
        dummySpec.setId(10L);
        dummySpec.setName("테스트왹져");
        dummySpec.setGrade(AlienSpec.Grade.NORMAL);
    }

    @Test
    @DisplayName("인젝터 정상 사용 시 pending 변이가 갱신되고 인젝터는 보드에서 삭제된다")
    void applyInjector_success() {
        // Given
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameInjector injector = session.spawnInjector(MutationType.BERSERK, 2, 2);

        Long alienId = alien.getId();
        Long injectorId = injector.getId();

        // When
        InGameAlien result = session.applyInjector(injectorId, alienId);

        // Then
        assertNotNull(result);
        assertEquals(alienId, result.getId());
        assertEquals(MutationType.BERSERK, result.getPendingMutationType());
        assertEquals(MutationType.NONE, result.getActiveMutationType()); // activeMutationType 보존 확인

        // Injector 삭제 검증
        assertFalse(session.getBoardObjects().containsKey(injectorId));
        assertNull(session.getGrid()[2][2]);

        // Alien ID 및 위치 유지 확인
        assertEquals(1, result.getGridX());
        assertEquals(1, result.getGridY());
        assertSame(alien, session.getBoardObject(alienId));
    }

    @Test
    @DisplayName("이미 pending 변이가 적용되어 있어도 새로운 인젝터 변이로 정상 덮어쓴다")
    void applyInjector_overwrite() {
        // Given
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.SWIFT, MutationType.NONE, 0, 1, 1);
        InGameInjector injector = session.spawnInjector(MutationType.TOXIC, 2, 2);

        // When
        InGameAlien result = session.applyInjector(injector.getId(), alien.getId());

        // Then
        assertEquals(MutationType.TOXIC, result.getPendingMutationType());
    }

    @Test
    @DisplayName("NONE 변이를 지닌 인젝터 생성 및 소환 시 예외가 발생한다")
    void spawnInjector_rejectNone() {
        // When & Then (NONE 지정 소환 시 생성자에서 검증 및 IllegalArgumentException 투하)
        assertThrows(IllegalArgumentException.class, () -> {
            session.spawnInjector(MutationType.NONE, 2, 2);
        });
    }

    @Test
    @DisplayName("BLANK(꽝) 변이를 지닌 인젝터 사용 시 예외가 발생한다")
    void applyInjector_rejectBlank() {
        // Given
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameInjector injector = session.spawnInjector(MutationType.BLANK, 2, 2);

        // When & Then
        assertThrows(IllegalArgumentException.class, () -> {
            session.applyInjector(injector.getId(), alien.getId());
        });
    }

    @Test
    @DisplayName("존재하지 않는 ID에 사용을 요구할 시 예외가 발생한다")
    void applyInjector_rejectNonExistentId() {
        // Given
        session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        session.spawnInjector(MutationType.BERSERK, 2, 2);

        // When & Then
        assertThrows(IllegalArgumentException.class, () -> {
            session.applyInjector(9999L, 8888L);
        });
    }

    @Test
    @DisplayName("타겟 ID의 타입이 뒤바뀐 경우 예외가 발생한다")
    void applyInjector_rejectSwappedTypes() {
        // Given
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameInjector injector = session.spawnInjector(MutationType.BERSERK, 2, 2);

        // When & Then
        // 왹져 자리에 인젝터 ID를 넣고 인젝터 자리에 왹져 ID를 넣은 경우
        assertThrows(IllegalArgumentException.class, () -> {
            session.applyInjector(alien.getId(), injector.getId());
        });
    }

    @Test
    @DisplayName("주입 작업 실패 시 왹져의 기존 상태가 변경되지 않고 인젝터도 필드에 유지된다")
    void applyInjector_rollbackOnFailure() {
        // Given
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.SWIFT, MutationType.NONE, 0, 1, 1);
        InGameInjector injector = session.spawnInjector(MutationType.BLANK, 2, 2); // BLANK는 예외 발생함

        // When
        assertThrows(IllegalArgumentException.class, () -> {
            session.applyInjector(injector.getId(), alien.getId());
        });

        // Then (상태 롤백 상태 보존 검증)
        assertEquals(MutationType.SWIFT, alien.getPendingMutationType()); // 변경되지 않고 원래의 SWIFT 유지
        assertTrue(session.getBoardObjects().containsKey(injector.getId())); // 인젝터 미삭제
        assertSame(injector, session.getGrid()[2][2]); // 그리드 유지
    }

    @Test
    @DisplayName("동일 인젝터를 연속 두 번 사용할 경우 두 번째 시도는 예외가 발생한다")
    void applyInjector_cannotUseTwice() {
        // Given
        InGameAlien alien1 = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien alien2 = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 2);
        InGameInjector injector = session.spawnInjector(MutationType.BERSERK, 2, 2);

        Long injectorId = injector.getId();

        // 1차 사용 (성공)
        session.applyInjector(injectorId, alien1.getId());

        // When & Then (2차 사용 시도 - 소멸되었으므로 실패)
        assertThrows(IllegalArgumentException.class, () -> {
            session.applyInjector(injectorId, alien2.getId());
        });
    }
}
