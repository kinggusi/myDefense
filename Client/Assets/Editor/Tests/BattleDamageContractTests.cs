using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleDamageContractTests
{
    [Test]
    public void UnitSpeciesColorsUseFixedSevenColorPaletteAcrossGrades()
    {
        MethodInfo method = typeof(FusionKidnapBoardView).GetMethod(
            "ColorForAlien",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var legendColors = Enumerable.Range(1, 7)
            .Select(id => (Color)method.Invoke(null, new object[] { (long)id }))
            .ToArray();
        var normalColors = Enumerable.Range(22, 7)
            .Select(id => (Color)method.Invoke(null, new object[] { (long)id }))
            .ToArray();

        Assert.That(legendColors.Distinct().Count(), Is.EqualTo(7));
        Assert.That(normalColors, Is.EqualTo(legendColors));
    }

    [Test]
    public void AttackSnapshotUsesPrecomputedStatsAndNormalizesMissingMutation()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 25f, 2f, 8f, " ");

        Assert.That(snapshot.AttackerServerId, Is.EqualTo(17));
        Assert.That(snapshot.Damage, Is.EqualTo(25f));
        Assert.That(snapshot.ActiveMutationType, Is.EqualTo("NONE"));
    }

    [Test]
    public void ActiveMutationAppliesCanonicalStatsAndMechanicOnlyOnce()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "TOXIC");
        var spec = new CanonicalMutationSpec(
            "TOXIC", true, true, true, 1,
            1.1f, 1f, 1f, 1f, 1f,
            "DOT", dotDamageMultiplier: 0.2f, dotTickCount: 3, dotTickIntervalSeconds: 1f);

        AlienAttackSnapshot result = MutationAttackSnapshotCalculator.Apply(snapshot, spec);

        Assert.That(result.Damage, Is.EqualTo(110f).Within(0.001f));
        Assert.That(result.DotDamagePerTick, Is.EqualTo(22f).Within(0.001f));
        Assert.That(result.DotTickCount, Is.EqualTo(3));
        Assert.That(result.DotTickIntervalSeconds, Is.EqualTo(1f));
    }

    [Test]
    public void SealedOrMissingMutationKeepsServerSnapshotUnchanged()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "NONE");
        var spec = new CanonicalMutationSpec(
            "GIANT", true, true, true, 1,
            1.35f, 1f, 0.9f, 1.1f, 1f,
            "SPLASH", splashRadius: 2.5f, splashDamageMultiplier: 0.65f);

        AlienAttackSnapshot result = MutationAttackSnapshotCalculator.Apply(snapshot, spec);

        Assert.That(result.Damage, Is.EqualTo(100f));
        Assert.That(result.SplashRadius, Is.EqualTo(0f));
    }

    [Test]
    public void BossAndGambleDamageAreDeterministicFromAuthorityProjectileId()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "OBESE");
        snapshot.BossDamageMultiplier = 2f;
        snapshot.GambleSuccessChance = 0.25f;
        snapshot.GambleSuccessMultiplier = 2.5f;
        snapshot.GambleFailureMultiplier = 0.5f;

        float first = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 42, true);
        float retry = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 42, true);
        float other = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 43, true);

        Assert.That(first, Is.EqualTo(retry));
        Assert.That(first, Is.EqualTo(100f).Or.EqualTo(500f));
        Assert.That(other, Is.EqualTo(100f).Or.EqualTo(500f));
    }

    [Test]
    public void ServerCalculatedAttackCatalogMustMatchPinnedCanonicalBalance()
    {
        var response = new FusionKidnapBoardView.BattleAttackSnapshotCatalogJson
        {
            balanceVersion = "1-version",
            contentHash = "ABCDEF"
        };

        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "1-version", "abcdef"), Is.True);
        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "2-version", "abcdef"), Is.False);
        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "1-version", "other"), Is.False);
    }

    [Test]
    public void FusionMutationStateFeedsAttackSnapshotMetadata()
    {
        GameObject unit = new GameObject("mutation-state-test");
        try
        {
            UnitData data = unit.AddComponent<UnitData>();
            FusionKidnapBoardView.ApplyMutationState(data, 3, "FROZEN");
            Assert.That(data.activeMutationType, Is.EqualTo("FROZEN"));
            Assert.That(data.pendingMutationType, Is.Null);

            FusionKidnapBoardView.ApplyMutationState(data, 4, "FROZEN");
            Assert.That(data.activeMutationType, Is.Null);
            Assert.That(data.pendingMutationType, Is.EqualTo("FROZEN"));
        }
        finally
        {
            Object.DestroyImmediate(unit);
        }
    }

    [Test]
    public void AuthoritativeBoardStateSync_RebindsMovedAndSwappedUnitsOnlyOnce()
    {
        MethodInfo sync = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyAuthoritativeUnitState",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(sync, Is.Not.Null);

        GameObject sourceObject = new GameObject("source-unit-test");
        GameObject targetObject = new GameObject("target-unit-test");
        try
        {
            UnitData source = sourceObject.AddComponent<UnitData>();
            UnitData target = targetObject.AddComponent<UnitData>();
            ApplyAuthoritativeState(sync, source, 1, 2, 22, 0, 0, null);
            ApplyAuthoritativeState(sync, target, 1, 5, 23, 1, 0, null);

            Assert.That(ApplyAuthoritativeState(sync, source, 1, 5, 22, 0, 0, null), Is.True);
            Assert.That(ApplyAuthoritativeState(sync, target, 1, 2, 23, 1, 0, null), Is.True);
            Assert.That(source.serverId, Is.EqualTo(((long)1 << 32) | 6u));
            Assert.That(target.serverId, Is.EqualTo(((long)1 << 32) | 3u));
            Assert.That(source.specId, Is.EqualTo(22));
            Assert.That(target.specId, Is.EqualTo(23));
            Assert.That(source.grade, Is.EqualTo("NORMAL"));
            Assert.That(target.grade, Is.EqualTo("EPIC"));

            Assert.That(ApplyAuthoritativeState(sync, source, 1, 5, 22, 0, 0, null), Is.False);
            Assert.That(ApplyAuthoritativeState(sync, target, 1, 2, 23, 1, 0, null), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void AuthoritativeBoardStateSync_RefreshesMergeAndActiveMutationButNotPendingDna()
    {
        MethodInfo sync = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyAuthoritativeUnitState",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(sync, Is.Not.Null);

        GameObject unitObject = new GameObject("merged-unit-test");
        try
        {
            UnitData data = unitObject.AddComponent<UnitData>();
            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 15, 2, 0, null), Is.True);
            Assert.That(data.serverId, Is.EqualTo(((long)2 << 32) | 8u));
            Assert.That(data.specId, Is.EqualTo(15));
            Assert.That(data.grade, Is.EqualTo("UNIQUE"));

            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 15, 2, 2, "TOXIC"), Is.False);
            Assert.That(data.pendingMutationType, Is.EqualTo("TOXIC"));
            Assert.That(data.activeMutationType, Is.Null);

            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 29, 4, 3, "FROZEN"), Is.True);
            Assert.That(data.specId, Is.EqualTo(29));
            Assert.That(data.grade, Is.EqualTo("MYTHIC"));
            Assert.That(data.pendingMutationType, Is.Null);
            Assert.That(data.activeMutationType, Is.EqualTo("FROZEN"));
            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 29, 4, 3, "FROZEN"), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void InitialMutationVisual_ShowsOnlyActiveMutationAndSealedStateClearsIt()
    {
        MethodInfo applyInitial = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyInitialActiveMutationVisual",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(applyInitial, Is.Not.Null);

        GameObject unit = new GameObject("mutation-visual-state-test");
        try
        {
            UnitData data = unit.AddComponent<UnitData>();
            FusionKidnapBoardView.ApplyMutationState(data, 2, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.False);
            Assert.That(unit.GetComponent<MutationAuraView>(), Is.Null);

            FusionKidnapBoardView.ApplyMutationState(data, 3, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.True);
            MutationAuraView aura = unit.GetComponent<MutationAuraView>();
            Assert.That(aura, Is.Not.Null);
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.True);
            Assert.That(unit.GetComponentInChildren<TextMesh>().text, Is.EqualTo("M:T"));

            FusionKidnapBoardView.ApplyMutationState(data, 4, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.False);
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.False);
            Assert.That(unit.GetComponentInChildren<TextMesh>().text, Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(unit);
        }
    }

    private static bool ApplyAuthoritativeState(
        MethodInfo method,
        UnitData data,
        int playerSlot,
        int slotIndex,
        long alienId,
        byte grade,
        byte mutationState,
        string mutationType)
        => (bool)method.Invoke(null, new object[]
        {
            data, playerSlot, slotIndex, alienId, grade, mutationState, mutationType
        });

    [Test]
    public void HitEventCarriesAuthorityIdentityAndRejectsInvalidDamage()
    {
        DamagePayload payload = new DamagePayload
        {
            BattleSessionId = "session",
            RuntimeProjectileId = 4,
            TargetRuntimeId = 9,
            AttackerId = 17,
            Amount = 20f,
            ActiveMutationType = "FROZEN"
        };
        HitEvent hit = new HitEvent("session", 4, 9, 17, payload, 12);

        Assert.That(hit.Payload.ActiveMutationType, Is.EqualTo("FROZEN"));
        Assert.That(hit.TargetRuntimeId, Is.EqualTo(9));
        Assert.Throws<System.ArgumentException>(() =>
            new HitEvent("session", 4, 9, 17, new DamagePayload { Amount = 0f }, 12));
    }

    [Test]
    public void SupportKillIsRecordedOnceWithoutChangingKillerOrGoldStats()
    {
        var deduplicator = new BattleKillDeduplicator();
        BattleRuntimeMonsterKey key = new BattleRuntimeMonsterKey("session", 1);
        BattleKillAuditRecord record = new BattleKillAuditRecord(
            key, "MONSTER", "killer", "owner", BattleMonsterLanePolicy.EACH_FIELD, 1, 10);

        Assert.That(deduplicator.TryRegister(record), Is.True);
        Assert.That(deduplicator.TryAttachSupport(key, "support"), Is.True);
        Assert.That(deduplicator.TryAttachSupport(key, "support-2"), Is.False);
        Assert.That(deduplicator.Records.Single().SupportPlayerId, Is.EqualTo("support"));
    }

    [Test]
    public void SummaryCountsSupportKillsSeparately()
    {
        var session = new BattleSessionContext("session", "balance", "hash", "battle", "battle-hash", 1);
        BattleRuntimeMonsterKey key = new BattleRuntimeMonsterKey("session", 1);
        BattleKillAuditRecord record = new BattleKillAuditRecord(
            key, "MONSTER", "killer", "owner", BattleMonsterLanePolicy.EACH_FIELD, 1, 10, "support");

        BattleSummary summary = BattleSummaryBuilder.Build(
            session,
            MatchState.CLEARED,
            1,
            new[]
            {
                new BattlePlayerSummarySeed("killer", false, null, 0, 0),
                new BattlePlayerSummarySeed("support", false, null, 0, 0)
            },
            new[] { record });

        Assert.That(summary.Players.Single(player => player.PlayerId == "killer").Kills, Is.EqualTo(1));
        Assert.That(summary.Players.Single(player => player.PlayerId == "support").SupportKills, Is.EqualTo(1));
    }
}
