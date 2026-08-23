package com.denfense.server.service;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.battle.BattleAttackSnapshotDtos;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.BalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.beans.factory.annotation.Value;

import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Service
@RequiredArgsConstructor
public class BattleEntryAttackSnapshotService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final BalanceRegistry balanceRegistry;
    private final AlienStatCalculator statCalculator;
    private final BalanceVersionRegistry balanceVersionRegistry;

    @Value("${mydefense.battle.allow-anonymous-entry-snapshots:false}")
    private boolean allowAnonymousEntrySnapshots;

    @Transactional(readOnly = true)
    public BattleAttackSnapshotDtos.Response getForPlayer(String playerId) {
        String normalizedPlayerId = playerId == null ? "" : playerId.trim();
        Map<Long, Integer> ownedLevels = loadOwnedLevels(normalizedPlayerId);

        List<BattleAttackSnapshotDtos.AlienAttack> aliens = balanceRegistry.getAllAlienSpecs().stream()
                .sorted(Comparator.comparingLong(AlienSpecBalance::alienId))
                .map(spec -> toSnapshot(spec, ownedLevels.getOrDefault(spec.alienId(), 1)))
                .toList();

        return new BattleAttackSnapshotDtos.Response(
                normalizedPlayerId,
                balanceVersionRegistry.getBalanceVersion(),
                balanceVersionRegistry.getContentHash(),
                List.copyOf(aliens));
    }

    private Map<Long, Integer> loadOwnedLevels(String playerId) {
        if (playerId.isBlank()) {
            if (allowAnonymousEntrySnapshots) {
                return Map.of();
            }
            throw new com.denfense.server.exception.BusinessException(
                    com.denfense.server.exception.ErrorCode.USER_NOT_FOUND);
        }
        User user = userRepository.findByUsername(playerId).orElse(null);
        if (user == null) {
            // Development Fusion identities do not always have a persistent
            // account yet. They still receive canonical level-one stats.
            if (allowAnonymousEntrySnapshots) {
                return Map.of();
            }
            throw new com.denfense.server.exception.BusinessException(
                    com.denfense.server.exception.ErrorCode.USER_NOT_FOUND);
        }

        Map<Long, Integer> levels = new HashMap<>();
        for (UserAlien userAlien : userAlienRepository.findAllByUser(user)) {
            levels.put(userAlien.getAlienSpec().getId(), Math.max(1, userAlien.getLevel()));
        }
        return levels;
    }

    void allowAnonymousEntrySnapshotsForTest() {
        this.allowAnonymousEntrySnapshots = true;
    }

    private BattleAttackSnapshotDtos.AlienAttack toSnapshot(AlienSpecBalance spec, int level) {
        AlienCurrentStat current = statCalculator.calculate(spec, level);
        return new BattleAttackSnapshotDtos.AlienAttack(
                spec.alienId(),
                level,
                current.currentAtk(),
                current.currentAtkSpeed(),
                current.currentRange());
    }
}
