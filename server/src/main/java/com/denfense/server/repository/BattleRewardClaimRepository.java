package com.denfense.server.repository;

import com.denfense.server.domain.BattleRewardClaim;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface BattleRewardClaimRepository extends JpaRepository<BattleRewardClaim, Long> {
    Optional<BattleRewardClaim> findByUserIdAndRewardKey(Long userId, String rewardKey);
    List<BattleRewardClaim> findByBattleSessionIdOrderByIdAsc(String battleSessionId);
}
