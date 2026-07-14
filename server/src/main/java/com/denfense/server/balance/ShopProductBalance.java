package com.denfense.server.balance;

public record ShopProductBalance(
        String productId,
        String name,
        String currencyType,
        int price,
        int drawCount,
        String gachaPoolId,
        boolean active
) {
}