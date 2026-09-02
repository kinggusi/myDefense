package com.denfense.server.repository;

import com.denfense.server.domain.QuestSettlementApplication;
import org.springframework.data.jpa.repository.JpaRepository;

public interface QuestSettlementApplicationRepository extends JpaRepository<QuestSettlementApplication, Long> {
    boolean existsByBattleSettlementIdAndUserIdAndQuestConditionId(
            Long battleSettlementId, Long userId, String questConditionId);
}
