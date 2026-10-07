#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class BucaPodiumTypography
{
    static void Text(Transform parent,TMP_FontAsset font,float width,float height,string value="")
    {
        foreach(Transform child in parent)if(child.name!="UIText")child.gameObject.SetActive(false);
        var existing=parent.Find("UIText");var go=existing!=null?existing.gameObject:new GameObject("UIText",typeof(RectTransform),typeof(TextMeshPro));
        go.transform.SetParent(parent,false);go.layer=31;
        go.transform.localPosition=Vector3.back*.025f;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
        var text=go.GetComponent<TextMeshPro>();text.font=font;text.fontSharedMaterial=font.material;
        text.color=new Color(.025f,.095f,.16f);text.fontStyle=FontStyles.Normal;
        text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;
        text.fontSizeMax=height*14;text.fontSizeMin=height*7;text.fontSize=height*14;
        text.characterSpacing=1.5f;text.textWrappingMode=TextWrappingModes.NoWrap;
        text.overflowMode=TextOverflowModes.Ellipsis;text.richText=false;
        text.rectTransform.sizeDelta=new Vector2(width,height*1.65f);text.text=value;
        text.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        text.ForceMeshUpdate(true);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
        var v=Object.FindFirstObjectByType<BucaPodiumLeaderboard3D>(FindObjectsInactive.Include);
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");
        if(font==null)throw new System.Exception("Roboto font missing");
        for(int i=0;i<5;i++)
        {
            Text(v.names[i],font,i<3?2.05f:3.6f,i<3?.38f:.42f);
            Text(v.scores[i],font,i<3?2.05f:1.85f,i<3?.44f:.43f);
            Text(v.ranks[i],font,.65f,i<3?.59f:.4f);
        }
        Text(v.status,font,4.3f,.34f);Text(v.footer,font,7.8f,.21f);
        var heading=v.stage.transform.Find("PuckPodiumPresentation/TopScores");
        heading.GetComponent<MeshRenderer>().enabled=false;
        heading.localScale=Vector3.one;
        Text(heading,font,6.5f,.5f,"TOP SCORES");
        v.Sync(v.panel);
        EditorSceneManager.MarkSceneDirty(v.gameObject.scene);EditorSceneManager.SaveScene(v.gameObject.scene);
        return "Roboto UI typography applied; sculpted logo and podium geometry preserved.";
    }
}
#endif
