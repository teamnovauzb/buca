#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
public static partial class BuildToyBoxMainMenu
{
    static Material BuildWindowLandscape()
    {
        const int w=768,h=768;
        var pixels=new Color[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float u=(float)x/w,v=(float)y/h;
            var c=Color.Lerp(new Color(.94f,.85f,.65f),new Color(.55f,.73f,.79f),Mathf.SmoothStep(.28f,1,v));
            float sun=Mathf.Exp(-((u-.72f)*(u-.72f)+(v-.75f)*(v-.75f))*65);
            c=Color.Lerp(c,new Color(1,.94f,.73f),sun*.62f);
            float far=.40f+.055f*Mathf.Sin(u*7)+.025f*Mathf.Sin(u*17);
            float mid=.27f+.065f*Mathf.Sin(u*6+2);
            float near=.13f+.035f*Mathf.Cos(u*9);
            if(v<far)c=new Color(.59f,.68f,.57f);
            if(v<mid)c=new Color(.43f,.57f,.39f);
            if(v<near)c=new Color(.32f,.46f,.28f);
            pixels[y*w+x]=c;
        }
        // Tiny distant branching trees, softly blended into the layered garden.
        var random=new System.Random(71);
        for(int tree=0;tree<11;tree++)
        {
            float cx=.04f+tree*.093f,cy=.22f+.025f*Mathf.Sin(tree*2),height=.12f+(float)random.NextDouble()*.09f;
            for(int y=(int)(cy*h);y<(cy+height)*h;y++)for(int dx=-1;dx<=1;dx++)
            {int x=(int)(cx*w)+dx;if(x>=0&&x<w&&y<h)pixels[y*w+x]=new Color(.37f,.40f,.28f);}
            for(int leaf=0;leaf<25;leaf++)
            {
                float a=(float)random.NextDouble()*6.28f,r=(float)random.NextDouble();
                float lx=cx+Mathf.Cos(a)*.042f*r,ly=cy+height*.83f+Mathf.Sin(a)*.055f*r;
                float radius=.010f+(float)random.NextDouble()*.018f;
                Color color=Color.Lerp(new Color(.40f,.52f,.31f),new Color(.68f,.71f,.42f),(float)random.NextDouble());
                int reach=Mathf.CeilToInt(radius*w);
                for(int dy=-reach;dy<=reach;dy++)for(int dx=-reach;dx<=reach;dx++)
                {int px=(int)(lx*w)+dx,py=(int)(ly*h)+dy;if(px<0||px>=w||py<0||py>=h)continue;float d=Mathf.Sqrt(dx*dx+dy*dy)/reach;float alpha=1-Mathf.SmoothStep(.65f,1,d);pixels[py*w+px]=Color.Lerp(pixels[py*w+px],color,alpha*.8f);}
            }
        }
        var tex=new Texture2D(w,h,TextureFormat.RGB24,true){name="SoftGardenVista",wrapMode=TextureWrapMode.Clamp};tex.SetPixels(pixels);tex.Apply();
        var path=StationFolder+"/SoftGardenVista.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(saved==null){AssetDatabase.CreateAsset(tex,path);saved=tex;}else{EditorUtility.CopySerialized(tex,saved);Object.DestroyImmediate(tex);}
        var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="CozyGardenVista"};mat.SetTexture("_BaseMap",saved);mat.SetColor("_BaseColor",Color.white);
        return ToyBoxGeometry.Save(mat,StationFolder+"/CozyGardenVista.mat");
    }
}
#endif
