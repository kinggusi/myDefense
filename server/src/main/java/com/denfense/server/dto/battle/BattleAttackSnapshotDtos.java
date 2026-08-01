package com.denfense.server.dto.battle;

import java.math.BigDecimal;
import java.util.List;

public final class BattleAttackSnapshotDtos {

    private BattleAttackSnapshotDtos() {
    }

    public record Response(
            String playerId,
            String balanceVersion,
            String contentHash,
            List<AlienAttack> aliens
    ) {
    }

    public record AlienAttack(
            long alienId,
            int level,
            BigDecimal damage,
            BigDecimal attackRate,
            BigDecimal range
    ) {
    }
}
