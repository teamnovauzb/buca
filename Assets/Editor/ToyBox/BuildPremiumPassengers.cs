#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
public static class BuildPremiumPassengers
{
    const string Folder="Assets/Art/AnimalPassengers/";
    public static string Apply()
    {
        AssetDatabase.ImportAsset(Folder+"PremiumPassengers.png",ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"PremiumPassengers.png");
        importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"PremiumPassengers.png");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"PremiumPassenger.mat");
        if(material==null){material=new Material(Shader.Find("BUCA/Premium Passenger"));AssetDatabase.CreateAsset(material,Folder+"PremiumPassenger.mat");}
        var collection=AnimalTrainCollection.Load();Sprite lion=null;
        for(int i=0;i<collection.friends.Length;i++){
            var friend=collection.friends[i];string path=Folder+"Passenger-"+friend.id+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null){sprite=Sprite.Create(texture,new Rect(i%3*512,(1-i/3)*512,512,512),new Vector2(.5f,0),200,0,SpriteMeshType.FullRect);sprite.name=friend.id;AssetDatabase.CreateAsset(sprite,path);}
            var root=new GameObject("Premium "+friend.id,typeof(SpriteRenderer),typeof(PremiumPassengerPortrait));
            var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
            friend.passenger=PrefabUtility.SaveAsPrefabAsset(root,Folder+"Premium-"+friend.id+".prefab");Object.DestroyImmediate(root);
            if(friend.id=="lion")lion=sprite;
        }
        // Finish the remaining simple celebration character with the same detailed lion artwork.
        if(lion==null){
            lion=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"Passenger-lion.asset");
            if(lion==null){lion=Sprite.Create(texture,new Rect(1024,0,512,512),new Vector2(.5f,0),200,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(lion,Folder+"Passenger-lion.asset");}
        }
        if(lion!=null){
        var gift=PrefabUtility.LoadPrefabContents("Assets/Art/RabbitGift/RabbitGiftReward.prefab");
        try{
            var r=gift.GetComponent<RabbitGiftReward>();r.rewardLevel=30;
            foreach(var text in gift.GetComponentsInChildren<TMP_Text>(true))text.text=text.text.Replace("RABBIT","LION").Replace("LEVEL 5","LEVEL 30");
            var image=r.rabbit.Find("RabbitFaceAndScarf").GetComponent<UnityEngine.UI.Image>();image.sprite=lion;image.preserveAspect=true;
            r.leftEar.GetComponent<UnityEngine.UI.Image>().enabled=false;r.rightEar.GetComponent<UnityEngine.UI.Image>().enabled=false;
            var paw=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"PremiumBearPaw.asset");
            r.leftPaw.GetComponent<UnityEngine.UI.Image>().sprite=paw;r.paw.GetComponent<UnityEngine.UI.Image>().sprite=paw;
            var prefab=PrefabUtility.SaveAsPrefabAsset(gift,Folder+"lionGift.prefab");
            foreach(var friend in collection.friends)if(friend.id=="lion")friend.celebration=prefab.GetComponent<RabbitGiftReward>();
        }finally{PrefabUtility.UnloadPrefabContents(gift);}
        }
        EditorUtility.SetDirty(collection);AssetDatabase.SaveAssets();return "Six detailed passengers installed; lion reward polished; burgundy carriages use native rounded joinery and spoked wheels.";
    }
}
#endif
