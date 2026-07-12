package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonPropertyOrder({
        "alienId",
        "name",
        "description",
        "grade",
        "baseAttack",
        "baseMp",
        "attackSpeed",
        "attackRange",
        "evolutionTargetId",
        "isLocked"
})
public record AlienSpecBalance(
        long alienId,
        String name,
        String description,
        String grade,
        int baseAttack,
        int baseMp,
        double attackSpeed,
        double attackRange,
        Long evolutionTargetId,
        boolean isLocked
) {
}
