#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HoneyTwoLaneAuthoring
{
    static GameObject Part(Transform p,string n,Mesh mesh,Material mat,Vector3 pos)
    {var t=p.Find(n);var g=t!=null?t.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(p,false);g.layer=30;g.transform.localPosition=pos;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode only");
        var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();var root=b.stage.transform;ToyBoxGeometry.Initialize();
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");var teal=b.dim;var amber=root.Find("HoneyCell00").GetComponent<Renderer>().sharedMaterial;
        amber.color=new Color(.85f,.40f,.035f);amber.SetFloat("_Smoothness",.85f);EditorUtility.SetDirty(amber);
        foreach(Transform t in root)if(t.name.StartsWith("HoneyCell")||t.name.StartsWith("Charge"))t.gameObject.SetActive(false);
        b.caption.text="WATCH";b.caption.fontSize=3.8f;b.caption.transform.localPosition=new Vector3(0,2.02f,-.55f);
        b.chargeDots=new Renderer[3];b.secondDots=new Renderer[3];
        for(int lane=0;lane<2;lane++)
        {
            float y=lane==0?.7f:-1.05f;
            Part(root,"LaneRim"+lane,ToyBoxGeometry.RoundedBox(new Vector3(7.45f,1.48f,.25f),.16f),teal,new Vector3(0,y,-.43f));
            Part(root,"LaneFloor"+lane,ToyBoxGeometry.RoundedBox(new Vector3(7.16f,1.20f,.12f),.10f),wood,new Vector3(0,y,-.58f));
            for(int r=-1;r<=1;r++)for(int col=-1;col<=1;col++)
            {
                var cell=Part(root,"HoneyGloss"+lane+r+col,ToyBoxGeometry.Disc(.24f,.07f,.03f),amber,new Vector3(col*.37f+(r%2)*.15f,y+r*.26f,-.71f));cell.transform.localRotation=Quaternion.Euler(90,0,0);
                var outline=Part(root,"CellRim"+lane+r+col,ToyBoxGeometry.Torus(.19f,.013f),b.lit,new Vector3(col*.37f+(r%2)*.15f,y+r*.26f,-.755f));outline.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            var goal=root.Find(lane==0?"Goal":"Goal2");if(goal==null){goal=Object.Instantiate(root.Find("Goal"),root);goal.name="Goal2";}goal.localPosition=new Vector3(3.1f,y,-.72f);goal.localScale=Vector3.one*.7f;
            var rim=root.Find(lane==0?"GoalRim":"GoalRim2");if(rim==null){rim=Object.Instantiate(root.Find("GoalRim"),root);rim.name="GoalRim2";}rim.localPosition=new Vector3(3.1f,y,-.78f);rim.localScale=Vector3.one*.7f;
            for(int i=0;i<3;i++)
            {var dot=Part(root,"Power"+lane+i,ToyBoxGeometry.RoundedBox(new Vector3(.21f,.23f,.07f),.06f),lane==1||i==0?b.lit:teal,new Vector3(-4.5f+i*.28f,y,-.5f));if(lane==0)b.chargeDots[i]=dot.GetComponent<Renderer>();else b.secondDots[i]=dot.GetComponent<Renderer>();}
            var indicator=new GameObject("Result"+lane,typeof(TMPro.TextMeshPro));indicator.layer=30;indicator.transform.SetParent(root,false);indicator.transform.localPosition=new Vector3(4.38f,y,-.5f);var text=indicator.GetComponent<TMPro.TextMeshPro>();text.font=b.caption.font;text.fontSize=2.6f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.03f,.20f,.16f);text.text=lane==0?"SLOW":"GOAL";text.rectTransform.sizeDelta=new Vector2(1.15f,.5f);
        }
        b.demoPuck.transform.localPosition=new Vector3(-3.2f,.7f,-.9f);b.honey.transform.localPosition=new Vector3(0,.7f,-.85f);b.honey.GetComponent<BoxCollider>().size=new Vector3(1.6f,1.1f,1);
        if(b.secondPuck==null){b.secondPuck=Object.Instantiate(b.demoPuck,root);b.secondPuck.name="StrongPuck";b.secondHoney=Object.Instantiate(b.honey,root);b.secondHoney.name="StrongHoney";}
        b.secondPuck.transform.localPosition=new Vector3(-3.2f,-1.05f,-.9f);b.secondHoney.transform.localPosition=new Vector3(0,-1.05f,-.85f);
        // Raise trigger volume without changing the visible patch or horizontal footprint.
        int count=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ToyBoxMenu/Levels","Assets/Prefabs/Levels"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            foreach(var patch in prefab.GetComponentsInChildren<IcePatch>(true))if(patch.patchDamping>1)
            {var box=patch.GetComponent<BoxCollider>();if(box==null)continue;float scale=Mathf.Abs(box.transform.lossyScale.y);var size=box.size;size.y=1.0f/Mathf.Max(.001f,scale);box.size=size;var center=box.center;center.y=.35f/Mathf.Max(.001f,scale);box.center=center;box.isTrigger=true;changed=true;count++;}
            if(changed)PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
        }
        EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);return "Two lanes authored; honey trigger heights fixed on "+count+" patches";
    }
}
#endif
