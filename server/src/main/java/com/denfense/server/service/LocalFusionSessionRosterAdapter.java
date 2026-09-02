package com.denfense.server.service;

import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.domain.User;
import com.denfense.server.domain.SessionSource;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.PlanetBattleBalanceRegistry;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

import java.util.HashSet;
import java.util.Set;

/**
 * Local/dev bridge from a Fusion State Authority roster into Spring.
 * This bean is deliberately absent from production profiles.
 *
 * FUTURE_AUTH_REPLACEMENT: the production implementation must use JWT-backed
 * matchmaking identities instead of accepting identities observed by a
 * client-hosted Fusion State Authority.
 */
@Service
@Profile({"local", "dev"})
@RequiredArgsConstructor
@Slf4j
public class LocalFusionSessionRosterAdapter implements BattleSessionRosterAuthorityAdapter {
    private final BattleSessionRosterRegistry registry;
    private final UserRepository users;
    private final BalanceVersionRegistry versions;
    private final PlanetBattleBalanceRegistry planetBattles;
    private final BattlePlanetEntryService battleEntries;

    @Override
    public BattleSessionRosterDtos.RegisterResponse register(BattleSessionRosterDtos.RegisterRequest request) {
        validate(request);
        var orderedPlayers = request.players().stream()
                .sorted(java.util.Comparator.comparingInt(BattleSessionRosterDtos.Player::playerSlot))
                .toList();
        User playerOne = users.findByUsername(orderedPlayers.get(0).playerId().trim()).orElseThrow();
        User playerTwo = users.findByUsername(orderedPlayers.get(1).playerId().trim()).orElseThrow();
        var entry = battleEntries.reserve(
                request.battleSessionId(), request.mapId(), playerOne.getId(), playerTwo.getId());
        boolean published = false;
        try {
            registry.registerComplete(
                    request.battleSessionId(),
                    request.mapId(),
                    request.balanceVersion(),
                    request.contentHash(),
                    request.players().stream()
                            .map(player -> new BattleSessionRosterRegistry.Player(
                                    player.playerSlot(), player.playerId().trim()))
                            .toList(),
                    request.battleSessionId().trim().startsWith("P1VAL-")
                            ? SessionSource.VALIDATION_FIXTURE
                            : SessionSource.LOCAL_DEVELOPMENT);
            published = true;
            registry.requireComplete(request.battleSessionId());
        } catch (RuntimeException registrationFailure) {
            if (!entry.alreadyProcessed()) {
                if (published) registry.remove(request.battleSessionId());
                try {
                    battleEntries.refund(request.battleSessionId(),
                            com.denfense.server.domain.BattleEntryRefundReason.SESSION_FAILED);
                } catch (RuntimeException refundFailure) {
                    registrationFailure.addSuppressed(refundFailure);
                    log.error("Failed to compensate Battle entry reservation for session {}",
                            request.battleSessionId(), refundFailure);
                }
            }
            throw registrationFailure;
        }
        return new BattleSessionRosterDtos.RegisterResponse(request.battleSessionId().trim(), "REGISTERED", 2);
    }

    private void validate(BattleSessionRosterDtos.RegisterRequest request) {
        if (request == null || blank(request.battleSessionId()) || blank(request.mapId())
                || blank(request.balanceVersion()) || blank(request.contentHash())
                || request.players() == null || request.players().size() != 2) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        if (!versions.getBalanceVersion().equals(request.balanceVersion().trim())
                || !versions.getContentHash().equalsIgnoreCase(request.contentHash().trim())) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        try {
            planetBattles.get(request.mapId().trim());
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }

        Set<Integer> slots = new HashSet<>();
        Set<String> playerIds = new HashSet<>();
        for (BattleSessionRosterDtos.Player player : request.players()) {
            if (player == null || player.playerSlot() < 1 || player.playerSlot() > 2
                    || blank(player.playerId()) || !slots.add(player.playerSlot())
                    || !playerIds.add(player.playerId().trim())) {
                throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
            }
        }
        if (!slots.equals(Set.of(1, 2))) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        var missingUsers = request.players().stream()
                .map(player -> player.playerId().trim())
                .filter(playerId -> users.findByUsername(playerId).isEmpty())
                .toList();
        if (missingUsers.stream().anyMatch(playerId -> !playerId.startsWith("dev-"))) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        if (!missingUsers.isEmpty()) {
            users.saveAllAndFlush(missingUsers.stream()
                    .map(playerId -> new User(playerId, "LOCAL_DEV_ONLY"))
                    .toList());
        }
    }

    private static boolean blank(String value) {
        return value == null || value.trim().isEmpty();
    }

}
