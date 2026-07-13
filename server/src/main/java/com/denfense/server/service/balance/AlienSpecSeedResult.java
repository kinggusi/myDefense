package com.denfense.server.service.balance;

public record AlienSpecSeedResult(
        boolean enabled,
        int insertedCount,
        int skippedCount
) {
    public static AlienSpecSeedResult disabled() {
        return new AlienSpecSeedResult(false, 0, 0);
    }
}
