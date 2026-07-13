package com.denfense.server.service.gacha;

import com.denfense.server.balance.GachaGradeEntryBalance;
import com.denfense.server.balance.GachaPoolBalance;
import com.denfense.server.balance.ShopProductBalance;
import com.denfense.server.service.balance.BalanceRegistry;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.LinkedList;
import java.util.List;
import java.util.Queue;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class GachaDrawServiceTest {

    private GachaDrawService gachaDrawService;

    @Mock
    private BalanceRegistry balanceRegistry;

    private TestGachaRandomGenerator randomGenerator;

    @BeforeEach
    void setUp() {
        randomGenerator = new TestGachaRandomGenerator();
        gachaDrawService = new GachaDrawService(balanceRegistry, randomGenerator);
    }

    private static class TestGachaRandomGenerator implements GachaRandomGenerator {
        private final Queue<Integer> values = new LinkedList<>();
        private int callCount = 0;

        public void addValue(int value) {
            values.add(value);
        }

        public int getCallCount() {
            return callCount;
        }

        @Override
        public int nextInt(int bound) {
            callCount++;
            if (values.isEmpty()) {
                throw new IllegalStateException("테스트 난수 값이 부족합니다.");
            }
            int val = values.poll();
            if (val < 0 || val >= bound) {
                throw new IllegalStateException("주입된 난수가 bound 범위를 벗어납니다. bound: " + bound + ", value: " + val);
            }
            return val;
        }
    }

    private ShopProductBalance createProduct(String productId, boolean active, int drawCount, String poolId) {
        return new ShopProductBalance(
                productId,
                "Test Product",
                "DIAMOND",
                500,
                drawCount,
                poolId,
                active
        );
    }

    private GachaPoolBalance createPool(String poolId, boolean active) {
        List<GachaGradeEntryBalance> entries = List.of(
                new GachaGradeEntryBalance("NORMAL", 6000, List.of(22L, 23L, 24L, 25L, 26L, 27L, 28L)),
                new GachaGradeEntryBalance("EPIC", 2800, List.of(15L, 16L, 17L, 18L, 19L, 20L, 21L)),
                new GachaGradeEntryBalance("UNIQUE", 900, List.of(8L, 9L, 10L, 11L, 12L, 13L, 14L)),
                new GachaGradeEntryBalance("LEGEND", 250, List.of(1L, 2L, 3L, 4L, 5L, 6L, 7L)),
                new GachaGradeEntryBalance("MYTHIC", 50, List.of(29L, 30L, 31L, 32L))
        );
        return new GachaPoolBalance(poolId, "Test Pool", active, entries);
    }

    // A. 상품/Pool
    @Test
    @DisplayName("1. single 상품 조회 후 결과 1개")
    void drawSingle() {
        when(balanceRegistry.getShopProduct("SINGLE")).thenReturn(createProduct("SINGLE", true, 1, "POOL1"));
        when(balanceRegistry.getGachaPool("POOL1")).thenReturn(createPool("POOL1", true));

        randomGenerator.addValue(0); // NORMAL
        randomGenerator.addValue(0); // alienId: 22

        List<GachaDrawResult> results = gachaDrawService.draw("SINGLE");

        assertThat(results).hasSize(1);
        GachaDrawResult res = results.get(0);
        assertThat(res.productId()).isEqualTo("SINGLE");
        assertThat(res.gachaPoolId()).isEqualTo("POOL1");
        assertThat(res.grade()).isEqualTo("NORMAL");
        assertThat(res.alienId()).isEqualTo(22L);
        assertThat(randomGenerator.getCallCount()).isEqualTo(2);
    }

    @Test
    @DisplayName("2. ten 상품 조회 후 결과 10개")
    void drawTen() {
        when(balanceRegistry.getShopProduct("TEN")).thenReturn(createProduct("TEN", true, 10, "POOL1"));
        when(balanceRegistry.getGachaPool("POOL1")).thenReturn(createPool("POOL1", true));

        for (int i = 0; i < 10; i++) {
            randomGenerator.addValue(9999); // MYTHIC
            randomGenerator.addValue(3); // alienId: 32
        }

        List<GachaDrawResult> results = gachaDrawService.draw("TEN");

        assertThat(results).hasSize(10);
        assertThat(results).allSatisfy(res -> {
            assertThat(res.productId()).isEqualTo("TEN");
            assertThat(res.gachaPoolId()).isEqualTo("POOL1");
            assertThat(res.grade()).isEqualTo("MYTHIC");
            assertThat(res.alienId()).isEqualTo(32L);
        });
        assertThat(randomGenerator.getCallCount()).isEqualTo(20);
    }

    @Test
    @DisplayName("3. 존재하지 않는 productId 실패")
    void productNotFound() {
        when(balanceRegistry.getShopProduct("NOT_FOUND")).thenReturn(null);

        assertThatThrownBy(() -> gachaDrawService.draw("NOT_FOUND"))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("존재하지 않는 상품입니다");
    }

    @Test
    @DisplayName("4. 비활성 상품 실패")
    void inactiveProduct() {
        when(balanceRegistry.getShopProduct("INACTIVE")).thenReturn(createProduct("INACTIVE", false, 1, "POOL1"));

        assertThatThrownBy(() -> gachaDrawService.draw("INACTIVE"))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("비활성 상품입니다");
    }

    @Test
    @DisplayName("5. 비활성 Pool 실패")
    void inactivePool() {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "INACTIVE_POOL"));
        when(balanceRegistry.getGachaPool("INACTIVE_POOL")).thenReturn(createPool("INACTIVE_POOL", false));

        assertThatThrownBy(() -> gachaDrawService.draw("PROD"))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("비활성 GachaPool입니다");
    }

    // B. 등급 경계
    private void assertGrade(int randValue, String expectedGrade) {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "POOL"));
        when(balanceRegistry.getGachaPool("POOL")).thenReturn(createPool("POOL", true));

        randomGenerator.addValue(randValue);
        randomGenerator.addValue(0);

        List<GachaDrawResult> results = gachaDrawService.draw("PROD");
        assertThat(results.get(0).grade()).isEqualTo(expectedGrade);
    }

    @Test
    @DisplayName("6. 0 -> NORMAL")
    void grade_0_Normal() {
        assertGrade(0, "NORMAL");
    }

    @Test
    @DisplayName("7. 5999 -> NORMAL")
    void grade_5999_Normal() {
        assertGrade(5999, "NORMAL");
    }

    @Test
    @DisplayName("8. 6000 -> EPIC")
    void grade_6000_Epic() {
        assertGrade(6000, "EPIC");
    }

    @Test
    @DisplayName("9. 8799 -> EPIC")
    void grade_8799_Epic() {
        assertGrade(8799, "EPIC");
    }

    @Test
    @DisplayName("10. 8800 -> UNIQUE")
    void grade_8800_Unique() {
        assertGrade(8800, "UNIQUE");
    }

    @Test
    @DisplayName("11. 9699 -> UNIQUE")
    void grade_9699_Unique() {
        assertGrade(9699, "UNIQUE");
    }

    @Test
    @DisplayName("12. 9700 -> LEGEND")
    void grade_9700_Legend() {
        assertGrade(9700, "LEGEND");
    }

    @Test
    @DisplayName("13. 9949 -> LEGEND")
    void grade_9949_Legend() {
        assertGrade(9949, "LEGEND");
    }

    @Test
    @DisplayName("14. 9950 -> MYTHIC")
    void grade_9950_Mythic() {
        assertGrade(9950, "MYTHIC");
    }

    @Test
    @DisplayName("15. 9999 -> MYTHIC")
    void grade_9999_Mythic() {
        assertGrade(9999, "MYTHIC");
    }

    // C. Alien 선택
    private void assertAlienId(int gradeRand, int alienRand, Long expectedAlienId) {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "POOL"));
        when(balanceRegistry.getGachaPool("POOL")).thenReturn(createPool("POOL", true));

        randomGenerator.addValue(gradeRand);
        randomGenerator.addValue(alienRand);

        List<GachaDrawResult> results = gachaDrawService.draw("PROD");
        assertThat(results.get(0).alienId()).isEqualTo(expectedAlienId);
    }

    @Test
    @DisplayName("16. NORMAL 첫 번째 alienId")
    void normal_FirstAlien() {
        assertAlienId(0, 0, 22L);
    }

    @Test
    @DisplayName("17. NORMAL 마지막 alienId")
    void normal_LastAlien() {
        assertAlienId(0, 6, 28L);
    }

    @Test
    @DisplayName("18. MYTHIC index 0 -> 29")
    void mythic_index0() {
        assertAlienId(9999, 0, 29L);
    }

    @Test
    @DisplayName("19. MYTHIC index 3 -> 32")
    void mythic_index3() {
        assertAlienId(9999, 3, 32L);
    }

    @Test
    @DisplayName("20. 같은 등급 내부 균등 index 매핑")
    void uniformIndexMapping() {
        // Just demonstrating that different indices map correctly inside MYTHIC
        assertAlienId(9999, 1, 30L);
        assertAlienId(9999, 2, 31L);
    }

    // D. 난수 호출
    @Test
    @DisplayName("21. 등급 추첨 bound가 10000인지 / 22. Alien 추첨 bound가 해당 alienIds.size인지")
    void boundValidation() {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "POOL"));
        when(balanceRegistry.getGachaPool("POOL")).thenReturn(createPool("POOL", true));

        randomGenerator = new TestGachaRandomGenerator() {
            @Override
            public int nextInt(int bound) {
                if (getCallCount() == 0) {
                    assertThat(bound).isEqualTo(10000); // 21
                    super.addValue(9999);
                } else if (getCallCount() == 1) {
                    assertThat(bound).isEqualTo(4); // MYTHIC size // 22
                    super.addValue(3);
                }
                return super.nextInt(bound);
            }
        };
        gachaDrawService = new GachaDrawService(balanceRegistry, randomGenerator);

        gachaDrawService.draw("PROD");
        assertThat(randomGenerator.getCallCount()).isEqualTo(2);
    }

    // E. 결과
    @Test
    @DisplayName("24. productId 반환 / 25. poolId 반환 / 26. grade 반환 / 27. alienId 반환")
    void checkResultFields() {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "POOL"));
        when(balanceRegistry.getGachaPool("POOL")).thenReturn(createPool("POOL", true));

        randomGenerator.addValue(9999);
        randomGenerator.addValue(3);

        List<GachaDrawResult> results = gachaDrawService.draw("PROD");
        GachaDrawResult res = results.get(0);

        assertThat(res.productId()).isEqualTo("PROD");
        assertThat(res.gachaPoolId()).isEqualTo("POOL");
        assertThat(res.grade()).isEqualTo("MYTHIC");
        assertThat(res.alienId()).isEqualTo(32L);
    }

    @Test
    @DisplayName("28. 결과 List 변경 불가")
    void resultListImmutable() {
        when(balanceRegistry.getShopProduct("PROD")).thenReturn(createProduct("PROD", true, 1, "POOL"));
        when(balanceRegistry.getGachaPool("POOL")).thenReturn(createPool("POOL", true));

        randomGenerator.addValue(9999);
        randomGenerator.addValue(3);

        List<GachaDrawResult> results = gachaDrawService.draw("PROD");

        assertThatThrownBy(() -> results.add(new GachaDrawResult("P", "P", "G", 1L)))
                .isInstanceOf(UnsupportedOperationException.class);
    }
}
