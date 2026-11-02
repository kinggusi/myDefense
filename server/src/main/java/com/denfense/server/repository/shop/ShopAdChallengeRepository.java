package com.denfense.server.repository.shop;
import com.denfense.server.domain.shop.ShopAdChallenge;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.*;
public interface ShopAdChallengeRepository extends JpaRepository<ShopAdChallenge,Long> {
    Optional<ShopAdChallenge> findByUserIdAndRequestId(long userId,UUID requestId);
    Optional<ShopAdChallenge> findByChallengeIdAndUserId(UUID challengeId,long userId);
}
