package com.denfense.server.dto.battle;

import java.util.List;

/** Shared transport shape for the Legendary -> Mythic choice window. */
public final class LegendaryChoiceStateDtos {
    private LegendaryChoiceStateDtos() {}

    public record State(
            String choiceId,
            String battleSessionId,
            long materialAlienIdA,
            long materialAlienIdB,
            List<Long> candidateAlienIds,
            int rerollCount,
            int freeRerollsRemaining,
            int paidRerollsRemaining,
            int selectionTimeoutSeconds,
            int remainingSeconds,
            String phase,
            Long selectedAlienId,
            String autoSelectPolicy,
            boolean battleContinuesDuringSelection) {}
}
