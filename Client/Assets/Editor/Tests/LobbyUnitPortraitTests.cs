using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyUnitPortraitTests
{
    [Serializable] private sealed class CanonicalDocument { public CanonicalAlien[] values; }
    [Serializable] private sealed class CanonicalAlien { public long alienId; public string grade; public string name; }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void LegendaryPortraitMatchesCanonicalIdentityAndPreservesSourceCanvas(int id)
    {
        var catalog = Resources.Load<LobbyUnitPortraitCatalog>(LobbyUnitPortraitCatalog.ResourcePath);
        Assert.That(catalog, Is.Not.Null);
        string path = LobbyUnitPortraitAssetBuilder.ArtPath + LobbyUnitPortraitAssetBuilder.FileNames[id - 1];
        Sprite sprite = catalog.Find(id, "LEGEND");
        Assert.That(sprite, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo(path));
        Assert.That(catalog.Find(id, "LEGENDARY"), Is.SameAs(sprite));
        Assert.That(catalog.Find(id, "NORMAL"), Is.Null);
        var canonical = JsonUtility.FromJson<CanonicalDocument>("{\"values\":" +
            File.ReadAllText("Assets/StreamingAssets/Balance/generated/alien-spec.json") + "}");
        var alien = canonical.values.Single(a => a.alienId == id);
        Assert.That(alien.grade, Is.EqualTo("LEGEND"));
        Assert.That(alien.name, Is.EqualTo("LEGEND " + id));
        Assert.That(sprite.rect, Is.EqualTo(new Rect(0, 0, sprite.texture.width, sprite.texture.height)));
        Assert.That(sprite.texture.width, Is.LessThanOrEqualTo(512));
        Assert.That(sprite.rect.width, Is.EqualTo(sprite.rect.height));
        Assert.That(sprite.pivot, Is.EqualTo(sprite.rect.size * .5f));
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput));
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(importer.mipmapEnabled, Is.False);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True);
            Assert.That(texture.width, Is.EqualTo(1254));
            Assert.That(texture.height, Is.EqualTo(1254));
            var pixels = texture.GetPixels32();
            Assert.That(pixels.Any(p => p.a == 0), Is.True, "Transparent background retained");
            Assert.That(pixels.Any(p => p.a == 255), Is.True, "Opaque subject retained");
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    [Test]
    public void CardRebindingClearsMappedPortraitAndPreservesFallbackSelectionAndTexts()
    {
        WithCard(card =>
        {
            long selected = -1;
            card.SetData(Data(1, "LEGEND"), id => selected = id);
            Assert.That(card.portraitImage.sprite, Is.Not.Null);
            Assert.That(card.portraitImage.enabled, Is.True);
            Assert.That(card.portraitImage.preserveAspect, Is.True);
            Assert.That(card.portraitFallbackText.gameObject.activeSelf, Is.False);
            Assert.That(card.text_Name.text, Is.EqualTo("unchanged name"));
            Assert.That(card.text_Level.text, Is.EqualTo("Lv.13"));
            Assert.That(card.text_Pieces.text, Is.EqualTo("4/15"));
            Assert.That(card.text_Grade.color, Is.EqualTo((Color)new Color32(0x42, 0xe8, 0x87, 255)));
            card.GetComponent<Button>().onClick.Invoke();
            Assert.That(selected, Is.EqualTo(1));
            card.SetData(Data(8, "UNIQUE"), id => selected = id);
            Assert.That(card.portraitImage.sprite, Is.Null);
            Assert.That(card.portraitImage.enabled, Is.False);
            Assert.That(card.portraitFallbackText.gameObject.activeSelf, Is.True);
            Assert.That(card.portraitFallbackText.text, Is.EqualTo("U"));
            card.GetComponent<Button>().onClick.Invoke();
            Assert.That(selected, Is.EqualTo(8));
        });
    }

    [Test]
    public void UnmappedCardRestoresExistingAuthoredSpriteAfterLegendaryRebind()
    {
        var texture = new Texture2D(8, 16);
        var original = Sprite.Create(texture, new Rect(0, 0, 8, 16), Vector2.one * .5f);
        try
        {
            WithCard(card =>
            {
                card.portraitImage.sprite = original;
                card.SetData(Data(8, "UNIQUE"), null);
                Assert.That(card.portraitImage.sprite, Is.SameAs(original));
                card.SetData(Data(2, "LEGEND"), null);
                Assert.That(card.portraitImage.sprite, Is.Not.SameAs(original));
                card.SetData(Data(8, "UNIQUE"), null);
                Assert.That(card.portraitImage.sprite, Is.SameAs(original));
                Assert.That(card.portraitFallbackText.gameObject.activeSelf, Is.False);
            });
        }
        finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(texture); }
    }

    [Test]
    public void UnknownIdentityOrMissingEntryKeepsSafeFallback()
    {
        var catalog = ScriptableObject.CreateInstance<LobbyUnitPortraitCatalog>();
        try
        {
            catalog.entries = new[] { new LobbyUnitPortraitCatalog.Entry { alienId = 1, grade = "LEGEND" }, null };
            Assert.That(catalog.Find(1, "LEGEND"), Is.Null);
            Assert.That(catalog.Find(999, null), Is.Null);
            catalog.entries = null;
            Assert.That(catalog.Find(1, "LEGEND"), Is.Null);
        }
        finally { UnityEngine.Object.DestroyImmediate(catalog); }
    }

    private static AlienInventoryDto Data(long id, string grade)
    {
        return new AlienInventoryDto { id = id, grade = grade, name = "unchanged name", owned = true,
            level = 13, pieces = 4, requiredPieces = 15 };
    }

    private static void WithCard(Action<UnitCardUI> action)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lobby/UnitCard.prefab");
        Assert.That(prefab, Is.Not.Null);
        var instance = UnityEngine.Object.Instantiate(prefab);
        try { action(instance.GetComponent<UnitCardUI>()); }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
