package com.denfense.server.service;

import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.domain.User;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.PlanetBattleBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

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
public class LocalFusionSessionRosterAdapter implements BattleSessionRosterAuthorityAdapter {
    private final BattleSessionRosterRegistry registry;
    private final UserRepository users;
    private final BalanceVersionRegistry versions;
    private final PlanetBattleBalanceRegistry planetBattles;

    @Override
    @Transactional
    public BattleSessionRosterDtos.RegisterResponse register(BattleSessionRosterDtos.RegisterRequest request) {
        validate(request);
        registry.registerComplete(
                request.battleSessionId(),
                request.mapId(),
                request.balanceVersion(),
                request.contentHash(),
                request.players().stream()
                        .map(player -> new BattleSessionRosterRegistry.Player(
                                player.playerSlot(), player.playerId().trim()))
                        .toList());
        registry.requireComplete(request.battleSessionId());
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
            ensureLocalDevelopmentUser(player.playerId().trim());
        }
        if (!slots.equals(Set.of(1, 2))) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
    }

    private static boolean blank(String value) {
        return value == null || value.trim().isEmpty();
    }

    private void ensureLocalDevelopmentUser(String playerId) {
        if (users.findByUsername(playerId).isPresent()) {
            return;
        }
        if (!playerId.startsWith("dev-")) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        // Local smoke-test convenience only. Production JWT matchmaking must
        // resolve an existing authenticated account and must never auto-create.
        users.save(new User(playerId, "LOCAL_DEV_ONLY"));
    }
}
