using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Balance;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public class BattleBalanceModelsTests
    {
        [Test]
        public void DataModels_ExposeNoPublicPropertySetters()
        {
            Type[] modelTypes =
            {
                typeof(BattleBalanceManifestData),
                typeof(BattleBalanceFileEntryData),
                typeof(BattleBalanceDocument<WaveSpecData>),
                typeof(WaveSpecData),
                typeof(WaveSpawnSpecData),
                typeof(BossPatternSpecData),
                typeof(SkillSpecData),
                typeof(AlienSkillLinkData),
                typeof(ProjectileSpecData),
                typeof(SkillEffectSpecData),
                typeof(BattleMonsterDefinition)
            };

            foreach (Type modelType in modelTypes)
            {
                PropertyInfo[] writableProperties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.SetMethod != null && property.SetMethod.IsPublic)
                    .ToArray();
                Assert.That(writableProperties, Is.Empty, modelType.Name + " must be immutable after construction.");
            }
        }

        [Test]
        public void DocumentItems_AreCopiedAndReadOnly()
        {
            var source = new List<WaveSpecData>
            {
                new WaveSpecData("WAVE_001", 1, WaveType.REGULAR, 1f, 0f, true)
            };
            var document = new BattleBalanceDocument<WaveSpecData>(1, "V1", BattleBalanceTestFixture.ContentHash, source);

            source.Add(new WaveSpecData("WAVE_002", 2, WaveType.REGULAR, 1f, 0f, true));

            Assert.That(document.Items.Count, Is.EqualTo(1));
            var mutableView = (IList<WaveSpecData>)document.Items;
            Assert.Throws<NotSupportedException>(() => mutableView.Add(source[1]));
        }

        [Test]
        public void RequiredEnums_ContainContractValues()
        {
            Assert.That(Enum.GetNames(typeof(WaveType)), Is.EquivalentTo(new[] { "REGULAR", "BOSS" }));
            Assert.That(Enum.GetNames(typeof(BattleLanePolicy)), Is.EquivalentTo(new[] { "EACH_ACTIVE_PLAYER_LANE", "BOSS_SHARED" }));
            Assert.That(Enum.GetNames(typeof(ProjectileMoveType)), Does.Contain("HOMING"));
            Assert.That(Enum.GetNames(typeof(ProjectileLostTargetPolicy)), Does.Contain("RETARGET"));
            Assert.That(Enum.GetNames(typeof(BattleSkillEffectType)), Does.Contain("DAMAGE_OVER_TIME"));
            Assert.That(Enum.GetNames(typeof(SkillMagnitudeSource)), Is.EquivalentTo(new[] { "FLAT", "ATTACK_SNAPSHOT_DAMAGE" }));
        }

        [Test]
        public void ResourceContract_UsesManifestAndSevenExtensionlessDocuments()
        {
            Assert.That(BattleBalanceResourcePaths.Manifest, Is.EqualTo("Balance/Battle/battle-balance-manifest"));
            Assert.That(BattleBalanceResourcePaths.RequiredDocumentPaths.Count, Is.EqualTo(7));
            Assert.That(BattleBalanceResourcePaths.RequiredDocumentPaths.All(path => !BattleBalanceResourcePaths.HasFileExtension(path)), Is.True);
        }

        [Test]
        public void OptionalNumericAmbiguity_IsRemovedFromModels()
        {
            Assert.That(typeof(SkillSpecData).GetProperty("MaxTargetCount").PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(typeof(BossPatternSpecData).GetProperty("ParameterValue").PropertyType, Is.EqualTo(typeof(float)));
        }
    }
}
