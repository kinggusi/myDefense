package com.denfense.server.service.balance;

import java.util.List;

public record AlienUpgradeBalanceFile(
        int maxLevel,
        List<AlienUpgradeCostBalance> costs
) {
}
