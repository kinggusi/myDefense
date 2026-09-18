package com.denfense.server.auth;

import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import java.time.Instant;
import java.util.List;
import java.util.Optional;

public interface RefreshSessionRepository extends JpaRepository<RefreshSession, String> {
    @Query("select s.familyId from RefreshSession s where s.tokenHash = :hash")
    Optional<String> findFamilyIdByHash(@Param("hash") String hash);
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select s from RefreshSession s where s.tokenHash = :hash")
    Optional<RefreshSession> findForUpdate(@Param("hash") String hash);
    List<RefreshSession> findByFamilyId(String familyId);
    boolean existsByFamilyIdAndRevokedFalseAndConsumedFalseAndExpiresAtAfter(String familyId, Instant now);
}
