#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Only the three Midnight background illustrations. Never edits .meta text.</summary>
public sealed class MidnightArtworkImporter : AssetPostprocessor
{
    public const int ImportVersion=1;
    public override uint GetVersion() => ImportVersion;
    public static bool IsMidnightArtwork(string path)
    {
        return path=="Assets/Resources/Lobby/Midnight/observatory-background.png"
            ||path=="Assets/Resources/Lobby/Midnight/gacha-prism.png"
            ||path=="Assets/Resources/Lobby/Midnight/neptune-observatory.png";
    }
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
