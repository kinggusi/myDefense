package com.denfense.server.service.impl;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.response.AlienUpgradeBlockReason;
import com.denfense.server.dto.response.AlienUpgradeResponseDto;
import com.denfense.server.dto.response.AlienUpgradeStatusResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.AlienCurrentStat;
import com.denfense.server.service.AlienService;
import com.denfense.server.service.AlienStatCalculator;
import com.denfense.server.service.UpgradeCost;
import com.denfense.server.service.UpgradeCostPolicy;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;

@Service
@RequiredArgsConstructor
public class AlienServiceImpl implements AlienService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final UpgradeCostPolicy upgradeCostPolicy;
    private final AlienStatCalculator alienStatCalculator;

    @Override
    @Transactional
    public AlienUpgradeResponseDto upgradeAlien(String username, int alienId) {
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
        AlienSpec spec = findSpec(alienId);
        if (spec.isLocked()) {
            throw new BusinessException(ErrorCode.ALIEN_SPEC_LOCKED);
        }
        UserAlien userAlien = userAlienRepository.findByUserAndAlienSpecForUpdate(user, spec)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_ALIEN_NOT_FOUND));

        int beforeLevel = userAlien.getLevel();
        UpgradeCost usedCost = upgradeCostPolicy.calculate(beforeLevel);
        int usedPieces = Math.min(userAlien.getPieces(), usedCost.getRequiredPieces());
        int usedUniversalPiece = usedCost.getRequiredPieces() - usedPieces;

        if (user.getUniversalPiece() < usedUniversalPiece) {
            throw new BusinessException(ErrorCode.INSUFFICIENT_ALIEN_PIECES);
        }

        user.spendUniversalPiece(usedUniversalPiece);
        user.spendGold(usedCost.getRequiredGold());
        user.spendGrowthCell(usedCost.getRequiredGrowthCell());
        userAlien.upgradeAlien(usedPieces);

        UpgradeAvailability next = availability(user, spec, userAlien);
        AlienCurrentStat currentStat = alienStatCalculator.calculate(spec, userAlien.getLevel());

        return AlienUpgradeResponseDto.builder()
                .alienId(spec.getId())
                .alienName(spec.getName())
                .beforeLevel(beforeLevel)
                .afterLevel(userAlien.getLevel())
                .requiredPieces(usedCost.getRequiredPieces())
                .usedPieces(usedPieces)
                .remainingPieces(userAlien.getPieces())
                .usedUniversalPiece(usedUniversalPiece)
                .remainingUniversalPiece(user.getUniversalPiece())
                .usedGold(usedCost.getRequiredGold())
                .remainingGold(user.getGold())
                .usedGrowthCell(usedCost.getRequiredGrowthCell())
                .remainingGrowthCell(user.getGrowthCell())
                .maxLevelReached(next.maxLevelReached())
                .maxLevel(upgradeCostPolicy.getMaxLevel())
                .canUpgrade(next.canUpgrade())
                .cannotUpgradeReason(next.reason())
                .nextRequiredPieces(next.requiredPieces())
                .nextRequiredUniversalPiece(next.requiredUniversalPiece())
                .nextRequiredGold(next.requiredGold())
                .nextRequiredGrowthCell(next.requiredGrowthCell())
                .currentAtk(currentStat.currentAtk())
                .currentMp(currentStat.currentMp())
                .currentAtkSpeed(currentStat.currentAtkSpeed())
                .currentRange(currentStat.currentRange())
                .build();
    }

    @Override
    @Transactional(Transactional.TxType.SUPPORTS)
    public AlienUpgradeStatusResponseDto getUpgradeStatus(String username, int alienId) {
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
        AlienSpec spec = findSpec(alienId);
        UserAlien userAlien = userAlienRepository.findByUserAndAlienSpec(user, spec).orElse(null);
        UpgradeAvailability availability = availability(user, spec, userAlien);
        AlienCurrentStat currentStat = userAlien == null ? null : alienStatCalculator.calculate(spec, userAlien.getLevel());

        return AlienUpgradeStatusResponseDto.builder()
                .alienId(spec.getId())
                .alienName(spec.getName())
                .grade(spec.getGrade() == null ? null : spec.getGrade().name())
                .owned(userAlien != null)
                .specLocked(spec.isLocked())
                .currentLevel(userAlien == null ? 0 : userAlien.getLevel())
                .currentPieces(userAlien == null ? 0 : userAlien.getPieces())
                .universalPiece(user.getUniversalPiece())
                .gold(user.getGold())
                .growthCell(user.getGrowthCell())
                .maxLevel(upgradeCostPolicy.getMaxLevel())
                .maxLevelReached(availability.maxLevelReached())
                .canUpgrade(availability.canUpgrade())
                .cannotUpgradeReason(availability.reason())
                .requiredPieces(availability.requiredPieces())
                .requiredUniversalPiece(availability.requiredUniversalPiece())
                .requiredGold(availability.requiredGold())
                .requiredGrowthCell(availability.requiredGrowthCell())
                .baseAtk(spec.getBaseAtk())
                .baseMp(spec.getBaseMp())
                .atkSpeed(spec.getAtkSpeed())
                .range(spec.getRange())
                .currentAtk(statValue(currentStat, StatField.ATK))
                .currentMp(statValue(currentStat, StatField.MP))
                .currentAtkSpeed(statValue(currentStat, StatField.ATK_SPEED))
                .currentRange(statValue(currentStat, StatField.RANGE))
                .build();
    }

    private AlienSpec findSpec(int alienId) {
        return alienSpecRepository.findById(Long.valueOf(alienId))
                .orElseThrow(() -> new BusinessException(ErrorCode.ALIEN_SPEC_NOT_FOUND));
    }

    private UpgradeAvailability availability(User user, AlienSpec spec, UserAlien userAlien) {
        int maxLevel = upgradeCostPolicy.getMaxLevel();
        if (userAlien == null) {
            return UpgradeAvailability.blocked(AlienUpgradeBlockReason.NOT_OWNED, false);
        }
        if (userAlien.getLevel() >= maxLevel) {
            return UpgradeAvailability.blocked(AlienUpgradeBlockReason.MAX_LEVEL, true);
        }

        UpgradeCost cost = upgradeCostPolicy.calculate(userAlien.getLevel());
        int requiredUniversalPiece = Math.max(0, cost.getRequiredPieces() - userAlien.getPieces());
        AlienUpgradeBlockReason reason = AlienUpgradeBlockReason.NONE;
        if (spec.isLocked()) {
            reason = AlienUpgradeBlockReason.SPEC_LOCKED;
        } else if ((long) userAlien.getPieces() + user.getUniversalPiece() < cost.getRequiredPieces()) {
            reason = AlienUpgradeBlockReason.INSUFFICIENT_PIECES;
        } else if (user.getGold() < cost.getRequiredGold()) {
            reason = AlienUpgradeBlockReason.INSUFFICIENT_GOLD;
        } else if (user.getGrowthCell() < cost.getRequiredGrowthCell()) {
            reason = AlienUpgradeBlockReason.INSUFFICIENT_GROWTH_CELL;
        }

        return new UpgradeAvailability(
                reason == AlienUpgradeBlockReason.NONE,
                reason,
                false,
                cost.getRequiredPieces(),
                requiredUniversalPiece,
                cost.getRequiredGold(),
                cost.getRequiredGrowthCell()
        );
    }

    private BigDecimal statValue(AlienCurrentStat stat, StatField field) {
        if (stat == null) return null;
        return switch (field) {
            case ATK -> stat.currentAtk();
            case MP -> stat.currentMp();
            case ATK_SPEED -> stat.currentAtkSpeed();
            case RANGE -> stat.currentRange();
        };
    }

    private enum StatField { ATK, MP, ATK_SPEED, RANGE }

    private record UpgradeAvailability(
            boolean canUpgrade,
            AlienUpgradeBlockReason reason,
            boolean maxLevelReached,
            int requiredPieces,
            int requiredUniversalPiece,
            int requiredGold,
            int requiredGrowthCell
    ) {
        private static UpgradeAvailability blocked(AlienUpgradeBlockReason reason, boolean maxLevelReached) {
            return new UpgradeAvailability(false, reason, maxLevelReached, 0, 0, 0, 0);
        }
    }
}
