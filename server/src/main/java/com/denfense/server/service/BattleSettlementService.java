package com.denfense.server.service;

import com.denfense.server.balance.WaveSpawnBalance;
import com.denfense.server.domain.BattleResult;
import com.denfense.server.domain.BattleSettlement;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.MonsterBalanceRegistry;
import com.denfense.server.service.balance.PlanetBattleBalanceRegistry;
import com.denfense.server.service.balance.WaveBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Set;

@Service
@RequiredArgsConstructor
public class BattleSettlementService {
    private static final String MODE_ID = "COOP_STANDARD";
    private static final String EACH_FIELD = "EACH_FIELD";
    private static final String BOSS_SHARED = "BOSS_SHARED";

    private final BattleSettlementRepository settlements;
    private final UserRepository users;
    private final MonsterBalanceRegistry monsters;
    private final BalanceVersionRegistry versions;
    private final BalanceRegistry balances;
    private final BattleSettlementLookup lookup;
    private final BattleSettlementWriter writer;
    private final PlanetBattleBalanceRegistry planetBattles;
    private final BattleRewardGrantService rewardGrantService;
    private final BattleSessionRosterRegistry battleSessionRosters;
    private final WaveBalanceRegistry waveBalances;
    private final BattlePlanetEntryService battleEntries;

    public BattleSettlementDtos.Response settle(BattleSettlementDtos.Request request) {
        validateEnvelope(request);
        battleEntries.assertUsable(request.battleSessionId());

        BattleSettlement byRequest = settlements.findByRequestId(request.requestId()).orElse(null);
        if (byRequest != null) {
            if (!byRequest.getBattleSessionId().equals(request.battleSessionId())
                    || !byRequest.getSummaryHash().equals(request.summaryHash())
                    || !Objects.equals(byRequest.getMapId(), request.mapId())) {
                throw new BusinessException(ErrorCode.BATTLE_REQUEST_CONFLICT);
            }
            return response(byRequest, true, request);
        }

        BattleSettlement bySession = settlements.findByBattleSessionId(request.battleSessionId()).orElse(null);
        if (bySession != null) {
            if (!bySession.getSummaryHash().equals(request.summaryHash())
                    || !Objects.equals(bySession.getMapId(), request.mapId())) {
                throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);
            }
            return response(bySession, true, request);
        }

