package com.denfense.server.service.balance;

import org.springframework.stereotype.Component;

import com.denfense.server.balance.AlienSpecBalance;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class BalanceRegistry {

    private boolean initialized = false;
    private GameRewardBalance gameRewardBalance;
    private Map<Long, AlienSpecBalance> alienSpecMap = Collections.emptyMap();

    private Map<String, com.denfense.server.balance.ShopProductBalance> shopProductMap = Collections.emptyMap();
    private Map<String, com.denfense.server.balance.GachaPoolBalance> gachaPoolMap = Collections.emptyMap();

    public synchronized void init(GameRewardBalance rewardBalance, List<AlienSpecBalance> specs,
                                  List<com.denfense.server.balance.ShopProductBalance> products, List<com.denfense.server.balance.GachaPoolBalance> pools) {
        if (this.initialized) {
            throw new IllegalStateException("BalanceRegistry는 이미 초기화되었습니다.");
        }
        if (rewardBalance == null || specs == null || products == null || pools == null) {
            throw new IllegalArgumentException("적재할 데이터가 null입니다.");
        }
        this.gameRewardBalance = rewardBalance;
        this.alienSpecMap = specs.stream().collect(Collectors.toUnmodifiableMap(AlienSpecBalance::alienId, Function.identity()));
        this.shopProductMap = products.stream().collect(Collectors.toUnmodifiableMap(com.denfense.server.balance.ShopProductBalance::productId, Function.identity()));
        this.gachaPoolMap = pools.stream().collect(Collectors.toUnmodifiableMap(com.denfense.server.balance.GachaPoolBalance::poolId, Function.identity()));
        this.initialized = true;
    }

    public GameRewardBalance getGameRewardBalance() {
        if (gameRewardBalance == null) {
            throw new IllegalStateException("GameRewardBalance가 로드되지 않았습니다.");
        }
        return gameRewardBalance;
    }

    public AlienSpecBalance getAlienSpec(long alienId) {
        if (alienSpecMap.isEmpty()) {
            throw new IllegalStateException("AlienSpec 데이터가 로드되지 않았습니다.");
        }
        AlienSpecBalance spec = alienSpecMap.get(alienId);
        if (spec == null) {
            throw new IllegalArgumentException("존재하지 않는 alienId입니다: " + alienId);
        }
        return spec;
    }

    public List<AlienSpecBalance> getAllAlienSpecs() {
        if (alienSpecMap.isEmpty()) {
            throw new IllegalStateException("AlienSpec 데이터가 로드되지 않았습니다.");
        }
        return alienSpecMap.values().stream()
                .sorted(java.util.Comparator.comparingLong(AlienSpecBalance::alienId))
                .toList();
    }

    public com.denfense.server.balance.ShopProductBalance getShopProduct(String productId) {
        if (shopProductMap.isEmpty()) {
            throw new IllegalStateException("ShopProduct 데이터가 로드되지 않았습니다.");
        }
        com.denfense.server.balance.ShopProductBalance product = shopProductMap.get(productId);
        if (product == null) {
            throw new IllegalArgumentException("존재하지 않는 productId입니다: " + productId);
        }
        return product;
    }

    public List<com.denfense.server.balance.ShopProductBalance> getAllShopProducts() {
        if (shopProductMap.isEmpty()) {
            throw new IllegalStateException("ShopProduct 데이터가 로드되지 않았습니다.");
        }
        return List.copyOf(shopProductMap.values());
    }

    public com.denfense.server.balance.GachaPoolBalance getGachaPool(String poolId) {
        if (gachaPoolMap.isEmpty()) {
            throw new IllegalStateException("GachaPool 데이터가 로드되지 않았습니다.");
        }
        com.denfense.server.balance.GachaPoolBalance pool = gachaPoolMap.get(poolId);
        if (pool == null) {
            throw new IllegalArgumentException("존재하지 않는 poolId입니다: " + poolId);
        }
        return pool;
    }

    public List<com.denfense.server.balance.GachaPoolBalance> getAllGachaPools() {
        if (gachaPoolMap.isEmpty()) {
            throw new IllegalStateException("GachaPool 데이터가 로드되지 않았습니다.");
        }
        return List.copyOf(gachaPoolMap.values());
    }
}
