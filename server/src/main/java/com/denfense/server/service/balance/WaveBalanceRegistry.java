package com.denfense.server.service.balance;

import com.denfense.server.balance.WaveSpawnBalance;
import com.denfense.server.balance.WaveSpecBalance;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class WaveBalanceRegistry {
    private volatile State state;

    public synchronized void init(List<WaveSpecBalance> waves, List<WaveSpawnBalance> spawns) {
        if (state != null) throw new IllegalStateException("WaveBalanceRegistry is already initialized.");
        Map<WaveKey, WaveSpecBalance> waveMap = waves.stream().collect(Collectors.toUnmodifiableMap(
                wave -> new WaveKey(wave.modeId(), wave.wave()), wave -> wave));
        Map<String, List<WaveSpawnBalance>> spawnMap = spawns.stream().collect(Collectors.groupingBy(
                WaveSpawnBalance::spawnGroupId,
                Collectors.collectingAndThen(Collectors.toList(), list -> list.stream()
                        .sorted(Comparator.comparingInt(WaveSpawnBalance::order)).toList())));
        state = new State(waveMap, Map.copyOf(spawnMap));
    }

    public WaveSpecBalance getWave(String modeId, int wave) {
        WaveSpecBalance result = requireState().waves().get(new WaveKey(modeId, wave));
        if (result == null) throw new IllegalArgumentException("Unknown wave: " + modeId + "/" + wave);
        return result;
    }

    public List<WaveSpawnBalance> getSpawns(String spawnGroupId) {
        List<WaveSpawnBalance> result = requireState().spawns().get(spawnGroupId);
        if (result == null) throw new IllegalArgumentException("Unknown spawnGroupId: " + spawnGroupId);
        return List.copyOf(result);
    }

    private State requireState() {
        State current = state;
        if (current == null) throw new IllegalStateException("Wave balance is not initialized.");
        return current;
    }

    private record WaveKey(String modeId, int wave) {
    }

    private record State(Map<WaveKey, WaveSpecBalance> waves, Map<String, List<WaveSpawnBalance>> spawns) {
    }
}
