using System;
using UnityEngine;

/// <summary>Lobby-only artwork references; never used as gameplay or balance data.</summary>
[CreateAssetMenu(menuName = "MyDefense/Lobby/Unit Portrait Catalog")]
public sealed class LobbyUnitPortraitCatalog : ScriptableObject
{
    public const string ResourcePath = "Lobby/UnitPortraitCatalog";

    [Serializable]
    public sealed class Entry
    {
        public long alienId;
        public string grade;
        public Sprite sprite;
    }

    public Entry[] entries = new Entry[0];

    public Sprite Find(long alienId, string grade)
    {
        if (entries == null) return null;
        string expected = NormalizeGrade(grade);
        foreach (var entry in entries)
            if (entry != null && entry.alienId == alienId &&
                NormalizeGrade(entry.grade) == expected && entry.sprite != null)
                return entry.sprite;
        return null;
    }

    private static string NormalizeGrade(string grade)
    {
        string value = (grade ?? string.Empty).Trim().ToUpperInvariant();
        return value == "LEGENDARY" ? "LEGEND" : value;
    }
}
