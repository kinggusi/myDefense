package com.denfense.server.repository;

import com.denfense.server.domain.UserPlanetUnlock;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface UserPlanetUnlockRepository extends JpaRepository<UserPlanetUnlock, Long> {
    List<UserPlanetUnlock> findAllByUserIdOrderByIdAsc(Long userId);
    Optional<UserPlanetUnlock> findByUserIdAndMapId(Long userId, String mapId);
    boolean existsByUserIdAndMapId(Long userId, String mapId);
}
