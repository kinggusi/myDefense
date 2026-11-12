#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Only the Midnight illustrations and six-icon atlas. Never edits .meta text.</summary>
public sealed class MidnightArtworkImporter : AssetPostprocessor
{
    public const int ImportVersion=3;
    public override uint GetVersion() => ImportVersion;
    private static readonly string[] paths={
        "Assets/Resources/Lobby/Midnight/observatory-background.png",
        "Assets/Resources/Lobby/Midnight/gacha-prism.png",
        "Assets/Resources/Lobby/Midnight/neptune-observatory.png",
        "Assets/Resources/Lobby/Midnight/status-material-icons-v3.png",
        "Assets/Resources/Lobby/Midnight/shop-item-icons-v4.png",
        "Assets/Resources/Lobby/Midnight/main-observatory-v4.png",
        "Assets/Resources/Lobby/Midnight/gacha-prism-cutout-v5.png"};
    public static System.Collections.Generic.IEnumerable<string> ArtworkPaths
    {get{foreach(string path in paths)yield return path;}}
    public static bool IsMidnightArtwork(string path)=>System.Array.IndexOf(paths,path)>=0;
    private void OnPreprocessTexture()
    {
        if(!IsMidnightArtwork(assetPath))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;
        importer.mipmapEnabled=false;
        importer.isReadable=false;
        importer.sRGBTexture=true;
        // Source widths/heights are not block-compression multiples. Keep exact pixels/aspect;
        // mobile platform compression can be profiled separately without resizing the source.
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
    }
}
#endif
