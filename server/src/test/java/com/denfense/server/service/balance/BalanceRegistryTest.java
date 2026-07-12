package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

public class BalanceRegistryTest {

    private BalanceRegistry registry;
    private GameRewardBalance rewardBalance;
    private Map<Integer, AlienUpgradeCostBalance> costMap;
    private List<AlienSpecBalance> specs;

    @BeforeEach
    void setUp() {
        registry = new BalanceRegistry();
        rewardBalance = new GameRewardBalance(100, 10, 1000);
        costMap = new HashMap<>();
        costMap.put(1, new AlienUpgradeCostBalance(1, 10, 100, 0));
        
        specs = List.of(
            new AlienSpecBalance(2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false),
            new AlienSpecBalance(1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
    }

    @Test
    @DisplayName("getAlienSpec 정상 조회")
    void getAlienSpec() {
        registry.init(rewardBalance, 2, costMap, specs);
        AlienSpecBalance spec = registry.getAlienSpec(1);
        assertThat(spec.name()).isEqualTo("A");
    }

    @Test
    @DisplayName("없는 ID 조회 시 명시적 예외")
    void getAlienSpecNotFound() {
        registry.init(rewardBalance, 2, costMap, specs);
        assertThrows(IllegalArgumentException.class, () -> registry.getAlienSpec(99));
    }

    @Test
    @DisplayName("getAllAlienSpecs alienId 오름차순 및 반환 List 수정 불가")
    void getAllAlienSpecsSortedAndImmutable() {
        registry.init(rewardBalance, 2, costMap, specs);
        List<AlienSpecBalance> all = registry.getAllAlienSpecs();
        
        // 오름차순 검증
        assertThat(all.get(0).alienId()).isEqualTo(1);
        assertThat(all.get(1).alienId()).isEqualTo(2);

        // 수정 불가 검증 (UnsupportedOperationException)
        assertThrows(UnsupportedOperationException.class, () -> all.add(new AlienSpecBalance(3, "C", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)));
    }

    @Test
    @DisplayName("AlienSpecBalance가 record 또는 불변 객체인지 확인")
    void checkRecord() {
        assertThat(AlienSpecBalance.class.isRecord()).isTrue();
    }

    @Test
    @DisplayName("Registry 중복 초기화 거절")
    void duplicateInit() {
        registry.init(rewardBalance, 2, costMap, specs);
        assertThrows(IllegalStateException.class, () -> registry.init(rewardBalance, 2, costMap, specs));
    }
}
