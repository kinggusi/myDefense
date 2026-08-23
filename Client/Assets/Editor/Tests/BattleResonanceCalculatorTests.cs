using System.Collections.Generic;
using MyDefense.Battle.Balance.Canonical;
using NUnit.Framework;

public sealed class BattleResonanceCalculatorTests
{
    private CanonicalResonanceRegistry _registry;

    [SetUp]
    public void SetUp()
    {
        _registry = new CanonicalResonanceRegistry(new List<CanonicalResonanceLevel>
        {
            Level(CanonicalResonanceTrack.NORMAL, 1, 400, 1.05f, 1.01f),
            Level(CanonicalResonanceTrack.NORMAL, 2, 800, 1.10f, 1.02f),
            Level(CanonicalResonanceTrack.NORMAL, 3, 1400, 1.15f, 1.03f),
            Level(CanonicalResonanceTrack.NORMAL, 4, 2200, 1.20f, 1.04f),
            Level(CanonicalResonanceTrack.NORMAL, 5, 3200, 1.25f, 1.05f),
            Level(CanonicalResonanceTrack.MYTHIC, 1, 800, 1.08f, 1.01f),
            Level(CanonicalResonanceTrack.MYTHIC, 2, 1600, 1.16f, 1.02f),
            Level(CanonicalResonanceTrack.MYTHIC, 3, 2800, 1.24f, 1.03f),
            Level(CanonicalResonanceTrack.MYTHIC, 4, 4400, 1.32f, 1.04f),
            Level(CanonicalResonanceTrack.MYTHIC, 5, 6500, 1.40f, 1.05f)
        });
    }

    [TestCase((byte)0, CanonicalResonanceTrack.NORMAL)]
    [TestCase((byte)1, CanonicalResonanceTrack.NORMAL)]
    [TestCase((byte)2, CanonicalResonanceTrack.NORMAL)]
    [TestCase((byte)3, CanonicalResonanceTrack.NORMAL)]
    [TestCase((byte)4, CanonicalResonanceTrack.MYTHIC)]
    public void TrackForGrade_SeparatesMythic(byte grade, CanonicalResonanceTrack expected)
        => Assert.That(BattleResonanceCalculator.TrackForGrade(grade), Is.EqualTo(expected));

    [Test]
    public void Apply_LevelZero_PreservesBaseStats()
    {
        BattleResonanceStats value = BattleResonanceCalculator.Apply(_registry, 0, 0, 0, 100f, 2f, 4f);
        Assert.That(value.Damage, Is.EqualTo(100f));
        Assert.That(value.AttackRate, Is.EqualTo(2f));
        Assert.That(value.Range, Is.EqualTo(4f));
    }

    [Test]
    public void Apply_NormalLevelFive_UsesNormalMultipliersAndPreservesRange()
    {
        BattleResonanceStats value = BattleResonanceCalculator.Apply(_registry, 3, 5, 2, 100f, 2f, 4f);
        Assert.That(value.Damage, Is.EqualTo(125f).Within(0.001f));
        Assert.That(value.AttackRate, Is.EqualTo(2.1f).Within(0.001f));
        Assert.That(value.Range, Is.EqualTo(4f).Within(0.001f));
    }

    [Test]
    public void Apply_MythicLevelFive_UsesMythicMultipliersAndPreservesRange()
    {
        BattleResonanceStats value = BattleResonanceCalculator.Apply(_registry, 4, 2, 5, 100f, 2f, 4f);
        Assert.That(value.Damage, Is.EqualTo(140f).Within(0.001f));
        Assert.That(value.AttackRate, Is.EqualTo(2.1f).Within(0.001f));
        Assert.That(value.Range, Is.EqualTo(4f).Within(0.001f));
    }

    [Test]
    public void TryGetNextCost_UsesCanonicalNextLevelAndStopsAtMax()
    {
        Assert.That(BattleResonanceCalculator.TryGetNextCost(_registry, CanonicalResonanceTrack.NORMAL, 2, out int cost), Is.True);
        Assert.That(cost, Is.EqualTo(1400));
        Assert.That(BattleResonanceCalculator.TryGetNextCost(_registry, CanonicalResonanceTrack.NORMAL, 5, out _), Is.False);
    }

    [Test]
    public void TryPurchaseNextLevel_ChargesExactlyOnceAndRejectsInsufficientOrMax()
    {
        Assert.That(BattleResonanceCalculator.TryPurchaseNextLevel(
            _registry, CanonicalResonanceTrack.NORMAL, 0, 1000, out int next, out int remaining), Is.True);
        Assert.That(next, Is.EqualTo(1));
        Assert.That(remaining, Is.EqualTo(600));

        Assert.That(BattleResonanceCalculator.TryPurchaseNextLevel(
            _registry, CanonicalResonanceTrack.NORMAL, 0, 399, out next, out remaining), Is.False);
        Assert.That(next, Is.EqualTo(0));
        Assert.That(remaining, Is.EqualTo(399));

        Assert.That(BattleResonanceCalculator.TryPurchaseNextLevel(
            _registry, CanonicalResonanceTrack.NORMAL, 5, 100000, out next, out remaining), Is.False);
        Assert.That(next, Is.EqualTo(5));
        Assert.That(remaining, Is.EqualTo(100000));
    }

    private static CanonicalResonanceLevel Level(
        CanonicalResonanceTrack track,
        int level,
        int gold,
        float attack,
        float speed)
        => new CanonicalResonanceLevel(track, level, gold, attack, speed, 1f);
}
