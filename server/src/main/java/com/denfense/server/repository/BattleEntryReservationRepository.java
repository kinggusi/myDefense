package com.denfense.server.repository;

import com.denfense.server.domain.BattleEntryReservation;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.Optional;

public interface BattleEntryReservationRepository extends JpaRepository<BattleEntryReservation, Long> {
    Optional<BattleEntryReservation> findByBattleSessionId(String battleSessionId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("SELECT r FROM BattleEntryReservation r WHERE r.battleSessionId = :battleSessionId")
    Optional<BattleEntryReservation> findByBattleSessionIdForUpdate(
            @Param("battleSessionId") String battleSessionId);
}
