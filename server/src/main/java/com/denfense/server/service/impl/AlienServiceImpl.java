package com.denfense.server.service.impl;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.AlienService;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import com.denfense.server.dto.response.AlienUpgradeResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.UpgradeCost;
import com.denfense.server.service.UpgradeCostPolicy;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class AlienServiceImpl implements AlienService {

    private final UserRepository userRepository;
    private final UserAlienRepository userAlienRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final UpgradeCostPolicy upgradeCostPolicy;

    @Transactional
    public AlienUpgradeResponseDto upgradeAlien(String username, int alienId) {
        // 1. 유저 확인 및 비관적 락 조회
        User user = userRepository.findByUsernameForUpdate(username)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND, "유저 없음"));

        AlienSpec spec = alienSpecRepository.findById(Long.valueOf(alienId))
                .orElseThrow(() -> new BusinessException(ErrorCode.INVALID_REQUEST, "존재하지 않는 왹져입니다."));

        // 2. 내 왹져 찾기 및 비관적 락 조회
        UserAlien myAlien = userAlienRepository.findByUserAndAlienSpecForUpdate(user, spec)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_ALIEN_NOT_FOUND, "보유하지 않은 왹져입니다."));

        int currentLevel = myAlien.getLevel();
        UpgradeCost cost = upgradeCostPolicy.calculate(currentLevel);

        int requiredPieces = cost.getRequiredPieces();
        int ownedPieces = myAlien.getPieces();
        int usedPieces = Math.min(ownedPieces, requiredPieces);
        int shortage = requiredPieces - usedPieces;
        int usedUniversalPiece = shortage;

        // 재화 확인 및 차감
        // 카드와 대체 코인 합이 부족한지 확인
        if (user.getUniversalPiece() < usedUniversalPiece) {
            throw new BusinessException(ErrorCode.INSUFFICIENT_ALIEN_PIECES, "왹져 조각 및 대체 코인이 부족합니다.");
        }

        user.spendUniversalPiece(usedUniversalPiece);
        user.spendGold(cost.getRequiredGold());
        user.spendGrowthCell(cost.getRequiredGrowthCell());

        // 4. 강화 실행
        myAlien.upgradeAlien(usedPieces); // UserAlien.java의 upgradeAlien 메서드를 수정해야 함

        return AlienUpgradeResponseDto.builder()
                .alienId(spec.getId())
                .alienName(spec.getName())
                .beforeLevel(currentLevel)
                .afterLevel(myAlien.getLevel())
                .requiredPieces(requiredPieces)
                .usedPieces(usedPieces)
                .remainingPieces(myAlien.getPieces())
                .usedUniversalPiece(usedUniversalPiece)
                .remainingUniversalPiece(user.getUniversalPiece())
                .usedGold(cost.getRequiredGold())
                .remainingGold(user.getGold())
                .usedGrowthCell(cost.getRequiredGrowthCell())
                .remainingGrowthCell(user.getGrowthCell())
                .maxLevelReached(myAlien.getLevel() >= upgradeCostPolicy.getMaxLevel())
                .build();
    }
}
