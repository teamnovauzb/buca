using UnityEngine;
using UnityEditor;
using System.IO;
public static class TutorialSpriteBaker
{
    const string Folder="Assets/Tutorials/BakedControls";
    public static Sprite Face(Color color,bool pressed,string name)
    {
        using(var vh=new UnityEngine.UI.VertexHelper())
        {FaceMesh(vh,color,pressed?1:0);return Save(vh,name);}
    }
    public static Sprite Honey()
    {
        using(var vh=new UnityEngine.UI.VertexHelper()){HoneyMesh(vh);return Save(vh,"HoneyPuck");}
    }
    static Sprite Save(UnityEngine.UI.VertexHelper vh,string name)
    {
        Directory.CreateDirectory(Folder);
        var mesh=new Mesh();vh.FillMesh(mesh);
        var vertices=mesh.vertices;
        for(int i=0;i<vertices.Length;i++) vertices[i]*=4f;var colors=mesh.colors;var tris=mesh.triangles;
        var pixels=new Color[512*512];
        for(int n=0;n<tris.Length;n+=3)
        {
            Vector2 a=(Vector2)vertices[tris[n]]+Vector2.one*256;
            Vector2 b=(Vector2)vertices[tris[n+1]]+Vector2.one*256;
            Vector2 c=(Vector2)vertices[tris[n+2]]+Vector2.one*256;
            float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.00001f)continue;
            int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,b.x,c.x)),0,511),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,b.x,c.x)),0,511);
            int y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,b.y,c.y)),0,511),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,b.y,c.y)),0,511);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                Vector2 p=new Vector2(x+.5f,y+.5f);
                float u=Cross(b-p,c-p)/area,v=Cross(c-p,a-p)/area,w=1-u-v;
                if(u<0 || v<0 || w<0)continue;
                Color src=colors[tris[n]]*u+colors[tris[n+1]]*v+colors[tris[n+2]]*w,dst=pixels[y*512+x];
                float alpha=src.a+dst.a*(1-src.a);
                if(alpha>0){Color result=(src*src.a+dst*dst.a*(1-src.a))/alpha;result.a=alpha;pixels[y*512+x]=result;}
            }
        }
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();
        string path=Folder+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());
        Object.DestroyImmediate(mesh);Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
        importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static void FaceMesh(UnityEngine.UI.VertexHelper vh,Color capColor,float depression)
    {
        vh.Clear();
        Rect r=new Rect(-64,-64,128,128);
        float size=Mathf.Min(r.width,r.height);
        Vector2 center=r.center;
        Disc(vh,center+Vector2.down*size*.07f,size*.49f,size*.36f,new Color(0,0,0,.05f),new Color(0,0,0,.4f));
        Disc(vh,center+Vector2.down*size*.025f,size*.45f,size*.34f,new Color(.22f,.29f,.33f),new Color(.09f,.14f,.17f));
        Disc(vh,center+Vector2.up*size*.025f,size*.45f,size*.34f,new Color(.96f,.98f,1),new Color(.38f,.48f,.55f));
        Disc(vh,center+Vector2.up*size*.03f,size*.38f,size*.285f,new Color(.04f,.055f,.06f),Color.black);
        Vector2 cap=center+Vector2.up*size*(.14f-.075f*depression);
        Disc(vh,cap+Vector2.down*size*.085f,size*.35f,size*.255f,capColor*.65f,new Color(capColor.r*.3f,capColor.g*.3f,capColor.b*.3f,1));
        Disc(vh,cap,size*.35f,size*.255f,Color.Lerp(capColor,Color.white,.36f),Color.Lerp(capColor,Color.black,.25f));
        Disc(vh,cap,size*.305f,size*.218f,Color.Lerp(capColor,Color.white,.16f),capColor);
        // A small curved highlight keeps the cap glossy without covering its color.
        Disc(vh,cap+new Vector2(-size*.12f,size*.13f),size*.12f,size*.025f,new Color(1,1,1,.7f),new Color(1,1,1,.02f));
    }
    static void Disc(UnityEngine.UI.VertexHelper vh,Vector2 center,float rx,float ry,Color top,Color bottom)
    {
        const int segments=192;
        int first=vh.currentVertCount;
        vh.AddVert(center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        for(int i=0;i<=segments;i++)
        {
            float angle=i*Mathf.PI*2/segments;
            float y=Mathf.Sin(angle);
            vh.AddVert(center+new Vector2(Mathf.Cos(angle)*rx,y*ry),Color.Lerp(bottom,top,(y+1)*.5f),Vector2.zero);
            if(i>0)vh.AddTriangle(first,first+i,first+i+1);
        }
    }
    static void HoneyMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        var r = new Rect(-64,-64,128,128);
        Ellipse(vh, r, new Vector2(0,-.19f), new Vector2(.50f,.31f), new Color(0,0,0,.2f));
        Ellipse(vh, r, new Vector2(0,-.05f), new Vector2(.45f,.40f), new Color(.015f,.12f,.46f));
        Ellipse(vh, r, new Vector2(0,.11f), new Vector2(.45f,.31f), new Color(.035f,.39f,.95f));
        Ellipse(vh, r, new Vector2(0,.13f), new Vector2(.37f,.24f), new Color(.04f,.32f,.86f));
        Ellipse(vh, r, new Vector2(-.14f,.27f), new Vector2(.15f,.035f), new Color(.45f,.8f,1,.65f));
    }
    static void Ellipse(UnityEngine.UI.VertexHelper vh, Rect r, Vector2 offset, Vector2 radius, Color tint)
    {
        int first=vh.currentVertCount;
        Vector2 c=r.center+Vector2.Scale(offset,r.size);
        vh.AddVert(c,tint,Vector2.zero);
        const int count=48;
        for(int i=0;i<=count;i++)
        {
            float a=i*Mathf.PI*2/count;
            vh.AddVert(c+new Vector2(Mathf.Cos(a)*radius.x*r.width,Mathf.Sin(a)*radius.y*r.height),tint,Vector2.zero);
            if(i>0)vh.AddTriangle(first,first+i,first+i+1);
        }
    }
}
