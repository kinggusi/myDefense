package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({"monsterId", "name", "monsterType", "baseHp", "moveSpeed", "killGold", "enabled"})
public record MonsterSpecBalance(
        String monsterId,
        String name,
        String monsterType,
        BigDecimal baseHp,
        BigDecimal moveSpeed,
        int killGold,
        boolean enabled
) {
}
