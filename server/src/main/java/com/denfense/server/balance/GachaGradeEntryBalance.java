package com.denfense.server.balance;

import java.util.List;

public record GachaGradeEntryBalance(
        String grade,
        int weight,
        List<Long> alienIds
) {
    public GachaGradeEntryBalance {
        if (alienIds != null) {
            alienIds = List.copyOf(alienIds);
        }
    }
}