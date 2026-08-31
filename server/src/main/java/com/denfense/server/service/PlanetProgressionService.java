package com.denfense.server.service;

import com.denfense.server.balance.PlanetBattleBalance;
import com.denfense.server.domain.PlanetUnlockSource;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserPlanetUnlock;
import com.denfense.server.dto.PlanetProgressionDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserPlanetUnlockRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.PlanetBattleBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Comparator;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class PlanetProgressionService {
    private final UserRepository users;
    private final UserPlanetUnlockRepository unlocks;
    private final PlanetBattleBalanceRegistry planets;

    @Transactional
    public PlanetProgressionDtos.Response getProgress(String username) {
        User found = users.findByUsername(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
        User user = users.findByIdForUpdate(found.getId()).orElseThrow();
        ensureDefaultUnlock(user);
        Map<String, UserPlanetUnlock> byMap = unlocks.findAllByUserIdOrderByIdAsc(user.getId()).stream()
                .collect(Collectors.toMap(UserPlanetUnlock::getMapId, Function.identity()));
        var entries = planets.getAll().stream()
                .map(planet -> {
                    UserPlanetUnlock unlock = byMap.get(planet.mapId());
                    return new PlanetProgressionDtos.Planet(
                            planet.mapId(), planet.order(), unlock != null,
                            unlock == null ? null : unlock.getUnlockedAt());
                })
                .toList();
        return new PlanetProgressionDtos.Response(user.getId(), entries);
    }

    @Transactional
    public void requireUnlocked(User user, String mapId) {
        PlanetBattleBalance planet = requirePlanet(mapId);
        ensureDefaultUnlock(user);
        if (!unlocks.existsByUserIdAndMapId(user.getId(), planet.mapId())) {
            throw new BusinessException(ErrorCode.PLANET_LOCKED);
        }
    }

    @Transactional
    public boolean unlockNext(User user, String clearedMapId, String battleSessionId) {
        PlanetBattleBalance cleared = requirePlanet(clearedMapId);
        PlanetBattleBalance next = planets.getAll().stream()
                .filter(candidate -> candidate.order() > cleared.order())
                .min(Comparator.comparingInt(PlanetBattleBalance::order))
                .orElse(null);
        if (next == null || unlocks.existsByUserIdAndMapId(user.getId(), next.mapId())) return false;
        unlocks.saveAndFlush(new UserPlanetUnlock(
                user, next.mapId(), PlanetUnlockSource.PREVIOUS_PLANET_CLEAR, battleSessionId));
        return true;
    }

    private void ensureDefaultUnlock(User user) {
        PlanetBattleBalance first = planets.getAll().stream()
                .min(Comparator.comparingInt(PlanetBattleBalance::order))
                .orElseThrow(() -> new BusinessException(ErrorCode.PLANET_NOT_FOUND));
        if (unlocks.existsByUserIdAndMapId(user.getId(), first.mapId())) return;
        unlocks.saveAndFlush(new UserPlanetUnlock(user, first.mapId(), PlanetUnlockSource.DEFAULT, null));
    }

    private PlanetBattleBalance requirePlanet(String mapId) {
        try {
            return planets.get(mapId == null ? "" : mapId.trim());
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.PLANET_NOT_FOUND);
        }
    }
}
