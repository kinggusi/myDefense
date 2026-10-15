package com.denfense.server.repository.shop;
import com.denfense.server.domain.shop.ShopReceipt;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.*;
public interface ShopReceiptRepository extends JpaRepository<ShopReceipt,Long> { Optional<ShopReceipt> findByUserIdAndRequestId(long userId,UUID requestId); }
