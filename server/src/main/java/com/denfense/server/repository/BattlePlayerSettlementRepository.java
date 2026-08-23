package com.denfense.server.repository;

import com.denfense.server.domain.BattlePlayerSettlement;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface BattlePlayerSettlementRepository extends JpaRepository<BattlePlayerSettlement, Long> {
    List<BattlePlayerSettlement> findByBattleSettlementId(Long battleSettlementId);
}
