package com.denfense.server.balance.tool.battle;

import java.util.List;

public final class BattleBalanceData {
    private BattleBalanceData() {}

    public record Data(
            List<WaveSpec> waves,
            List<WaveSpawnSpec> spawns,
            List<BossPatternSpec> bossPatterns,
            List<SkillSpec> skills,
            List<AlienSkillLink> alienSkillLinks,
            List<ProjectileSpec> projectiles,
            List<SkillEffectSpec> skillEffects) {}

    public record WaveSpec(
            String waveId,
            int roundNumber,
            String waveType,
            double nextWaveDelaySeconds,
            double bossTimeLimitSeconds,
            boolean enabled) {}

    public record WaveSpawnSpec(
            String waveId,
            int spawnOrder,
            String lanePolicy,
            String monsterId,
            int spawnCount,
            double spawnDelaySeconds,
            double spawnIntervalSeconds,
            double hpMultiplier,
            double moveSpeedMultiplier) {}

    public record BossPatternSpec(
            String waveId,
            int patternOrder,
            String patternType,
            String triggerType,
            double triggerValue,
            double cooldownSeconds,
            String skillId,
            String parameterKey,
            double parameterValue,
            boolean enabled) {}

    public record SkillSpec(
            String skillId,
            String nameKey,
            String descriptionKey,
            String skillType,
            String triggerType,
            double cooldownSeconds,
            double mpCost,
            double castRange,
            String targetPolicy,
            int maxTargetCount,
            String projectileId,
            String animationKey,
            String vfxKey,
            String sfxKey,
            boolean enabled) {}

    public record AlienSkillLink(
            long alienId,
            String skillId,
            int slotIndex,
            int castPriority,
            boolean enabled) {}

    public record ProjectileSpec(
            String projectileId,
            String prefabKey,
            String moveType,
            double speed,
            double lifetimeSeconds,
            double hitRadius,
            int pierceCount,
            boolean destroyOnHit,
            String lostTargetPolicy,
            boolean enabled) {}

    public record SkillEffectSpec(
            String skillId,
            int executionOrder,
            String triggerPhase,
            String effectType,
            String magnitudeSource,
            double baseMagnitude,
            double coefficient,
            double chance,
            double durationSeconds,
            double tickIntervalSeconds,
            double radius,
            int maxStacks,
            String stackPolicy,
            double bossMultiplier) {}
}
