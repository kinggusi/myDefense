package com.denfense.server.repository;

import com.denfense.server.domain.MythicBreedingRequest;
import com.denfense.server.domain.User;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MythicBreedingRequestRepository extends JpaRepository<MythicBreedingRequest, Long> {
    Optional<MythicBreedingRequest> findByUserAndRequestId(User user, String requestId);
}
