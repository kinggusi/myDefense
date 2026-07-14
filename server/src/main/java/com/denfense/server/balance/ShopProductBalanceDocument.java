package com.denfense.server.balance;

import java.util.List;

public record ShopProductBalanceDocument(List<ShopProductBalance> products) {
    public ShopProductBalanceDocument {
        if (products != null) {
            products = List.copyOf(products);
        }
    }
}