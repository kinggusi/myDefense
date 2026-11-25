using System;
using System.Collections.Generic;
using System.IO;
using MyDefense.Battle.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MyDefense.Battle.Editor
{
    public static class BattlePresentationRefinementAssetGenerator
    {
        public const string KoreanFont="Assets/Art/Battle/UI/Fonts/NotoSansKR-Medium.otf";
        public const string NumberFont="Assets/Space_Exploration_GUI_Kit/Fonts/Font Sources/static/Asap-Bold.ttf";
        public const string Button="Assets/Art/Battle/UI/KidnapButton.png";
        public const string FontLicense="Assets/Art/Battle/UI/Fonts/NotoSansKR-OFL.txt";
        private const string Folder="Assets/Resources/Battle/Presentation";
        private sealed class Parsed
        {public string Path;public RectInt[] Frames;public Vector2[] Pivots;public float Height;public bool Body;}

        [MenuItem("MyDefense/Battle/Generate Presentation Refinement Assets")]
        public static void Generate()
        {
            string[] paths={"Assets/Art/Battle/Monsters/NORMAL_MONSTER_Walk.png","Assets/Art/Battle/Monsters/ELITE_MONSTER_Walk.png","Assets/Art/Battle/Monsters/WAVE_BOSS_Walk.png",
                "Assets/Art/Battle/Effects/MonsterSpawnPortal.png","Assets/Art/Battle/Effects/MergeAccepted.png"};
            foreach(string path in paths)Require(path);Require(KoreanFont);Require(NumberFont);Require(Button);Require(FontLicense);
            // Decode and validate every cell before changing any persistent importer/catalog.
            var parsed=new Parsed[5];for(int i=0;i<5;i++)parsed[i]=Analyze(paths[i],i<3?3:i==3?4:3,i<3?4:2,i<3);
            Analyze(Button,1,1,false);
            AssetDatabase.Refresh();
            var atlases=new BattlePresentationAtlas[5];for(int i=0;i<5;i++)atlases[i]=Import(parsed[i]);
            var buttonImporter=(TextureImporter)AssetImporter.GetAtPath(Button);
            buttonImporter.textureType=TextureImporterType.Sprite;buttonImporter.spriteImportMode=SpriteImportMode.Single;buttonImporter.spritePixelsPerUnit=100;
            buttonImporter.spriteBorder=Vector4.zero;buttonImporter.mipmapEnabled=true;buttonImporter.alphaIsTransparency=true;buttonImporter.isReadable=false;
            buttonImporter.textureCompression=TextureImporterCompression.Uncompressed;buttonImporter.maxTextureSize=4096;buttonImporter.filterMode=FilterMode.Trilinear;
            buttonImporter.SaveAndReimport();
            Font korean=AssetDatabase.LoadAssetAtPath<Font>(KoreanFont),numbers=AssetDatabase.LoadAssetAtPath<Font>(NumberFont);
            Sprite button=AssetDatabase.LoadAssetAtPath<Sprite>(Button);
            if(korean==null||numbers==null||button==null)throw new InvalidOperationException("HUD font/button import failed.");
            foreach(char character in "왹져납치합성관전")if(!korean.HasCharacter(character))throw new InvalidOperationException("Required Korean font glyph missing: "+character);
            Shader monster=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Battle/Presentation/BattleMonsterArt.shader"),effects=Shader.Find("Sprites/Default");
            if(monster==null||effects==null)throw new InvalidOperationException("Monster/effect presentation shader missing.");
            EnsureFolder(Folder);
            var hud=AssetDatabase.LoadAssetAtPath<BattleHudStyleCatalog>(Folder+"/BattleHudStyleCatalog.asset");
            if(hud==null){hud=ScriptableObject.CreateInstance<BattleHudStyleCatalog>();AssetDatabase.CreateAsset(hud,Folder+"/BattleHudStyleCatalog.asset");}
            var catalog=AssetDatabase.LoadAssetAtPath<BattleMonsterArtCatalog>(Folder+"/BattleMonsterArtCatalog.asset");
            if(catalog==null){catalog=ScriptableObject.CreateInstance<BattleMonsterArtCatalog>();AssetDatabase.CreateAsset(catalog,Folder+"/BattleMonsterArtCatalog.asset");}
            hud.Configure(korean,numbers,button);catalog.Configure(atlases[0],atlases[1],atlases[2],atlases[3],atlases[4],monster,effects);
            if(!hud.Available||!catalog.Available)throw new InvalidOperationException("Incomplete presentation refinement catalog.");
            EditorUtility.SetDirty(hud);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("[BattlePresentation] Generated HUD/three canonical Monster/Portal/Merge catalogs; gameplay and original PNG pixels unchanged.");
        }
        private static string FilePath(string asset)=>Path.Combine(Application.dataPath,asset.Substring("Assets/".Length));
        public static void CopyFontNotice(string executablePath)
        {
            Require(FontLicense);
            string directory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executablePath)),"ThirdPartyNotices");
            Directory.CreateDirectory(directory);File.Copy(FilePath(FontLicense),Path.Combine(directory,"NotoSansKR-OFL.txt"),true);
        }
        private static void Require(string path){if(!File.Exists(FilePath(path)))throw new FileNotFoundException("Required presentation artwork/font missing",path);}
        private static Parsed Analyze(string path,int columns,int rows,bool body)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!texture.LoadImage(File.ReadAllBytes(FilePath(path))))throw new InvalidOperationException("Cannot decode "+path);
                Color32[] pixels=texture.GetPixels32();bool transparent=false,opaque=false;
                foreach(Color32 pixel in pixels){transparent|=pixel.a==0;opaque|=pixel.a>=240;}
                if(!transparent||!opaque)throw new InvalidOperationException("Expected real RGBA transparency: "+path);
                var frames=new RectInt[columns*rows];var pivots=new Vector2[frames.Length];
                for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
                {
                    RectInt cell=GridCell(texture.width,texture.height,columns,rows,col,row);int minX=cell.xMax,minY=cell.yMax,maxX=-1,maxY=-1;
                    for(int y=cell.yMin;y<cell.yMax;y++)for(int x=cell.xMin;x<cell.xMax;x++)
                        if(pixels[y*texture.width+x].a>=24){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                    if(maxX<minX||maxY<minY)throw new InvalidOperationException($"Empty presentation frame {row}/{col}: {path}");
                    int index=row*columns+col;
                    if(body)
                    {
                        if(minX==cell.xMin||minY==cell.yMin||maxX==cell.xMax-1||maxY==cell.yMax-1)throw new InvalidOperationException("Clipped Monster cell: "+path);
                        int x0=Mathf.Max(cell.xMin,minX-2),y0=Mathf.Max(cell.yMin,minY-2),x1=Mathf.Min(cell.xMax,maxX+3),y1=Mathf.Min(cell.yMax,maxY+3);
                        frames[index]=new RectInt(x0,y0,x1-x0,y1-y0);
                        pivots[index]=new Vector2((cell.center.x-x0)/(x1-x0),(float)(minY-y0)/(y1-y0));
                    }
                    else{frames[index]=cell;pivots[index]=Vector2.one*.5f;}
                }
                return new Parsed{Path=path,Frames=frames,Pivots=pivots,Height=(float)texture.height/rows,Body=body};
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static RectInt GridCell(int width,int height,int columns,int rows,int col,int row)
        {
            int x0=Mathf.RoundToInt((float)width*col/columns),x1=Mathf.RoundToInt((float)width*(col+1)/columns);
            int y0=Mathf.RoundToInt((float)height*(rows-row-1)/rows),y1=Mathf.RoundToInt((float)height*(rows-row)/rows);
            return new RectInt(x0,y0,x1-x0,y1-y0);
        }
        private static BattlePresentationAtlas Import(Parsed parsed)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(parsed.Path);if(importer==null)throw new InvalidOperationException("Missing texture importer: "+parsed.Path);
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.isReadable=false;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.anisoLevel=4;
            importer.mipMapsPreserveCoverage=parsed.Body;importer.alphaTestReferenceValue=.12f;importer.SaveAndReimport();
            return new BattlePresentationAtlas(AssetDatabase.LoadAssetAtPath<Texture2D>(parsed.Path),parsed.Frames,parsed.Pivots,parsed.Height,parsed.Body);
        }
        private static void EnsureFolder(string path)
        {string[] parts=path.Split('/');string parent=parts[0];for(int i=1;i<parts.Length;i++){string next=parent+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(parent,parts[i]);parent=next;}}
    }
    public sealed class BattlePresentationFontNoticePostprocessor:IPostprocessBuildWithReport
    {
        public int callbackOrder=>100;
        public void OnPostprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.StandaloneWindows&&report.summary.platform!=BuildTarget.StandaloneWindows64)return;
            if(Resources.Load<BattleHudStyleCatalog>(BattleHudStyleCatalog.ResourcePath)==null)return;
            BattlePresentationRefinementAssetGenerator.CopyFontNotice(report.summary.outputPath);
        }
    }
}
