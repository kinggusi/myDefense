package com.denfense.server.balance;

import java.util.List;

public record GachaPoolBalance(
        String poolId,
        String name,
        boolean active,
        List<GachaGradeEntryBalance> gradeEntries
) {
    public GachaPoolBalance {
        if (gradeEntries != null) {
            gradeEntries = List.copyOf(gradeEntries);
        }
    }
}