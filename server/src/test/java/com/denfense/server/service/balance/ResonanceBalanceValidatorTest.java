package com.denfense.server.service.balance;

import com.denfense.server.balance.ResonanceBalance;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertThrows;

class ResonanceBalanceValidatorTest {

    private final BalanceDataValidator validator = new BalanceDataValidator();

    @Test
    void acceptsCanonicalNormalAndMythicTracks() {
        assertDoesNotThrow(() -> validator.validateResonanceBalance(validRows()));
    }

    @Test
    void rejectsMissingDuplicateAndUnknownTrackRows() {
        List<ResonanceBalance> missing = validRows();
        missing.remove(missing.size() - 1);
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(missing));

        List<ResonanceBalance> duplicate = validRows();
        duplicate.set(9, duplicate.get(8));
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(duplicate));

        List<ResonanceBalance> unknown = validRows();
        ResonanceBalance original = unknown.get(0);
        unknown.set(0, new ResonanceBalance("ALL", original.level(), original.requiredGold(),
                original.attackMultiplier(), original.attackSpeedMultiplier(), original.rangeMultiplier(), true));
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(unknown));
    }

    @Test
    void rejectsNonIncreasingCostsOrMultipliersAndRangeGrowth() {
        List<ResonanceBalance> badCost = validRows();
        ResonanceBalance normalTwo = badCost.get(1);
        badCost.set(1, replace(normalTwo, 400, normalTwo.attackMultiplier(), normalTwo.attackSpeedMultiplier(), BigDecimal.ONE));
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(badCost));

        List<ResonanceBalance> badAttack = validRows();
        badAttack.set(1, replace(normalTwo, normalTwo.requiredGold(), new BigDecimal("1.05"),
                normalTwo.attackSpeedMultiplier(), BigDecimal.ONE));
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(badAttack));

        List<ResonanceBalance> badRange = validRows();
        badRange.set(0, replace(badRange.get(0), 400, new BigDecimal("1.05"),
                new BigDecimal("1.01"), new BigDecimal("1.01")));
        assertThrows(IllegalStateException.class, () -> validator.validateResonanceBalance(badRange));
    }

    private static ResonanceBalance replace(ResonanceBalance source, int gold, BigDecimal attack,
                                            BigDecimal speed, BigDecimal range) {
        return new ResonanceBalance(source.track(), source.level(), gold, attack, speed, range, source.enabled());
    }

    private static List<ResonanceBalance> validRows() {
        int[] normalCosts = {400, 800, 1400, 2200, 3200};
        int[] mythicCosts = {800, 1600, 2800, 4400, 6500};
        String[] normalAttack = {"1.05", "1.10", "1.15", "1.20", "1.25"};
        String[] mythicAttack = {"1.08", "1.16", "1.24", "1.32", "1.40"};
        String[] speed = {"1.01", "1.02", "1.03", "1.04", "1.05"};
        var rows = new ArrayList<ResonanceBalance>();
        for (int index = 0; index < 5; index++) {
            rows.add(row("NORMAL", index + 1, normalCosts[index], normalAttack[index], speed[index]));
        }
        for (int index = 0; index < 5; index++) {
            rows.add(row("MYTHIC", index + 1, mythicCosts[index], mythicAttack[index], speed[index]));
        }
        return rows;
    }

    private static ResonanceBalance row(String track, int level, int cost, String attack, String speed) {
        return new ResonanceBalance(track, level, cost, new BigDecimal(attack), new BigDecimal(speed), BigDecimal.ONE, true);
    }
}
