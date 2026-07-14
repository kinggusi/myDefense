package com.denfense.server.service.gacha;

import com.denfense.server.balance.GachaGradeEntryBalance;
import com.denfense.server.balance.GachaPoolBalance;
import com.denfense.server.balance.ShopProductBalance;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.balance.BalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class GachaDrawService {

    private final BalanceRegistry balanceRegistry;
    private final GachaRandomGenerator randomGenerator;

    public List<GachaDrawResult> draw(String productId) {
        // 1. ShopProduct 조회
        ShopProductBalance product = getShopProduct(productId);
        if (product == null) {
            throw new BusinessException(ErrorCode.SHOP_PRODUCT_NOT_FOUND);
        }

        // 2. 상품 active 확인
        if (!product.active()) {
            throw new BusinessException(ErrorCode.SHOP_PRODUCT_INACTIVE);
        }

        // drawCount만큼 drawOne 반복
        int drawCount = product.drawCount();
        if (drawCount <= 0) {
            throw new IllegalStateException("상품의 뽑기 횟수가 유효하지 않습니다: " + drawCount);
        }

        List<GachaDrawResult> results = new ArrayList<>(drawCount);
        for (int i = 0; i < drawCount; i++) {
            results.add(drawOne(product));
        }

        // 10. 결과 불변성 보장
        return List.copyOf(results);
    }

    private GachaDrawResult drawOne(ShopProductBalance product) {
        // 3. 연결된 GachaPool 조회
        String poolId = product.gachaPoolId();
        if (poolId == null || poolId.trim().isEmpty()) {
            throw new BusinessException(ErrorCode.GACHA_POOL_NOT_FOUND);
        }

        GachaPoolBalance pool = getGachaPool(poolId);
        if (pool == null) {
            throw new BusinessException(ErrorCode.GACHA_POOL_NOT_FOUND);
        }

        // 4. Pool active 확인
        if (!pool.active()) {
            throw new BusinessException(ErrorCode.GACHA_POOL_INACTIVE);
        }

        List<GachaGradeEntryBalance> gradeEntries = pool.gradeEntries();
        if (gradeEntries == null || gradeEntries.isEmpty()) {
            throw new IllegalStateException("GachaPool에 추첨 가능한 등급(gradeEntries)이 없습니다: " + poolId);
        }

        // 5. 등급 추첨
        // Validator에서 합계 10000을 보장하지만, 여기서는 누적 가중치로 처리
        int totalWeight = gradeEntries.stream().mapToInt(GachaGradeEntryBalance::weight).sum();
        if (totalWeight <= 0) {
            throw new IllegalStateException("총 가중치가 0 이하입니다: " + poolId);
        }

        int gradeRandomValue = randomGenerator.nextInt(totalWeight);
        int cumulative = 0;
        GachaGradeEntryBalance selectedEntry = null;

        for (GachaGradeEntryBalance entry : gradeEntries) {
            cumulative += entry.weight();
            if (gradeRandomValue < cumulative) {
                selectedEntry = entry;
                break;
            }
        }

        if (selectedEntry == null) {
            throw new IllegalStateException("등급을 선택하지 못했습니다. 난수: " + gradeRandomValue + ", 누적합: " + cumulative);
        }

        // 6. 등급 내 Alien 추첨
        List<Long> alienIds = selectedEntry.alienIds();
        if (alienIds == null || alienIds.isEmpty()) {
            throw new IllegalStateException("선택된 등급에 Alien ID가 존재하지 않습니다. 등급: " + selectedEntry.grade());
        }

        int alienIndex = randomGenerator.nextInt(alienIds.size());
        Long alienId = alienIds.get(alienIndex);

        // 7. 결과 반환
        return new GachaDrawResult(
                product.productId(),
                pool.poolId(),
                selectedEntry.grade(),
                alienId
        );
    }

    private ShopProductBalance getShopProduct(String productId) {
        try {
            return balanceRegistry.getShopProduct(productId);
        } catch (IllegalArgumentException e) {
            throw new BusinessException(ErrorCode.SHOP_PRODUCT_NOT_FOUND);
        }
    }

    private GachaPoolBalance getGachaPool(String poolId) {
        try {
            return balanceRegistry.getGachaPool(poolId);
        } catch (IllegalArgumentException e) {
            throw new BusinessException(ErrorCode.GACHA_POOL_NOT_FOUND);
        }
    }
}
