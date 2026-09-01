package com.denfense.server.repository;

import com.denfense.server.domain.DailyContentProgress;
import com.denfense.server.domain.DailyContentType;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;

import java.util.List;
import java.util.Optional;

public interface DailyContentProgressRepository extends JpaRepository<DailyContentProgress, Long> {
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    Optional<DailyContentProgress> findByUserIdAndContentType(Long userId, DailyContentType contentType);
    List<DailyContentProgress> findAllByUserId(Long userId);
}
