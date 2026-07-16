package com.denfense.server.balance.tool.battle;

import com.denfense.server.balance.tool.BalanceConversionException;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import static com.denfense.server.balance.tool.battle.BattleBalanceData.*;

public final class BattleBalanceValidator {
    private static final Set<String> WAVE_TYPES = Set.of("REGULAR", "BOSS");
    private static final Set<String> LANE_POLICIES = Set.of("EACH_ACTIVE_PLAYER_LANE", "BOSS_SHARED");
    private static final Set<String> BOSS_PATTERN_TYPES = Set.of("CAST_SKILL", "WAIT", "SET_PHASE", "SET_MOVE_SPEED_MULTIPLIER");
    private static final Set<String> BOSS_TRIGGER_TYPES = Set.of("TIME", "HP_PERCENT", "LOOP", "ON_SPAWN", "ON_DEATH");
    private static final Set<String> SKILL_TYPES = Set.of("BASIC_ATTACK", "ACTIVE", "PASSIVE", "BOSS");
    private static final Set<String> SKILL_TRIGGER_TYPES = Set.of("BASIC_ATTACK", "AUTO_COOLDOWN", "MP_THRESHOLD", "PASSIVE", "BOSS_PATTERN");
    private static final Set<String> TARGET_POLICIES = Set.of("DEFAULT_PROGRESS", "HIGHEST_HP", "LOWEST_HP", "BOSS_FIRST", "DENSEST_AREA", "MOST_CROWDED_LANE", "SELF");
    private static final Set<String> PROJECTILE_MOVE_TYPES = Set.of("HOMING", "LINEAR", "BALLISTIC", "INSTANT");
    private static final Set<String> LOST_TARGET_POLICIES = Set.of("DESTROY", "CONTINUE_LAST_DIRECTION", "RETARGET");
    private static final Set<String> EFFECT_TRIGGER_PHASES = Set.of("ON_CAST", "ON_HIT", "ON_KILL", "ON_EXPIRE");
    private static final Set<String> EFFECT_TYPES = Set.of("DAMAGE", "SPLASH_DAMAGE", "DAMAGE_OVER_TIME", "SLOW", "STUN", "ATTACK_SPEED_BUFF", "ATTACK_DAMAGE_BUFF", "RANGE_BUFF");
    private static final Set<String> MAGNITUDE_SOURCES = Set.of("FLAT", "ATTACK_SNAPSHOT_DAMAGE");
    private static final Set<String> STACK_POLICIES = Set.of("NONE", "ADD", "MULTIPLY", "REPLACE", "MAX_ONLY");

    public void validate(Data data) {
        List<String> errors = new ArrayList<>();
        Map<String, WaveSpec> waves = validateWaves(data.waves(), errors);
        Map<String, ProjectileSpec> projectiles = uniqueByProjectileId(data.projectiles(), errors);
        Map<String, SkillSpec> skills = validateSkills(data.skills(), projectiles, errors);
        validateSpawns(data.spawns(), waves, errors);
        validateBossPatterns(data.bossPatterns(), waves, skills, errors);
        validateAlienSkillLinks(data.alienSkillLinks(), skills, errors);
        validateProjectiles(data.projectiles(), errors);
        validateSkillEffects(data.skillEffects(), skills, errors);
        if (!errors.isEmpty())
            throw new BalanceConversionException("Battle balance validation failed:\n - " + String.join("\n - ", errors));
    }

    private static Map<String, WaveSpec> validateWaves(List<WaveSpec> values, List<String> errors) {
        Map<String, WaveSpec> result = new HashMap<>();
        Set<Integer> enabledRounds = new HashSet<>();
        for (WaveSpec wave : values) {
            if (result.putIfAbsent(wave.waveId(), wave) != null) errors.add("duplicate waveId: " + wave.waveId());
            enumValue("WaveSpec.waveType", wave.waveType(), WAVE_TYPES, errors);
            if (wave.roundNumber() < 1) errors.add(wave.waveId() + " roundNumber must be >= 1");
            if (wave.nextWaveDelaySeconds() < 0) errors.add(wave.waveId() + " nextWaveDelaySeconds must be >= 0");
            if ("REGULAR".equals(wave.waveType()) && wave.bossTimeLimitSeconds() != 0)
                errors.add(wave.waveId() + " REGULAR bossTimeLimitSeconds must be 0");
            if ("BOSS".equals(wave.waveType()) && wave.bossTimeLimitSeconds() <= 0)
                errors.add(wave.waveId() + " BOSS bossTimeLimitSeconds must be > 0");
            if (wave.enabled() && !enabledRounds.add(wave.roundNumber()))
                errors.add("duplicate enabled roundNumber: " + wave.roundNumber());
        }
        return result;
    }

