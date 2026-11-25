#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public enum BattleShowroomConcept
    {
        Canonical,
        Diad,
        Mecharon,
        PuppeteerPlan1,
        PuppeteerPlan2
    }

    public enum BattleShowroomConceptArt
    {
        None,
        Diad,
        Mecharon,
        Puppeteer
    }

    public enum BattleShowroomConceptAction
    {
        Idle,
        Move,
        Basic,
        Skill,
        Deploy
    }

    public enum BattleShowroomConceptPose
    {
        Idle,
        Windup,
        Attack,
        Recover
    }

    public readonly struct BattleShowroomConceptDefinition
    {
        public BattleShowroomConceptArt Art { get; }
        public int DollCount { get; }
        public int MaximumDollCount { get; }
        public BattleShowroomConceptAction AutomaticAction { get; }

        private BattleShowroomConceptDefinition(
            BattleShowroomConceptArt art, int dollCount = 0, int maximumDollCount = 0,
            BattleShowroomConceptAction automaticAction = BattleShowroomConceptAction.Idle)
        {
            Art = art;
            DollCount = dollCount;
            MaximumDollCount = maximumDollCount;
            AutomaticAction = automaticAction;
        }

        public static BattleShowroomConceptDefinition For(BattleShowroomConcept concept) => concept switch
        {
            BattleShowroomConcept.Diad => new(BattleShowroomConceptArt.Diad),
            BattleShowroomConcept.Mecharon => new(BattleShowroomConceptArt.Mecharon),
            BattleShowroomConcept.PuppeteerPlan1 => new(BattleShowroomConceptArt.Puppeteer, 3, 6, BattleShowroomConceptAction.Deploy),
            BattleShowroomConcept.PuppeteerPlan2 => new(BattleShowroomConceptArt.Puppeteer, 1, 1, BattleShowroomConceptAction.Skill),
            _ => new(BattleShowroomConceptArt.None)
        };
    }

    public sealed class BattleShowroomConceptPlayback
    {
        public const float AutomaticInterval = 10f;
        public const float SynchronizedBurstTime = 0.88f;
        public const float DollStrikeImpact = 0.46f;
        private const float DollStrikeEnd = 1.04f;

        private bool _previousAuto;
        private bool _automaticAction;
        private float _clock;
        private float _actionStarted;
        private float _nextAutomatic;
        private int _basicCount;
        private int _dollsBeforeAction;
        private int _dolls;
        private int _firstDeployingDoll;
        private bool _repositioned;
        private BattleShowroomConceptAction _manualAction;

        public BattleShowroomConcept Concept { get; private set; }
        public BattleShowroomConceptAction Action { get; private set; }
        public BattleShowroomConceptDefinition Definition => BattleShowroomConceptDefinition.For(Concept);
        public float Clock => _clock;
        public float Elapsed => _clock - _actionStarted;
        public float NextAutomaticIn => Mathf.Max(0f, _nextAutomatic - _clock);
        public bool IsAutomatic => _automaticAction;
        public bool IsAlternateBasic => _basicCount > 0 && _basicCount % 2 == 0;
        public int ActiveDollCount => _dolls;
        public int DollsAtSkillStart => _dollsBeforeAction;
        public int FirstDeployingDoll => _firstDeployingDoll;
        public bool Repositioned => _repositioned;

        public void Select(BattleShowroomConcept concept)
        {
            Concept = concept;
            _clock = 0f;
            _basicCount = 0;
            _dolls = Definition.DollCount;
            _dollsBeforeAction = _dolls;
            _firstDeployingDoll = 0;
            _repositioned = false;
            _manualAction = BattleShowroomConceptAction.Idle;
            _previousAuto = false;
            _nextAutomatic = AutomaticInterval;
            Start(BattleShowroomConceptAction.Idle, automatic: false);
        }

        public void Play(BattleShowroomConceptAction action)
        {
            if (Concept == BattleShowroomConcept.PuppeteerPlan1
                && (action == BattleShowroomConceptAction.Basic || action == BattleShowroomConceptAction.Skill)
                && _dolls == 0)
                return;
            _manualAction = action;
            Start(action, automatic: false);
        }

        public void Advance(float deltaTime, bool loop, bool auto)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                return;

            bool autoEnabled = auto && Definition.AutomaticAction != BattleShowroomConceptAction.Idle;
            if (autoEnabled && !_previousAuto)
                _nextAutomatic = _clock + AutomaticInterval;
            _previousAuto = autoEnabled;
            _clock += deltaTime;
            if (Concept == BattleShowroomConcept.PuppeteerPlan1
                && Action == BattleShowroomConceptAction.Skill && Elapsed >= SynchronizedBurstTime)
                _dolls = 0;

            bool actionComplete = Action == BattleShowroomConceptAction.Idle || Elapsed >= ActionDuration(Concept, Action);
            if (autoEnabled && _clock >= _nextAutomatic && actionComplete)
            {
                _nextAutomatic = _clock + AutomaticInterval;
                Start(Definition.AutomaticAction, automatic: true);
                return;
            }

            if (Action != BattleShowroomConceptAction.Idle && actionComplete)
            {
                BattleShowroomConceptAction next = loop ? _manualAction : BattleShowroomConceptAction.Idle;
                if (next == BattleShowroomConceptAction.Skill
                    && Concept == BattleShowroomConcept.PuppeteerPlan1 && _dolls == 0)
                    _dolls = Definition.DollCount;
                Start(next, automatic: false);
            }
        }

        public static float ActionDuration(BattleShowroomConcept concept, BattleShowroomConceptAction action)
        {
            return action switch
            {
                BattleShowroomConceptAction.Move => 1.4f,
                BattleShowroomConceptAction.Basic => concept == BattleShowroomConcept.Mecharon
                    || concept == BattleShowroomConcept.PuppeteerPlan1 ? 1.65f : 1.25f,
                BattleShowroomConceptAction.Deploy => 1.25f,
                BattleShowroomConceptAction.Skill => concept == BattleShowroomConcept.Diad ? 1.6f : 2.1f,
                _ => float.PositiveInfinity
            };
        }

        public static BattleShowroomConceptPose PoseAtElapsed(BattleShowroomConcept concept,
            BattleShowroomConceptAction action, float elapsed)
        {
            if (action == BattleShowroomConceptAction.Idle || action == BattleShowroomConceptAction.Move)
                return BattleShowroomConceptPose.Idle;

            if (concept == BattleShowroomConcept.Mecharon && action == BattleShowroomConceptAction.Basic)
            {
                const float firstShot = 0.19f, spacing = 0.215f;
                if (elapsed < firstShot)
                    return BattleShowroomConceptPose.Windup;
                int shot = Mathf.Min(4, Mathf.FloorToInt((elapsed - firstShot) / spacing));
                float age = elapsed - firstShot - shot * spacing;
                if (age < 0.075f)
                    return BattleShowroomConceptPose.Attack;
                if (age < 0.16f || shot == 4)
                    return age < 0.38f ? BattleShowroomConceptPose.Recover : BattleShowroomConceptPose.Idle;
                return BattleShowroomConceptPose.Windup;
            }

            float release = action == BattleShowroomConceptAction.Skill ? 0.43f : 0.28f;
            float recovery = action == BattleShowroomConceptAction.Skill ? 0.98f : 0.76f;
            float end = ActionDuration(concept, action) - 0.10f;
            if (concept == BattleShowroomConcept.Mecharon && action == BattleShowroomConceptAction.Skill)
            {
                release = 0.33f;
                recovery = 1.20f;
            }
            if (action == BattleShowroomConceptAction.Deploy)
            {
                release = 0.23f;
                recovery = 0.82f;
            }
            if (elapsed < release)
                return BattleShowroomConceptPose.Windup;
            if (elapsed < recovery)
                return BattleShowroomConceptPose.Attack;
            return elapsed < end ? BattleShowroomConceptPose.Recover : BattleShowroomConceptPose.Idle;
        }

        public static BattleShowroomConceptPose DollPoseAtElapsed(float strikeAge)
        {
            if (strikeAge < 0f || strikeAge >= DollStrikeEnd)
                return BattleShowroomConceptPose.Idle;
            if (strikeAge < 0.25f)
                return BattleShowroomConceptPose.Windup;
            return strikeAge < 0.71f ? BattleShowroomConceptPose.Attack : BattleShowroomConceptPose.Recover;
        }

        public static Vector3 BodyMotionOffset(BattleShowroomConceptAction action, float elapsed, Vector3 heading)
        {
            if (action != BattleShowroomConceptAction.Move || elapsed <= 0f || elapsed >= 1.4f)
                return Vector3.zero;
            float progress = elapsed < 0.7f ? elapsed / 0.7f : (1.4f - elapsed) / 0.7f;
            return heading * (Mathf.SmoothStep(0f, 1f, progress) * 0.24f);
        }

        public static int ClosestAvailableTarget(Vector2 origin, Vector2[] targets, int occupiedMask)
        {
            int selected = -1;
            float shortest = float.PositiveInfinity;
            for (int index = 0; index < targets.Length; index++)
            {
                if ((occupiedMask & (1 << index)) != 0)
                    continue;
                float squaredDistance = (targets[index] - origin).sqrMagnitude;
                if (squaredDistance < shortest)
                {
                    selected = index;
                    shortest = squaredDistance;
                }
            }
            return selected;
        }

        public static Vector2 MoveBetweenHomeAndApex(Vector2 from, Vector2 to, float progress, bool rising)
        {
            float t = Mathf.Clamp01(progress);
            float heightProgress = rising ? 1f - (1f - t) * (1f - t) : t * t;
            return new Vector2(Mathf.SmoothStep(from.x, to.x, t), Mathf.Lerp(from.y, to.y, heightProgress));
        }

        public static float DollStrikeAge(float elapsed, int dollIndex)
            => elapsed - (dollIndex % 3) * 0.115f - (dollIndex / 3) * 0.12f;

        public static Vector2 DollStrikePosition(Vector2 stationed, Vector2 target, float age)
        {
            Vector2 direction = (target - stationed).normalized;
            Vector2 winding = stationed - direction * 24f;
            if (age <= 0f || age >= DollStrikeEnd)
                return stationed;
            if (age < 0.25f)
                return Vector2.Lerp(stationed, winding, Mathf.SmoothStep(0f, 1f, age / 0.25f));
            if (age < DollStrikeImpact)
                return Vector2.Lerp(winding, target, Mathf.SmoothStep(0f, 1f,
                    (age - 0.25f) / (DollStrikeImpact - 0.25f)));
            float recovery = Mathf.SmoothStep(0f, 1f, (age - DollStrikeImpact) / (DollStrikeEnd - DollStrikeImpact));
            Vector2 side = new(-direction.y, direction.x);
            return Vector2.Lerp(target, stationed, recovery) + side * (16f * Mathf.Sin(recovery * Mathf.PI));
        }

        private void Start(BattleShowroomConceptAction action, bool automatic)
        {
            if (Concept == BattleShowroomConcept.PuppeteerPlan1)
            {
                _dollsBeforeAction = _dolls;
                if (action == BattleShowroomConceptAction.Deploy)
                {
                    _firstDeployingDoll = _dolls;
                    _dolls = Mathf.Min(Definition.MaximumDollCount, _dolls + Definition.DollCount);
                }
                if (action == BattleShowroomConceptAction.Move)
                    _repositioned = !_repositioned;
            }
            Action = action;
            _actionStarted = _clock;
            _automaticAction = automatic;
            if (action == BattleShowroomConceptAction.Basic)
                _basicCount++;
        }
    }
}
#endif
