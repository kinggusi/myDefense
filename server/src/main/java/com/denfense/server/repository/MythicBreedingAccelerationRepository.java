package com.denfense.server.repository;

import com.denfense.server.domain.MythicBreedingAcceleration;
import com.denfense.server.domain.User;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MythicBreedingAccelerationRepository extends JpaRepository<MythicBreedingAcceleration, Long> {
    Optional<MythicBreedingAcceleration> findByUserAndRequestId(User user, String requestId);
}
