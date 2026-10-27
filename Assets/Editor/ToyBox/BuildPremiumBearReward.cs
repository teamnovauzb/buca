#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public static class BuildPremiumBearReward
{
    const string Folder="Assets/Art/AnimalPassengers/";
    static Sprite Slice(Texture2D texture,string name,int x,int y,int w,int h)
    {
        string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        var sprite=Sprite.Create(texture,new Rect(x,texture.height-y-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    public static string Apply()
    {
        AssetDatabase.ImportAsset(Folder+"PremiumBearAtlas.png",ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"PremiumBearAtlas.png");
        importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"PremiumBearAtlas.png");
        var body=Slice(texture,"PremiumBearBody",1064,48,461,584);
        var left=Slice(texture,"PremiumBearLeftEar",28,550,248,411);
        var right=Slice(texture,"PremiumBearRightEar",282,550,247,411);
        var paw=Slice(texture,"PremiumBearPaw",605,565,336,397);
        var root=PrefabUtility.LoadPrefabContents("Assets/Art/RabbitGift/RabbitGiftReward.prefab");
        try{
            var r=root.GetComponent<RabbitGiftReward>();r.rewardLevel=15;
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))text.text=text.text.Replace("RABBIT","BEAR").Replace("LEVEL 5","LEVEL 15");
            r.rabbit.Find("RabbitFaceAndScarf").GetComponent<UnityEngine.UI.Image>().sprite=body;
            r.leftEar.GetComponent<UnityEngine.UI.Image>().sprite=left;r.rightEar.GetComponent<UnityEngine.UI.Image>().sprite=right;
            r.leftEar.GetComponent<UnityEngine.UI.Image>().enabled=false;
            r.rightEar.GetComponent<UnityEngine.UI.Image>().enabled=false;
            r.leftEar.anchoredPosition=new Vector2(-66,194);r.rightEar.anchoredPosition=new Vector2(66,194);
            r.leftPaw.GetComponent<UnityEngine.UI.Image>().sprite=paw;r.paw.GetComponent<UnityEngine.UI.Image>().sprite=paw;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"bearGift.prefab");
            var collection=AnimalTrainCollection.Load();foreach(var friend in collection.friends)if(friend.id=="bear")friend.celebration=prefab.GetComponent<RabbitGiftReward>();EditorUtility.SetDirty(collection);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Level 15 bear now uses detailed furry sprite artwork with the rabbit's animation and celebration.";
    }
}
#endif
