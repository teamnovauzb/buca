using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WoodenTutorialConsoleBaker
{
    [MenuItem("RealBuca/Prebuild/Wooden Tutorial Console")]
    public static void Bake()
    {
        const string imagePath="Assets/Tutorials/WoodenConsole/ConsoleBackground.png";
        AssetDatabase.ImportAsset(imagePath);var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.isReadable=true;importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>40){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        var sprite=Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.5f),100);sprite.name="Wooden console";
        const string spritePath="Assets/Tutorials/WoodenConsole/ConsoleSprite.asset";
        var old=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);if(old!=null){EditorUtility.CopySerialized(sprite,old);Object.DestroyImmediate(sprite);sprite=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(sprite,spritePath);
        const string path="Assets/Resources/Tutorials/BoardCoachTutorial.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view=root.GetComponent<BoardCoachTutorial>();view.woodenConsole=true;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Anton SDF.asset");
            Place(view.footer,.015f,.985f,.012f,.265f);
            var background=view.footer.GetComponent<Image>();background.sprite=sprite;background.type=Image.Type.Simple;background.color=Color.white;background.raycastTarget=false;
            foreach(var shadow in view.footer.GetComponentsInChildren<Shadow>(true))Object.DestroyImmediate(shadow);
            // The status now belongs to the console. Retain the hidden charge reference for playback logic.
            view.caption.transform.SetParent(view.footer,false);Place(view.caption.rectTransform,.277f,.719f,.465f,.80f);
            Style(view.caption,font,36,new Color(.025f,.16f,.17f));view.caption.enableAutoSizing=true;view.caption.fontSizeMin=20;view.caption.fontSizeMax=36;view.caption.margin=new Vector4(12,2,12,2);
            view.chargeAnchor.gameObject.SetActive(false);
            var clockParent=(RectTransform)view.clock.transform.parent;clockParent.SetParent(view.footer,false);Place(clockParent,.28f,.72f,.17f,.265f);ClearBackground(clockParent);
            Place(view.clock.rectTransform,0,1,0,1);Style(view.clock,font,23,new Color(.10f,.20f,.19f));view.clock.text="LESSON 1 OF 3     |     0:12 / 0:12";
            var track=(RectTransform)view.progress.transform.parent;track.SetParent(view.footer,false);Place(track,.281f,.716f,.285f,.355f);ClearBackground(track);
            Place(view.progress.rectTransform,0,1,0,1);view.progress.color=new Color(.24f,.80f,.59f);view.progress.fillAmount=1;
            Button(view.pause,.055f,.19f,font,"PAUSE");Button(view.replay,.741f,.853f,font,"WATCH AGAIN");Button(view.play,.864f,.975f,font,"NEXT LESSON");
            view.caption.text="READY FOR THE NEXT LESSON";
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    static void ClearBackground(RectTransform rect){var image=rect.GetComponent<Image>();if(image!=null)image.color=Color.clear;}
    static void Style(TMP_Text label,TMP_FontAsset font,float size,Color color){label.font=font;label.fontSharedMaterial=font.material;label.fontSize=size;label.fontStyle=FontStyles.Bold;label.characterSpacing=0;label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;}
    static void Button(UnityEngine.UI.Button button,float left,float right,TMP_FontAsset font,string text)
    {
        Place((RectTransform)button.transform,left,right,.12f,.88f);button.image.color=Color.clear;
        var face=button.GetComponentInChildren<ArcadeButtonFace>(true);var rect=face.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.61f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(132,132);face.color=Color.white;
        face.raisedSprite=TutorialSpriteBaker.Face(face.capColor,false,face.arcadeButton+"Raised");
        face.pressedSprite=TutorialSpriteBaker.Face(face.capColor,true,face.arcadeButton+"Pressed");face.sprite=face.raisedSprite;
        var label=button.GetComponentInChildren<TMP_Text>(true);Place(label.rectTransform,0,1,.085f,.33f);Style(label,font,20,new Color(.025f,.16f,.17f));label.text=text;label.characterSpacing=1.5f;label.enableAutoSizing=true;label.fontSizeMin=16;label.fontSizeMax=20;label.margin=new Vector4(9,0,9,0);
        var old=button.transform.Find("WoodenLabelPlate");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var plaque=new GameObject("WoodenLabelPlate",typeof(RectTransform),typeof(Image));plaque.transform.SetParent(button.transform,false);plaque.transform.SetAsFirstSibling();Place(plaque.GetComponent<RectTransform>(),0,1,.07f,.345f);
        var image=plaque.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/HeartReward/RoundedCard.png");image.type=Image.Type.Sliced;image.color=new Color(.16f,.105f,.045f);image.raycastTarget=false;
        var shadow=plaque.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.28f);shadow.effectDistance=new Vector2(0,-3);
        var rim=new GameObject("BrassRim",typeof(RectTransform),typeof(Image));rim.transform.SetParent(plaque.transform,false);
        Place(rim.GetComponent<RectTransform>(),0,1,0,1);rim.GetComponent<RectTransform>().offsetMin=new Vector2(1,3);rim.GetComponent<RectTransform>().offsetMax=new Vector2(-1,0);
        var ri=rim.GetComponent<Image>();ri.sprite=image.sprite;ri.type=Image.Type.Sliced;ri.color=new Color(.77f,.60f,.32f);ri.raycastTarget=false;
        var enamel=new GameObject("IvoryEnamel",typeof(RectTransform),typeof(Image));enamel.transform.SetParent(rim.transform,false);
        Place(enamel.GetComponent<RectTransform>(),0,1,0,1);enamel.GetComponent<RectTransform>().offsetMin=new Vector2(3,3);enamel.GetComponent<RectTransform>().offsetMax=new Vector2(-3,-2);
        var ei=enamel.GetComponent<Image>();ei.sprite=image.sprite;ei.type=Image.Type.Sliced;ei.color=new Color(1f,.94f,.76f);ei.raycastTarget=false;
    }
    static void Place(RectTransform rect,float x0,float x1,float y0,float y1){rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
