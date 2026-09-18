package com.denfense.server.auth;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import java.util.Optional;
public interface AuthSessionFamilyRepository extends JpaRepository<AuthSessionFamily, String> {
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select f from AuthSessionFamily f where f.id = :id")
    Optional<AuthSessionFamily> findForUpdate(@Param("id") String id);
}
