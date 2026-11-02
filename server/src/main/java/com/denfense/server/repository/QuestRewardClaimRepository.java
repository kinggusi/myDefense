package com.denfense.server.repository;

import com.denfense.server.domain.QuestRewardClaim;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface QuestRewardClaimRepository extends JpaRepository<QuestRewardClaim, Long> {
    Optional<QuestRewardClaim> findByUserIdAndRequestId(Long userId, String requestId);
    Optional<QuestRewardClaim> findByUserIdAndRewardKey(Long userId, String rewardKey);
    List<QuestRewardClaim> findAllByUserIdAndRewardKeyStartingWith(Long userId, String rewardKeyPrefix);
}
