package com.denfense.server;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.dao.InvalidDataAccessApiUsageException;
import org.springframework.orm.jpa.JpaSystemException;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest
@Transactional
public class AlienSpecFixedIdIntegrationTest {

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private UserAlienRepository userAlienRepository;

    @Autowired
    private BalanceRegistry balanceRegistry;

    @BeforeEach
    void setUp() {
        userAlienRepository.deleteAll();
        alienSpecRepository.deleteAll();
        userRepository.deleteAll();
    }

    @Test
    @DisplayName("수동 ID 저장 및 findById 정상 동작")
    void manualIdSaveAndFind() {
        AlienSpec spec = new AlienSpec();
        spec.setId(10L);
        spec.setName("테스트 10");
        spec.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(spec);

        AlienSpec found = alienSpecRepository.findById(10L).orElse(null);
        assertThat(found).isNotNull();
        assertThat(found.getId()).isEqualTo(10L);
        assertThat(found.getName()).isEqualTo("테스트 10");
    }

    @Test
    @DisplayName("삽입 순서 무관하게 각 ID가 유지됨")
    void insertionOrderIndependent() {
        AlienSpec spec48 = new AlienSpec();
        spec48.setId(48L);
        spec48.setName("마지막 왹져");
        spec48.setGrade(AlienSpec.Grade.MYTHIC);
        alienSpecRepository.save(spec48);

        AlienSpec spec1 = new AlienSpec();
        spec1.setId(1L);
        spec1.setName("처음 왹져");
        spec1.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(spec1);

        assertThat(alienSpecRepository.findById(48L).get().getName()).isEqualTo("마지막 왹져");
        assertThat(alienSpecRepository.findById(1L).get().getName()).isEqualTo("처음 왹져");
    }

    @Test
    @DisplayName("DataInit 결과 48건 및 ID 정확히 1~48 유지 검증")
    void dataInitResults() {
        for (AlienSpecBalance b : balanceRegistry.getAllAlienSpecs()) {
            AlienSpec s = new AlienSpec();
            s.setId((long)b.alienId());
            s.setName(b.name());
            s.setGrade(AlienSpec.Grade.valueOf(b.grade()));
            alienSpecRepository.save(s);
        }

        List<AlienSpec> all = alienSpecRepository.findAll();
        assertThat(all).hasSize(48);

        Set<Long> dbIds = all.stream().map(AlienSpec::getId).collect(Collectors.toSet());
        Set<Long> jsonIds = balanceRegistry.getAllAlienSpecs().stream()
                .map(s -> (long) s.alienId())
                .collect(Collectors.toSet());

        assertThat(dbIds).containsExactlyInAnyOrderElementsOf(jsonIds);
        for (long i = 1; i <= 48; i++) {
            assertThat(dbIds).contains(i);
        }
    }

    @Test
    @DisplayName("JSON과 DB ID 일치 검증")
    void jsonAndDbIdMatch() {
        for (AlienSpecBalance b : balanceRegistry.getAllAlienSpecs()) {
            AlienSpec s = new AlienSpec();
            s.setId((long)b.alienId());
            s.setName(b.name());
            s.setGrade(AlienSpec.Grade.valueOf(b.grade()));
            alienSpecRepository.save(s);
        }

        Set<Long> dbIds = alienSpecRepository.findAll().stream().map(AlienSpec::getId).collect(Collectors.toSet());
        Set<Long> jsonIds = balanceRegistry.getAllAlienSpecs().stream()
                .map(s -> (long) s.alienId())
                .collect(Collectors.toSet());

        assertThat(dbIds).isEqualTo(jsonIds);
    }

