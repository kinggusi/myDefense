package com.denfense.server.repository;

import com.denfense.server.domain.DailyContentRun;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;

import java.util.Optional;

public interface DailyContentRunRepository extends JpaRepository<DailyContentRun, Long> {
    Optional<DailyContentRun> findByUserIdAndEntryRequestId(Long userId, String entryRequestId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    Optional<DailyContentRun> findByRunId(String runId);
}
