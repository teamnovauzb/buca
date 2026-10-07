#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
public static class BoardCoachVisualPolish
{
    [InitializeOnLoadMethod] static void Schedule(){EditorApplication.delayCall+=Apply;}
    [MenuItem("RealBuca/Toy Box 3D/Polish Board Coach Text")]
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/Resources/Tutorials/BoardCoachTutorial.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)return;
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            if(root.GetComponent<BoardCoachTutorial>().woodenConsole)return;
            var display=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CoachDisplay.asset");
            if(display==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Bangers-Regular.ttf");
                if(source==null)return;
                display=TMP_FontAsset.CreateFontAsset(source);
                AssetDatabase.CreateAsset(display,"Assets/Fonts/CoachDisplay.asset");
                AssetDatabase.AddObjectToAsset(display.material,display);
                foreach(var atlas in display.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,display);
                display.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ,.—:>/");
                EditorUtility.SetDirty(display);AssetDatabase.SaveAssets();
            }
            var view=root.GetComponent<BoardCoachTutorial>();
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font=display;text.fontSharedMaterial=display.material;text.characterSpacing=2;text.enableAutoSizing=false;
                text.fontSize=text.transform.parent.name=="WoodTitle"?46:28;
                text.color=new Color(.99f,.95f,.82f);
                if(text.transform.parent.name=="WoodTitle" || text.transform.parent.name=="YourTurn" || text.transform.parent.name=="Replay" || text.transform.parent.name=="Pause")text.color=new Color(.025f,.13f,.14f);
            }
            view.header.anchorMin=new Vector2(.27f,.815f);view.header.anchorMax=new Vector2(.73f,.978f);
            view.header.offsetMin=view.header.offsetMax=Vector2.zero;
            foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if(image.color.a<.1f || image.type==UnityEngine.UI.Image.Type.Filled)continue;
                var shadow=image.GetComponent<UnityEngine.UI.Shadow>();
                if(shadow==null)shadow=image.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor=new Color(.015f,.06f,.06f,.55f);shadow.effectDistance=new Vector2(0,-5);
            }
            view.caption.fontSize=26;view.clock.fontSize=22;
            view.ApplyArcadePresentation();
            PrefabUtility.SaveAsPrefabAsset(root,path);
            const string promptPath="Assets/Prefabs/Prebuilt/ReturnPlayerPrompt.prefab";
            var prompt=PrefabUtility.LoadPrefabContents(promptPath);
            try
            {
                foreach(var label in prompt.GetComponentsInChildren<TMP_Text>(true))
                    label.text=label.text.Replace("WHITE  BACK","RED  BACK");
                PrefabUtility.SaveAsPrefabAsset(prompt,promptPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prompt);}
            Debug.Log("BOARD_COACH_POLISHED: display typography, compact header, panel depth, high contrast steps.");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
