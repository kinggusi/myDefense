package com.denfense.server.tutorial;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
public interface TutorialReceiptRepository extends JpaRepository<TutorialReceipt,Long> {
    Optional<TutorialReceipt> findByUserIdAndRequestId(long userId,UUID requestId);
}
