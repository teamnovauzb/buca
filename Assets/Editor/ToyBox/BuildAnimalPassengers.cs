#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

public static class BuildAnimalPassengers
{
    const string Folder="Assets/Art/AnimalPassengers";
    static Material Mat(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.38f);EditorUtility.SetDirty(m);return m;
    }
    static Transform Ball(string name,Transform parent,Vector3 p,Vector3 scale,Material m)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g.transform;
    }
    static void FoxEar(Transform parent,float side,Material material)
    {
        string path=Folder+"/FoxEar.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.22f,0,-.14f),new Vector3(.22f,0,-.14f),new Vector3(0,.55f,0),new Vector3(-.22f,0,.14f),new Vector3(.22f,0,.14f)};mesh.triangles=new[]{0,2,1,3,4,2,0,3,2,1,2,4,0,1,4,0,4,3};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);}
        var g=new GameObject("Pointed fox ear",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(side*.37f,1.93f,0);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static GameObject Model(string id)
    {
        var root=new GameObject(id);var t=root.transform;
        var cream=Mat("Ivory",new Color(.96f,.88f,.72f));var dark=Mat("Dark",new Color(.035f,.025f,.025f));
        var pink=Mat("Pink",new Color(.96f,.48f,.46f));var gold=Mat("Gold",new Color(1,.58f,.08f));
        var brown=Mat("Brown",new Color(.49f,.22f,.075f));var orange=Mat("Fox",new Color(.9f,.28f,.035f));
        var body=id=="fox"?orange:id=="bear"?brown:id=="lion"?gold:id=="penguin"?dark:cream;
        Ball("Body",t,new Vector3(0,.65f,0),new Vector3(1,1.25f,.8f),body);
        if(id=="lion")for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Ball("Mane",t,new Vector3(Mathf.Sin(a)*.56f,1.55f+Mathf.Cos(a)*.56f,.02f),Vector3.one*.43f,brown);}
        Ball("Head",t,new Vector3(0,1.55f,0),new Vector3(1.16f,1.08f,.95f),body);
        if(id=="penguin")Ball("White bib",t,new Vector3(0,.95f,-.35f),new Vector3(.8f,1.35f,.35f),cream);
        foreach(float side in new[]{-1f,1f}){
            if(id!="penguin"){
                float length=id=="rabbit"?1f:id=="fox"?.58f:.36f;
                if(id=="fox")FoxEar(t,side,body);else Ball("Ear",t,new Vector3(side*.36f,2.05f+length*.22f,0),new Vector3(.28f,length,.24f),id=="panda"?dark:body);
                if(id=="rabbit")Ball("Inner ear",t,new Vector3(side*.36f,2.05f+length*.22f,-.11f),new Vector3(.15f,length*.72f,.06f),pink);
            }
            if(id=="panda")Ball("Eye patch",t,new Vector3(side*.27f,1.65f,-.42f),new Vector3(.37f,.42f,.16f),dark);
            Ball("Eye",t,new Vector3(side*.25f,1.65f,-.49f),new Vector3(.14f,.19f,.09f),dark);
            Ball("Eye highlight",t,new Vector3(side*.25f-.02f,1.69f,-.54f),Vector3.one*.043f,cream);
            Ball("Foot",t,new Vector3(side*.32f,.13f,-.13f),new Vector3(.4f,.27f,.5f),id=="penguin"?gold:id=="panda"?dark:body);
            var arm=new GameObject(side>0?"GreetingPaw":"RestingPaw").transform;arm.SetParent(t,false);arm.localPosition=new Vector3(side*.48f,1.05f,0);
            Ball("Arm",arm,new Vector3(side*.1f,-.17f,0),new Vector3(.31f,.65f,.32f),id=="panda"?dark:body);
        }
        if(id!="penguin")Ball("Muzzle",t,new Vector3(0,1.36f,-.44f),new Vector3(.61f,.38f,.28f),cream);
        Ball("Nose",t,new Vector3(0,1.48f,-.61f),id=="penguin"?new Vector3(.35f,.18f,.3f):new Vector3(.19f,.13f,.13f),id=="penguin"?gold:dark);
        var scarf=Mat(id+" scarf",id=="rabbit"?new Color(.27f,.7f,.57f):id=="bear"?new Color(.8f,.08f,.025f):id=="penguin"?new Color(1,.58f,.08f):new Color(.08f,.36f,.4f));
        Ball("Scarf collar",t,new Vector3(0,1.1f,0),new Vector3(1.04f,.2f,.87f),scarf);
        Ball("Scarf tail",t,new Vector3(.23f,.85f,-.43f),new Vector3(.23f,.5f,.13f),scarf);
        if(id=="fox"){Ball("Fluffy tail",t,new Vector3(-.6f,.6f,.35f),new Vector3(.5f,.95f,.5f),orange);Ball("White tail tip",t,new Vector3(-.65f,1f,.35f),new Vector3(.35f,.4f,.35f),cream);foreach(float side in new[]{-1f,1f})Ball("White cheek",t,new Vector3(side*.3f,1.4f,-.39f),new Vector3(.42f,.36f,.24f),cream);}
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+id+".prefab");Object.DestroyImmediate(root);return prefab;
    }
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        System.IO.Directory.CreateDirectory(Folder);System.IO.Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<AnimalTrainCollection>("Assets/Resources/AnimalTrainCollection.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<AnimalTrainCollection>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/AnimalTrainCollection.asset");}
        string[] ids={"rabbit","fox","bear","panda","penguin","lion"};catalog.friends=new AnimalTrainCollection.Friend[ids.Length];
        for(int i=0;i<ids.Length;i++){
            var model=Model(ids[i]);string path="Assets/Art/RabbitGift/RabbitGiftReward.prefab";
            if(i>0){
                var reward=PrefabUtility.LoadPrefabContents(path);
                try{
                    var r=reward.GetComponent<RabbitGiftReward>();r.rewardLevel=(i+1)*5;
                    foreach(var label in reward.GetComponentsInChildren<TMP_Text>(true))label.text=label.text.Replace("RABBIT",ids[i].ToUpperInvariant()).Replace("LEVEL 5", "LEVEL "+r.rewardLevel);
                    foreach(var graphic in r.rabbit.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
                    var portrait=new GameObject("Toy animal portrait",typeof(RectTransform),typeof(RawImage));var rect=portrait.GetComponent<RectTransform>();rect.SetParent(r.rabbit,false);rect.sizeDelta=new Vector2(300,375);rect.anchoredPosition=new Vector2(0,115);
                    var image=portrait.GetComponent<RawImage>();image.raycastTarget=false;
                    var renderer=reward.AddComponent<AnimalGiftPortrait>();renderer.passengerPrefab=model;renderer.image=image;
                    path=Folder+"/"+ids[i]+"Gift.prefab";PrefabUtility.SaveAsPrefabAsset(reward,path);
                }finally{PrefabUtility.UnloadPrefabContents(reward);}
            }
            catalog.friends[i]=new AnimalTrainCollection.Friend{id=ids[i],rewardLevel=(i+1)*5,passenger=model,celebration=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RabbitGiftReward>()};
        }
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();if(System.IO.File.Exists(Folder+"/PremiumFoxAtlas.png"))BuildPremiumFoxReward.Apply();if(System.IO.File.Exists(Folder+"/PremiumBearAtlas.png"))BuildPremiumBearReward.Apply();if(System.IO.File.Exists(Folder+"/PremiumPandaAtlas.png"))BuildPremiumPandaReward.Apply();if(System.IO.File.Exists(Folder+"/PremiumPenguinAtlas.png"))BuildPremiumPenguinReward.Apply();if(System.IO.File.Exists(Folder+"/PremiumPassengers.png"))BuildPremiumPassengers.Apply();return "Six animal rewards and passenger prefabs saved at levels 5, 10, 15, 20, 25, 30.";
    }
}
#endif
