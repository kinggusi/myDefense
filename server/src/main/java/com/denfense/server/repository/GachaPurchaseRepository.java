package com.denfense.server.repository;

import com.denfense.server.domain.GachaPurchase;
import com.denfense.server.domain.User;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface GachaPurchaseRepository extends JpaRepository<GachaPurchase, Long> {
    Optional<GachaPurchase> findByUserAndPurchaseRequestId(User user, UUID purchaseRequestId);
    List<GachaPurchase> findByUser(User user);
}
