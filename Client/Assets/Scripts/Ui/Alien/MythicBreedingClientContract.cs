using System;
using System.Text;

public static class MythicBreedingClientContract
{
    public static string SlotsPath(string escapedUsername) => "/mythic-breeding/slots?username=" + escapedUsername;
    public static string CandidatesPath(string escapedUsername) => "/mythic-breeding/candidates?username=" + escapedUsername;
    public static string SlotActionPath(int slotNo, string action, string escapedUsername) =>
        "/mythic-breeding/slots/" + slotNo + "/" + action + "?username=" + escapedUsername;

    public static int CalculateAccelerationUnits(DateTime readyAtUtc, DateTime nowUtc, int unitSeconds)
    {
        if (unitSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(unitSeconds));
        double remainingSeconds = Math.Max(0, (readyAtUtc.ToUniversalTime() - nowUtc.ToUniversalTime()).TotalSeconds);
        return Math.Max(1, (int)Math.Ceiling(remainingSeconds / unitSeconds));
    }

    public static bool CanSelectParents(string slotStatus) => slotStatus == "AVAILABLE";

    public static string FormatRemainingTime(DateTime readyAtUtc, DateTime nowUtc)
    {
        long remainingSeconds = Math.Max(0L,
            (long)Math.Ceiling((readyAtUtc.ToUniversalTime() - nowUtc.ToUniversalTime()).TotalSeconds));
        long hours = remainingSeconds / 3600L;
        long minutes = remainingSeconds % 3600L / 60L;
        long seconds = remainingSeconds % 60L;
        return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    public static string IntentKey(string action, int slotNo, long valueA = 0, long valueB = 0)
    {
        if (action == "start" && valueA > valueB) (valueA, valueB) = (valueB, valueA);
        return action + ":" + slotNo + ":" + valueA + ":" + valueB;
    }

    public static int CountRewardReady(MythicBreedingSlotsResponse response)
    {
        int count = 0;
        if (response?.slots == null) return count;
        foreach (MythicBreedingSlotDto slot in response.slots)
            if (slot != null && slot.status == "REWARD_READY") count++;
        return count;
    }

    public static string BuildShortcutStatus(MythicBreedingSlotsResponse response)
    {
        if (response?.slots == null) return "교배 상태 확인";
        int ready = 0;
        int breeding = 0;
        int available = 0;
        foreach (MythicBreedingSlotDto slot in response.slots)
        {
            if (slot == null) continue;
            if (slot.status == "REWARD_READY") ready++;
            else if (slot.status == "BREEDING") breeding++;
            else if (slot.status == "AVAILABLE") available++;
        }
        if (ready > 0) return "보상 수령 가능 " + ready + "개";
        if (breeding > 0) return "교배 진행 중 " + breeding + "개";
        return available > 0 ? "사용 가능 슬롯 " + available + "개" : "슬롯 해금 필요";
    }

    public static string BuildCombinationTable(MythicBreedingRecipeDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("부모 조합별 결과 후보");
        builder.AppendLine("일반 후보 각 19.2% / 교배 전용 M19·M20 각 2%");
        builder.AppendLine();
        if (document?.recipes == null) return builder.Append("조합표를 불러오지 못했습니다.").ToString();
        foreach (MythicBreedingRecipeDto recipe in document.recipes)
        {
            if (recipe == null || !recipe.enabled) continue;
            builder.Append(ToMythicLabel(recipe.parentAlienIdA)).Append(" + ")
                .Append(ToMythicLabel(recipe.parentAlienIdB)).Append("  →  ");
            if (recipe.standardResultAlienIds != null)
                for (int i = 0; i < recipe.standardResultAlienIds.Length; i++)
                {
                    if (i > 0) builder.Append(", ");
                    builder.Append(ToMythicLabel(recipe.standardResultAlienIds[i]));
                }
            builder.Append(", ").Append(ToMythicLabel(recipe.exclusive19AlienId)).Append("★")
                .Append(", ").Append(ToMythicLabel(recipe.exclusive20AlienId)).Append("★")
                .AppendLine();
        }
        return builder.ToString();
    }

    private static string ToMythicLabel(long alienId) => "M" + (alienId - 28).ToString("00");
}
