package com.denfense.server.clan;

import jakarta.persistence.LockModeType;
import org.springframework.data.domain.*;
import org.springframework.data.jpa.repository.*;
import org.springframework.data.repository.query.Param;
import java.util.Optional;

public interface ClanRepository extends JpaRepository<Clan,Long> {
    @Lock(LockModeType.PESSIMISTIC_WRITE) @Query("select c from Clan c where c.id=:id")
    Optional<Clan> lockById(@Param("id") long id);
    boolean existsByNormalizedName(String normalizedName);
    // Spring Data's derived Containing query escapes %, _ and the escape character.
    Slice<Clan> findByActiveTrueAndNormalizedNameContainingOrderByIdAsc(String query,Pageable pageable);
}
