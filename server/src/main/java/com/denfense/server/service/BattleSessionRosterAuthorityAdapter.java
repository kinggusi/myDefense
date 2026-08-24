package com.denfense.server.service;

import com.denfense.server.dto.battle.BattleSessionRosterDtos;

/**
 * Trusted matchmaking boundary for Settlement participants.
 *
 * FUTURE_AUTH_REPLACEMENT: replace the local implementation with an adapter
 * that obtains player identities from verified JWT principals and the
 * production matchmaking session. Settlement and roster storage must not need
 * to change when that replacement is made.
 */
public interface BattleSessionRosterAuthorityAdapter {
    BattleSessionRosterDtos.RegisterResponse register(BattleSessionRosterDtos.RegisterRequest request);
}
