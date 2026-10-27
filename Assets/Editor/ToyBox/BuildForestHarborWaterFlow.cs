#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Water-only masks follow the original 1672 x 941 illustration.</summary>
public static class BuildForestHarborWaterFlow
{
    const string Folder = "Assets/TwilightMap/ForestHarbor";
    static Vector2 P(float x,float y) => new Vector2(x,y);
    static readonly Vector2[][] Falls = {
        new[]{P(675,157),P(687,155),P(709,156),P(725,159),P(729,172),P(732,197),P(736,223),P(743,240),P(733,250),P(706,249),P(697,238),P(690,213),P(685,187)},
        new[]{P(847,338),P(862,337),P(875,342),P(894,343),P(911,351),P(920,370),P(928,395),P(933,405),P(921,417),P(899,414),P(879,412),P(871,400),P(867,379),P(861,359)}
    };
    static readonly Vector2[][] Currents = {
        new[]{P(628,129),P(652,130),P(662,137),P(680,140),P(685,151),P(674,156),P(657,147),P(640,142)},
        new[]{P(819,301),P(844,287),P(874,280),P(910,282),P(913,294),P(904,309),P(903,324),P(914,341),P(878,340),P(864,328),P(836,327)},
        new[]{P(848,440),P(882,437),P(907,430),P(942,432),P(952,441),P(943,454),P(932,465),P(925,490),P(939,520),P(954,540),P(961,558),P(943,553),P(929,534),P(917,521),P(899,514),P(875,495),P(862,468)},
        // Harbor channel, open sea and dock basin. Insets keep masonry and controls still.
        new[]{P(995,600),P(1020,624),P(1070,642),P(1100,665),P(1120,700),P(1120,729),P(1080,738),P(1040,715),P(1010,674)},
        new[]{P(1175,746),P(1240,745),P(1300,760),P(1400,775),P(1460,770),P(1470,795),P(1250,795),P(1200,780)},
        new[]{P(1465,786),P(1530,762),P(1600,756),P(1671,759),P(1671,940),P(1500,940),P(1490,885),P(1475,820)},
        new[]{P(1308,476),P(1330,469),P(1360,477),P(1360,516),P(1322,506)},
        new[]{P(1465,512),P(1515,521),P(1570,535),P(1580,540),P(1555,550),P(1515,548),P(1475,535)}
    };
    static readonly Vector2[][] Foam = {
        new[]{P(700,242),P(719,237),P(739,244),P(755,239),P(775,250),P(762,258),P(728,262),P(703,260)},
        new[]{P(860,410),P(882,405),P(905,414),P(930,405),P(950,414),P(944,430),P(915,441),P(890,437),P(867,444),P(845,436)}
    };
    // The woodland stream passes behind the stone bridge, railway, rocks and footer.
    // A blue-water key refines these corridors so their banks and bridge stay fixed.
    static readonly Vector2[][] Streams = {
        new[]{P(151,547),P(181,548),P(207,554),P(236,554),P(263,563),P(294,571),P(292,584),P(269,592),P(247,605),P(219,599),P(196,586),P(171,576)},
        new[]{P(294,625),P(310,619),P(328,624),P(338,636),P(355,640),P(377,640),P(400,633),P(421,624),P(448,623),P(446,641),P(426,649),P(397,657),P(369,663),P(337,661),P(310,656),P(286,648)},
        new[]{P(325,759),P(362,745),P(403,741),P(451,741),P(490,735),P(507,746),P(502,762),P(493,775),P(516,786),P(548,791),P(576,796),P(609,803),P(631,812),P(585,813),P(549,808),P(523,811),P(492,806),P(463,800),P(439,793),P(418,782),P(395,792),P(370,790),P(343,796),P(321,790)},
        new[]{P(680,824),P(706,821),P(725,832),P(741,830),P(756,822),P(760,849),P(738,856),P(717,850),P(695,845),P(680,838)},
        new[]{P(917,887),P(938,877),P(953,882),P(960,892),P(985,894),P(1004,896),P(1027,901),P(1048,903),P(1072,912),P(1090,918),P(1110,930),P(1139,940),P(1044,940),P(1041,932),P(1022,924),P(997,919),P(971,917),P(947,917),P(923,910)}
    };
    // Water contact line below the outer dock, bollard and individual shoreline rocks.
    static readonly Vector2[] Shoreline = {
        P(1260,716),P(1280,708),P(1320,721),P(1366,735),P(1372,742),P(1372,754),
        P(1380,761),P(1393,762),P(1400,759),P(1411,763),P(1424,765),P(1441,758),
        P(1460,745),P(1468,740),P(1483,738),P(1500,729),P(1508,721),P(1516,719),
        P(1524,721),P(1533,721),P(1544,721),P(1551,720),P(1560,724),P(1570,724),
        P(1581,722),P(1589,719),P(1594,713),P(1600,710),P(1605,702),P(1608,704),
        P(1618,712),P(1629,715),P(1640,713),P(1650,711),P(1660,704),P(1672,698)
    };

