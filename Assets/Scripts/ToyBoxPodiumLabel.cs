using TMPro;
using UnityEngine;

/// <summary>Chooses saved glyph meshes; never constructs a mesh, material or object at runtime.</summary>
public sealed class ToyBoxPodiumLabel : MonoBehaviour
{
    public TMP_Text source;
    public string characters;
    public Mesh[] glyphs;
    public MeshFilter[] places;
    public float height;
    string previous;
    public void Refresh()
    {
        string value=(source.text??"").ToUpperInvariant().Replace(",","");
        if(value==previous) return;
        previous=value;
        bool supported=value.Length<=places.Length;
        float width=0;
        foreach(char c in value)
        {
            int index=characters.IndexOf(c);
            if(c!=' ' && index<0) supported=false;
            width+=c==' '? .40f:index>=0?glyphs[index].bounds.size.x+.13f:.7f;
        }
        // Preserve names in scripts outside the baked alphabet with the original font.
        source.GetComponent<Renderer>().enabled=!supported;
        float scale=Mathf.Min(height,source.rectTransform.sizeDelta.x/Mathf.Max(.01f,width));
        float cursor=source.alignment==TextAlignmentOptions.Left?-source.rectTransform.sizeDelta.x*.5f:
            source.alignment==TextAlignmentOptions.Right?source.rectTransform.sizeDelta.x*.5f-width*scale:-width*scale*.5f;
        for(int i=0;i<places.Length;i++)
        {
            int index=i<value.Length?characters.IndexOf(value[i]):-1;
            bool visible=supported && index>=0;
            places[i].gameObject.SetActive(visible);
            if(i>=value.Length) continue;
            float advance=index>=0?glyphs[index].bounds.size.x+.13f:.40f;
            if(visible)
            {
                places[i].sharedMesh=glyphs[index];
                places[i].transform.localScale=Vector3.one*scale;
                places[i].transform.localPosition=new Vector3(cursor+advance*scale*.5f,-glyphs[index].bounds.center.y*scale,-.025f);
            }
            cursor+=advance*scale;
        }
    }
}
