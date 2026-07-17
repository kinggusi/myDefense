using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Balance
{
    public sealed class WaveCatalog
    {
        private readonly IReadOnlyList<WaveSpecData> _all;
        private readonly Dictionary<int, WaveSpecData> _byRound;
        private readonly Dictionary<string, WaveSpecData> _byId;
        private readonly Dictionary<string, IReadOnlyList<WaveSpawnSpecData>> _spawnsByWave;

        public IReadOnlyList<WaveSpecData> All => _all;

        internal WaveCatalog(IEnumerable<WaveSpecData> waves, IEnumerable<WaveSpawnSpecData> spawns)
        {
            var sortedWaves = new List<WaveSpecData>(waves);
            sortedWaves.Sort(CompareWaves);
            _all = BattleBalanceCollections.Copy(sortedWaves);
            _byRound = new Dictionary<int, WaveSpecData>();
            _byId = new Dictionary<string, WaveSpecData>(StringComparer.Ordinal);
            for (int index = 0; index < sortedWaves.Count; index++)
            {
                WaveSpecData wave = sortedWaves[index];
                _byId.Add(wave.WaveId, wave);
                if (!_byRound.ContainsKey(wave.RoundNumber))
                    _byRound.Add(wave.RoundNumber, wave);
            }

            var groupedSpawns = new Dictionary<string, List<WaveSpawnSpecData>>(StringComparer.Ordinal);
            foreach (WaveSpawnSpecData spawn in spawns)
            {
                List<WaveSpawnSpecData> list;
                if (!groupedSpawns.TryGetValue(spawn.WaveId, out list))
                {
                    list = new List<WaveSpawnSpecData>();
                    groupedSpawns.Add(spawn.WaveId, list);
                }
                list.Add(spawn);
            }

            _spawnsByWave = new Dictionary<string, IReadOnlyList<WaveSpawnSpecData>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<WaveSpawnSpecData>> pair in groupedSpawns)
            {
                pair.Value.Sort(CompareSpawns);
                _spawnsByWave.Add(pair.Key, BattleBalanceCollections.Copy(pair.Value));
            }
        }

        public bool TryGetByRound(int roundNumber, out WaveSpecData wave)
        {
            return _byRound.TryGetValue(roundNumber, out wave);
        }

        public bool TryGetById(string waveId, out WaveSpecData wave)
        {
            wave = null;
            return waveId != null && _byId.TryGetValue(waveId, out wave);
        }

        public bool TryGetNextEnabledWave(int currentRoundNumber, out WaveSpecData wave)
        {
            for (int index = 0; index < _all.Count; index++)
            {
                WaveSpecData candidate = _all[index];
                if (candidate.Enabled && candidate.RoundNumber > currentRoundNumber)
                {
                    wave = candidate;
                    return true;
                }
            }

            wave = null;
            return false;
        }

        public IReadOnlyList<WaveSpawnSpecData> GetSpawns(string waveId)
        {
            IReadOnlyList<WaveSpawnSpecData> result;
            return waveId != null && _spawnsByWave.TryGetValue(waveId, out result)
                ? result
                : Array.AsReadOnly(Array.Empty<WaveSpawnSpecData>());
        }

        private static int CompareWaves(WaveSpecData left, WaveSpecData right)
        {
            int order = left.RoundNumber.CompareTo(right.RoundNumber);
            return order != 0 ? order : string.CompareOrdinal(left.WaveId, right.WaveId);
        }

        private static int CompareSpawns(WaveSpawnSpecData left, WaveSpawnSpecData right)
        {
            int waveOrder = string.CompareOrdinal(left.WaveId, right.WaveId);
            return waveOrder != 0 ? waveOrder : left.SpawnOrder.CompareTo(right.SpawnOrder);
        }
    }

    public sealed class BossPatternCatalog
    {
        private readonly Dictionary<string, IReadOnlyList<BossPatternSpecData>> _byWave;

        internal BossPatternCatalog(IEnumerable<BossPatternSpecData> patterns)
        {
            var grouped = new Dictionary<string, List<BossPatternSpecData>>(StringComparer.Ordinal);
            foreach (BossPatternSpecData pattern in patterns)
            {
                List<BossPatternSpecData> list;
                if (!grouped.TryGetValue(pattern.WaveId, out list))
                {
                    list = new List<BossPatternSpecData>();
                    grouped.Add(pattern.WaveId, list);
                }
                list.Add(pattern);
            }

            _byWave = new Dictionary<string, IReadOnlyList<BossPatternSpecData>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<BossPatternSpecData>> pair in grouped)
            {
                pair.Value.Sort((left, right) => left.PatternOrder.CompareTo(right.PatternOrder));
                _byWave.Add(pair.Key, BattleBalanceCollections.Copy(pair.Value));
            }
        }

        public IReadOnlyList<BossPatternSpecData> GetByWave(string waveId)
        {
            IReadOnlyList<BossPatternSpecData> result;
            return waveId != null && _byWave.TryGetValue(waveId, out result)
                ? result
                : Array.AsReadOnly(Array.Empty<BossPatternSpecData>());
        }
    }

    public sealed class SkillCatalog
    {
        private readonly IReadOnlyList<SkillSpecData> _all;
        private readonly Dictionary<string, SkillSpecData> _byId;

        public IReadOnlyList<SkillSpecData> All => _all;

        internal SkillCatalog(IEnumerable<SkillSpecData> skills)
        {
            var sorted = new List<SkillSpecData>(skills);
            sorted.Sort((left, right) => string.CompareOrdinal(left.SkillId, right.SkillId));
            _all = BattleBalanceCollections.Copy(sorted);
            _byId = new Dictionary<string, SkillSpecData>(StringComparer.Ordinal);
            foreach (SkillSpecData skill in sorted) _byId.Add(skill.SkillId, skill);
        }

        public bool TryGet(string skillId, out SkillSpecData skill)
        {
            skill = null;
            return skillId != null && _byId.TryGetValue(skillId, out skill);
        }
    }

    public sealed class AlienSkillCatalog
    {
        private readonly Dictionary<string, AlienSkillLinkData> _byCompositeKey;
        private readonly Dictionary<long, IReadOnlyList<AlienSkillLinkData>> _byAlien;

        internal AlienSkillCatalog(IEnumerable<AlienSkillLinkData> links)
        {
            var sorted = new List<AlienSkillLinkData>(links);
            sorted.Sort(CompareLinks);
            _byCompositeKey = new Dictionary<string, AlienSkillLinkData>(StringComparer.Ordinal);
            var grouped = new Dictionary<long, List<AlienSkillLinkData>>();
            foreach (AlienSkillLinkData link in sorted)
            {
                _byCompositeKey.Add(BuildKey(link.AlienId, link.SlotIndex), link);
                List<AlienSkillLinkData> list;
                if (!grouped.TryGetValue(link.AlienId, out list))
                {
                    list = new List<AlienSkillLinkData>();
                    grouped.Add(link.AlienId, list);
                }
                list.Add(link);
            }

            _byAlien = new Dictionary<long, IReadOnlyList<AlienSkillLinkData>>();
            foreach (KeyValuePair<long, List<AlienSkillLinkData>> pair in grouped)
                _byAlien.Add(pair.Key, BattleBalanceCollections.Copy(pair.Value));
        }

        public bool TryGet(long alienId, int slotIndex, out AlienSkillLinkData link)
        {
            return _byCompositeKey.TryGetValue(BuildKey(alienId, slotIndex), out link);
        }

        public IReadOnlyList<AlienSkillLinkData> GetByAlien(long alienId)
        {
            IReadOnlyList<AlienSkillLinkData> result;
            return _byAlien.TryGetValue(alienId, out result)
                ? result
                : Array.AsReadOnly(Array.Empty<AlienSkillLinkData>());
        }

        private static int CompareLinks(AlienSkillLinkData left, AlienSkillLinkData right)
        {
            int alienOrder = left.AlienId.CompareTo(right.AlienId);
            return alienOrder != 0 ? alienOrder : left.SlotIndex.CompareTo(right.SlotIndex);
        }

        internal static string BuildKey(long alienId, int slotIndex)
        {
            return alienId + ":" + slotIndex;
        }
    }

    public sealed class ProjectileCatalog
    {
        private readonly IReadOnlyList<ProjectileSpecData> _all;
        private readonly Dictionary<string, ProjectileSpecData> _byId;

        public IReadOnlyList<ProjectileSpecData> All => _all;

        internal ProjectileCatalog(IEnumerable<ProjectileSpecData> projectiles)
        {
            var sorted = new List<ProjectileSpecData>(projectiles);
            sorted.Sort((left, right) => string.CompareOrdinal(left.ProjectileId, right.ProjectileId));
            _all = BattleBalanceCollections.Copy(sorted);
            _byId = new Dictionary<string, ProjectileSpecData>(StringComparer.Ordinal);
            foreach (ProjectileSpecData projectile in sorted) _byId.Add(projectile.ProjectileId, projectile);
        }

        public bool TryGet(string projectileId, out ProjectileSpecData projectile)
        {
            projectile = null;
            return projectileId != null && _byId.TryGetValue(projectileId, out projectile);
        }
    }

    public sealed class SkillEffectCatalog
    {
        private readonly Dictionary<string, IReadOnlyList<SkillEffectSpecData>> _bySkill;

        internal SkillEffectCatalog(IEnumerable<SkillEffectSpecData> effects)
        {
            var grouped = new Dictionary<string, List<SkillEffectSpecData>>(StringComparer.Ordinal);
            foreach (SkillEffectSpecData effect in effects)
            {
                List<SkillEffectSpecData> list;
                if (!grouped.TryGetValue(effect.SkillId, out list))
                {
                    list = new List<SkillEffectSpecData>();
                    grouped.Add(effect.SkillId, list);
                }
                list.Add(effect);
            }

            _bySkill = new Dictionary<string, IReadOnlyList<SkillEffectSpecData>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<SkillEffectSpecData>> pair in grouped)
            {
                pair.Value.Sort((left, right) => left.ExecutionOrder.CompareTo(right.ExecutionOrder));
                _bySkill.Add(pair.Key, BattleBalanceCollections.Copy(pair.Value));
            }
        }

        public IReadOnlyList<SkillEffectSpecData> GetBySkill(string skillId)
        {
            IReadOnlyList<SkillEffectSpecData> result;
            return skillId != null && _bySkill.TryGetValue(skillId, out result)
                ? result
                : Array.AsReadOnly(Array.Empty<SkillEffectSpecData>());
        }
    }

    public sealed class BattleBalanceCatalog
    {
        public WaveCatalog Waves { get; }
        public BossPatternCatalog BossPatterns { get; }
        public SkillCatalog Skills { get; }
        public AlienSkillCatalog AlienSkills { get; }
        public ProjectileCatalog Projectiles { get; }
        public SkillEffectCatalog SkillEffects { get; }

        internal BattleBalanceCatalog(BattleBalanceDocuments documents)
        {
            Waves = new WaveCatalog(documents.Waves.Items, documents.Spawns.Items);
            BossPatterns = new BossPatternCatalog(documents.BossPatterns.Items);
            Skills = new SkillCatalog(documents.Skills.Items);
            AlienSkills = new AlienSkillCatalog(documents.AlienSkills.Items);
            Projectiles = new ProjectileCatalog(documents.Projectiles.Items);
            SkillEffects = new SkillEffectCatalog(documents.SkillEffects.Items);
        }

        public bool TryGetProjectileForSkill(string skillId, out ProjectileSpecData projectile)
        {
            projectile = null;
            SkillSpecData skill;
            return Skills.TryGet(skillId, out skill)
                && !string.IsNullOrEmpty(skill.ProjectileId)
                && Projectiles.TryGet(skill.ProjectileId, out projectile);
        }
    }
}
