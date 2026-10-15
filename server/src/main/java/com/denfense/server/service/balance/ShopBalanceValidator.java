package com.denfense.server.service.balance;

import com.denfense.server.balance.*;
import java.util.*;

public final class ShopBalanceValidator {
    private ShopBalanceValidator() {}
    public static void validate(ShopCatalogBalance data, List<ShopProductBalance> products, List<AlienSpecBalance> aliens) {
        if (data == null || data.policy() == null || data.offers() == null || data.offers().isEmpty()) fail();
        var p = data.policy();
        if (!"DEFAULT".equals(p.policyId()) || !"Asia/Seoul".equals(p.resetZone())
                || p.luckyWeight() < 0 || p.luckyTotalWeight() <= 0 || p.luckyWeight() > p.luckyTotalWeight()
                || p.dailyAdRerolls() < 0 || p.weeklyDiscountCount() < 0 || p.discountPrice() <= 0
                || p.adChallengeTtlSeconds() < 30 || p.adChallengeTtlSeconds() > 3600) fail();
        Set<String> ids = new HashSet<>();
        Set<String> dailyGrades = new HashSet<>();
        Map<String, String> groups = new HashMap<>();
        for (var o : data.offers()) {
            if (o == null || o.offerId() == null || !o.offerId().matches("[A-Z][A-Z0-9_]{0,63}") || !ids.add(o.offerId())
                    || !Set.of("GACHA", "RESOURCE", "DAILY", "LUCKY").contains(o.category())
                    || !Set.of("GOLD", "DIAMOND").contains(o.currencyType())
                    || !Set.of("GACHA", "GOLD", "DNA", "GROWTH_CELL", "ALIEN_PIECE").contains(o.rewardType())
                    || !Set.of("NONE", "DAY", "WEEK").contains(o.limitPeriod())
                    || o.price() <= 0 || o.rewardAmount() <= 0 || o.limitCount() < 0
                    || o.grade() == null || o.gachaProductId() == null || o.limitGroup() == null) fail();
            if ("NONE".equals(o.limitPeriod())) {
                if (o.limitCount() != 0 || !o.limitGroup().isEmpty()) fail();
            } else {
                if (o.limitCount() <= 0 || o.limitGroup().isBlank()) fail();
                String shape = o.limitPeriod() + ":" + o.limitCount();
                if (groups.putIfAbsent(o.limitGroup(), shape) != null && !groups.get(o.limitGroup()).equals(shape)) fail();
            }
            if ("GACHA".equals(o.category())) {
                var product = products.stream().filter(x -> x.productId().equals(o.gachaProductId())).findFirst().orElse(null);
                if (!"GACHA".equals(o.rewardType()) || !"DIAMOND".equals(o.currencyType()) || product == null
                        || product.price() != o.price() || product.drawCount() != o.rewardAmount() || !o.grade().isEmpty()
                        || (o.rewardAmount() != 1 && o.rewardAmount() != 10)) fail();
            } else if (!o.gachaProductId().isEmpty() || "GACHA".equals(o.rewardType())) fail();
            if ("DAILY".equals(o.category())) {
                if (!Set.of("NORMAL", "EPIC", "UNIQUE", "LEGEND").contains(o.grade()) || !dailyGrades.add(o.grade())
                        || !"ALIEN_PIECE".equals(o.rewardType()) || !"GOLD".equals(o.currencyType())
                        || !"DAY".equals(o.limitPeriod()) || o.limitCount() != 1
                        || aliens.stream().filter(a -> a.grade().equals(o.grade())).count() < 2) fail();
            } else if (!o.grade().isEmpty() || "ALIEN_PIECE".equals(o.rewardType())) fail();
            if ("LUCKY".equals(o.category()) && (!"GOLD".equals(o.currencyType())
                    || !Set.of("DNA", "GROWTH_CELL").contains(o.rewardType())
                    || !"WEEK".equals(o.limitPeriod()) || !"LUCKY".equals(o.limitGroup()))) fail();
            if ("RESOURCE".equals(o.category()) && (!"DIAMOND".equals(o.currencyType())
                    || !Set.of("GOLD", "DNA", "GROWTH_CELL").contains(o.rewardType()))) fail();
        }
        if (dailyGrades.size() != 4 || data.offers().stream().filter(o -> o.active() && "LUCKY".equals(o.category())).count() != 2) fail();
        var discount = data.offers().stream().filter(o -> o.offerId().equals(p.discountOfferId())).findFirst().orElse(null);
        if (discount == null || !"GACHA".equals(discount.category()) || discount.rewardAmount() != 10
                || p.discountPrice() >= discount.price()) fail();
    }
    private static void fail() { throw new IllegalStateException("Invalid canonical ShopOffer/ShopPolicy balance."); }
}
