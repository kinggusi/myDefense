package com.denfense.server;

import com.denfense.server.exception.BusinessException;
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
        assertThrows(BusinessException.class, () -> {
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
        assertThrows(BusinessException.class, () -> {
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
        assertThrows(BusinessException.class, () -> {
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
        assertThrows(BusinessException.class, () -> {
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
        assertThrows(BusinessException.class, () -> {
            session.applyInjector(injectorId, alien2.getId());
        });
    }

    @Test
    @DisplayName("새 GameSession의 최초 Kidnap 비용은 50 Gold이다")
    void kidnapCost_initialIs50() {
        assertEquals(50, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("Alien Kidnap 성공 시 골드가 차감되고 성공 횟수 및 비용이 정상 우상향한다")
    void kidnapAlien_successIncrementsCost() {
        // Given
        int initialGold = session.getInGameGold(); // 500

        // When (1회 성공)
        BoardObject first = session.kidnapAlien(dummySpec);
        assertNotNull(first);

        // Then (50 Gold 차감 및 비용 60 갱신)
        assertEquals(initialGold - 50, session.getInGameGold());
        assertEquals(1, session.getKidnapSuccessCount());
        assertEquals(60, session.getCurrentKidnapCost());

        // When (2회 성공)
        BoardObject second = session.kidnapAlien(dummySpec);
        assertNotNull(second);

        // Then (60 Gold 추가 차감 및 비용 70 갱신)
        assertEquals(initialGold - 50 - 60, session.getInGameGold());
        assertEquals(2, session.getKidnapSuccessCount());
        assertEquals(70, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("Injector Kidnap 성공 시에도 동일하게 성공 횟수 및 비용이 인상된다")
    void kidnapInjector_successIncrementsCost() {
        int initialGold = session.getInGameGold();

        BoardObject injector = session.kidnapInjector(MutationType.BERSERK);
        assertNotNull(injector);

        assertEquals(initialGold - 50, session.getInGameGold());
        assertEquals(1, session.getKidnapSuccessCount());
        assertEquals(60, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("골드가 부족한 상태에서 Kidnap 시도 시 예외가 나고 골드와 횟수가 보존된다")
    void kidnap_rejectWhenGoldInsufficient() {
        // Given (골드 부족 유도: 세션 골드를 40으로 강제 세팅)
        session.spendGold(460); // 남은 골드 40
        int cost = session.getCurrentKidnapCost(); // 50

        // When & Then (골드 부족 실패 확인)
        assertThrows(BusinessException.class, () -> {
            session.kidnapAlien(dummySpec);
        });

        // 상태 유지 확인
        assertEquals(40, session.getInGameGold());
        assertEquals(0, session.getKidnapSuccessCount());
        assertEquals(50, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("보드가 가득 찬 상태에서 Kidnap 시도 시 예외가 나고 골드와 횟수가 보존된다")
    void kidnap_rejectWhenBoardFull() {
        // Given (보드 24칸 가득 채우기)
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 6; j++) {
                session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, i, j);
            }
        }
        int initialGold = session.getInGameGold();

        // When & Then (가득 차서 실패)
        assertThrows(BusinessException.class, () -> {
            session.kidnapAlien(dummySpec);
        });

        // 상태 유지 확인
        assertEquals(initialGold, session.getInGameGold());
        assertEquals(0, session.getKidnapSuccessCount());
    }

    @Test
    @DisplayName("라운드가 변경되더라도 소환 비용이 변함없이 유지된다")
    void kidnap_keepsCostAcrossWaves() {
        session.kidnapAlien(dummySpec); // 1회 성공 -> 비용 60으로 증가
        assertEquals(60, session.getCurrentKidnapCost());

        session.nextWave(); // 라운드 전환

        assertEquals(60, session.getCurrentKidnapCost()); // 비용 유지 검증
    }

    @Test
    @DisplayName("새 GameSession 생성 시 소환 비용은 다시 50으로 초기화된다")
    void kidnap_resetCostOnNewSession() {
        session.kidnapAlien(dummySpec); // 1회 성공 -> 비용 60
        assertEquals(60, session.getCurrentKidnapCost());

        GameSession newSession = new GameSession(1L); // 새 세션 생성
        assertEquals(50, newSession.getCurrentKidnapCost()); // 초기화 확인
    }

    @Test
    @DisplayName("Kidnap 배치 시 첫 빈칸 순차 배치 규칙이 엄격히 준수된다")
    void kidnap_followsSequentialGridPlacement() {
        // (3,0) ~ (3,5) 에 이미 객체가 있다면, 다음 스폰은 (2,0) 에 배치되어야 한다.
        for (int j = 0; j < 6; j++) {
            session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 3, j);
        }

        BoardObject result = session.kidnapAlien(dummySpec);

        assertEquals(2, result.getGridX());
        assertEquals(0, result.getGridY());
    }

    @Test
    @DisplayName("동시 Kidnap 요청 시 임계 영역 동기화가 보장되어 경쟁 상태 없이 각각 정상 가격이 누적 차감된다")
    void kidnap_concurrentRequestsSafety() throws InterruptedException {
        // Given (2개 동시 스레드 실행 준비)
        java.util.concurrent.ExecutorService executor = java.util.concurrent.Executors.newFixedThreadPool(2);
        java.util.concurrent.CountDownLatch latch = new java.util.concurrent.CountDownLatch(1);

        executor.submit(() -> {
            try {
                latch.await();
                session.kidnapAlien(dummySpec); // 50 Gold 또는 60 Gold 차감
            } catch (Exception ignored) {}
        });

        executor.submit(() -> {
            try {
                latch.await();
                session.kidnapAlien(dummySpec); // 50 Gold 또는 60 Gold 차감
            } catch (Exception ignored) {}
        });

        // When
        latch.countDown(); // 동시 트리거
        executor.shutdown();
        executor.awaitTermination(5, java.util.concurrent.TimeUnit.SECONDS);

        // Then (경쟁 상태 없이 두 소환이 모두 성공하면 50 + 60 = 110 Gold 차감되어 골드는 390이 됨)
        assertEquals(390, session.getInGameGold());
        assertEquals(2, session.getKidnapSuccessCount());
        assertEquals(70, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("Alien을 빈칸으로 이동시키면 정상 성공하고 이전 칸은 null이 되며 좌표가 갱신된다")
    void moveBoardObject_alienToEmpty_success() {
        InGameAlien alien = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);

        BoardObject result = session.moveBoardObject(alien.getId(), 2, 2);

        assertNotNull(result);
        assertEquals(alien.getId(), result.getId());
        assertEquals(2, result.getGridX());
        assertEquals(2, result.getGridY());
        assertNull(session.getGrid()[1][1]);
        assertEquals(alien, session.getGrid()[2][2]);
    }

    @Test
    @DisplayName("Injector를 빈칸으로 이동시키면 정상 성공하고 이전 칸은 null이 되며 좌표가 갱신된다")
    void moveBoardObject_injectorToEmpty_success() {
        InGameInjector injector = session.spawnInjector(MutationType.BERSERK, 1, 1);

        BoardObject result = session.moveBoardObject(injector.getId(), 2, 2);

        assertNotNull(result);
        assertEquals(injector.getId(), result.getId());
        assertEquals(2, result.getGridX());
        assertEquals(2, result.getGridY());
        assertNull(session.getGrid()[1][1]);
        assertEquals(injector, session.getGrid()[2][2]);
    }

    @Test
    @DisplayName("Alien과 Alien을 Swap하면 서로 위치가 교환되고 내부 좌표가 변경된다")
    void moveBoardObject_alienToAlien_swapSuccess() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien target = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        session.moveBoardObject(source.getId(), 2, 2);

        // source 좌표 갱신 확인
        assertEquals(2, source.getGridX());
        assertEquals(2, source.getGridY());
        // target 좌표 갱신 확인
        assertEquals(1, target.getGridX());
        assertEquals(1, target.getGridY());

        // grid 참조 확인
        assertEquals(target, session.getGrid()[1][1]);
        assertEquals(source, session.getGrid()[2][2]);

        // 객체 참조 유지 검증
        assertSame(source, session.getBoardObject(source.getId()));
        assertSame(target, session.getBoardObject(target.getId()));
    }

    @Test
    @DisplayName("Alien과 Injector를 Swap하면 서로 위치가 교환되고 내부 좌표가 변경된다")
    void moveBoardObject_alienToInjector_swapSuccess() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameInjector target = session.spawnInjector(MutationType.BERSERK, 2, 2);

        session.moveBoardObject(source.getId(), 2, 2);

        assertEquals(2, source.getGridX());
        assertEquals(2, source.getGridY());
        assertEquals(1, target.getGridX());
        assertEquals(1, target.getGridY());

        assertEquals(target, session.getGrid()[1][1]);
        assertEquals(source, session.getGrid()[2][2]);

        // 객체 참조 유지 검증
        assertSame(source, session.getBoardObject(source.getId()));
        assertSame(target, session.getBoardObject(target.getId()));
    }

    @Test
    @DisplayName("Injector와 Injector를 Swap하면 서로 위치가 교환되고 내부 좌표가 변경된다")
    void moveBoardObject_injectorToInjector_swapSuccess() {
        InGameInjector source = session.spawnInjector(MutationType.BERSERK, 1, 1);
        InGameInjector target = session.spawnInjector(MutationType.GREEDY, 2, 2);

        session.moveBoardObject(source.getId(), 2, 2);

        assertEquals(2, source.getGridX());
        assertEquals(2, source.getGridY());
        assertEquals(1, target.getGridX());
        assertEquals(1, target.getGridY());

        assertEquals(target, session.getGrid()[1][1]);
        assertEquals(source, session.getGrid()[2][2]);

        // 객체 참조 유지 검증
        assertSame(source, session.getBoardObject(source.getId()));
        assertSame(target, session.getBoardObject(target.getId()));
    }

    @Test
    @DisplayName("동일 제자리 위치로의 이동은 no-op 성공한다")
    void moveBoardObject_samePosition_noopSuccess() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);

        BoardObject result = session.moveBoardObject(source.getId(), 1, 1);

        assertEquals(source, result);
        assertEquals(1, source.getGridX());
        assertEquals(1, source.getGridY());
        assertEquals(source, session.getGrid()[1][1]);
    }

    @Test
    @DisplayName("존재하지 않는 objectId로 이동 시도 시 IllegalArgumentException이 발생하고 상태가 유지된다")
    void moveBoardObject_rejectInvalidObjectId() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        int initialGold = session.getInGameGold();

        assertThrows(BusinessException.class, () -> session.moveBoardObject(9999L, 2, 2));

        // 상태 유지 확인
        assertEquals(1, source.getGridX());
        assertEquals(1, source.getGridY());
        assertEquals(source, session.getGrid()[1][1]);
        assertSame(source, session.getBoardObject(source.getId()));
        assertEquals(1, session.getBoardObjectCount());
        assertEquals(initialGold, session.getInGameGold());
        assertEquals(50, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("범위 밖 좌표로 이동 시도 시 IllegalArgumentException이 발생하고 상태가 유지된다")
    void moveBoardObject_rejectOutOfBoundCoordinates() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        int initialGold = session.getInGameGold();

        // x 범위 음수
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), -1, 2));
        // x 범위 4 이상
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 4, 2));
        // y 범위 음수
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 2, -1));
        // y 범위 6 이상
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 2, 6));

        // 상태 유지 확인
        assertEquals(1, source.getGridX());
        assertEquals(1, source.getGridY());
        assertEquals(source, session.getGrid()[1][1]);
        assertSame(source, session.getBoardObject(source.getId()));
        assertEquals(1, session.getBoardObjectCount());
        assertEquals(initialGold, session.getInGameGold());
        assertEquals(50, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("그리드와 맵의 정합성이 불일치할 때 IllegalStateException이 발생하고 상태가 유지된다")
    void moveBoardObject_rejectInconsistentState() {
        InGameAlien source = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        int initialGold = session.getInGameGold();

        // 1. grid와 source 불일치 유발 (강제로 grid 위치 null 처리)
        session.getGrid()[1][1] = null;
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 2, 2));
        session.getGrid()[1][1] = source; // 복구

        // 2. target 내부 좌표 불일치 유발
        InGameAlien target = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);
        target.setGridX(0); // 강제로 불일치 유발
        assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 2, 2));
        target.setGridX(2); // 복구

        // 3. target이 boardObjects에 없는 상태 (강제로 target을 boardObjects 맵에서만 삭제하여 불일치 유발)
        session.getBoardObjects().remove(target.getId());
        BusinessException ex = assertThrows(BusinessException.class, () -> session.moveBoardObject(source.getId(), 2, 2));
        assertEquals("보드 상태 불일치: 대상 객체가 boardObjects와 일치하지 않습니다.", ex.getMessage());
        session.getBoardObjects().put(target.getId(), target); // 복구

        // 최종 상태 유지 검증
        assertEquals(1, source.getGridX());
        assertEquals(1, source.getGridY());
        assertEquals(source, session.getGrid()[1][1]);
        assertSame(source, session.getBoardObject(source.getId()));
        assertSame(target, session.getBoardObject(target.getId()));
        assertEquals(2, session.getBoardObjectCount());
        assertEquals(initialGold, session.getInGameGold());
        assertEquals(50, session.getCurrentKidnapCost());
    }

    @Test
    @DisplayName("동일 세션에 대한 동시 이동 및 Swap 요청 시 정합성이 온전히 보호된다")
    void moveBoardObject_concurrentRequests_keepsConsistency() throws InterruptedException {
        // Given
        InGameAlien alien1 = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 1, 1);
        InGameAlien alien2 = session.spawnAlien(dummySpec, MutationType.NONE, MutationType.NONE, 0, 2, 2);

        java.util.concurrent.ExecutorService executor = java.util.concurrent.Executors.newFixedThreadPool(2);
        java.util.concurrent.CountDownLatch latch = new java.util.concurrent.CountDownLatch(1);

        executor.submit(() -> {
            try {
                latch.await();
                session.moveBoardObject(alien1.getId(), 2, 2); // Swap 시도
            } catch (Exception ignored) {}
        });

        executor.submit(() -> {
            try {
                latch.await();
                session.moveBoardObject(alien2.getId(), 1, 1); // Swap 시도
            } catch (Exception ignored) {}
        });

        // When
        latch.countDown();
        executor.shutdown();
        executor.awaitTermination(5, java.util.concurrent.TimeUnit.SECONDS);

        // Then (경쟁 상태 속에서도 grid와 객체 좌표가 어긋나지 않고 동기화 락에 의해 최종 1회 혹은 Swap 결과가 정합되게 배치되어 있어야 함)
        BoardObject o1 = session.getGrid()[1][1];
        BoardObject o2 = session.getGrid()[2][2];

        assertNotNull(o1);
        assertNotNull(o2);
        assertEquals(o1.getGridX(), 1);
        assertEquals(o1.getGridY(), 1);
        assertEquals(o2.getGridX(), 2);
        assertEquals(o2.getGridY(), 2);
    }
}