    private static void validateSpawns(List<WaveSpawnSpec> values, Map<String, WaveSpec> waves, List<String> errors) {
        Set<String> keys = new HashSet<>();
        Map<String, Integer> rowCounts = new HashMap<>();
        Map<String, Integer> bossSpawnCounts = new HashMap<>();
        for (WaveSpawnSpec spawn : values) {
            String key = spawn.waveId() + ":" + spawn.spawnOrder();
            if (!keys.add(key)) errors.add("duplicate WaveSpawnSpec key: " + key);
            if (spawn.spawnOrder() < 1) errors.add(key + " spawnOrder must be >= 1");
            if (spawn.monsterId().isBlank()) errors.add(key + " monsterId must not be blank");
            if (spawn.spawnCount() < 1) errors.add(key + " spawnCount must be >= 1");
            if (spawn.spawnDelaySeconds() < 0 || spawn.spawnIntervalSeconds() < 0)
                errors.add(key + " spawn time values must be >= 0");
            if (spawn.hpMultiplier() <= 0 || spawn.moveSpeedMultiplier() <= 0)
                errors.add(key + " multipliers must be > 0");
            enumValue("WaveSpawnSpec.lanePolicy", spawn.lanePolicy(), LANE_POLICIES, errors);

            WaveSpec wave = waves.get(spawn.waveId());
            if (wave == null) {
                errors.add(key + " references unknown waveId");
                continue;
            }
            rowCounts.merge(wave.waveId(), 1, Integer::sum);
            if ("REGULAR".equals(wave.waveType()) && !"EACH_ACTIVE_PLAYER_LANE".equals(spawn.lanePolicy()))
                errors.add(key + " REGULAR must use EACH_ACTIVE_PLAYER_LANE");
            if ("BOSS".equals(wave.waveType()) && !"BOSS_SHARED".equals(spawn.lanePolicy()))
                errors.add(key + " BOSS must use BOSS_SHARED");
            if ("BOSS".equals(wave.waveType())) bossSpawnCounts.merge(wave.waveId(), spawn.spawnCount(), Integer::sum);
        }
        for (WaveSpec wave : waves.values()) {
            if ("BOSS".equals(wave.waveType()) && bossSpawnCounts.getOrDefault(wave.waveId(), 0) != 1)
                errors.add(wave.waveId() + " BOSS total spawnCount must be exactly 1");
            if (wave.enabled() && "REGULAR".equals(wave.waveType()) && rowCounts.getOrDefault(wave.waveId(), 0) < 1)
                errors.add(wave.waveId() + " enabled REGULAR requires at least one spawn row");
        }
    }

    private static void validateBossPatterns(List<BossPatternSpec> values, Map<String, WaveSpec> waves, Map<String, SkillSpec> skills, List<String> errors) {
        Set<String> keys = new HashSet<>();
        for (BossPatternSpec pattern : values) {
            String key = pattern.waveId() + ":" + pattern.patternOrder();
            if (!keys.add(key)) errors.add("duplicate BossPatternSpec key: " + key);
            if (pattern.patternOrder() < 1) errors.add(key + " patternOrder must be >= 1");
            enumValue("BossPatternSpec.patternType", pattern.patternType(), BOSS_PATTERN_TYPES, errors);
            enumValue("BossPatternSpec.triggerType", pattern.triggerType(), BOSS_TRIGGER_TYPES, errors);
            if (pattern.triggerValue() < 0 || pattern.cooldownSeconds() < 0)
                errors.add(key + " triggerValue/cooldownSeconds must be >= 0");
            WaveSpec wave = waves.get(pattern.waveId());
            if (wave == null || !"BOSS".equals(wave.waveType())) errors.add(key + " must reference a BOSS wave");
            if ("CAST_SKILL".equals(pattern.patternType()) && !skills.containsKey(pattern.skillId()))
                errors.add(key + " CAST_SKILL references unknown skillId");
        }
    }

    private static Map<String, SkillSpec> validateSkills(List<SkillSpec> values, Map<String, ProjectileSpec> projectiles, List<String> errors) {
        Map<String, SkillSpec> result = new HashMap<>();
        for (SkillSpec skill : values) {
            if (result.putIfAbsent(skill.skillId(), skill) != null) errors.add("duplicate skillId: " + skill.skillId());
            if (skill.nameKey().isBlank()) errors.add(skill.skillId() + " nameKey must not be blank");
            enumValue("SkillSpec.skillType", skill.skillType(), SKILL_TYPES, errors);
            enumValue("SkillSpec.triggerType", skill.triggerType(), SKILL_TRIGGER_TYPES, errors);
            enumValue("SkillSpec.targetPolicy", skill.targetPolicy(), TARGET_POLICIES, errors);
            if (skill.cooldownSeconds() < 0 || skill.mpCost() < 0 || skill.castRange() < 0)
                errors.add(skill.skillId() + " cooldown/mp/range must be >= 0");
            if (skill.maxTargetCount() < 0) errors.add(skill.skillId() + " maxTargetCount must be >= 0");
            if (!skill.projectileId().isBlank() && !projectiles.containsKey(skill.projectileId()))
                errors.add(skill.skillId() + " references unknown projectileId");
        }
        return result;
    }

