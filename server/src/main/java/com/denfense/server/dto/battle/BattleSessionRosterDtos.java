package com.denfense.server.dto.battle;

import java.util.List;

public final class BattleSessionRosterDtos {
    private BattleSessionRosterDtos() {
    }

    public record Player(int playerSlot, String playerId) {
    }

    public record RegisterRequest(
            String battleSessionId,
            String mapId,
            String balanceVersion,
            String contentHash,
            List<Player> players) {
    }

    public record RegisterResponse(String battleSessionId, String status, int playerCount) {
    }

    public record RefundRequest(String reason) {
    }

    public record RefundResponse(String battleSessionId, String status, boolean alreadyProcessed) {
    }
}
