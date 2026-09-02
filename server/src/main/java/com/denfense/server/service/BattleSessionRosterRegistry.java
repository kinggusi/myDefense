package com.denfense.server.service;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.domain.SessionSource;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.LongSupplier;

/**
 * Stores rosters supplied by a trusted in-process matchmaking/session authority.
 * Public client APIs must never populate this registry from request parameters.
 * Until that authority is wired, Settlement fails closed and no permanent reward
 * can be granted.
 */
@Component
public class BattleSessionRosterRegistry {
    static final long ROSTER_TTL_MILLIS = 4L * 60L * 60L * 1000L;
    private final Map<String, MutableRoster> rosters = new ConcurrentHashMap<>();
    private final LongSupplier nowMillis;

    public BattleSessionRosterRegistry() {
        this(System::currentTimeMillis);
    }

    BattleSessionRosterRegistry(LongSupplier nowMillis) {
        this.nowMillis = nowMillis;
    }

    public void register(String battleSessionId, int playerSlot, String playerId,
                         String mapId, String balanceVersion, String contentHash) {
        register(battleSessionId, playerSlot, playerId, mapId, balanceVersion, contentHash,
                SessionSource.LOCAL_DEVELOPMENT);
    }

    public void register(String battleSessionId, int playerSlot, String playerId,
                         String mapId, String balanceVersion, String contentHash,
                         SessionSource sessionSource) {
        purgeExpired();
        if (blank(battleSessionId) || blank(playerId) || blank(mapId)
                || blank(balanceVersion) || blank(contentHash)
                || playerSlot < 1 || playerSlot > 2 || sessionSource == null) {
            throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        }
        rosters.compute(battleSessionId.trim(), (key, current) -> {
            MutableRoster roster = current == null
                    ? new MutableRoster(mapId.trim(), balanceVersion.trim(), contentHash.trim(),
                    sessionSource, nowMillis.getAsLong())
                    : current;
            roster.register(playerSlot, playerId.trim(), mapId.trim(), balanceVersion.trim(),
                    contentHash.trim(), sessionSource);
            return roster;
        });
    }

    /**
     * Atomically publishes a complete two-player roster. Unlike two calls to
     * {@link #register}, a competing request can never leave mixed slots in
     * this in-memory registry.
     */
    public void registerComplete(String battleSessionId, String mapId, String balanceVersion,
                                 String contentHash, List<Player> requestedPlayers) {
        registerComplete(battleSessionId, mapId, balanceVersion, contentHash, requestedPlayers,
                SessionSource.LOCAL_DEVELOPMENT);
    }

    public void registerComplete(String battleSessionId, String mapId, String balanceVersion,
                                 String contentHash, List<Player> requestedPlayers,
                                 SessionSource sessionSource) {
        purgeExpired();
        if (blank(battleSessionId) || blank(mapId) || blank(balanceVersion) || blank(contentHash)
                || requestedPlayers == null || requestedPlayers.size() != 2 || sessionSource == null) {
            throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        }
        String sessionKey = battleSessionId.trim();
        MutableRoster candidate = new MutableRoster(
                mapId.trim(), balanceVersion.trim(), contentHash.trim(), sessionSource, nowMillis.getAsLong());
        for (Player player : requestedPlayers) {
            if (player == null) throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            candidate.register(player.playerSlot(), player.playerId(), mapId.trim(),
                    balanceVersion.trim(), contentHash.trim(), sessionSource);
        }
        Roster requested = candidate.snapshot();
        rosters.compute(sessionKey, (key, current) -> {
            if (current == null) return candidate;
            Roster existing = current.snapshot();
            if (!existing.equals(requested)) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
            return current;
        });
    }

    public Roster requireComplete(String battleSessionId) {
        purgeExpired();
        MutableRoster roster = rosters.get(battleSessionId);
        if (roster == null) throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        return roster.snapshot();
    }

    void clearForTest() {
        rosters.clear();
    }

    public void remove(String battleSessionId) {
        if (battleSessionId != null) rosters.remove(battleSessionId.trim());
    }

    private void purgeExpired() {
        long cutoff = nowMillis.getAsLong() - ROSTER_TTL_MILLIS;
        rosters.entrySet().removeIf(entry -> entry.getValue().createdAtMillis < cutoff);
    }

    private static boolean blank(String value) {
        return value == null || value.trim().isEmpty();
    }

    public record Player(int playerSlot, String playerId) {
    }

    public record Roster(String mapId, String balanceVersion, String contentHash,
                         SessionSource sessionSource, List<Player> players) {
    }

    private static final class MutableRoster {
        private final String mapId;
        private final String balanceVersion;
        private final String contentHash;
        private final SessionSource sessionSource;
        private final long createdAtMillis;
        private final Map<Integer, String> players = new ConcurrentHashMap<>();

        private MutableRoster(String mapId, String balanceVersion, String contentHash,
                              SessionSource sessionSource, long createdAtMillis) {
            this.mapId = mapId;
            this.balanceVersion = balanceVersion;
            this.contentHash = contentHash;
            this.sessionSource = sessionSource;
            this.createdAtMillis = createdAtMillis;
        }

        private synchronized void register(int slot, String playerId, String requestedMapId,
                                           String requestedBalanceVersion, String requestedContentHash,
                                           SessionSource requestedSessionSource) {
            if (!mapId.equals(requestedMapId)
                    || !balanceVersion.equals(requestedBalanceVersion)
                    || !contentHash.equalsIgnoreCase(requestedContentHash)
                    || sessionSource != requestedSessionSource) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
            String existing = players.get(slot);
            if (existing != null && !existing.equals(playerId)) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
            if (players.entrySet().stream().anyMatch(entry -> entry.getKey() != slot && entry.getValue().equals(playerId))) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
            players.put(slot, playerId);
        }

        private synchronized Roster snapshot() {
            if (players.size() != 2 || !players.keySet().containsAll(List.of(1, 2))) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
            var result = new ArrayList<Player>(2);
            players.forEach((slot, playerId) -> result.add(new Player(slot, playerId)));
            result.sort(Comparator.comparingInt(Player::playerSlot));
            return new Roster(mapId, balanceVersion, contentHash, sessionSource, List.copyOf(result));
        }
    }
}
