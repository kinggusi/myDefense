using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Balance
{
    public sealed class BattleBalanceParseResult<T>
    {
        public bool IsValid { get; }
        public T Value { get; }
        public IReadOnlyList<string> Errors { get; }

        internal BattleBalanceParseResult(T value, IEnumerable<string> errors)
        {
            Value = value;
            Errors = BattleBalanceCollections.Copy(errors);
            IsValid = Errors.Count == 0 && value != null;
        }
    }

    public sealed class BattleBalanceJsonParser
    {
        private const int Sha256HexLength = 64;

        public BattleBalanceParseResult<BattleBalanceManifestData> ParseManifest(string json)
        {
            var errors = new List<string>();
            RawManifest raw = Deserialize<RawManifest>(json, "manifest", errors);
            if (raw == null)
                return new BattleBalanceParseResult<BattleBalanceManifestData>(null, errors);

            ValidateRootField(json, "schemaVersion", errors, "manifest");
            ValidateRootField(json, "balanceVersion", errors, "manifest");
            ValidateRootField(json, "bundleHash", errors, "manifest");
            ValidateRootField(json, "files", errors, "manifest");
            ValidateSchemaVersion(raw.schemaVersion, errors, "manifest");
            ValidateRequiredText(raw.balanceVersion, "manifest.balanceVersion", errors);
            ValidateHash(raw.bundleHash, "manifest.bundleHash", errors);
            if (raw.files == null)
                errors.Add("manifest.files must not be null.");

            var files = new List<BattleBalanceFileEntryData>();
            if (raw.files != null)
            {
                for (int index = 0; index < raw.files.Count; index++)
                {
                    RawFileEntry file = raw.files[index];
                    if (file == null)
                    {
                        errors.Add("manifest.files[" + index + "] must not be null.");
                        continue;
                    }

                    ValidateRequiredText(file.resourcePath, "manifest.files[" + index + "].resourcePath", errors);
                    ValidateHash(file.contentHash, "manifest.files[" + index + "].contentHash", errors);
                    files.Add(new BattleBalanceFileEntryData(file.resourcePath, file.contentHash));
                }
            }

            BattleBalanceManifestData value = errors.Count == 0
                ? new BattleBalanceManifestData(raw.schemaVersion, raw.balanceVersion, raw.bundleHash, files)
                : null;
            return new BattleBalanceParseResult<BattleBalanceManifestData>(value, errors);
        }

        public BattleBalanceParseResult<BattleBalanceDocument<WaveSpecData>> ParseWaveDocument(string json)
        {
            return ParseDocument<RawWave, WaveSpecData>(json, "WaveSpec", ConvertWave);
        }

        public BattleBalanceParseResult<BattleBalanceDocument<WaveSpawnSpecData>> ParseWaveSpawnDocument(string json)
        {
            return ParseDocument<RawWaveSpawn, WaveSpawnSpecData>(json, "WaveSpawnSpec", ConvertWaveSpawn);
        }

        public BattleBalanceParseResult<BattleBalanceDocument<BossPatternSpecData>> ParseBossPatternDocument(string json)
        {
            return ParseDocument<RawBossPattern, BossPatternSpecData>(json, "BossPatternSpec", ConvertBossPattern, "parameterValue");
        }

        public BattleBalanceParseResult<BattleBalanceDocument<SkillSpecData>> ParseSkillDocument(string json)
        {
            return ParseDocument<RawSkill, SkillSpecData>(json, "SkillSpec", ConvertSkill, "maxTargetCount");
        }

        public BattleBalanceParseResult<BattleBalanceDocument<AlienSkillLinkData>> ParseAlienSkillLinkDocument(string json)
        {
            return ParseDocument<RawAlienSkillLink, AlienSkillLinkData>(json, "AlienSkillLink", ConvertAlienSkillLink);
        }

        public BattleBalanceParseResult<BattleBalanceDocument<ProjectileSpecData>> ParseProjectileDocument(string json)
        {
            return ParseDocument<RawProjectile, ProjectileSpecData>(json, "ProjectileSpec", ConvertProjectile);
        }

        public BattleBalanceParseResult<BattleBalanceDocument<SkillEffectSpecData>> ParseSkillEffectDocument(string json)
        {
            return ParseDocument<RawSkillEffect, SkillEffectSpecData>(json, "SkillEffectSpec", ConvertSkillEffect);
        }

        private static BattleBalanceParseResult<BattleBalanceDocument<TData>> ParseDocument<TRaw, TData>(
            string json,
            string documentName,
            Func<TRaw, int, List<string>, TData> converter,
            params string[] requiredItemFields)
        {
            var errors = new List<string>();
            RawDocument<TRaw> raw = Deserialize<RawDocument<TRaw>>(json, documentName, errors);
            if (raw == null)
                return new BattleBalanceParseResult<BattleBalanceDocument<TData>>(null, errors);

            ValidateRootField(json, "schemaVersion", errors, documentName);
            ValidateRootField(json, "balanceVersion", errors, documentName);
            ValidateRootField(json, "contentHash", errors, documentName);
            ValidateRootField(json, "items", errors, documentName);
            ValidateSchemaVersion(raw.schemaVersion, errors, documentName);
            ValidateRequiredText(raw.balanceVersion, documentName + ".balanceVersion", errors);
            ValidateHash(raw.contentHash, documentName + ".contentHash", errors);
            if (raw.items == null)
                errors.Add(documentName + ".items must not be null.");
            else
                ValidateRequiredItemFields(json, documentName, raw.items.Count, requiredItemFields, errors);

            var items = new List<TData>();
            if (raw.items != null)
            {
                for (int index = 0; index < raw.items.Count; index++)
                {
                    TRaw rawItem = raw.items[index];
                    if (ReferenceEquals(rawItem, null))
                    {
                        errors.Add(documentName + ".items[" + index + "] must not be null.");
                        continue;
                    }

                    TData item = converter(rawItem, index, errors);
                    if (!ReferenceEquals(item, null))
                        items.Add(item);
                }
            }

            BattleBalanceDocument<TData> value = errors.Count == 0
                ? new BattleBalanceDocument<TData>(raw.schemaVersion, raw.balanceVersion, raw.contentHash, items)
                : null;
            return new BattleBalanceParseResult<BattleBalanceDocument<TData>>(value, errors);
        }

        private static WaveSpecData ConvertWave(RawWave raw, int index, List<string> errors)
        {
            WaveType waveType;
            if (!TryParseEnum(raw.waveType, "WaveSpec.items[" + index + "].waveType", errors, out waveType))
                return null;

            return new WaveSpecData(raw.waveId, raw.roundNumber, waveType, raw.nextWaveDelaySeconds, raw.bossTimeLimitSeconds, raw.enabled);
        }

        private static WaveSpawnSpecData ConvertWaveSpawn(RawWaveSpawn raw, int index, List<string> errors)
        {
            BattleLanePolicy lanePolicy;
            if (!TryParseEnum(raw.lanePolicy, "WaveSpawnSpec.items[" + index + "].lanePolicy", errors, out lanePolicy))
                return null;

            return new WaveSpawnSpecData(raw.waveId, raw.spawnOrder, lanePolicy, raw.monsterId, raw.spawnCount, raw.spawnDelaySeconds, raw.spawnIntervalSeconds, raw.hpMultiplier, raw.moveSpeedMultiplier);
        }

        private static BossPatternSpecData ConvertBossPattern(RawBossPattern raw, int index, List<string> errors)
        {
            BossPatternType patternType;
            BossTriggerType triggerType;
            bool valid = TryParseEnum(raw.patternType, "BossPatternSpec.items[" + index + "].patternType", errors, out patternType);
            valid &= TryParseEnum(raw.triggerType, "BossPatternSpec.items[" + index + "].triggerType", errors, out triggerType);
            if (!valid) return null;

            return new BossPatternSpecData(raw.waveId, raw.patternOrder, patternType, triggerType, raw.triggerValue, raw.cooldownSeconds, raw.skillId, raw.parameterKey, raw.parameterValue, raw.enabled);
        }

        private static SkillSpecData ConvertSkill(RawSkill raw, int index, List<string> errors)
        {
            BattleSkillType skillType;
            BattleSkillTriggerType triggerType;
            BattleTargetPolicy targetPolicy;
            bool valid = TryParseEnum(raw.skillType, "SkillSpec.items[" + index + "].skillType", errors, out skillType);
            valid &= TryParseEnum(raw.triggerType, "SkillSpec.items[" + index + "].triggerType", errors, out triggerType);
            valid &= TryParseEnum(raw.targetPolicy, "SkillSpec.items[" + index + "].targetPolicy", errors, out targetPolicy);
            if (!valid) return null;

            return new SkillSpecData(raw.skillId, raw.nameKey, raw.descriptionKey, skillType, triggerType, raw.cooldownSeconds, raw.mpCost, raw.castRange, targetPolicy, raw.maxTargetCount, raw.projectileId, raw.animationKey, raw.vfxKey, raw.sfxKey, raw.enabled);
        }

        private static AlienSkillLinkData ConvertAlienSkillLink(RawAlienSkillLink raw, int index, List<string> errors)
        {
            return new AlienSkillLinkData(raw.alienId, raw.skillId, raw.slotIndex, raw.castPriority, raw.enabled);
        }

        private static ProjectileSpecData ConvertProjectile(RawProjectile raw, int index, List<string> errors)
        {
            ProjectileMoveType moveType;
            ProjectileLostTargetPolicy lostTargetPolicy;
            bool valid = TryParseEnum(raw.moveType, "ProjectileSpec.items[" + index + "].moveType", errors, out moveType);
            valid &= TryParseEnum(raw.lostTargetPolicy, "ProjectileSpec.items[" + index + "].lostTargetPolicy", errors, out lostTargetPolicy);
            if (!valid) return null;

            return new ProjectileSpecData(raw.projectileId, raw.prefabKey, moveType, raw.speed, raw.lifetimeSeconds, raw.hitRadius, raw.pierceCount, raw.destroyOnHit, lostTargetPolicy, raw.enabled);
        }

        private static SkillEffectSpecData ConvertSkillEffect(RawSkillEffect raw, int index, List<string> errors)
        {
            SkillEffectTriggerPhase triggerPhase;
            BattleSkillEffectType effectType;
            SkillMagnitudeSource magnitudeSource;
            SkillEffectStackPolicy stackPolicy;
            bool valid = TryParseEnum(raw.triggerPhase, "SkillEffectSpec.items[" + index + "].triggerPhase", errors, out triggerPhase);
            valid &= TryParseEnum(raw.effectType, "SkillEffectSpec.items[" + index + "].effectType", errors, out effectType);
            valid &= TryParseEnum(raw.magnitudeSource, "SkillEffectSpec.items[" + index + "].magnitudeSource", errors, out magnitudeSource);
            valid &= TryParseEnum(raw.stackPolicy, "SkillEffectSpec.items[" + index + "].stackPolicy", errors, out stackPolicy);
            if (!valid) return null;

            return new SkillEffectSpecData(raw.skillId, raw.executionOrder, triggerPhase, effectType, magnitudeSource, raw.baseMagnitude, raw.coefficient, raw.chance, raw.durationSeconds, raw.tickIntervalSeconds, raw.radius, raw.maxStacks, stackPolicy, raw.bossMultiplier);
        }

        private static T Deserialize<T>(string json, string label, List<string> errors) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add(label + " JSON is empty.");
                return null;
            }

            string trimmed = json.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
            {
                errors.Add(label + " JSON root must be an object.");
                return null;
            }

            try
            {
                T value = JsonUtility.FromJson<T>(json);
                if (value == null)
                    errors.Add(label + " JSON could not be parsed.");
                return value;
            }
            catch (Exception exception)
            {
                errors.Add(label + " JSON syntax is invalid: " + exception.Message);
                return null;
            }
        }

        private static void ValidateRootField(string json, string fieldName, List<string> errors, string label)
        {
            if (json.IndexOf("\"" + fieldName + "\"", StringComparison.Ordinal) < 0)
                errors.Add(label + " is missing required root field '" + fieldName + "'.");
        }

        private static void ValidateRequiredItemFields(
            string json,
            string documentName,
            int expectedItemCount,
            IReadOnlyList<string> requiredFields,
            List<string> errors)
        {
            if (requiredFields == null || requiredFields.Count == 0) return;

            List<string> itemObjects = ExtractItemObjects(json);
            if (itemObjects == null || itemObjects.Count != expectedItemCount)
            {
                errors.Add(documentName + ".items could not be inspected for required fields.");
                return;
            }

            for (int itemIndex = 0; itemIndex < itemObjects.Count; itemIndex++)
            {
                for (int fieldIndex = 0; fieldIndex < requiredFields.Count; fieldIndex++)
                {
                    string fieldName = requiredFields[fieldIndex];
                    if (!ContainsJsonProperty(itemObjects[itemIndex], fieldName))
                        errors.Add(documentName + ".items[" + itemIndex + "] is missing required field '" + fieldName + "'.");
                }
            }
        }

        private static List<string> ExtractItemObjects(string json)
        {
            int itemsIndex = json.IndexOf("\"items\"", StringComparison.Ordinal);
            if (itemsIndex < 0) return null;
            int arrayStart = json.IndexOf('[', itemsIndex);
            if (arrayStart < 0) return null;

            var result = new List<string>();
            bool inString = false;
            bool escaped = false;
            int arrayDepth = 0;
            int objectDepth = 0;
            int objectStart = -1;
            for (int index = arrayStart; index < json.Length; index++)
            {
                char character = json[index];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') inString = false;
                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }
                if (character == '[') arrayDepth++;
                else if (character == ']')
                {
                    arrayDepth--;
                    if (arrayDepth == 0) return result;
                }
                else if (character == '{')
                {
                    if (arrayDepth == 1 && objectDepth == 0) objectStart = index;
                    objectDepth++;
                }
                else if (character == '}')
                {
                    objectDepth--;
                    if (arrayDepth == 1 && objectDepth == 0 && objectStart >= 0)
                    {
                        result.Add(json.Substring(objectStart, index - objectStart + 1));
                        objectStart = -1;
                    }
                }
            }

            return null;
        }

        private static bool ContainsJsonProperty(string jsonObject, string fieldName)
        {
            string token = "\"" + fieldName + "\"";
            int searchIndex = 0;
            while (searchIndex < jsonObject.Length)
            {
                int tokenIndex = jsonObject.IndexOf(token, searchIndex, StringComparison.Ordinal);
                if (tokenIndex < 0) return false;
                int next = tokenIndex + token.Length;
                while (next < jsonObject.Length && char.IsWhiteSpace(jsonObject[next])) next++;
                if (next < jsonObject.Length && jsonObject[next] == ':') return true;
                searchIndex = tokenIndex + token.Length;
            }
            return false;
        }

        private static void ValidateSchemaVersion(int schemaVersion, List<string> errors, string label)
        {
            if (schemaVersion != BattleBalanceSchema.Version)
                errors.Add(label + ".schemaVersion must be " + BattleBalanceSchema.Version + " but was " + schemaVersion + ".");
        }

        private static void ValidateRequiredText(string value, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors.Add(label + " must not be empty.");
        }

        private static void ValidateHash(string value, string label, List<string> errors)
        {
            if (string.IsNullOrEmpty(value) || value.Length != Sha256HexLength)
            {
                errors.Add(label + " must be a 64-character hexadecimal SHA-256 string.");
                return;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isHex = character >= '0' && character <= '9'
                    || character >= 'a' && character <= 'f'
                    || character >= 'A' && character <= 'F';
                if (!isHex)
                {
                    errors.Add(label + " must be a 64-character hexadecimal SHA-256 string.");
                    return;
                }
            }
        }

        private static bool TryParseEnum<T>(string value, string label, List<string> errors, out T parsed) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value)
                || !Enum.TryParse(value, false, out parsed)
                || !Enum.IsDefined(typeof(T), parsed))
            {
                parsed = default(T);
                errors.Add(label + " has unsupported value '" + (value ?? "<null>") + "'. Enum values are case-sensitive.");
                return false;
            }

            return true;
        }

        [Serializable]
        private sealed class RawManifest
        {
            public int schemaVersion;
            public string balanceVersion;
            public string bundleHash;
            public List<RawFileEntry> files;
        }

        [Serializable]
        private sealed class RawFileEntry
        {
            public string resourcePath;
            public string contentHash;
        }

        [Serializable]
        private sealed class RawDocument<T>
        {
            public int schemaVersion;
            public string balanceVersion;
            public string contentHash;
            public List<T> items;
        }

        [Serializable]
        private sealed class RawWave
        {
            public string waveId;
            public int roundNumber;
            public string waveType;
            public float nextWaveDelaySeconds;
            public float bossTimeLimitSeconds;
            public bool enabled;
        }

        [Serializable]
        private sealed class RawWaveSpawn
        {
            public string waveId;
            public int spawnOrder;
            public string lanePolicy;
            public string monsterId;
            public int spawnCount;
            public float spawnDelaySeconds;
            public float spawnIntervalSeconds;
            public float hpMultiplier;
            public float moveSpeedMultiplier;
        }

        [Serializable]
        private sealed class RawBossPattern
        {
            public string waveId;
            public int patternOrder;
            public string patternType;
            public string triggerType;
            public float triggerValue;
            public float cooldownSeconds;
            public string skillId;
            public string parameterKey;
            public float parameterValue;
            public bool enabled;
        }

        [Serializable]
        private sealed class RawSkill
        {
            public string skillId;
            public string nameKey;
            public string descriptionKey;
            public string skillType;
            public string triggerType;
            public float cooldownSeconds;
            public float mpCost;
            public float castRange;
            public string targetPolicy;
            public int maxTargetCount;
            public string projectileId;
            public string animationKey;
            public string vfxKey;
            public string sfxKey;
            public bool enabled;
        }

        [Serializable]
        private sealed class RawAlienSkillLink
        {
            public long alienId;
            public string skillId;
            public int slotIndex;
            public int castPriority;
            public bool enabled;
        }

        [Serializable]
        private sealed class RawProjectile
        {
            public string projectileId;
            public string prefabKey;
            public string moveType;
            public float speed;
            public float lifetimeSeconds;
            public float hitRadius;
            public int pierceCount;
            public bool destroyOnHit;
            public string lostTargetPolicy;
            public bool enabled;
        }

        [Serializable]
        private sealed class RawSkillEffect
        {
            public string skillId;
            public int executionOrder;
            public string triggerPhase;
            public string effectType;
            public string magnitudeSource;
            public float baseMagnitude;
            public float coefficient;
            public float chance;
            public float durationSeconds;
            public float tickIntervalSeconds;
            public float radius;
            public int maxStacks;
            public string stackPolicy;
            public float bossMultiplier;
        }
    }
}
