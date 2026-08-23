using System;
using System.Collections.Generic;
using MyDefense.Battle.Balance;

namespace MyDefense.Battle
{
    /// <summary>
    /// State-Authority-only evaluator for canonical Boss pattern rows.
    /// It is intentionally independent of presentation and emits immutable
    /// pattern commands for BattleWaveExecutor to apply.
    /// </summary>
    public sealed class BattleBossPatternRuntime
    {
        private readonly IReadOnlyList<BossPatternSpecData> _patterns;
        private readonly HashSet<int> _completedOrders = new();
        private readonly Dictionary<int, float> _lastLoopTriggerAt = new();

        public BattleBossPatternRuntime(IReadOnlyList<BossPatternSpecData> patterns)
        {
            _patterns = patterns ?? Array.Empty<BossPatternSpecData>();
        }

        public int CompletedCount => _completedOrders.Count;

        public void Tick(float elapsedSeconds, float hpRatio, Action<BossPatternSpecData> execute)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            float elapsed = Math.Max(0f, elapsedSeconds);
            float hp = Math.Clamp(hpRatio, 0f, 1f);
            for (int index = 0; index < _patterns.Count; index++)
            {
                BossPatternSpecData pattern = _patterns[index];
                if (pattern == null || !pattern.Enabled || !ShouldTrigger(pattern, elapsed, hp))
                    continue;
                execute(pattern);
                if (pattern.TriggerType == BossTriggerType.LOOP)
                    _lastLoopTriggerAt[pattern.PatternOrder] = elapsed;
                else
                    _completedOrders.Add(pattern.PatternOrder);
            }
        }

        private bool ShouldTrigger(BossPatternSpecData pattern, float elapsed, float hpRatio)
        {
            if (pattern.TriggerType != BossTriggerType.LOOP && _completedOrders.Contains(pattern.PatternOrder))
                return false;
            return pattern.TriggerType switch
            {
                BossTriggerType.ON_SPAWN => true,
                BossTriggerType.TIME => elapsed >= pattern.TriggerValue,
                BossTriggerType.HP_PERCENT => hpRatio <= NormalizeHpThreshold(pattern.TriggerValue),
                BossTriggerType.LOOP => IsLoopReady(pattern, elapsed),
                BossTriggerType.ON_DEATH => hpRatio <= 0f,
                _ => false
            };
        }

        private bool IsLoopReady(BossPatternSpecData pattern, float elapsed)
        {
            float interval = pattern.CooldownSeconds > 0f ? pattern.CooldownSeconds : pattern.TriggerValue;
            if (interval <= 0f || elapsed < pattern.TriggerValue)
                return false;
            return !_lastLoopTriggerAt.TryGetValue(pattern.PatternOrder, out float last)
                || elapsed - last >= interval;
        }

        private static float NormalizeHpThreshold(float value) => value > 1f ? value / 100f : value;
    }
}
