using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class BuildToyBoxMainMenu
{
    static void BuildCozyPlayroomDetails(Transform room,Material oak)
    {
        var yarn=Material("CozyCreamYarn",new Color(.82f,.75f,.60f),.18f);
        var edging=Material("CozyBlueYarn",new Color(.38f,.52f,.57f),.16f);
        var wicker=Material("CozyBasketWeave",new Color(.77f,.62f,.40f),.24f);
        var rug=Group("BraidedOvalRug",room,new Vector3(0,-.02f,-.75f));
        var backing=Disc("RugBacking",rug,Vector3.zero,1,.055f,edging,.02f);
        backing.transform.localScale=new Vector3(8.35f,1,4.95f);
        // Each braid is a small solid rope, all baked into two material batches.
        for(int row=1;row<=52;row++)for(int strand=0;strand<2;strand++)
        {
            float r=row/52f;
            var path=new List<Vector3>();int count=192;
            for(int i=0;i<=count;i++)
            {
                float a=i*Mathf.PI*2/count,wave=a*64+strand*Mathf.PI;
                float across=Mathf.Cos(wave)*.032f;
                path.Add(new Vector3(Mathf.Cos(a)*(8.23f*r+across),.026f+Mathf.Sin(wave)*.012f,Mathf.Sin(a)*(4.83f*r+across)));
            }
            MeshObject("BraidedRugStrand",rug,Vector3.zero,CozyTube("CozyRug_"+row+"_"+strand,path,.036f),row>48?edging:yarn);
        }

        var basket=Group("SmallToyBasket",room,new Vector3(-10.3f,0,1.30f));
        var baseDisc=Disc("BasketBottom",basket,new Vector3(0,.12f,0),1,.12f,wicker,.04f);baseDisc.transform.localScale=new Vector3(1.12f,1,.85f);
        for(int row=0;row<12;row++)
        {
            var path=new List<Vector3>();
            for(int i=0;i<=128;i++)
            {
                float a=i*Mathf.PI*2/128,r=1.02f+.075f*Mathf.Sin(row/11f*Mathf.PI)+.028f*Mathf.Sin(a*30+row*Mathf.PI);
                path.Add(new Vector3(Mathf.Cos(a)*r,.20f+row*.085f,Mathf.Sin(a)*r*.79f));
            }
            MeshObject("WovenBasketRow",basket,Vector3.zero,CozyTube("CozyBasketRow_"+row,path,.055f),wicker);
        }
        for(int i=0;i<30;i++)
        {
            float a=i*Mathf.PI*2/30;
            WorkshopRod("BasketUpright",basket,new Vector3(Mathf.Cos(a),.20f,Mathf.Sin(a)*.79f),new Vector3(Mathf.Cos(a),1.17f,Mathf.Sin(a)*.79f),.028f,wicker);
        }
        var lip=MeshObject("BasketRoundLip",basket,new Vector3(0,1.19f,0),ToyBoxGeometry.Torus(1.035f,.075f),wicker);lip.transform.localScale=new Vector3(1,1,.79f);
        var redBlock=Box("CoralBlock",basket,new Vector3(-.39f,1.03f,-.06f),new Vector3(.63f,.67f,.63f),coral,.10f);redBlock.transform.localRotation=Quaternion.Euler(0,12,0);
        var goldBlock=Box("GoldBlock",basket,new Vector3(.37f,1.08f,.06f),new Vector3(.64f,.74f,.64f),yellow,.10f);goldBlock.transform.localRotation=Quaternion.Euler(0,-15,0);

        var pucks=Group("ThreeStackedPucks",room,new Vector3(10.2f,0,1.30f));
        Disc("BluePuck",pucks,new Vector3(0,.15f,0),.57f,.30f,blue,.07f);
        Disc("MintPuck",pucks,new Vector3(.05f,.43f,.02f),.53f,.28f,mint,.06f);
        Disc("GoldPuck",pucks,new Vector3(.01f,.69f,.01f),.49f,.26f,yellow,.06f);

        var cubby=Group("LowBookCubby",room,new Vector3(-10.0f,0,5.1f));
        Box("CubbyBack",cubby,new Vector3(0,1.26f,.68f),new Vector3(3.25f,2.35f,.15f),oak,.10f);
        foreach(int sign in new[]{-1,1})Box("CubbySide",cubby,new Vector3(sign*1.60f,1.26f,0),new Vector3(.25f,2.48f,1.50f),oak,.11f);
        Box("CubbyTop",cubby,new Vector3(0,2.45f,0),new Vector3(3.45f,.25f,1.50f),oak,.11f);
        Box("CubbyBase",cubby,new Vector3(0,.24f,0),new Vector3(3.40f,.27f,1.50f),oak,.10f);
        var bookColors=new[]{coral,blue,yellow,mint,Material("CozyOrangeBook",new Color(.80f,.36f,.09f),.4f)};
        for(int i=0;i<5;i++)
        {
            var book=Group("SolidBook",cubby,new Vector3(-1.15f+i*.57f,1.20f,-.13f));
            Box("RoundedCover",book,Vector3.zero,new Vector3(.48f,1.65f,1.06f),bookColors[i],.07f);
            Box("PageBlock",book,new Vector3(0,.785f,.025f),new Vector3(.37f,.05f,.89f),cream,.012f);
        }

        var stand=Group("LampSideStand",room,new Vector3(10.15f,0,4.55f));
        Disc("RoundStandTop",stand,new Vector3(0,1.45f,0),1.06f,.22f,oak,.09f);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            WorkshopRod("StandLeg",stand,new Vector3(Mathf.Cos(a)*.61f,1.35f,Mathf.Sin(a)*.61f),new Vector3(Mathf.Cos(a)*.83f,.10f,Mathf.Sin(a)*.83f),.13f,oak);
        }
        Disc("BlueLampBase",stand,new Vector3(0,1.83f,0),.39f,.51f,blue,.22f);
        Disc("LampStem",stand,new Vector3(0,2.13f,0),.13f,.26f,cream,.04f);
        var shade=Material("CozyLampShade",new Color(1,.93f,.68f),.29f);
        shade.EnableKeyword("_EMISSION");shade.SetColor("_EmissionColor",new Color(1,.79f,.38f)*.72f);EditorUtility.SetDirty(shade);
        MeshObject("SolidRoundedLampShade",stand,new Vector3(0,2.22f,0),CozyLampMesh(),shade);
        var glow=Group("WarmLampPool",stand,new Vector3(0,2.17f,0)).gameObject.AddComponent<Light>();
        glow.type=LightType.Spot;glow.transform.localRotation=Quaternion.Euler(90,0,0);glow.spotAngle=115;glow.innerSpotAngle=50;
        glow.color=new Color(1,.82f,.53f);glow.intensity=.85f;glow.range=4;glow.shadows=LightShadows.None;

        var stars=Group("FiveWoodenStars",room,new Vector3(0,0,7.52f));
        var cord=new List<Vector3>();
        for(int i=0;i<=48;i++){float x=-3.5f+i*7f/48;cord.Add(new Vector3(x,6.76f-.58f*(1-x*x/12.25f),0));}
        MeshObject("HangingCord",stars,Vector3.zero,CozyTube("CozyStarCord",cord,.026f),wicker);
        for(int i=0;i<5;i++)
        {
            float x=-2.9f+i*1.45f,y=6.76f-.58f*(1-x*x/12.25f);
            WorkshopRod("StarTie",stars,new Vector3(x,y,0),new Vector3(x,y-.14f,-.09f),.018f,wicker);
            var star=MeshObject("WoodenStar",stars,new Vector3(x,y-.42f,-.12f),ToyBoxGeometry.MapPrism(10,.43f,.16f,.035f,true),new[]{yellow,mint,coral,yellow,mint}[i]);
            star.transform.localRotation=Quaternion.AngleAxis(-24+(i-2)*3,Vector3.forward)*Quaternion.Euler(90,0,0);
        }
        for(int sign=-1;sign<=1;sign+=2)
        {
            var pin=Disc("CordWallPeg",stars,new Vector3(sign*3.5f,6.76f,-.02f),.12f,.14f,oak,.06f);pin.transform.localRotation=Quaternion.Euler(90,0,0);
        }
    }

    static Mesh CozyTube(string name,List<Vector3> path,float radius)
    {
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int sides=6;
        for(int i=0;i<path.Count;i++)
        {
            Vector3 tangent=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)]).normalized;
            Vector3 across=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 up=Vector3.Cross(tangent,across).normalized;
            for(int j=0;j<=sides;j++)
            {
                float a=j*Mathf.PI*2/sides;var normal=across*Mathf.Cos(a)+up*Mathf.Sin(a);
                v.Add(path[i]+normal*radius);n.Add(normal);uv.Add(new Vector2(j/(float)sides,i*.05f));
                if(i>0&&j<sides){int b=i*(sides+1)+j,p=b-sides-1;t.AddRange(new[]{p,p+1,b+1,p,b+1,b});}
            }
        }
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/"+name+".asset");
    }

    static Mesh CozyLampMesh()
    {
        var profile=new List<Vector2>{new Vector2(0,0),new Vector2(.49f,0),new Vector2(.57f,.025f),new Vector2(.62f,.08f),new Vector2(.64f,.15f)};
        for(int i=1;i<=24;i++)
        {
            float a=i*Mathf.PI*.5f/24;
            profile.Add(new Vector2(Mathf.Cos(a)*.64f,.15f+Mathf.Sin(a)*.75f));
        }
        var v=new List<Vector3>();var t=new List<int>();const int segments=64;
        for(int row=0;row<profile.Count;row++)for(int i=0;i<=segments;i++)
        {
            float a=i*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));
            if(row>0&&i<segments){int p=row*(segments+1)+i,b=p-segments-1;t.AddRange(new[]{b,p,p+1,b,p+1,b+1});}
        }
        var mesh=new Mesh{name="CozyRoundedLampShade"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/CozyRoundedLampShade.asset");
    }
}