    @Test
    @DisplayName("UserAlien FK 연결 유지 검증")
    void userAlienFk() {
        AlienSpec spec = new AlienSpec();
        spec.setId(1L);
        spec.setName("에일리언 1");
        spec.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(spec);

        User u = new User("tester123", "pw");
        userRepository.save(u);

        UserAlien ua = new UserAlien(u, spec);
        userAlienRepository.save(ua);

        UserAlien foundUa = userAlienRepository.findById(ua.getId()).get();
        assertThat(foundUa.getAlienSpec().getId()).isEqualTo(1L);
    }

    @Test
    @DisplayName("null ID 저장 시도 시 예외 타입 확인")
    void saveNullId() {
        AlienSpec spec = new AlienSpec();
        spec.setId(null);
        spec.setName("널 아이디");
        spec.setGrade(AlienSpec.Grade.NORMAL);

        assertThatThrownBy(() -> alienSpecRepository.save(spec))
                .isInstanceOf(JpaSystemException.class)
                .hasMessageContaining("must be manually assigned before calling 'persist()'");
    }

    @Test
    @DisplayName("동일 ID 재저장 시 실제 동작 (merge/update) 검증")
    void saveSameIdAgain() {
        AlienSpec spec = new AlienSpec();
        spec.setId(500L);
        spec.setName("원본 이름");
        spec.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(spec);

        long countBefore = alienSpecRepository.count();

        AlienSpec specUpdated = new AlienSpec();
        specUpdated.setId(500L);
        specUpdated.setName("수정된 이름");
        specUpdated.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(specUpdated);

        long countAfter = alienSpecRepository.count();
        AlienSpec found = alienSpecRepository.findById(500L).get();

        assertThat(countAfter).isEqualTo(countBefore);
        assertThat(found.getName()).isEqualTo("수정된 이름");
    }

    @Test
    @DisplayName("evolutionTargetId DB 저장값과 JSON 스펙 일치 검증")
    void evolutionTargetIdsMatchRegistry() {
        for (AlienSpecBalance b : balanceRegistry.getAllAlienSpecs()) {
            AlienSpec s = new AlienSpec();
            s.setId((long)b.alienId());
            s.setName(b.name());
            s.setGrade(AlienSpec.Grade.valueOf(b.grade()));
            s.setEvolutionTargetId(b.evolutionTargetId());
            alienSpecRepository.save(s);
        }

        List<AlienSpecBalance> registrySpecs = balanceRegistry.getAllAlienSpecs();
        for (AlienSpecBalance registrySpec : registrySpecs) {
            AlienSpec dbSpec = alienSpecRepository.findById((long) registrySpec.alienId()).get();
            assertThat(dbSpec.getEvolutionTargetId()).isEqualTo(registrySpec.evolutionTargetId());
        }

        // 대표 값 명시적 검증 (UNIQUE 8~14 -> LEGEND 1~7)
        assertThat(alienSpecRepository.findById(8L).get().getEvolutionTargetId()).isEqualTo(1L);
        assertThat(alienSpecRepository.findById(14L).get().getEvolutionTargetId()).isEqualTo(7L);

        // 대표 값 명시적 검증 (EPIC 15~21 -> UNIQUE 8~14)
        assertThat(alienSpecRepository.findById(15L).get().getEvolutionTargetId()).isEqualTo(8L);
        assertThat(alienSpecRepository.findById(21L).get().getEvolutionTargetId()).isEqualTo(14L);

        // 대표 값 명시적 검증 (NORMAL 22~28 -> EPIC 15~21)
        assertThat(alienSpecRepository.findById(22L).get().getEvolutionTargetId()).isEqualTo(15L);
        assertThat(alienSpecRepository.findById(28L).get().getEvolutionTargetId()).isEqualTo(21L);

        // 대표 값 명시적 검증 (LEGEND 1~7 -> null)
        assertThat(alienSpecRepository.findById(1L).get().getEvolutionTargetId()).isNull();

        // 대표 값 명시적 검증 (MYTHIC 29~32 -> null)
        assertThat(alienSpecRepository.findById(29L).get().getEvolutionTargetId()).isNull();
    }
}
