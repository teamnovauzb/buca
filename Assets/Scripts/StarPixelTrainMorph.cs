using UnityEngine;

/// <summary>A front-to-rear material reveal with a narrow stream of gold, blue and violet cubes.</summary>
public sealed class StarPixelTrainMorph : System.IDisposable
{
    readonly SuitcaseTrainMechanism mechanism;
    readonly Renderer[] wooden, moon;
    readonly Transform cloud;
    readonly Transform[] cubes = new Transform[96];
    readonly Material[] materials = new Material[3];
    readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    public StarPixelTrainMorph(SuitcaseTrainMechanism mechanism)
    {
        this.mechanism = mechanism;
        wooden = mechanism.woodland.body.GetComponentsInChildren<MeshRenderer>(true);
        moon = mechanism.moonlight.body.GetComponentsInChildren<MeshRenderer>(true);
        cloud = new GameObject("StarPixelTransformation").transform;
        cloud.SetParent(mechanism.transform, false);
        var colors = new[] { new Color(1,.66f,.12f), new Color(.035f,.33f,1), new Color(.40f,.16f,.88f) };
        for (int i=0;i<materials.Length;i++)
        {
            materials[i] = new Material(wooden[0].sharedMaterial) { name="StarPixel"+i };
            materials[i].enableInstancing = true;
            materials[i].SetColor("_BaseColor", colors[i]);
            materials[i].SetColor("_EmissionColor", colors[i]*(i==0?.65f:.22f));
            materials[i].SetFloat("_GrainStrength",0);
            materials[i].SetFloat("_UpgradeSide",0);
        }
        for(int i=0;i<cubes.Length;i++)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name="StarPixel"+i; cube.layer=mechanism.gameObject.layer;
            cube.transform.SetParent(cloud,false); cubes[i]=cube.transform;
            var collider=cube.GetComponent<Collider>(); collider.enabled=false; Release(collider);
            cube.GetComponent<Renderer>().sharedMaterial=materials[i%3];
        }
    }

    public void Sample(float seconds)
    {
        float progress=Mathf.Clamp01((seconds-.7f)/3.65f);
        float edge=Mathf.Lerp(2.45f,-2.45f,progress);
        Vector3 normal=mechanism.transform.right;
        Vector3 point=mechanism.transform.TransformPoint(Vector3.right*edge);
        Vector4 plane=new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point));
        Apply(wooden,plane,progress>0&&progress<1?-1:0);
        Apply(moon,plane,progress>0&&progress<1?1:0);
        mechanism.woodland.body.SetActive(progress<1);
        mechanism.moonlight.body.SetActive(progress>0);
        cloud.gameObject.SetActive(progress>0&&progress<1);
        float envelope=Mathf.SmoothStep(0,1,progress/.1f)*Mathf.SmoothStep(0,1,(1-progress)/.1f);
        for(int i=0;i<cubes.Length;i++)
        {
            float phase=Mathf.Repeat(seconds*.65f+i*.618034f,1);
            float drift=Mathf.Sin(phase*Mathf.PI);
            float y=.45f+(i%12)*.23f;
            float z=((i/12)%4-1.5f)*.46f;
            cubes[i].localPosition=new Vector3(edge+(phase-.5f)*.85f,
                y+drift*(.18f+(i%5)*.12f),z+Mathf.Sin(i*2.4f)*drift*.35f);
            cubes[i].localScale=Vector3.one*(.09f+(i%4)*.025f)*envelope;
            cubes[i].localRotation=Quaternion.Euler(i*17+seconds*26,i*23+seconds*32,i*11);
        }
    }

    void Apply(Renderer[] renderers,Vector4 plane,float side)
    {
        foreach(var renderer in renderers)
        {
            renderer.GetPropertyBlock(block);
            block.SetVector("_UpgradePlane",plane); block.SetFloat("_UpgradeSide",side);
            renderer.SetPropertyBlock(block);
        }
    }
    public void Dispose()
    {
        foreach(var material in materials) Release(material);
        if(cloud!=null)Release(cloud.gameObject);
    }
    static void Release(Object item){if(Application.isPlaying)Object.Destroy(item);else Object.DestroyImmediate(item);}
}
