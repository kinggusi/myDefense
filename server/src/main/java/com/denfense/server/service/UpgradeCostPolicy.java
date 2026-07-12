package com.denfense.server.service;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.balance.AlienUpgradeCostBalance;
import com.denfense.server.service.balance.BalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class UpgradeCostPolicy {
    
    private final BalanceRegistry registry;
    
    // 호환성을 위해 Registry의 최대 레벨 반환
    public int getMaxLevel() {
        return registry.getMaxAlienLevel();
    }
    
    public UpgradeCost calculate(int currentLevel) {
        AlienUpgradeCostBalance costBalance = registry.getUpgradeCost(currentLevel);
        return new UpgradeCost(costBalance.requiredPieces(), costBalance.requiredGold(), costBalance.requiredGrowthCell());
    }
}
