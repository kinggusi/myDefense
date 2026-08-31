package com.denfense.server.service.balance;

import com.denfense.server.balance.MythicBreedingConfigBalance;
import com.denfense.server.balance.MythicBreedingResultBalance;
import com.denfense.server.balance.MythicBreedingRecipeBalance;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
public class MythicBreedingBalanceRegistry {
    private MythicBreedingConfigBalance config;
    private List<MythicBreedingResultBalance> results = List.of();
    private List<MythicBreedingRecipeBalance> recipes = List.of();
    private final Map<Long, MythicBreedingResultBalance> byAlienId = new ConcurrentHashMap<>();
    private final Map<String, MythicBreedingRecipeBalance> byParentPair = new ConcurrentHashMap<>();

    public synchronized void init(MythicBreedingConfigBalance config, List<MythicBreedingResultBalance> results,
                                  List<MythicBreedingRecipeBalance> recipes) {
        if (this.config != null) throw new IllegalStateException("Mythic breeding balance already initialized");
        this.config = config;
        this.results = List.copyOf(results);
        this.recipes = List.copyOf(recipes);
        results.forEach(r -> byAlienId.put(r.alienId(), r));
        recipes.forEach(r -> byParentPair.put(pairKey(r.parentAlienIdA(), r.parentAlienIdB()), r));
    }
    public MythicBreedingConfigBalance getConfig() { if (config == null) throw new IllegalStateException("Breeding balance not loaded"); return config; }
    public List<MythicBreedingResultBalance> getResults() { return results; }
    public List<MythicBreedingRecipeBalance> getRecipes() { return recipes; }
    public MythicBreedingRecipeBalance getRecipe(long parentAlienIdA, long parentAlienIdB) {
        MythicBreedingRecipeBalance recipe = byParentPair.get(pairKey(parentAlienIdA, parentAlienIdB));
        if (recipe == null || !recipe.enabled()) throw new IllegalStateException("Breeding recipe not found");
        return recipe;
    }
    private static String pairKey(long a, long b) { return Math.min(a, b) + ":" + Math.max(a, b); }
}
