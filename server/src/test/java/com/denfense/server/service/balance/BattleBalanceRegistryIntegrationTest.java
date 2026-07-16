package com.denfense.server.service.balance;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest(properties = {
        "spring.datasource.url=jdbc:h2:mem:battle-balance-registry;MODE=MySQL;DB_CLOSE_DELAY=-1",
        "spring.jpa.hibernate.ddl-auto=create-drop",
        "balance.alien-spec.seed-enabled=false",
        "balance.alien-spec.consistency-mode=OFF"
})
class BattleBalanceRegistryIntegrationTest {

    @Autowired MonsterBalanceRegistry monsters;
    @Autowired WaveBalanceRegistry waves;
    @Autowired BattleRuleBalanceRegistry rules;

    @Test
    void exposesImmutableValidatedBattleBalance() {
        assertThat(monsters.getById("NORMAL_MONSTER").baseHp()).isEqualByComparingTo("30.00");
        assertThat(monsters.getAll()).hasSize(3);
        assertThat(waves.getWave("COOP_STANDARD", 10).isBossWave()).isTrue();
        assertThat(waves.getSpawns("WAVE_10_BOSS")).singleElement().satisfies(spawn -> {
            assertThat(spawn.lanePolicy()).isEqualTo("BOSS_SHARED");
            assertThat(spawn.spawnCountPerField()).isEqualTo(1);
            assertThat(spawn.monsterId()).isEqualTo("WAVE_BOSS");
        });
        assertThat(waves.getSpawns("WAVE_05")).hasSize(2)
                .allSatisfy(spawn -> assertThat(spawn.lanePolicy()).isEqualTo("EACH_FIELD"))
                .extracting(spawn -> spawn.order()).containsExactly(1, 2);

        assertThat(rules.getFieldLimit("COOP_STANDARD").maxAliveMonsterCountPerField()).isEqualTo(100);
        assertThat(rules.getSummonBalance("COOP_STANDARD", "KIDNAP").maxUses()).isEqualTo(-1);
        assertThat(rules.getMergeRule("LEGEND").resultType()).isEqualTo("MYTHIC_CHOICE");
        assertThat(List.of("NORMAL", "EPIC", "UNIQUE", "LEGEND", "MYTHIC"))
                .allSatisfy(grade -> assertThat(rules.getMergeRule(grade).sameSpeciesRequired()).isTrue());
        assertThat(rules.getMythicChoiceBalance("COOP_STANDARD").candidateCount()).isEqualTo(3);
        assertThat(rules.getEnabledMythicAlienIds()).containsExactlyElementsOf(
                java.util.stream.LongStream.rangeClosed(29, 48).boxed().toList());

        List<Long> mythics = rules.getEnabledMythicAlienIds();
        assertThatThrownBy(() -> mythics.add(49L)).isInstanceOf(UnsupportedOperationException.class);
        assertThatThrownBy(() -> waves.getSpawns("WAVE_05").add(waves.getSpawns("WAVE_05").get(0)))
                .isInstanceOf(UnsupportedOperationException.class);
    }

    @Test
    void unknownKeysFailFast() {
        assertThatThrownBy(() -> monsters.getById("UNKNOWN")).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> waves.getWave("COOP_STANDARD", 999)).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> rules.getMergeRule("INVALID")).isInstanceOf(IllegalArgumentException.class);
    }
}
