#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Imports supplied portraits without touching scenes, prefabs or canonical balance.</summary>
public static class LobbyUnitPortraitAssetBuilder
{
    public const string ArtPath = "Assets/Art/Lobby/Units/Legendary/";
    public const string CatalogPath = "Assets/Resources/Lobby/UnitPortraitCatalog.asset";
    public static readonly string[] FileNames =
    {
        "01_flame.png", "02_twin_blades.png", "03_golem.png", "04_cannon.png",
        "05_psionic.png", "06_crescent.png", "07_guardian.png"
    };

    [MenuItem("Tools/MyDefense/Lobby/Import Legendary Portraits")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 적용하세요.");
        foreach (string file in FileNames)
            if (!File.Exists(ArtPath + file))
                throw new FileNotFoundException("레전더리 일러스트 원본 누락", ArtPath + file);

        var entries = new LobbyUnitPortraitCatalog.Entry[FileNames.Length];
        for (int i = 0; i < FileNames.Length; i++)
        {
            string path = ArtPath + FileNames[i];
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("PNG importer 누락: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            // Portrait wells are about 160 UI pixels wide; retain source PNGs but avoid 42 MB at runtime.
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Sprite import 실패: " + path);
            // Canonical alien-spec.json identifies LEGEND 1..7 as alienId 1..7.
            entries[i] = new LobbyUnitPortraitCatalog.Entry { alienId = i + 1, grade = "LEGEND", sprite = sprite };
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Lobby"))
            AssetDatabase.CreateFolder("Assets/Resources", "Lobby");
        var catalog = AssetDatabase.LoadAssetAtPath<LobbyUnitPortraitCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LobbyUnitPortraitCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        // Keep future/non-Legendary entries when this import is repeated.
        catalog.entries = (catalog.entries ?? new LobbyUnitPortraitCatalog.Entry[0])
            .Where(e => e != null && !(e.alienId >= 1 && e.alienId <= 7 &&
                (string.Equals(e.grade, "LEGEND", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(e.grade, "LEGENDARY", StringComparison.OrdinalIgnoreCase))))
            .Concat(entries).ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[LobbyPortraits] 레전더리 1~7 일러스트 연결 완료. 게임 데이터/Scene/Prefab 변경 없음.");
    }
}
#endif
