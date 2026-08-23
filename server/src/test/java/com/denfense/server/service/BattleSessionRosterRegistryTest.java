package com.denfense.server.service;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import org.junit.jupiter.api.Test;

import java.util.concurrent.atomic.AtomicLong;

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
}
