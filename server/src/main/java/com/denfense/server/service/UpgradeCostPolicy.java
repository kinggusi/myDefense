package com.denfense.server.service;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import org.springframework.stereotype.Component;

@Component
public class UpgradeCostPolicy {
    
    // MVP 기준 임시 최대 레벨
    public static final int MAX_LEVEL = 50;
    
    public UpgradeCost calculate(int currentLevel) {
        if (currentLevel >= MAX_LEVEL) {
            throw new BusinessException(ErrorCode.MAX_ALIEN_LEVEL_REACHED, "최대 레벨에 도달했습니다.");
        }
        
        // 레벨 1~9: pieces + gold
        // 레벨 10~49: pieces + gold + growthCell
        int requiredPieces = currentLevel * 5; 
        int requiredGold = currentLevel * 100;
        
        // 9 -> 10 강화 시 (currentLevel = 9) : 성장 세포 미사용
        // 10 -> 11 강화 시 (currentLevel = 10) : 성장 세포 사용 시작
        int requiredGrowthCell = 0;
        if (currentLevel >= 10) {
            requiredGrowthCell = (currentLevel - 9) * 2;
        }
        
        return new UpgradeCost(requiredPieces, requiredGold, requiredGrowthCell);
    }
}
