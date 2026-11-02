package com.denfense.server.repository.shop;
import com.denfense.server.domain.shop.ShopState;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.Optional;
public interface ShopStateRepository extends JpaRepository<ShopState,Long> { Optional<ShopState> findByUserId(long userId); }
