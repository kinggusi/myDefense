package com.denfense.server.balance;

import java.util.List;

public record MythicBreedingRecipeBalance(
        String recipeKey,
        long parentAlienIdA,
        long parentAlienIdB,
        List<Long> standardResultAlienIds,
        int standardWeightEach,
        long exclusive19AlienId,
        int exclusive19Weight,
        long exclusive20AlienId,
        int exclusive20Weight,
        boolean enabled
) {}
