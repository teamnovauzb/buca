#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public static class BuildPremiumFoxReward
{
    const string Folder="Assets/Art/AnimalPassengers/";
    static Sprite Slice(Texture2D texture,string name,int x,int y,int w,int h)
    {
        string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        var sprite=Sprite.Create(texture,new Rect(x,texture.height-y-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    public static string Apply()
    {
        AssetDatabase.ImportAsset(Folder+"PremiumFoxAtlas.png",ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"PremiumFoxAtlas.png");
        importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"PremiumFoxAtlas.png");
        var body=Slice(texture,"PremiumFoxBody",1070,62,419,498);
        var left=Slice(texture,"PremiumFoxLeftEar",28,550,248,411);
        var right=Slice(texture,"PremiumFoxRightEar",282,550,247,411);
        var paw=Slice(texture,"PremiumFoxPaw",605,565,336,397);
        var root=PrefabUtility.LoadPrefabContents("Assets/Art/RabbitGift/RabbitGiftReward.prefab");
        try{
            var r=root.GetComponent<RabbitGiftReward>();r.rewardLevel=10;
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))text.text=text.text.Replace("RABBIT","FOX").Replace("LEVEL 5","LEVEL 10");
            r.rabbit.Find("RabbitFaceAndScarf").GetComponent<UnityEngine.UI.Image>().sprite=body;
            r.leftEar.GetComponent<UnityEngine.UI.Image>().sprite=left;r.rightEar.GetComponent<UnityEngine.UI.Image>().sprite=right;
            r.leftEar.sizeDelta=r.rightEar.sizeDelta=new Vector2(93,112);
            r.leftEar.anchoredPosition=new Vector2(-66,194);r.rightEar.anchoredPosition=new Vector2(66,194);
            r.leftPaw.GetComponent<UnityEngine.UI.Image>().sprite=paw;r.paw.GetComponent<UnityEngine.UI.Image>().sprite=paw;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"foxGift.prefab");
            var collection=AnimalTrainCollection.Load();foreach(var friend in collection.friends)if(friend.id=="fox")friend.celebration=prefab.GetComponent<RabbitGiftReward>();EditorUtility.SetDirty(collection);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Level 10 fox now uses detailed furry sprite artwork with the rabbit's animation and celebration.";
    }
}
#endif
