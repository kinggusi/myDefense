package com.denfense.server.service.balance;

import com.denfense.server.balance.MythicBreedingConfigBalance;
import com.denfense.server.balance.MythicBreedingResultBalance;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
public class MythicBreedingBalanceRegistry {
    private MythicBreedingConfigBalance config;
    private List<MythicBreedingResultBalance> results = List.of();
    private final Map<Long, MythicBreedingResultBalance> byAlienId = new ConcurrentHashMap<>();

    public synchronized void init(MythicBreedingConfigBalance config, List<MythicBreedingResultBalance> results) {
        if (this.config != null) throw new IllegalStateException("Mythic breeding balance already initialized");
        this.config = config;
        this.results = List.copyOf(results);
        results.forEach(r -> byAlienId.put(r.alienId(), r));
    }
    public MythicBreedingConfigBalance getConfig() { if (config == null) throw new IllegalStateException("Breeding balance not loaded"); return config; }
    public List<MythicBreedingResultBalance> getResults() { return results; }
}
