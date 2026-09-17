package com.denfense.server.repository;

import com.denfense.server.domain.QuestCycleProgress;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface QuestCycleProgressRepository extends JpaRepository<QuestCycleProgress, Long> {
    Optional<QuestCycleProgress> findByUserIdAndQuestIdAndCycleKey(Long userId, String questId, String cycleKey);
    List<QuestCycleProgress> findAllByUserIdAndCycleKey(Long userId, String cycleKey);
}
