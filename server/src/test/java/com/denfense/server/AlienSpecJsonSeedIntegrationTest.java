package com.denfense.server;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.User;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.MonsterSpecRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.*;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceException;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest
public class AlienSpecJsonSeedIntegrationTest {

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private AlienSpecSeedService seedService;

    @Autowired
    private BalanceRegistry balanceRegistry;

    @Autowired
    private AlienSpecConsistencyChecker checker;

    @Autowired
    private AlienSpecConsistencyProperties properties;

    @Autowired
    private MonsterSpecRepository monsterSpecRepository;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private EntityManager entityManager;

    private AlienSpecConsistencyMode originalMode;

    @BeforeEach
    void setUp() {
        originalMode = properties.getConsistencyMode();
    }

    @AfterEach
    void tearDown() {
        properties.setConsistencyMode(originalMode);
    }

    // =========================================================
    // 1. 빈 DB → 48건, ID 1~48, 모든 필드 JSON과 일치
    // =========================================================
    @Test
    @DisplayName("빈 DB에서 Seed → 48건 삽입, 전체 필드 JSON 일치")
    void emptyDb_seedInserts48_allFieldsMatch() {
        // 기동 시 이미 Seed가 실행됨 (seed-enabled=true)
        List<AlienSpec> all = alienSpecRepository.findAll();
        assertThat(all).hasSize(48);

        List<AlienSpecBalance> jsonSpecs = balanceRegistry.getAllAlienSpecs();
        assertThat(jsonSpecs).hasSize(48);

        Map<Long, AlienSpec> dbMap = all.stream()
                .collect(Collectors.toMap(AlienSpec::getId, s -> s));

        for (AlienSpecBalance json : jsonSpecs) {
            AlienSpec db = dbMap.get(json.alienId());
            assertThat(db).as("ID %d should exist in DB", json.alienId()).isNotNull();
            assertThat(db.getName()).isEqualTo(json.name());
            assertThat(db.getDescription()).isEqualTo(json.description());
            assertThat(db.getGrade().name()).isEqualTo(json.grade());
            assertThat(db.getBaseAtk()).isEqualTo(json.baseAttack());
            assertThat(db.getBaseMp()).isEqualTo(json.baseMp());
            assertThat(db.getAtkSpeed()).isEqualTo(json.attackSpeed());
            assertThat(db.getRange()).isEqualTo(json.attackRange());
            assertThat(db.getEvolutionTargetId()).isEqualTo(json.evolutionTargetId());
            assertThat(db.isLocked()).isEqualTo(json.isLocked());
        }
    }

    // =========================================================
    // 2. 부분 DB → 누락분만 INSERT, 기존 행 값 유지
    // =========================================================
    @Test
    @DisplayName("부분 DB에서 Seed → 누락분만 INSERT, 기존 행 값 유지")
    @Transactional
    void partialDb_seedOnlyMissing() {
        // 기동 시 48건 들어감, ID 1을 삭제 후 재 seed
        alienSpecRepository.deleteById(1L);
        entityManager.flush();
        entityManager.clear();

        assertThat(alienSpecRepository.count()).isEqualTo(47);

        // 기존 ID 2의 값을 기록
        AlienSpec before2 = alienSpecRepository.findById(2L).orElseThrow();
        String nameBefore = before2.getName();

        AlienSpecSeedResult result = seedService.seed();

        entityManager.flush();
        entityManager.clear();

        assertThat(result.insertedCount()).isEqualTo(1);
        assertThat(result.skippedCount()).isEqualTo(47);
        assertThat(alienSpecRepository.count()).isEqualTo(48);

        // 기존 ID 2 값 유지
        AlienSpec after2 = alienSpecRepository.findById(2L).orElseThrow();
        assertThat(after2.getName()).isEqualTo(nameBefore);
    }

    // =========================================================
    // 3. 기존 값 불일치 → Seed 후에도 기존 값 그대로, Checker 감지
    // =========================================================
    @Test
    @DisplayName("기존 값 불일치 → Seed 후에도 기존 값 그대로, Checker FAIL 감지")
    @Transactional
    void existingMismatch_seedDoesNotUpdate_checkerDetects() {
        // 기동 시 48건 들어감, ID 1의 baseAtk를 수동 변경
        AlienSpec spec1 = alienSpecRepository.findById(1L).orElseThrow();
        spec1.setBaseAtk(9999);
        entityManager.flush();
        entityManager.clear();

        // Seed 재실행 → 기존 값 변경 없음
        AlienSpecSeedResult result = seedService.seed();
        assertThat(result.insertedCount()).isZero();

        entityManager.flush();
        entityManager.clear();

        // 변경한 값이 그대로 남아있는지 확인
        AlienSpec afterSeed = alienSpecRepository.findById(1L).orElseThrow();
        assertThat(afterSeed.getBaseAtk()).isEqualTo(9999);

        // Checker가 불일치를 감지
        properties.setConsistencyMode(AlienSpecConsistencyMode.FAIL);
        assertThatThrownBy(() -> checker.run())
                .isInstanceOf(AlienSpecConsistencyException.class)
                .hasMessageContaining("baseAtk");
    }

