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
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

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

    @Value("${balance.reward.path:classpath:balance/generated/game-reward.json}")
    private String rewardFilePath;

    @Value("${balance.upgrade.path:classpath:balance/generated/alien-upgrade.json}")
    private String upgradeFilePath;

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
        this.upgradeFilePath = upgradeFilePath;
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

            Resource upgradeRes = resourceLoader.getResource(upgradeFilePath);
            if (!upgradeRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + upgradeFilePath);
            }
            AlienUpgradeBalanceFile upgradeFile = strictMapper.readValue(upgradeRes.getInputStream(), AlienUpgradeBalanceFile.class);
            validator.validateAlienUpgrade(upgradeFile);

            Map<Integer, AlienUpgradeCostBalance> costMap = upgradeFile.costs().stream()
                    .collect(Collectors.toMap(AlienUpgradeCostBalance::currentLevel, Function.identity()));

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

            registry.init(rewardBalance, upgradeFile.maxLevel(), costMap, specs, productDoc.products(), poolDoc.pools());

            log.info("Balance 데이터 로딩 완료. MaxLevel: {}", upgradeFile.maxLevel());
        } catch (Exception e) {
            log.error("Balance 데이터 로딩 실패! 서버 시작을 중단합니다.", e);
            throw e;
        }
    }
}