        validateNewSettlement(request);
        BattleSettlement settlement;
        try {
            BattleSettlementWriter.WriteResult write = writer.create(request);
            if (!write.created()) {
                validateRecoveredSettlement(write.settlement(), request);
                return response(write.settlement(), true, request);
            }
            settlement = write.settlement();
        } catch (DataIntegrityViolationException exception) {
            BattleSettlement winner = lookup.byRequest(request.requestId());
            if (winner != null) return response(winner, true, request);
            winner = lookup.bySession(request.battleSessionId());
            if (winner != null) {
                if (!winner.getSummaryHash().equals(request.summaryHash())
                        || !Objects.equals(winner.getMapId(), request.mapId())) {
                    throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);
                }
                return response(winner, true, request);
            }
            throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
        }
        return response(settlement, false, request);
    }

    private void validateRecoveredSettlement(
            BattleSettlement existing,
            BattleSettlementDtos.Request request
    ) {
        boolean payloadMatches = existing.getSummaryHash().equals(request.summaryHash())
                && Objects.equals(existing.getMapId(), request.mapId());
        if (existing.getRequestId().equals(request.requestId())) {
            if (!payloadMatches) throw new BusinessException(ErrorCode.BATTLE_REQUEST_CONFLICT);
            return;
        }
        if (!payloadMatches) throw new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT);
    }

    private BattleSettlementDtos.Response response(
            BattleSettlement settlement,
            boolean alreadyProcessed,
            BattleSettlementDtos.Request request
    ) {
        List<BattleSettlementDtos.Reward> rewards = rewardGrantService.grant(settlement, request);
        battleEntries.completeIfReserved(settlement.getBattleSessionId());
        return new BattleSettlementDtos.Response(
                settlement.getBattleSessionId(),
                settlement.getStatus().name(),
                alreadyProcessed,
                rewards);
    }

    private void validateEnvelope(BattleSettlementDtos.Request request) {
        if (request == null
                || blank(request.requestId())
                || blank(request.battleSessionId())
                || blank(request.balanceVersion())
                || blank(request.contentHash())
                || blank(request.summaryHash())
                || blank(request.mapId())
                || !validResult(request.result())
                || request.players() == null
                || request.players().size() != 2
                || request.monsterKills() == null
                || request.startedAt() == null
                || request.finishedAt() == null
                || request.startedAt().isAfter(request.finishedAt())
                || request.startedAt().getNano() != 0
                || request.finishedAt().getNano() != 0) {
            invalidSummary();
        }
        try {
            if (!request.summaryHash().equals(BattleSettlementSummaryHasher.compute(request))) invalidSummary();
        } catch (RuntimeException exception) {
            invalidSummary();
        }
    }

    private void validateNewSettlement(BattleSettlementDtos.Request request) {
        int maxWave = balances.getBattleRewardBalance().maxWave();
        if (request.finalWave() < 0
                || request.finalWave() > maxWave
                || ("VICTORY".equals(request.result()) && request.finalWave() != maxWave)) {
            invalidSummary();
        }
        if (!versions.getBalanceVersion().equals(request.balanceVersion())) {
            throw new BusinessException(ErrorCode.BATTLE_BALANCE_VERSION_MISMATCH);
        }
        if (!versions.getContentHash().equals(request.contentHash())) {
            throw new BusinessException(ErrorCode.BATTLE_CONTENT_HASH_MISMATCH);
        }

        BattleSessionRosterRegistry.Roster roster = battleSessionRosters.requireComplete(request.battleSessionId());
        if (!roster.mapId().equals(request.mapId())
                || !roster.balanceVersion().equals(request.balanceVersion())
                || !roster.contentHash().equalsIgnoreCase(request.contentHash())) {
            throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        }

        Map<Integer, BattleSettlementDtos.Player> playerBySlot = validatePlayers(request, roster);
        try {
            planetBattles.get(request.mapId());
        } catch (IllegalArgumentException exception) {
            invalidSummary();
        }

        Map<String, Integer> submittedKills = new HashMap<>();
        int monsterTotal = 0;
        int submittedBossKills = 0;
        Set<String> monsterIds = new HashSet<>();
        for (BattleSettlementDtos.Monster monster : request.monsterKills()) {
            if (monster == null
                    || blank(monster.monsterSpecId())
                    || !monsterIds.add(monster.monsterSpecId())
                    || monster.totalKills() < 0
                    || monster.bossKills() < 0
                    || monster.bossKills() > monster.totalKills()
                    || monster.totalKillGold() < 0) {
                invalidSummary();
            }
            try {
                var spec = monsters.getById(monster.monsterSpecId());
                if (!spec.enabled()) throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);
                if (monster.totalKillGold() != spec.killGold() * monster.totalKills()) invalidSummary();
            } catch (IllegalArgumentException exception) {
                throw new BusinessException(ErrorCode.BATTLE_UNKNOWN_MONSTER);
            }
            monsterTotal += monster.totalKills();
            submittedBossKills += monster.bossKills();
            submittedKills.put(monster.monsterSpecId(), monster.totalKills());
        }

        Map<String, Integer> expectedKills = expectedKillsThrough(request.finalWave(), playerBySlot);
        PartialTotals partialTotals = validatePartialWaveKills(request, playerBySlot);
        partialTotals.byMonster().forEach((monsterId, count) -> expectedKills.merge(monsterId, count, Integer::sum));
        validatePartialAttributionLowerBounds(request.players(), partialTotals);

        int playerKills = request.players().stream().mapToInt(BattleSettlementDtos.Player::kills).sum();
        int playerBossKills = request.players().stream().mapToInt(BattleSettlementDtos.Player::bossKills).sum();
        int expectedBossKills = expectedBossKillsThrough(request.finalWave()) + partialTotals.bossKills();
        if (playerKills != monsterTotal
                || !submittedKills.equals(expectedKills)
                || submittedBossKills != expectedBossKills
                || playerBossKills != expectedBossKills) {
            invalidSummary();
        }
    }

    private Map<Integer, BattleSettlementDtos.Player> validatePlayers(
            BattleSettlementDtos.Request request,
            BattleSessionRosterRegistry.Roster roster
    ) {
        Set<String> ids = new HashSet<>();
        Set<Integer> slots = new HashSet<>();
        Map<Integer, BattleSettlementDtos.Player> bySlot = new HashMap<>();
        int maxEliminatedWave = "DEFEAT".equals(request.result())
                ? Math.min(balances.getBattleRewardBalance().maxWave(), request.finalWave() + 1)
                : request.finalWave();
        for (BattleSettlementDtos.Player player : request.players()) {
            if (player == null
                    || blank(player.playerId())
                    || !ids.add(player.playerId())
                    || !slots.add(player.playerSlot())
                    || player.playerSlot() < 1
                    || player.playerSlot() > 2
                    || users.findByUsername(player.playerId()).isEmpty()
                    || player.kills() < 0
                    || player.supportKills() < 0
                    || player.bossKills() < 0
                    || player.initialInGameGold() < 0
                    || player.inGameGoldEarned() < 0
                    || player.inGameGoldSpent() < 0
                    || player.finalInGameGold() < 0
                    || (long) player.initialInGameGold() + player.inGameGoldEarned() - player.inGameGoldSpent()
                    != player.finalInGameGold()
                    || (player.eliminated() && player.eliminatedWave() == null)
                    || (!player.eliminated() && player.eliminatedWave() != null)
                    || (player.eliminatedWave() != null
                    && (player.eliminatedWave() <= 0 || player.eliminatedWave() > maxEliminatedWave))) {
                invalidSummary();
            }
            bySlot.put(player.playerSlot(), player);
        }
        if (!slots.equals(Set.of(1, 2))) invalidSummary();
        for (BattleSessionRosterRegistry.Player authorized : roster.players()) {
            BattleSettlementDtos.Player submitted = bySlot.get(authorized.playerSlot());
            if (submitted == null || !authorized.playerId().equals(submitted.playerId())) {
                throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
            }
        }
        return bySlot;
    }

    private PartialTotals validatePartialWaveKills(
            BattleSettlementDtos.Request request,
            Map<Integer, BattleSettlementDtos.Player> playerBySlot
    ) {
        Map<String, BattleSettlementDtos.WaveSpawnFact> factsByRuntimeId =
                validateWaveSpawnFacts(request, playerBySlot);
        List<BattleSettlementDtos.PartialWaveKill> kills = request.partialWaveKills();
        if (kills.isEmpty()) return PartialTotals.empty();
        if (!"DEFEAT".equals(request.result())
                || request.finalWave() >= balances.getBattleRewardBalance().maxWave()) {
            invalidSummary();
        }

        int partialWave = request.finalWave() + 1;
        Set<String> runtimeIds = new HashSet<>();
        Map<String, Integer> byMonster = new HashMap<>();
        Map<Integer, Integer> byKiller = new HashMap<>();
        Map<Integer, Integer> bySupport = new HashMap<>();
        Map<Integer, Integer> bossByKiller = new HashMap<>();
        long previousRuntimeId = 0;
        boolean hasPreviousRuntimeId = false;
        int bossKills = 0;

        for (BattleSettlementDtos.PartialWaveKill kill : kills) {
            if (kill == null
                    || blank(kill.runtimeMonsterId())
                    || kill.spawnWave() != partialWave
                    || blank(kill.spawnGroupId())
                    || blank(kill.monsterSpecId())
                    || blank(kill.lanePolicy())
                    || kill.spawnOrder() < 1
                    || kill.spawnOrdinal() < 1
                    || !playerBySlot.containsKey(kill.killerPlayerSlot())
                    || !isLaneActiveAtWave(playerBySlot.get(kill.killerPlayerSlot()), partialWave)
                    || (kill.supportPlayerSlot() != null
                    && (!playerBySlot.containsKey(kill.supportPlayerSlot())
                    || !isLaneActiveAtWave(playerBySlot.get(kill.supportPlayerSlot()), partialWave)
                    || kill.supportPlayerSlot() == kill.killerPlayerSlot()))) {
                invalidSummary();
            }

            long runtimeId;
            try {
                runtimeId = Long.parseUnsignedLong(kill.runtimeMonsterId());
            } catch (NumberFormatException exception) {
                invalidSummary();
                return null;
            }
            if (runtimeId == 0
                    || !Long.toUnsignedString(runtimeId).equals(kill.runtimeMonsterId())
                    || !runtimeIds.add(kill.runtimeMonsterId())
                    || (hasPreviousRuntimeId && Long.compareUnsigned(previousRuntimeId, runtimeId) >= 0)) {
                invalidSummary();
            }
            previousRuntimeId = runtimeId;
            hasPreviousRuntimeId = true;

            BattleSettlementDtos.WaveSpawnFact fact = factsByRuntimeId.get(kill.runtimeMonsterId());
            if (fact == null
                    || fact.spawnWave() != kill.spawnWave()
                    || !fact.spawnGroupId().equals(kill.spawnGroupId())
                    || !fact.monsterSpecId().equals(kill.monsterSpecId())
                    || !fact.lanePolicy().equals(kill.lanePolicy())
                    || !Objects.equals(fact.fieldOwnerPlayerSlot(), kill.fieldOwnerPlayerSlot())
                    || fact.spawnOrder() != kill.spawnOrder()
                    || fact.spawnOrdinal() != kill.spawnOrdinal()) {
                invalidSummary();
            }

            if (BOSS_SHARED.equals(kill.lanePolicy())) {
                bossKills++;
            }
            byMonster.merge(kill.monsterSpecId(), 1, Integer::sum);
            byKiller.merge(kill.killerPlayerSlot(), 1, Integer::sum);
            if (kill.supportPlayerSlot() != null) bySupport.merge(kill.supportPlayerSlot(), 1, Integer::sum);
            if (BOSS_SHARED.equals(kill.lanePolicy())) bossByKiller.merge(kill.killerPlayerSlot(), 1, Integer::sum);
        }
        return new PartialTotals(byMonster, byKiller, bySupport, bossByKiller, bossKills);
    }

    private Map<String, BattleSettlementDtos.WaveSpawnFact> validateWaveSpawnFacts(
            BattleSettlementDtos.Request request,
            Map<Integer, BattleSettlementDtos.Player> playerBySlot
    ) {
        List<BattleSettlementDtos.WaveSpawnFact> facts = request.waveSpawnFacts();
        if (facts.isEmpty()) {
            if (!request.partialWaveKills().isEmpty()) invalidSummary();
            return Map.of();
        }
        if (!"DEFEAT".equals(request.result())
                || request.finalWave() >= balances.getBattleRewardBalance().maxWave()) {
            invalidSummary();
        }

        int partialWave = request.finalWave() + 1;
        var wave = waveBalances.getWave(MODE_ID, partialWave);
        List<WaveSpawnBalance> spawns = waveBalances.getSpawns(wave.spawnGroupId());
        Map<Integer, WaveSpawnBalance> spawnByOrder = new HashMap<>();
        for (WaveSpawnBalance spawn : spawns) spawnByOrder.put(spawn.order(), spawn);

        Map<String, BattleSettlementDtos.WaveSpawnFact> byRuntimeId = new HashMap<>();
        Set<String> spawnPositions = new HashSet<>();
        long previousRuntimeId = 0;
        boolean hasPreviousRuntimeId = false;
        for (BattleSettlementDtos.WaveSpawnFact fact : facts) {
            if (fact == null
                    || blank(fact.runtimeMonsterId())
                    || fact.spawnWave() != partialWave
                    || !wave.spawnGroupId().equals(fact.spawnGroupId())
                    || blank(fact.monsterSpecId())
                    || blank(fact.lanePolicy())
                    || fact.spawnOrder() < 1
                    || fact.spawnOrdinal() < 1) {
                invalidSummary();
            }

            long runtimeId;
            try {
                runtimeId = Long.parseUnsignedLong(fact.runtimeMonsterId());
            } catch (NumberFormatException exception) {
                invalidSummary();
                return null;
            }
            if (runtimeId == 0
                    || !Long.toUnsignedString(runtimeId).equals(fact.runtimeMonsterId())
                    || byRuntimeId.putIfAbsent(fact.runtimeMonsterId(), fact) != null
                    || (hasPreviousRuntimeId && Long.compareUnsigned(previousRuntimeId, runtimeId) >= 0)) {
                invalidSummary();
            }
            previousRuntimeId = runtimeId;
            hasPreviousRuntimeId = true;

            WaveSpawnBalance spawn = spawnByOrder.get(fact.spawnOrder());
            if (spawn == null
                    || !spawn.monsterId().equals(fact.monsterSpecId())
                    || !spawn.lanePolicy().equals(fact.lanePolicy())
                    || fact.spawnOrdinal() > spawn.spawnCountPerField()) {
                invalidSummary();
            }

            String positionKey;
            if (EACH_FIELD.equals(fact.lanePolicy())) {
                if (fact.fieldOwnerPlayerSlot() == null
                        || !playerBySlot.containsKey(fact.fieldOwnerPlayerSlot())
                        || !isLaneActiveAtWave(playerBySlot.get(fact.fieldOwnerPlayerSlot()), partialWave)) {
                    invalidSummary();
                }
                positionKey = fact.spawnOrder() + ":" + fact.fieldOwnerPlayerSlot() + ":" + fact.spawnOrdinal();
            } else if (BOSS_SHARED.equals(fact.lanePolicy())) {
                if (fact.fieldOwnerPlayerSlot() != null) invalidSummary();
                positionKey = fact.spawnOrder() + ":shared:" + fact.spawnOrdinal();
            } else {
                invalidSummary();
                return null;
            }
            if (!spawnPositions.add(positionKey)) invalidSummary();
        }
        return byRuntimeId;
    }

    private void validatePartialAttributionLowerBounds(
            List<BattleSettlementDtos.Player> players,
            PartialTotals partialTotals
    ) {
        for (BattleSettlementDtos.Player player : players) {
            if (player.kills() < partialTotals.byKiller().getOrDefault(player.playerSlot(), 0)
                    || player.supportKills() < partialTotals.bySupport().getOrDefault(player.playerSlot(), 0)
                    || player.bossKills() < partialTotals.bossByKiller().getOrDefault(player.playerSlot(), 0)) {
                invalidSummary();
            }
        }
    }

    private Map<String, Integer> expectedKillsThrough(
            int finalWave,
            Map<Integer, BattleSettlementDtos.Player> playerBySlot
    ) {
        Map<String, Integer> result = new HashMap<>();
        for (int waveNumber = 1; waveNumber <= finalWave; waveNumber++) {
            var wave = waveBalances.getWave(MODE_ID, waveNumber);
            if (!wave.enabled()) invalidSummary();
            for (WaveSpawnBalance spawn : waveBalances.getSpawns(wave.spawnGroupId())) {
                int lanes;
                if (EACH_FIELD.equals(spawn.lanePolicy())) {
                    lanes = 0;
                    for (BattleSettlementDtos.Player player : playerBySlot.values()) {
                        if (isLaneActiveAtWave(player, waveNumber)) lanes++;
                    }
                } else if (BOSS_SHARED.equals(spawn.lanePolicy())) {
                    lanes = 1;
                } else {
                    invalidSummary();
                    return null;
                }
                result.merge(spawn.monsterId(), spawn.spawnCountPerField() * lanes, Integer::sum);
            }
        }
        return result;
    }

    private int expectedBossKillsThrough(int finalWave) {
        int result = 0;
        for (int waveNumber = 1; waveNumber <= finalWave; waveNumber++) {
            var wave = waveBalances.getWave(MODE_ID, waveNumber);
            for (WaveSpawnBalance spawn : waveBalances.getSpawns(wave.spawnGroupId())) {
                if (BOSS_SHARED.equals(spawn.lanePolicy())) result += spawn.spawnCountPerField();
            }
        }
        return result;
    }

    private boolean isLaneActiveAtWave(BattleSettlementDtos.Player player, int wave) {
        return player.eliminatedWave() == null || wave <= player.eliminatedWave();
    }

    private boolean blank(String value) {
        return value == null || value.trim().isEmpty();
    }

    private boolean validResult(String result) {
        try {
            BattleResult.valueOf(result);
            return true;
        } catch (IllegalArgumentException | NullPointerException exception) {
            return false;
        }
    }

    private void invalidSummary() {
        throw new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID);
    }

    private record PartialTotals(
            Map<String, Integer> byMonster,
            Map<Integer, Integer> byKiller,
            Map<Integer, Integer> bySupport,
            Map<Integer, Integer> bossByKiller,
            int bossKills
    ) {
        private static PartialTotals empty() {
            return new PartialTotals(Map.of(), Map.of(), Map.of(), Map.of(), 0);
        }
    }
}
