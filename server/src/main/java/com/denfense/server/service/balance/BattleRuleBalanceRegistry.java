package com.denfense.server.service.balance;

import com.denfense.server.balance.*;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class BattleRuleBalanceRegistry {
    private volatile State state;

    public synchronized void init(
            List<FieldLimitBalance> fieldLimits,
            List<SummonBalance> summons,
            List<MergeRuleBalance> mergeRules,
            List<MythicChoiceBalance> mythicChoices,
            List<SummonPoolBalance> summonPools,
            List<AlienSpecBalance> alienSpecs
    ) {
        if (state != null) throw new IllegalStateException("BattleRuleBalanceRegistry is already initialized.");
        Map<String, FieldLimitBalance> fieldMap = fieldLimits.stream()
                .collect(Collectors.toUnmodifiableMap(FieldLimitBalance::modeId, Function.identity()));
        Map<SummonKey, SummonBalance> summonMap = summons.stream()
                .collect(Collectors.toUnmodifiableMap(s -> new SummonKey(s.modeId(), s.summonType()), Function.identity()));
        Map<String, MergeRuleBalance> mergeMap = mergeRules.stream()
                .collect(Collectors.toUnmodifiableMap(MergeRuleBalance::sourceGrade, Function.identity()));
        Map<String, MythicChoiceBalance> choiceMap = mythicChoices.stream()
                .collect(Collectors.toUnmodifiableMap(MythicChoiceBalance::modeId, Function.identity()));
        Map<String, SummonPoolBalance> summonPoolMap = summonPools.stream()
                .collect(Collectors.toUnmodifiableMap(SummonPoolBalance::poolId, Function.identity()));
        List<Long> mythicIds = alienSpecs.stream()
                .filter(spec -> "MYTHIC".equals(spec.grade()))
                .map(AlienSpecBalance::alienId)
                .sorted()
                .toList();
        state = new State(fieldMap, summonMap, mergeMap, choiceMap, summonPoolMap, mythicIds);
    }

    public FieldLimitBalance getFieldLimit(String modeId) {
        FieldLimitBalance value = requireState().fieldLimits().get(modeId);
        if (value == null) throw new IllegalArgumentException("Unknown field limit modeId: " + modeId);
        return value;
    }

    public SummonBalance getSummonBalance(String modeId, String summonType) {
        SummonBalance value = requireState().summons().get(new SummonKey(modeId, summonType));
        if (value == null) throw new IllegalArgumentException("Unknown summon balance: " + modeId + "/" + summonType);
        return value;
    }

    public MergeRuleBalance getMergeRule(String sourceGrade) {
        MergeRuleBalance value = requireState().mergeRules().get(sourceGrade);
        if (value == null) throw new IllegalArgumentException("Unknown merge sourceGrade: " + sourceGrade);
        return value;
    }

    public MythicChoiceBalance getMythicChoiceBalance(String modeId) {
        MythicChoiceBalance value = requireState().mythicChoices().get(modeId);
        if (value == null) throw new IllegalArgumentException("Unknown MythicChoice modeId: " + modeId);
        return value;
    }

    public SummonPoolBalance getSummonPool(String poolId) {
        SummonPoolBalance value = requireState().summonPools().get(poolId);
        if (value == null) throw new IllegalArgumentException("Unknown summon pool: " + poolId);
        return value;
    }

    public List<Long> getEnabledMythicAlienIds() {
        return List.copyOf(requireState().mythicAlienIds());
    }

    private State requireState() {
        State current = state;
        if (current == null) throw new IllegalStateException("Battle rule balance is not initialized.");
        return current;
    }

    private record SummonKey(String modeId, String summonType) {
    }

    private record State(
            Map<String, FieldLimitBalance> fieldLimits,
            Map<SummonKey, SummonBalance> summons,
            Map<String, MergeRuleBalance> mergeRules,
            Map<String, MythicChoiceBalance> mythicChoices,
            Map<String, SummonPoolBalance> summonPools,
            List<Long> mythicAlienIds
    ) {
    }
}
