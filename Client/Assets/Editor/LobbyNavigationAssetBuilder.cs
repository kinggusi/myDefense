#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates only bottom-navigation presentation. Existing Button callbacks and layout are retained.</summary>
public static class LobbyNavigationAssetBuilder
{
    public const string DirectoryPath = "Assets/Art/Lobby/Final/Navigation/";
    public const string MascotSourcePath = "Assets/Art/Lobby/Final/WakjeoMascot.jpg";
    public const string MascotPath = "Assets/Art/Lobby/Final/WakjeoMascot.png";
    public static readonly string[] ButtonNames = { "Button_Shop", "Button_Unit", "Button_Main", "Button_Clan", "Button_Etc" };
    private static readonly string[] AssetNames = { "Shop", null, "Battle", "Clan", "Content" };

    [MenuItem("Tools/MyDefense/Lobby/Apply Supplied Navigation Icons")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 적용하세요.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity" || scene.isDirty)
            throw new InvalidOperationException("SampleScene을 열고 저장한 뒤 적용하세요.");
        var lobby = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
        var presenter = lobby == null ? null : lobby.GetComponent<LobbyFinalPresentation>();
        ValidateNavigation(presenter);
        // Complete file/import checks before changing any scene object.
        foreach (var name in AssetNames.Where(n => n != null))
            foreach (var state in new[] { "Active", "Inactive" })
                RequireFile(DirectoryPath + name + state + ".png");
        RequireFile(MascotSourcePath);
        PrepareMascot();
        var active = new Sprite[5];
        var inactive = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            if (i == 1)
            {
                using (var source = new LoadedTexture(MascotPath))
                    active[i] = inactive[i] = ImportSprite(MascotPath, AlphaBounds(source.Texture.GetPixels32(), source.Texture.width, source.Texture.height));
                continue;
            }
            string first = DirectoryPath + AssetNames[i] + "Active.png";
            string second = DirectoryPath + AssetNames[i] + "Inactive.png";
            using (var a = new LoadedTexture(first))
            using (var b = new LoadedTexture(second))
            {
                if (a.Texture.width != b.Texture.width || a.Texture.height != b.Texture.height)
                    throw new InvalidOperationException("활성/비활성 아이콘 원본 크기가 다릅니다: " + AssetNames[i]);
                var bounds = UnionBounds(AlphaBounds(a.Texture.GetPixels32(), a.Texture.width, a.Texture.height),
                    AlphaBounds(b.Texture.GetPixels32(), b.Texture.width, b.Texture.height));
                active[i] = ImportSprite(first, bounds);
                inactive[i] = ImportSprite(second, bounds);
            }
        }
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Ui/LobbyIconSaturation.shader");
        int selected = Array.FindIndex(lobby.viewObjects, v => v != null && v.activeSelf);
        Configure(presenter, active, inactive, shader, selected < 0 ? 2 : selected);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[LobbyNavigation] 제공 아이콘과 원본 마스코트 투명 배경 적용. 버튼 기능/프로필/게임 데이터 변경 없음.");
    }

    public static void ValidateNavigation(LobbyFinalPresentation presenter)
    {
        if (presenter == null || presenter.navigation == null)
            throw new InvalidOperationException("기존 LobbyFinalPresentation / bottomNav 참조가 필요합니다.");
        foreach (string name in ButtonNames)
        {
            var button = presenter.navigation.Find(name);
            if (button == null || button.GetComponent<Button>() == null || button.Find("FinalIcon") == null)
                throw new InvalidOperationException("기존 메뉴 버튼/FinalIcon 누락: " + name);
        }
    }

    public static void Configure(LobbyFinalPresentation presenter, Sprite[] active, Sprite[] inactive, Shader shader, int selected)
    {
        ValidateNavigation(presenter);
        if (active == null || inactive == null || active.Length != 5 || inactive.Length != 5 ||
            active.Any(s => s == null) || inactive.Any(s => s == null) || shader == null)
            throw new InvalidOperationException("5개 탭의 활성/비활성 Sprite와 마스코트 Shader를 모두 준비하세요.");
        var images = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var icon = presenter.navigation.Find(ButtonNames[i]).Find("FinalIcon");
            foreach (var symbol in icon.GetComponents<LobbyNavIconGraphic>()) UnityEngine.Object.DestroyImmediate(symbol);
            var image = LobbyFinalPresentation.Get<Image>(icon.gameObject);
            image.enabled = true;
            image.sprite = inactive[i];
            image.overrideSprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.material = null;
            image.color = Color.white;
            images[i] = image;
        }
        presenter.navigationImages = images;
        presenter.activeNavigationSprites = (Sprite[])active.Clone();
        presenter.inactiveNavigationSprites = (Sprite[])inactive.Clone();
        presenter.symbols = new LobbyNavIconGraphic[0];
        presenter.mascot = images[1];
        presenter.iconShader = shader;
        presenter.SelectTab(selected);
        EditorUtility.SetDirty(presenter);
    }

    public static void PrepareMascot()
    {
        RequireFile(MascotSourcePath);
        using (var source = new LoadedTexture(MascotSourcePath))
        {
            File.WriteAllBytes(MascotPath, EncodeTransparentMascot(source.Texture));
        }
        AssetDatabase.ImportAsset(MascotPath, ImportAssetOptions.ForceSynchronousImport);
    }

    public static byte[] EncodeTransparentMascot(Texture2D source)
    {
        // LoadImage(JPEG) changes the source format to RGB24, which cannot retain alpha.
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        try
        {
            output.SetPixels32(RemoveEdgeConnectedWhite(source.GetPixels32(), source.width, source.height));
            output.Apply();
            return output.EncodeToPNG();
        }
        finally { UnityEngine.Object.DestroyImmediate(output); }
    }

    // Only alpha changes. Enclosed white eye highlights and every non-background RGB value are retained.
    public static Color32[] RemoveEdgeConnectedWhite(Color32[] pixels, int width, int height)
    {
        ValidatePixels(pixels, width, height);
        var result = (Color32[])pixels.Clone();
        var visited = new bool[pixels.Length];
        var queue = new Queue<int>();
        Action<int> visit = index =>
        {
            if (visited[index]) return;
            visited[index] = true;
            Color32 p = pixels[index];
            int maximum = Math.Max(p.r, Math.Max(p.g, p.b));
            int minimum = Math.Min(p.r, Math.Min(p.g, p.b));
            if (minimum < 235 || maximum - minimum > 20) return;
            result[index].a = 0;
            queue.Enqueue(index);
        };
        for (int x = 0; x < width; x++) { visit(x); visit((height - 1) * width + x); }
        for (int y = 0; y < height; y++) { visit(y * width); visit(y * width + width - 1); }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue(), x = index % width, y = index / width;
            if (x > 0) visit(index - 1);
            if (x + 1 < width) visit(index + 1);
            if (y > 0) visit(index - width);
            if (y + 1 < height) visit(index + width);
        }
        return result;
    }

    public static Rect AlphaBounds(Color32[] pixels, int width, int height)
    {
        ValidatePixels(pixels, width, height);
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0) continue;
            int x = i % width, y = i / width;
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
        if (maxX < 0) throw new InvalidOperationException("아이콘에 보이는 픽셀이 없습니다.");
        return Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
    }

    public static Rect UnionBounds(Rect a, Rect b)
    {
        return Rect.MinMaxRect(Math.Min(a.xMin, b.xMin), Math.Min(a.yMin, b.yMin), Math.Max(a.xMax, b.xMax), Math.Max(a.yMax, b.yMax));
    }

    private static Sprite ImportSprite(string path, Rect bounds)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("TextureImporter 누락: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        // A one-sprite sheet provides a stable alpha-union crop without modifying the source pixels.
#pragma warning disable 0618
        importer.spritesheet = new[] { new SpriteMetaData { name = Path.GetFileNameWithoutExtension(path), rect = bounds, alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) } };
#pragma warning restore 0618
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Sprite 생성 실패: " + path);
        return sprite;
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("아이콘 원본 파일 누락. Scene은 변경하지 않았습니다.", path);
    }
    private static void ValidatePixels(Color32[] pixels, int width, int height)
    {
        if (pixels == null || width <= 0 || height <= 0 || pixels.Length != width * height)
            throw new ArgumentException("잘못된 픽셀 크기입니다.");
    }
    private sealed class LoadedTexture : IDisposable
    {
        public readonly Texture2D Texture;
        public LoadedTexture(string path)
        {
            RequireFile(path);
            Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!Texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(Texture);
                throw new InvalidOperationException("이미지 읽기 실패: " + path);
            }
        }
        public void Dispose() { UnityEngine.Object.DestroyImmediate(Texture); }
    }
}
#endif
