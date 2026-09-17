using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class LobbyNavigationAssetTests
{
    [Test]
    public void RgbMascotSourceEncodesRealAlphaInPngWithoutChangingOriginal()
    {
        var source = new Texture2D(3, 3, TextureFormat.RGB24, false);
        var decoded = new Texture2D(2, 2);
        try
        {
            var pixels = Enumerable.Repeat(Color.white, 9).ToArray(); pixels[4] = Color.green;
            source.SetPixels(pixels); source.Apply();
            Assert.That(decoded.LoadImage(LobbyNavigationAssetBuilder.EncodeTransparentMascot(source)), Is.True);
            Assert.That(decoded.GetPixels32()[0].a, Is.Zero);
            Assert.That(decoded.GetPixels32()[4].a, Is.EqualTo(255));
            Assert.That(source.format, Is.EqualTo(TextureFormat.RGB24));
            Assert.That(source.GetPixels32()[0].a, Is.EqualTo(255));
        }
        finally { Object.DestroyImmediate(source); Object.DestroyImmediate(decoded); }
    }
    [Test]
    public void WhiteMaskRemovesOnlyBoundaryConnectedNearWhiteAndPreservesClosedHighlights()
    {
        var pixels = Enumerable.Repeat(new Color32(250, 249, 248, 255), 49).ToArray();
        var green = new Color32(34, 188, 81, 255);
        for (int y = 1; y <= 5; y++)
            for (int x = 1; x <= 5; x++) pixels[y * 7 + x] = green;
        pixels[3 * 7 + 3] = new Color32(255, 255, 255, 255);
        pixels[0] = new Color32(255, 239, 40, 255); // yellow antenna detail on boundary
        var original = (Color32[])pixels.Clone();
        var result = LobbyNavigationAssetBuilder.RemoveEdgeConnectedWhite(pixels, 7, 7);
        Assert.That(pixels, Is.EqualTo(original));
        Assert.That(result[1].a, Is.Zero);
        Assert.That(result[3 * 7 + 3].a, Is.EqualTo(255));
        Assert.That(result[0], Is.EqualTo(original[0]));
        for (int i = 0; i < pixels.Length; i++)
        {
            Assert.That(result[i].r, Is.EqualTo(original[i].r));
            Assert.That(result[i].g, Is.EqualTo(original[i].g));
            Assert.That(result[i].b, Is.EqualTo(original[i].b));
        }
    }

    [Test]
    public void AlphaUnionIncludesAllVisiblePixelsInBothStates()
    {
        var a = new Color32[100]; var b = new Color32[100];
        a[12] = new Color32(255, 0, 0, 1); a[45] = new Color32(255, 0, 0, 255);
        b[23] = new Color32(255, 0, 0, 255); b[78] = new Color32(255, 0, 0, 255);
        var union = LobbyNavigationAssetBuilder.UnionBounds(
            LobbyNavigationAssetBuilder.AlphaBounds(a, 10, 10), LobbyNavigationAssetBuilder.AlphaBounds(b, 10, 10));
        Assert.That(union, Is.EqualTo(new Rect(2, 1, 7, 7)));
        Assert.Throws<InvalidOperationException>(() => LobbyNavigationAssetBuilder.AlphaBounds(new Color32[4], 2, 2));
    }

    [Test]
    public void SuppliedIconsFollowOpenTabAndKeepButtonsLayoutAndCallbacksAfterRepeatedSetup()
    {
        var root = new GameObject("NavigationAssetTest", typeof(LobbyManager), typeof(LobbyFinalPresentation));
        var texture = new Texture2D(2, 2);
        var sprites = Enumerable.Range(0, 10).Select(_ => Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f)).ToArray();
        try
        {
            var presenter = root.GetComponent<LobbyFinalPresentation>();
            var lobby = root.GetComponent<LobbyManager>();
            presenter.navigation = LobbyFinalPresentation.Child(root.transform, "bottomNav");
            presenter.indicators = new GameObject[5];
            lobby.viewObjects = new GameObject[5];
            var buttons = new Button[5];
            var before = new string[5];
            int calls = 0;
            for (int i = 0; i < 5; i++)
            {
                var button = LobbyFinalPresentation.Child(presenter.navigation, LobbyNavigationAssetBuilder.ButtonNames[i]);
                button.anchoredPosition = new Vector2(i * 201, 10); button.sizeDelta = new Vector2(190, 160);
                buttons[i] = button.gameObject.AddComponent<Button>();
                int tab = i;
                buttons[i].onClick.AddListener(() => { calls++; lobby.OpenTab(tab); });
                var icon = LobbyFinalPresentation.Child(button, "FinalIcon");
                icon.gameObject.AddComponent<LobbyNavIconGraphic>();
                presenter.indicators[i] = LobbyFinalPresentation.Child(button, "SelectedUnderline").gameObject;
                lobby.viewObjects[i] = LobbyFinalPresentation.Child(root.transform, "View" + i).gameObject;
                before[i] = UnityEditor.EditorJsonUtility.ToJson(button);
            }
            var active = sprites.Take(5).ToArray(); var inactive = sprites.Skip(5).ToArray();
            var shader = Shader.Find("UI/MyDefense/LobbyIconSaturation");
            Assert.That(shader, Is.Not.Null);
            LobbyNavigationAssetBuilder.Configure(presenter, active, inactive, shader, 2);
            LobbyNavigationAssetBuilder.Configure(presenter, active, inactive, shader, 2);
            // Reapplying the older whole-screen design must retain the supplied sprite mode.
            typeof(LobbyFinalUiBuilder).GetMethod("StyleNavigation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { lobby, null });
            for (int selected = 0; selected < 5; selected++)
            {
                buttons[selected].onClick.Invoke();
                for (int i = 0; i < 5; i++)
                {
                    Assert.That(lobby.viewObjects[i].activeSelf, Is.EqualTo(i == selected));
                    Assert.That(presenter.indicators[i].activeSelf, Is.EqualTo(i == selected));
                    Assert.That(presenter.navigationImages[i].sprite, Is.SameAs(i == selected ? active[i] : inactive[i]));
                    Assert.That(presenter.navigationImages[i].preserveAspect, Is.True);
                    Assert.That(presenter.navigationImages[i].raycastTarget, Is.False);
                    if (i != 1) Assert.That(presenter.navigationImages[i].color, Is.EqualTo(Color.white));
                    Assert.That(presenter.navigationImages[i].GetComponents<Image>().Length, Is.EqualTo(1));
                    Assert.That(UnityEditor.EditorJsonUtility.ToJson(buttons[i].transform), Is.EqualTo(before[i]));
                }
            }
            Assert.That(calls, Is.EqualTo(5));
            Assert.That(root.GetComponentsInChildren<LobbyNavIconGraphic>(true), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
            foreach (var sprite in sprites) Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void MissingSpriteFailsBeforeAnySceneGraphicIsReplaced()
    {
        var root = new GameObject("NavigationMissingAssetTest", typeof(LobbyFinalPresentation));
        try
        {
            var presenter = root.GetComponent<LobbyFinalPresentation>();
            presenter.navigation = LobbyFinalPresentation.Child(root.transform, "bottomNav");
            foreach (string name in LobbyNavigationAssetBuilder.ButtonNames)
            {
                var button = LobbyFinalPresentation.Child(presenter.navigation, name);
                button.gameObject.AddComponent<Button>();
                LobbyFinalPresentation.Child(button, "FinalIcon").gameObject.AddComponent<LobbyNavIconGraphic>();
            }
            Assert.Throws<InvalidOperationException>(() => LobbyNavigationAssetBuilder.Configure(presenter,
                new Sprite[5], new Sprite[5], Shader.Find("UI/MyDefense/LobbyIconSaturation"), 0));
            Assert.That(root.GetComponentsInChildren<LobbyNavIconGraphic>(true).Length, Is.EqualTo(5));
            Assert.That(root.GetComponentsInChildren<Image>(true), Is.Empty);
            Assert.That(presenter.navigationImages, Is.Null);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