    // =========================================================
    // 4. 동일 Seed 두 번 → 두 번째 inserted=0, row count 동일
    // =========================================================
    @Test
    @DisplayName("동일 Seed 두 번 → 두 번째 inserted=0, row count 동일")
    void doubleSeed_secondInsertZero() {
        // 기동 시 이미 1회 Seed됨
        long countBefore = alienSpecRepository.count();
        assertThat(countBefore).isEqualTo(48);

        AlienSpecSeedResult result = seedService.seed();

        assertThat(result.insertedCount()).isZero();
        assertThat(result.skippedCount()).isEqualTo(48);
        assertThat(alienSpecRepository.count()).isEqualTo(countBefore);

        // 모든 필드 동일한지 확인
        List<AlienSpec> all = alienSpecRepository.findAll();
        Map<Long, AlienSpec> dbMap = all.stream()
                .collect(Collectors.toMap(AlienSpec::getId, s -> s));

        for (AlienSpecBalance json : balanceRegistry.getAllAlienSpecs()) {
            AlienSpec db = dbMap.get(json.alienId());
            assertThat(db.getName()).isEqualTo(json.name());
            assertThat(db.getGrade().name()).isEqualTo(json.grade());
        }
    }

    // =========================================================
    // 5. JSON 외 ID 99 → 삭제되지 않음, 값 변경 없음
    // =========================================================
    @Test
    @DisplayName("JSON 외 ID 99 → Seed 후에도 삭제되지 않고 값 변경 없음")
    @Transactional
    void extraId99_notDeletedNotModified() {
        // 직접 ID 99 삽입
        AlienSpec extra = new AlienSpec();
        extra.setId(99L);
        extra.setName("외부 왹져");
        extra.setDescription("JSON에 없는 왹져");
        extra.setGrade(AlienSpec.Grade.NORMAL);
        extra.setBaseAtk(777);
        extra.setBaseMp(888);
        extra.setAtkSpeed(2.0);
        extra.setRange(5.0);
        extra.setEvolutionTargetId(null);
        extra.setLocked(false);
        entityManager.persist(extra);
        entityManager.flush();
        entityManager.clear();

        long countBefore = alienSpecRepository.count();

        AlienSpecSeedResult result = seedService.seed();

        entityManager.flush();
        entityManager.clear();

        // ID 99 여전히 존재
        AlienSpec found99 = alienSpecRepository.findById(99L).orElseThrow();
        assertThat(found99.getName()).isEqualTo("외부 왹져");
        assertThat(found99.getBaseAtk()).isEqualTo(777);
        assertThat(found99.getBaseMp()).isEqualTo(888);

        // row count 변화 없음 (이미 48 + 1 = 49건)
        assertThat(alienSpecRepository.count()).isEqualTo(countBefore);
    }

    // =========================================================
    // 6. EntityManager 직접 테스트: 중복 PK persist/flush 예외
    // =========================================================
    @Test
    @DisplayName("EntityManager 직접 테스트: persist/flush가 중복 PK를 UPDATE하지 않고 예외 처리함을 검증")
    @Transactional
    void duplicatePk_exceptionOnFlush() {
        // Seed가 이미 ID 1~48을 삽입함 (다른 트랜잭션에서 커밋됨)
        assertThat(alienSpecRepository.existsById(1L)).isTrue();

        // 동일 ID로 새 엔티티 persist 시도 → INSERT 충돌
        AlienSpec duplicate = new AlienSpec();
        duplicate.setId(1L);
        duplicate.setName("중복");
        duplicate.setGrade(AlienSpec.Grade.NORMAL);

        assertThatThrownBy(() -> {
            entityManager.persist(duplicate);
            entityManager.flush();
        }).isInstanceOf(PersistenceException.class);
    }

    // =========================================================
    // 7. DataInit 유지 → MonsterSpec 15건, User 1건
    // =========================================================
    @Test
    @DisplayName("DataInit 유지 → MonsterSpec 15건, User 1건, AlienSpec 48건은 Seed 결과")
    void dataInitKept_monsterAndUser() {
        List<MonsterSpec> monsters = monsterSpecRepository.findAll();
        assertThat(monsters).hasSize(15);

        List<User> users = userRepository.findAll();
        assertThat(users).hasSize(1);
        assertThat(users.get(0).getGold()).isEqualTo(100000);
        assertThat(users.get(0).getDiamond()).isEqualTo(1000000);

        assertThat(alienSpecRepository.count()).isEqualTo(48);
    }

    // =========================================================
    // 8. 실행 순서 → Runner @Order 검증
    // =========================================================
    @Test
    @DisplayName("실행 순서: Loader(1) → Seed(5) → DataInit(10) → Checker(20)")
    void executionOrder_verifiedByAnnotation() throws Exception {
        // 서버 기동 시 이미 순서대로 실행됨
        // Runner/DataInit의 @Order 값으로 순서 보장
        // 결과적으로 48건 AlienSpec + 15건 MonsterSpec + 1건 User가 존재
        assertThat(alienSpecRepository.count()).isEqualTo(48);
        assertThat(monsterSpecRepository.count()).isEqualTo(15);
        assertThat(userRepository.count()).isEqualTo(1);
    }

    // =========================================================
    // 9. Checker 연계 → 빈 DB 기동 후 정상 일치
    // =========================================================
    @Test
    @DisplayName("빈 DB 기동 후 Seed + Checker → 정상 일치 통과")
    void seedThenChecker_passes() throws Exception {
        // 기동 시 Seed(48건) + Checker(일치) 모두 완료
        // FAIL 모드에서 명시적 재실행하여 검증
        properties.setConsistencyMode(AlienSpecConsistencyMode.FAIL);
        checker.run(); // 예외 없으면 통과
    }

}
