package com.denfense.server.service;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.domain.SessionSource;
import org.junit.jupiter.api.Test;

import java.util.concurrent.atomic.AtomicLong;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BattleSessionRosterRegistryTest {

    @Test
    void trustedRosterExpiresAndFailsClosed() {
        AtomicLong now = new AtomicLong(1_000L);
        BattleSessionRosterRegistry registry = new BattleSessionRosterRegistry(now::get);
        registry.register("session", 1, "player-a", "NEPTUNE", "version", "hash");
        registry.register("session", 2, "player-b", "NEPTUNE", "version", "hash");

        assertEquals(2, registry.requireComplete("session").players().size());

        now.addAndGet(BattleSessionRosterRegistry.ROSTER_TTL_MILLIS + 1L);
        BusinessException exception = assertThrows(
                BusinessException.class,
                () -> registry.requireComplete("session"));
        assertEquals(ErrorCode.BATTLE_PARTICIPANT_MISMATCH, exception.getErrorCode());
    }

    @Test
    void completeRosterConflictNeverLeavesMixedPlayerSlots() {
        BattleSessionRosterRegistry registry = new BattleSessionRosterRegistry();
        registry.registerComplete("session", "NEPTUNE", "version", "hash", List.of(
                new BattleSessionRosterRegistry.Player(1, "player-a"),
                new BattleSessionRosterRegistry.Player(2, "player-b")));

        BusinessException exception = assertThrows(BusinessException.class, () ->
                registry.registerComplete("session", "NEPTUNE", "version", "hash", List.of(
                        new BattleSessionRosterRegistry.Player(1, "player-c"),
                        new BattleSessionRosterRegistry.Player(2, "player-d"))));

        assertEquals(ErrorCode.BATTLE_PARTICIPANT_MISMATCH, exception.getErrorCode());
        assertEquals(List.of(
                new BattleSessionRosterRegistry.Player(1, "player-a"),
                new BattleSessionRosterRegistry.Player(2, "player-b")),
                registry.requireComplete("session").players());
    }

    @Test
    void sourceIsServerOwnedAndPartOfRosterIdentity() {
        BattleSessionRosterRegistry registry = new BattleSessionRosterRegistry();
        registry.registerComplete("production-session", "NEPTUNE", "version", "hash", List.of(
                new BattleSessionRosterRegistry.Player(1, "player-a"),
                new BattleSessionRosterRegistry.Player(2, "player-b")), SessionSource.PRODUCTION);

        assertEquals(SessionSource.PRODUCTION,
                registry.requireComplete("production-session").sessionSource());
        BusinessException exception = assertThrows(BusinessException.class, () ->
                registry.registerComplete("production-session", "NEPTUNE", "version", "hash", List.of(
                        new BattleSessionRosterRegistry.Player(1, "player-a"),
                        new BattleSessionRosterRegistry.Player(2, "player-b")),
                        SessionSource.LOCAL_DEVELOPMENT));
        assertEquals(ErrorCode.BATTLE_PARTICIPANT_MISMATCH, exception.getErrorCode());
    }
}
