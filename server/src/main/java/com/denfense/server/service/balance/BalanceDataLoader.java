package com.denfense.server.service.balance;

import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;

import com.denfense.server.balance.AlienSpecBalance;
import com.fasterxml.jackson.core.type.TypeReference;
import java.util.List;

import org.springframework.core.annotation.Order;

@Slf4j
@Component
@RequiredArgsConstructor
@Order(1)
public class BalanceDataLoader implements ApplicationRunner {

    private final ResourceLoader resourceLoader;
    private final ObjectMapper baseObjectMapper;
    private final BalanceDataValidator validator;
    private final BalanceRegistry registry;
    private final AlienUpgradeBalanceRegistry alienUpgradeRegistry;

    @Value("${balance.reward.path:classpath:balance/generated/game-reward.json}")
    private String rewardFilePath;

    @Value("${balance.upgrade-cost.path:classpath:balance/generated/alien-upgrade-cost.json}")
    private String upgradeCostFilePath;

    @Value("${balance.level-stat.path:classpath:balance/generated/alien-level-stat.json}")
    private String levelStatFilePath;

    @Value("${balance.spec.path:classpath:balance/generated/alien-spec.json}")
    private String specFilePath;

    @Value("${balance.pool.path:classpath:balance/generated/gacha-pools.json}")
    private String poolFilePath;

    @Value("${balance.product.path:classpath:balance/generated/shop-products.json}")
    private String productFilePath;

    public void setRewardFilePath(String rewardFilePath) {
        this.rewardFilePath = rewardFilePath;
    }

    public void setUpgradeFilePath(String upgradeFilePath) {
        this.upgradeCostFilePath = upgradeFilePath;
    }

    public void setLevelStatFilePath(String levelStatFilePath) {
        this.levelStatFilePath = levelStatFilePath;
    }

    public void setSpecFilePath(String specFilePath) {
        this.specFilePath = specFilePath;
    }

    public void setPoolFilePath(String poolFilePath) {
        this.poolFilePath = poolFilePath;
    }

    public void setProductFilePath(String productFilePath) {
        this.productFilePath = productFilePath;
    }

    @Override
    public void run(ApplicationArguments args) throws Exception {
        loadData();
    }

    public void loadData() throws Exception {
        log.info("Balance 데이터 로딩 시작...");

        try {
            ObjectMapper strictMapper = baseObjectMapper.copy()
                .enable(com.fasterxml.jackson.databind.DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES);

            Resource rewardRes = resourceLoader.getResource(rewardFilePath);
            if (!rewardRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + rewardFilePath);
            }
            GameRewardBalance rewardBalance = strictMapper.readValue(rewardRes.getInputStream(), GameRewardBalance.class);
            validator.validateGameReward(rewardBalance);

            Resource upgradeRes = resourceLoader.getResource(upgradeCostFilePath);
            if (!upgradeRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + upgradeCostFilePath);
            }
            List<AlienUpgradeCostBalance> upgradeCosts = strictMapper.readValue(
                    upgradeRes.getInputStream(), new TypeReference<List<AlienUpgradeCostBalance>>() {});

            Resource levelStatRes = resourceLoader.getResource(levelStatFilePath);
            if (!levelStatRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + levelStatFilePath);
            }
            List<AlienLevelStatBalance> levelStats = strictMapper.readValue(
                    levelStatRes.getInputStream(), new TypeReference<List<AlienLevelStatBalance>>() {});
            validator.validateAlienLevelStats(levelStats);
            int maxLevel = levelStats.stream().mapToInt(AlienLevelStatBalance::level).max().orElseThrow();
            validator.validateAlienUpgradeCosts(upgradeCosts, maxLevel);

            Resource specRes = resourceLoader.getResource(specFilePath);
            if (!specRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + specFilePath);
            }
            List<AlienSpecBalance> specs = strictMapper.readValue(specRes.getInputStream(), new TypeReference<List<AlienSpecBalance>>() {});
            validator.validateAlienSpec(specs);

            Resource poolRes = resourceLoader.getResource(poolFilePath);
            if (!poolRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + poolFilePath);
            }
            com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = strictMapper.readValue(poolRes.getInputStream(), com.denfense.server.balance.GachaPoolBalanceDocument.class);
            validator.validateGachaPool(poolDoc, specs);

            Resource productRes = resourceLoader.getResource(productFilePath);
            if (!productRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + productFilePath);
            }
            com.denfense.server.balance.ShopProductBalanceDocument productDoc = strictMapper.readValue(productRes.getInputStream(), com.denfense.server.balance.ShopProductBalanceDocument.class);
            validator.validateShopProduct(productDoc, poolDoc);

            alienUpgradeRegistry.init(upgradeCosts, levelStats);
            registry.init(rewardBalance, specs, productDoc.products(), poolDoc.pools());

            log.info("Balance 데이터 로딩 완료. MaxLevel: {}", maxLevel);
        } catch (Exception e) {
            log.error("Balance 데이터 로딩 실패! 서버 시작을 중단합니다.", e);
            throw e;
        }
    }
}
