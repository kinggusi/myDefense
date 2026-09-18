package com.denfense.server.auth;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import java.util.Optional;
public interface AuthChallengeRepository extends JpaRepository<AuthChallenge, String> {
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select c from AuthChallenge c where c.id = :id")
    Optional<AuthChallenge> findForUpdate(@Param("id") String id);
}
