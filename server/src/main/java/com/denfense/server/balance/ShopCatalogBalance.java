package com.denfense.server.balance;

import java.util.List;

public record ShopCatalogBalance(List<ShopOfferBalance> offers, ShopPolicyBalance policy) {}
