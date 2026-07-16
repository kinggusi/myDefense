package com.denfense.server;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.service.balance.AlienSpecConsistencyProperties;
import com.denfense.server.service.balance.AlienSpecSeedResult;
import com.denfense.server.service.balance.AlienSpecSeedService;
import com.denfense.server.service.balance.BalanceRegistry;
import jakarta.persistence.EntityManager;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Collections;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
public class AlienSpecSeedServiceTest {

    @Mock
    private AlienSpecConsistencyProperties properties;

    @Mock
    private AlienSpecRepository alienSpecRepository;

    @Mock
    private BalanceRegistry balanceRegistry;

    @Mock
    private EntityManager entityManager;

    @InjectMocks
    private AlienSpecSeedService seedService;

    @Test
    @DisplayName("disabled일 때 모든 의존성 무접근")
    void whenDisabled_thenNoInteractions() {
        when(properties.isSeedEnabled()).thenReturn(false);

        AlienSpecSeedResult result = seedService.seed();

        assertThat(result.enabled()).isFalse();
        assertThat(result.insertedCount()).isZero();
        assertThat(result.skippedCount()).isZero();

        verifyNoInteractions(alienSpecRepository, balanceRegistry, entityManager);
    }

    @Test
    @DisplayName("enabled + 전체 기존 → persist 0회, inserted=0, skipped=48")
    void whenAllExist_thenNoPersist() {
        when(properties.isSeedEnabled()).thenReturn(true);

        // 48 existing IDs
        List<Long> existingIds = new java.util.ArrayList<>();
        for (long i = 1; i <= 48; i++) existingIds.add(i);
        when(alienSpecRepository.findAllIds()).thenReturn(existingIds);

        // 48 balances from registry
        List<AlienSpecBalance> balances = new java.util.ArrayList<>();
        for (long i = 1; i <= 48; i++) {
            balances.add(new AlienSpecBalance(i, "N" + i, "D" + i, "NORMAL", 10, 5, 1.0, 2.0, null, false));
        }
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(balances);

        AlienSpecSeedResult result = seedService.seed();

        assertThat(result.enabled()).isTrue();
        assertThat(result.insertedCount()).isZero();
        assertThat(result.skippedCount()).isEqualTo(48);

        verify(entityManager, never()).persist(any());
        verify(entityManager, never()).flush();
    }

    @Test
    @DisplayName("enabled + 일부 누락 → 누락 ID만 persist, 기존 ID는 persist하지 않음")
    void whenPartialMissing_thenPersistOnlyMissing() {
        when(properties.isSeedEnabled()).thenReturn(true);

        // 기존 ID 1~32는 존재하고 신규 ID 33~48만 누락
        List<Long> existingIds = new java.util.ArrayList<>();
        for (long i = 1; i <= 32; i++) existingIds.add(i);
        when(alienSpecRepository.findAllIds()).thenReturn(existingIds);

        List<AlienSpecBalance> balances = new java.util.ArrayList<>();
        for (long i = 1; i <= 48; i++) {
            balances.add(new AlienSpecBalance(i, "N" + i, "D" + i, "NORMAL", 10, 5, 1.0, 2.0, null, false));
        }
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(balances);

        AlienSpecSeedResult result = seedService.seed();

        assertThat(result.enabled()).isTrue();
        assertThat(result.insertedCount()).isEqualTo(16);
        assertThat(result.skippedCount()).isEqualTo(32);

        ArgumentCaptor<AlienSpec> captor = ArgumentCaptor.forClass(AlienSpec.class);
        verify(entityManager, times(16)).persist(captor.capture());

        List<AlienSpec> persisted = captor.getAllValues();
        assertThat(persisted).extracting(AlienSpec::getId)
                .containsExactly(33L, 34L, 35L, 36L, 37L, 38L, 39L, 40L,
                        41L, 42L, 43L, 44L, 45L, 46L, 47L, 48L);

        verify(entityManager, times(1)).flush();
    }

    @Test
    @DisplayName("빈 DB → 48건 전부 persist")
    void whenEmptyDb_thenPersistAll() {
        when(properties.isSeedEnabled()).thenReturn(true);
        when(alienSpecRepository.findAllIds()).thenReturn(Collections.emptyList());

        List<AlienSpecBalance> balances = new java.util.ArrayList<>();
        for (long i = 1; i <= 48; i++) {
            balances.add(new AlienSpecBalance(i, "N" + i, "D" + i, "NORMAL", 10, 5, 1.0, 2.0, null, false));
        }
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(balances);

        AlienSpecSeedResult result = seedService.seed();

        assertThat(result.enabled()).isTrue();
        assertThat(result.insertedCount()).isEqualTo(48);
        assertThat(result.skippedCount()).isZero();

        verify(entityManager, times(48)).persist(any(AlienSpec.class));
        verify(entityManager, times(1)).flush();
    }

    @Test
    @DisplayName("JSON → Entity 필드 매핑 정확성")
    void mapToEntity_allFieldsMapped() {
        when(properties.isSeedEnabled()).thenReturn(true);
        when(alienSpecRepository.findAllIds()).thenReturn(Collections.emptyList());

        AlienSpecBalance balance = new AlienSpecBalance(
                5L, "테스트 왹져", "강력한 왹져", "EPIC",
                42, 100, 1.5, 3.5, 10L, true
        );
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(balance));

        seedService.seed();

        ArgumentCaptor<AlienSpec> captor = ArgumentCaptor.forClass(AlienSpec.class);
        verify(entityManager).persist(captor.capture());

        AlienSpec entity = captor.getValue();
        assertThat(entity.getId()).isEqualTo(5L);
        assertThat(entity.getName()).isEqualTo("테스트 왹져");
        assertThat(entity.getDescription()).isEqualTo("강력한 왹져");
        assertThat(entity.getGrade()).isEqualTo(AlienSpec.Grade.EPIC);
        assertThat(entity.getBaseAtk()).isEqualTo(42);
        assertThat(entity.getBaseMp()).isEqualTo(100);
        assertThat(entity.getAtkSpeed()).isEqualTo(1.5);
        assertThat(entity.getRange()).isEqualTo(3.5);
        assertThat(entity.getEvolutionTargetId()).isEqualTo(10L);
        assertThat(entity.isLocked()).isTrue();
    }

    @Test
    @DisplayName("null evolutionTargetId 유지")
    void mapToEntity_nullEvolutionTargetId() {
        when(properties.isSeedEnabled()).thenReturn(true);
        when(alienSpecRepository.findAllIds()).thenReturn(Collections.emptyList());

        AlienSpecBalance balance = new AlienSpecBalance(
                1L, "N1", "D1", "LEGEND",
                50, 500, 1.0, 3.5, null, false
        );
        when(balanceRegistry.getAllAlienSpecs()).thenReturn(List.of(balance));

        seedService.seed();

        ArgumentCaptor<AlienSpec> captor = ArgumentCaptor.forClass(AlienSpec.class);
        verify(entityManager).persist(captor.capture());

        assertThat(captor.getValue().getEvolutionTargetId()).isNull();
    }
}
