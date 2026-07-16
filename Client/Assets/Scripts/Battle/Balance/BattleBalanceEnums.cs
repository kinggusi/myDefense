namespace MyDefense.Battle.Balance
{
    public enum WaveType
    {
        REGULAR,
        BOSS
    }

    public enum BattleLanePolicy
    {
        EACH_ACTIVE_PLAYER_LANE,
        BOSS_SHARED
    }

    public enum BossPatternType
    {
        CAST_SKILL,
        WAIT,
        SET_PHASE,
        SET_MOVE_SPEED_MULTIPLIER
    }

    public enum BossTriggerType
    {
        TIME,
        HP_PERCENT,
        LOOP,
        ON_SPAWN,
        ON_DEATH
    }

    public enum BattleSkillType
    {
        BASIC_ATTACK,
        ACTIVE,
        PASSIVE,
        BOSS
    }

    public enum BattleSkillTriggerType
    {
        BASIC_ATTACK,
        AUTO_COOLDOWN,
        MP_THRESHOLD,
        PASSIVE,
        BOSS_PATTERN
    }

    public enum BattleTargetPolicy
    {
        DEFAULT_PROGRESS,
        HIGHEST_HP,
        LOWEST_HP,
        BOSS_FIRST,
        DENSEST_AREA,
        MOST_CROWDED_LANE,
        SELF
    }

    public enum ProjectileMoveType
    {
        HOMING,
        LINEAR,
        BALLISTIC,
        INSTANT
    }

    public enum ProjectileLostTargetPolicy
    {
        DESTROY,
        CONTINUE_LAST_DIRECTION,
        RETARGET
    }

    public enum SkillEffectTriggerPhase
    {
        ON_CAST,
        ON_HIT,
        ON_KILL,
        ON_EXPIRE
    }

    public enum BattleSkillEffectType
    {
        DAMAGE,
        SPLASH_DAMAGE,
        DAMAGE_OVER_TIME,
        SLOW,
        STUN,
        ATTACK_SPEED_BUFF,
        ATTACK_DAMAGE_BUFF,
        RANGE_BUFF
    }

    public enum SkillMagnitudeSource
    {
        FLAT,
        ATTACK_SNAPSHOT_DAMAGE
    }

    public enum SkillEffectStackPolicy
    {
        NONE,
        ADD,
        MULTIPLY,
        REPLACE,
        MAX_ONLY
    }
}