    private static void validateAlienSkillLinks(List<AlienSkillLink> values, Map<String, SkillSpec> skills, List<String> errors) {
        Set<String> keys = new HashSet<>();
        for (AlienSkillLink link : values) {
            String key = link.alienId() + ":" + link.slotIndex();
            if (!keys.add(key)) errors.add("duplicate AlienSkillLink key: " + key);
            if (link.alienId() <= 0) errors.add(key + " alienId must be > 0");
            if (link.slotIndex() < 0 || link.castPriority() < 0) errors.add(key + " slotIndex/castPriority must be >= 0");
            if (!skills.containsKey(link.skillId())) errors.add(key + " references unknown skillId");
        }
    }

    private static Map<String, ProjectileSpec> uniqueByProjectileId(List<ProjectileSpec> values, List<String> errors) {
        Map<String, ProjectileSpec> result = new HashMap<>();
        for (ProjectileSpec projectile : values) {
            if (result.putIfAbsent(projectile.projectileId(), projectile) != null)
                errors.add("duplicate projectileId: " + projectile.projectileId());
        }
        return result;
    }

    private static void validateProjectiles(List<ProjectileSpec> values, List<String> errors) {
        for (ProjectileSpec projectile : values) {
            if (projectile.prefabKey().isBlank()) errors.add(projectile.projectileId() + " prefabKey must not be blank");
            enumValue("ProjectileSpec.moveType", projectile.moveType(), PROJECTILE_MOVE_TYPES, errors);
            enumValue("ProjectileSpec.lostTargetPolicy", projectile.lostTargetPolicy(), LOST_TARGET_POLICIES, errors);
            if (projectile.speed() < 0 || (!"INSTANT".equals(projectile.moveType()) && projectile.speed() == 0))
                errors.add(projectile.projectileId() + " speed is invalid for moveType " + projectile.moveType());
            if (projectile.lifetimeSeconds() <= 0 || projectile.hitRadius() < 0 || projectile.pierceCount() < 0)
                errors.add(projectile.projectileId() + " projectile values are out of range");
            if (projectile.destroyOnHit() && projectile.pierceCount() != 0)
                errors.add(projectile.projectileId() + " destroyOnHit requires pierceCount=0");
        }
    }

    private static void validateSkillEffects(List<SkillEffectSpec> values, Map<String, SkillSpec> skills, List<String> errors) {
        Set<String> keys = new HashSet<>();
        for (SkillEffectSpec effect : values) {
            String key = effect.skillId() + ":" + effect.executionOrder();
            if (!keys.add(key)) errors.add("duplicate SkillEffectSpec key: " + key);
            if (!skills.containsKey(effect.skillId())) errors.add(key + " references unknown skillId");
            if (effect.executionOrder() < 1) errors.add(key + " executionOrder must be >= 1");
            enumValue("SkillEffectSpec.triggerPhase", effect.triggerPhase(), EFFECT_TRIGGER_PHASES, errors);
            enumValue("SkillEffectSpec.effectType", effect.effectType(), EFFECT_TYPES, errors);
            enumValue("SkillEffectSpec.magnitudeSource", effect.magnitudeSource(), MAGNITUDE_SOURCES, errors);
            enumValue("SkillEffectSpec.stackPolicy", effect.stackPolicy(), STACK_POLICIES, errors);
            if (effect.baseMagnitude() < 0 || effect.coefficient() < 0) errors.add(key + " magnitude values must be >= 0");
            if (effect.chance() < 0 || effect.chance() > 1) errors.add(key + " chance must be within 0..1");
            if (effect.durationSeconds() < 0 || effect.tickIntervalSeconds() < 0 || effect.radius() < 0)
                errors.add(key + " duration/tick/radius must be >= 0");
            if (effect.maxStacks() < 1) errors.add(key + " maxStacks must be >= 1");
            if (effect.bossMultiplier() < 0) errors.add(key + " bossMultiplier must be >= 0");
            if ("DAMAGE_OVER_TIME".equals(effect.effectType())
                    && (effect.durationSeconds() <= 0 || effect.tickIntervalSeconds() <= 0 || effect.tickIntervalSeconds() > effect.durationSeconds()))
                errors.add(key + " DOT duration/tick rule failed");
            if ("SPLASH_DAMAGE".equals(effect.effectType()) && effect.radius() <= 0)
                errors.add(key + " SPLASH_DAMAGE radius must be > 0");
        }
    }

    private static void enumValue(String label, String value, Set<String> allowed, List<String> errors) {
        if (!allowed.contains(value)) errors.add(label + " has unsupported case-sensitive value: " + value);
    }
}
