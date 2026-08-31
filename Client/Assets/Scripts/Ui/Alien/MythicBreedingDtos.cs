using System;

[Serializable] public sealed class MythicBreedingSlotDto { public int slotNo; public string status; public string unlockSource; public string startedAt; public string readyAt; }
[Serializable] public sealed class MythicBreedingSlotsResponse { public MythicBreedingSlotDto[] slots; public int accountLevel; public int diamond; public int slot2UnlockLevel; public int slot2GemPrice; public int slot3GemPrice; public int durationSeconds; public int accelerationUnitSeconds; public int accelerationUnitDiamondCost; }
[Serializable] public sealed class MythicBreedingCandidateDto { public long userAlienId; public long alienId; public string name; public int level; public bool selectable; }
[Serializable] public sealed class MythicBreedingCandidatesResponse { public MythicBreedingCandidateDto[] candidates; }
[Serializable] public sealed class MythicBreedingUnlockRequest { public string requestId; }
[Serializable] public sealed class MythicBreedingStartRequest { public long parentUserAlienIdA; public long parentUserAlienIdB; public string requestId; }
[Serializable] public sealed class MythicBreedingClaimRequest { public string requestId; }
[Serializable] public sealed class MythicBreedingAccelerateRequest { public string requestId; public int units; }
[Serializable] public sealed class MythicBreedingStartResponse { public int slotNo; public string status; public string readyAt; }
[Serializable] public sealed class MythicBreedingClaimResponse { public int slotNo; public long resultAlienId; public string status; public string claimedAt; }
[Serializable] public sealed class MythicBreedingAccelerateResponse { public int slotNo; public string status; public int requestedUnits; public int appliedUnits; public int spentDiamond; public int remainingDiamond; public string readyAt; }
[Serializable] public sealed class MythicBreedingRecipeDocument { public MythicBreedingRecipeDto[] recipes; }
[Serializable] public sealed class MythicBreedingRecipeDto
{
    public string recipeKey;
    public long parentAlienIdA;
    public long parentAlienIdB;
    public long[] standardResultAlienIds;
    public int standardWeightEach;
    public long exclusive19AlienId;
    public int exclusive19Weight;
    public long exclusive20AlienId;
    public int exclusive20Weight;
    public bool enabled;
}
