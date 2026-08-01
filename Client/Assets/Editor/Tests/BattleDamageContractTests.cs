using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleDamageContractTests
{
    [Test]
    public void AttackSnapshotUsesPrecomputedStatsAndNormalizesMissingMutation()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 25f, 2f, 8f, " ");

        Assert.That(snapshot.AttackerServerId, Is.EqualTo(17));
        Assert.That(snapshot.Damage, Is.EqualTo(25f));
        Assert.That(snapshot.ActiveMutationType, Is.EqualTo("NONE"));
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
