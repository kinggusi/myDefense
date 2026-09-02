package com.denfense.server.repository;

import com.denfense.server.domain.QuestProgress;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.Optional;

public interface QuestProgressRepository extends JpaRepository<QuestProgress, Long> {
    Optional<QuestProgress> findByUserIdAndQuestConditionId(Long userId, String questConditionId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select q from QuestProgress q where q.user.id = :userId and q.questConditionId = :conditionId")
    Optional<QuestProgress> findForUpdate(@Param("userId") Long userId,
                                          @Param("conditionId") String questConditionId);
}
