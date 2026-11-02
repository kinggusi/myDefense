package com.denfense.server.service.balance;

import com.denfense.server.balance.*;
import com.fasterxml.jackson.databind.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.*;
import org.springframework.core.annotation.Order;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;
import java.util.*;

@Component @Order(2)
public class ShopBalanceRegistry implements ApplicationRunner {
    private final ResourceLoader resources;
    private final ObjectMapper mapper;
    private final BalanceRegistry balance;
    private ShopCatalogBalance data;
    @Value("${balance.shop-catalog.path:classpath:balance/generated/shop-catalog.json}") private String path;
    public ShopBalanceRegistry(ResourceLoader resources, ObjectMapper mapper, BalanceRegistry balance) {
        this.resources = resources; this.mapper = mapper; this.balance = balance;
    }
    @Override public void run(ApplicationArguments args) throws Exception {
        try (var in = resources.getResource(path).getInputStream()) {
            var loaded = mapper.copy().enable(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES).readValue(in, ShopCatalogBalance.class);
            ShopBalanceValidator.validate(loaded, balance.getAllShopProducts(), balance.getAllAlienSpecs());
            data = loaded;
        }
    }
    public ShopPolicyBalance policy() { return data.policy(); }
    public List<ShopOfferBalance> offers() {
        return data.offers().stream().filter(ShopOfferBalance::active).sorted(Comparator.comparingInt(ShopOfferBalance::displayOrder)).toList();
    }
    public ShopOfferBalance offer(String id) { return offers().stream().filter(o -> o.offerId().equals(id)).findFirst().orElse(null); }
}
