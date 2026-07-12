package com.denfense.server.repository;

import com.denfense.server.domain.GameSettlement;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface GameSettlementRepository extends JpaRepository<GameSettlement, Long> {
    Optional<GameSettlement> findBySessionId(String sessionId);
}