    [MenuItem("RealBuca/Forest Harbor/Add Flowing Waterfalls")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.illustratedMenu==null||!map.illustratedMenu.forestHarborPresentation)
            throw new Exception("Forest Harbor map required.");
        var image=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
        if(image==null||image.texture==null) throw new Exception("Artwork missing.");
        var shader=Shader.Find("BUCA/Forest Harbor Water Flow");
        if(shader==null||ShaderUtil.ShaderHasError(shader)) throw new Exception("Water shader did not compile.");
        Directory.CreateDirectory("output/water-flow");
        const string backup="output/water-flow/MainMenu.before-water-flow.unity";
        if(!File.Exists(backup)) File.Copy(scene.path,backup);
        var mask=BakeMask();
        var shore=BakeShoreMask();
        string path=Folder+"/ForestHarborWaterFlow.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;material.SetTexture("_FlowMask",mask);material.SetFloat("_FlowTime",0);
        material.SetTexture("_ShoreWaveData",shore);
        Undo.RecordObject(image,"Animate Forest Harbor water");image.material=material;
        var flow=image.GetComponent<ForestHarborWaterFlow>();
        if(flow==null) flow=Undo.AddComponent<ForestHarborWaterFlow>(image.gameObject);
        flow.Configure(image);
        // The boat-removal patches must move with the surrounding water, too.
        var repair=map.illustratedMenu.artwork.Find("HarborWaterRepair");
        if(repair!=null)
        {
            var patch=repair.GetComponent<UnityEngine.UI.RawImage>();
            string repairPath=Folder+"/HarborLife/HarborAnimatedRepair.mat";
            var patchMaterial=AssetDatabase.LoadAssetAtPath<Material>(repairPath);
            if(patchMaterial==null){patchMaterial=new Material(material);AssetDatabase.CreateAsset(patchMaterial,repairPath);}
            patchMaterial.CopyPropertiesFromMaterial(material);patchMaterial.shader=shader;patchMaterial.SetFloat("_RepairOnly",1);
            patch.material=patchMaterial;patch.raycastTarget=false;
            var patchFlow=patch.GetComponent<ForestHarborWaterFlow>();
            if(patchFlow==null)patchFlow=patch.gameObject.AddComponent<ForestHarborWaterFlow>();
            patchFlow.Configure(patch);EditorUtility.SetDirty(patchMaterial);
        }
        // Keep every water repair in phase with the underlying illustration.
        foreach(var raw in map.illustratedMenu.artwork.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
            if(raw.material!=null&&raw.material.shader==shader)
            {
                raw.material.SetTexture("_FlowMask",mask);
                raw.material.SetTexture("_ShoreWaveData",shore);
                EditorUtility.SetDirty(raw.material);
            }
        EditorUtility.SetDirty(material);EditorUtility.SetDirty(image);EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved: flowing rivers and waterfalls, harbor reflections and rolling shoreline waves with foam around the rocks and dock posts.";
    }

    static Texture2D BakeShoreMask()
    {
        var texture=new Texture2D(1672,941,TextureFormat.RGBA32,false,true);
        var pixels=new Color32[1672*941];
        var artwork=new Texture2D(2,2,TextureFormat.RGBA32,false);
        artwork.LoadImage(File.ReadAllBytes(Folder+"/ForestHarborBackground.png"));
        var source=artwork.GetPixels32();
        for(int y=680;y<865;y++)for(int x=1260;x<1672;x++)
        {
            Vector2 p=P(x+.5f,y+.5f),normal=Vector2.up;
            float closest=float.MaxValue,signed=0;
            for(int i=1;i<Shoreline.Length;i++)
            {
                var a=Shoreline[i-1];var edge=Shoreline[i]-a;
                var q=a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude);
                float distance=(p-q).sqrMagnitude;
                if(distance>=closest)continue;
                closest=distance;normal=new Vector2(-edge.y,edge.x).normalized;
                signed=Vector2.Dot(p-q,normal);
            }
            if(signed<=0)continue;
            float d=Mathf.Sqrt(closest);
            float coverage=Mathf.SmoothStep(0,1,Mathf.Clamp01(d/3));
            coverage*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,94,d));
            coverage*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1260,1282,x));
            // Only water and blue-white foam can move at the immediate contact edge.
            Color c=source[(940-y)*1672+x];
            float blue=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.11f,c.b-c.r));
            blue*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.01f,.06f,c.b-c.g));
            coverage*=Mathf.Lerp(blue,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,d)));
            pixels[(940-y)*1672+x]=new Color(coverage,Mathf.Clamp01(d/96),normal.x*.5f+.5f,normal.y*.5f+.5f);
        }
        Object.DestroyImmediate(artwork);
        texture.SetPixels32(pixels);texture.Apply();
        const string path=Folder+"/ShoreWaveData.png";
        File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=false;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Texture2D BakeMask()
    {
        var tex=new Texture2D(1672,941,TextureFormat.RGBA32,false,true);
        var artwork=new Texture2D(2,2,TextureFormat.RGBA32,false);
        artwork.LoadImage(File.ReadAllBytes(Folder+"/ForestHarborBackground.png"));
        var source=artwork.GetPixels32();
        var pixels=new Color32[1672*941];
        for(int y=0;y<941;y++) for(int x=0;x<1672;x++)
        {
            var p=P(x+.5f,940.5f-y);
            Color c=source[y*1672+x];
            float waterKey=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.12f,c.b-c.r));
            waterKey*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.015f,.045f,c.b-c.g));
            pixels[y*1672+x]=new Color(Coverage(p,Falls,3),Coverage(p,Currents,4),Coverage(p,Foam,4),Coverage(p,Streams,3)*waterKey);
        }
        Object.DestroyImmediate(artwork);
        tex.SetPixels32(pixels);tex.Apply();
        string path=Folder+"/WaterFlowMask.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=false;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static float Coverage(Vector2 p,Vector2[][] polygons,float feather)
    {
        float result=0;
        foreach(var poly in polygons)
        {
            bool inside=false;float distance=float.MaxValue;
            for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)
            {
                var a=poly[j];var b=poly[i];
                if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
                distance=Mathf.Min(distance,Vector2.Distance(p,a+ab*t));
            }
            if(inside) result=Mathf.Max(result,Mathf.SmoothStep(0,1,distance/feather));
        }
        return result;
    }
}
#endif
