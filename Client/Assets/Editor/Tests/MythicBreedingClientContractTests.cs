using System;
using NUnit.Framework;
using UnityEngine;

public sealed class MythicBreedingClientContractTests
{
    [Test]
    public void PathsMatchSpringControllerContract()
    {
        Assert.AreEqual("/mythic-breeding/slots?username=sh1", MythicBreedingClientContract.SlotsPath("sh1"));
        Assert.AreEqual("/mythic-breeding/candidates?username=sh1", MythicBreedingClientContract.CandidatesPath("sh1"));
        Assert.AreEqual("/mythic-breeding/slots/2/accelerate?username=sh1",
            MythicBreedingClientContract.SlotActionPath(2, "accelerate", "sh1"));
    }

    [Test]
    public void InstantAccelerationRoundsUpTenMinuteUnits()
    {
        DateTime now = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(2, MythicBreedingClientContract.CalculateAccelerationUnits(now.AddSeconds(601), now, 600));
        Assert.AreEqual(1, MythicBreedingClientContract.CalculateAccelerationUnits(now, now, 600));
    }

    [Test]
    public void CountdownUsesTotalHoursAndNeverShowsSystemTimestamp()
    {
        DateTime now = new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual("24:00:00", MythicBreedingClientContract.FormatRemainingTime(now.AddHours(24), now));
        Assert.AreEqual("00:00:01", MythicBreedingClientContract.FormatRemainingTime(now.AddMilliseconds(1), now));
        Assert.AreEqual("00:00:00", MythicBreedingClientContract.FormatRemainingTime(now.AddSeconds(-1), now));
    }

    [TestCase("AVAILABLE", true)]
    [TestCase("BREEDING", false)]
    [TestCase("REWARD_READY", false)]
    [TestCase("LOCKED", false)]
    public void ParentSelectionIsOnlyAllowedForAvailableSlot(string status, bool expected)
    {
        Assert.AreEqual(expected, MythicBreedingClientContract.CanSelectParents(status));
    }

    [Test]
    public void RequestJsonUsesServerFieldNames()
    {
        string start = JsonUtility.ToJson(new MythicBreedingStartRequest
            { parentUserAlienIdA = 11, parentUserAlienIdB = 12, requestId = "request" });
        StringAssert.Contains("\"parentUserAlienIdA\":11", start);
        StringAssert.Contains("\"parentUserAlienIdB\":12", start);
        StringAssert.Contains("\"requestId\":\"request\"", start);
    }

    [Test]
    public void StartIntentKeyIgnoresParentOrderButSeparatesPayloads()
    {
        Assert.AreEqual(MythicBreedingClientContract.IntentKey("start", 1, 11, 12),
            MythicBreedingClientContract.IntentKey("start", 1, 12, 11));
        Assert.AreNotEqual(MythicBreedingClientContract.IntentKey("start", 1, 11, 12),
            MythicBreedingClientContract.IntentKey("start", 1, 11, 13));
        Assert.AreNotEqual(MythicBreedingClientContract.IntentKey("accelerate", 1, 1),
            MythicBreedingClientContract.IntentKey("accelerate", 1, 2));
    }

    [Test]
    public void ShortcutStatusPrioritizesReadyReward()
    {
        var response = new MythicBreedingSlotsResponse
        {
            slots = new[]
            {
                new MythicBreedingSlotDto { status = "BREEDING" },
                new MythicBreedingSlotDto { status = "REWARD_READY" },
                new MythicBreedingSlotDto { status = "AVAILABLE" }
            }
        };
        Assert.AreEqual(1, MythicBreedingClientContract.CountRewardReady(response));
        Assert.AreEqual("보상 수령 가능 1개", MythicBreedingClientContract.BuildShortcutStatus(response));
    }

    [Test]
    public void CombinationTableUsesCanonicalMythicLabelsAndExclusiveMarkers()
    {
        var document = new MythicBreedingRecipeDocument
        {
            recipes = new[]
            {
                new MythicBreedingRecipeDto
                {
                    enabled = true,
                    parentAlienIdA = 29,
                    parentAlienIdB = 30,
                    standardResultAlienIds = new long[] { 29, 31, 32, 33, 34 },
                    exclusive19AlienId = 47,
                    exclusive20AlienId = 48
                }
            }
        };
        string table = MythicBreedingClientContract.BuildCombinationTable(document);
        StringAssert.Contains("M01 + M02", table);
        StringAssert.Contains("M19★", table);
        StringAssert.Contains("M20★", table);
    }
}
