package com.denfense.server.clan;

import org.springframework.data.jpa.repository.JpaRepository;
import java.util.Optional;
import java.util.UUID;

public interface ClanCommandReceiptRepository extends JpaRepository<ClanCommandReceipt,Long> {
    Optional<ClanCommandReceipt> findByUserIdAndRequestId(long userId,UUID requestId);
}
