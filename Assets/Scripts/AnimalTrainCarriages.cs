using System.Collections.Generic;
using UnityEngine;

/// <summary>Five connected wagons follow the engine; friends board after their completed milestones.</summary>
[DefaultExecutionOrder(200)]
public sealed partial class AnimalTrainCarriages : MonoBehaviour
{
    public TwilightRailwayMap map;
    readonly List<Transform> cars=new List<Transform>();
    readonly List<Transform[]> wheels=new List<Transform[]>();
    readonly List<Transform> couplings=new List<Transform>();
    readonly List<Material> materials=new List<Material>();
    readonly HashSet<string> aboard=new HashSet<string>();
    readonly Dictionary<string,int> carIndices=new Dictionary<string,int>();
    readonly List<PremiumPassengerPortrait> portraits=new List<PremiumPassengerPortrait>();
    readonly List<GameObject> passengerObjects=new List<GameObject>();
    bool initialized;
    AnimalTrainCollection collection;
    SuitcaseLocomotiveView view;
    float originalSize;
    Vector2 originalRect;
    Material wood, gold, red, iron, walnut;
    void Start() => Initialize();
    public int CarriageCount => cars.Count;
    public int PassengerCount => aboard.Count;
    public void Initialize()
    {
        if(initialized || map==null || map.train==null)return;
        initialized=true;
        collection=AnimalTrainCollection.Load();
        view=map.GetComponent<SuitcaseLocomotiveView>();
        if(view!=null){originalSize=view.verticalSize;originalRect=map.illustratedMenu.locomotive.sizeDelta;}
        if(collection!=null)foreach(var friend in collection.friends)CreateCar(friend);
    }
    Material Make(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.5f);materials.Add(m);return m;
    }
    Transform Part(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=map.train.gameObject.layer;
        var collider=go.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
        if(type==PrimitiveType.Cube)go.GetComponent<MeshFilter>().sharedMesh=CarriageCraftMesh.RoundedBox;
        go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
    }
    void CreateCar(AnimalTrainCollection.Friend friend)
    {
        if(carIndices.ContainsKey(friend.id))return;
        if(wood==null){
            wood=Make("Burgundy enamel",new Color(.30f,.025f,.055f));gold=Make("Polished brass",new Color(.78f,.49f,.16f));
            red=Make("Burgundy wheel inset",new Color(.12f,.012f,.024f));iron=Make("Dark iron",new Color(.027f,.023f,.022f));walnut=Make("Dark walnut",new Color(.095f,.040f,.018f));
            gold.SetFloat("_Metallic",.72f);gold.SetFloat("_Smoothness",.68f);wood.SetFloat("_Smoothness",.42f);iron.SetFloat("_Metallic",.45f);
        }
        var car=new GameObject(friend.id+" carriage").transform;car.SetParent(map.train.parent,false);car.localScale=map.train.localScale;
        Part("Undercarriage",car,PrimitiveType.Cube,new Vector3(0,.69f,0),new Vector3(2.72f,.25f,1.5f),iron);
        Part("Floor",car,PrimitiveType.Cube,new Vector3(0,.85f,0),new Vector3(2.6f,.17f,1.45f),walnut);
        foreach(float z in new[]{-.68f,.68f}){
            Part("Burgundy panel",car,PrimitiveType.Cube,new Vector3(0,1.2f,z),new Vector3(2.6f,.65f,.16f),wood);
            Part("Rounded brass rim",car,PrimitiveType.Cube,new Vector3(0,1.56f,z),new Vector3(2.72f,.12f,.18f),gold);
            foreach(float y in new[]{.96f,1.43f})Part("Brass inlay",car,PrimitiveType.Cube,new Vector3(0,y,z*1.13f),new Vector3(2.35f,.032f,.025f),gold);
            foreach(float x in new[]{-1.16f,1.16f}){
                Part("Brass corner",car,PrimitiveType.Cube,new Vector3(x,1.23f,z*1.1f),new Vector3(.14f,.69f,.08f),gold);
                foreach(float y in new[]{1f,1.44f})Part("Rivet",car,PrimitiveType.Sphere,new Vector3(x,y,z*1.17f),Vector3.one*.085f,gold);
            }
        }
        foreach(float x in new[]{-1.23f,1.23f}){
            Part("Burgundy end",car,PrimitiveType.Cube,new Vector3(x,1.2f,0),new Vector3(.15f,.65f,1.4f),wood);
            Part("Rounded brass end rim",car,PrimitiveType.Cube,new Vector3(x,1.56f,0),new Vector3(.18f,.12f,1.5f),gold);
        }
        var coupling=Part("Articulated coupling",map.train.parent,PrimitiveType.Cube,Vector3.zero,Vector3.one,gold);
        couplings.Add(coupling);
        var rolling=new List<Transform>();
        foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-.8f,.8f}){
            var pivot=new GameObject("Rolling wheel").transform;pivot.SetParent(car,false);pivot.localPosition=new Vector3(x,.45f,z);
            float outward=z<0?-1:1;
            var disc=Part("Iron wheel rim",pivot,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.85f,.12f,.85f),iron);disc.localRotation=Quaternion.Euler(90,0,0);
            var inset=Part("Recessed wheel",pivot,PrimitiveType.Cylinder,new Vector3(0,0,outward*.13f),new Vector3(.68f,.025f,.68f),red);inset.localRotation=Quaternion.Euler(90,0,0);
            for(int spoke=0;spoke<8;spoke++){
                float angle=spoke*45f;float rad=angle*Mathf.Deg2Rad;
                var bar=Part("Brass spoke",pivot,PrimitiveType.Cube,new Vector3(Mathf.Cos(rad)*.19f,Mathf.Sin(rad)*.19f,outward*.17f),new Vector3(.27f,.045f,.04f),gold);bar.localRotation=Quaternion.Euler(0,0,angle);
            }
            var hub=Part("Brass axle hub",pivot,PrimitiveType.Sphere,new Vector3(0,0,outward*.2f),new Vector3(.21f,.21f,.12f),gold);
            rolling.Add(pivot);
        }
        GameObject passenger=null;PremiumPassengerPortrait portrait=null;
        if(friend.passenger!=null){
            passenger=Instantiate(friend.passenger,car);passenger.transform.localPosition=new Vector3(0,.98f,0);
            foreach(var t in passenger.GetComponentsInChildren<Transform>(true))t.gameObject.layer=map.train.gameObject.layer;
            portrait=passenger.GetComponent<PremiumPassengerPortrait>();
            if(portrait!=null){portrait.enabled=false;portrait.renderingCamera=view!=null?view.trainCamera:null;portrait.FaceCamera();}
            passenger.SetActive(false);
        }
        carIndices.Add(friend.id,cars.Count);cars.Add(car);wheels.Add(rolling.ToArray());
        passengerObjects.Add(passenger);portraits.Add(portrait);
        if(view!=null){float size=originalSize+cars.Count*2.5f*Mathf.Abs(map.train.localScale.x);view.verticalSize=size;map.illustratedMenu.locomotive.sizeDelta=originalRect*(size/originalSize);view.EnsurePassengerResolution(size/originalSize);}
    }
    void Add(AnimalTrainCollection.Friend friend)
    {
        CreateCar(friend);
        int index=carIndices[friend.id];
        if(passengerObjects[index]!=null)passengerObjects[index].SetActive(true);
        if(portraits[index]!=null)portraits[index].GetComponent<SpriteRenderer>().color=Color.white;
        aboard.Add(friend.id);
    }
    Vector3 Point(float distance)
    {
        if(distance>=0)return map.Point(distance);
        return map.Point(0)+(map.Point(.1f)-map.Point(0)).normalized*distance;
    }
    // The train is photographed into a UI image. Route coordinates are map pixels,
    // not camera-space coordinates: invert that projection for every carriage.
    Vector3 RenderPosition(Vector3 routePoint)
    {
        if(view==null)return routePoint;
        var camera=view.trainCamera;
        var parent=map.train.parent;
        var origin=map.train.localPosition;
        var screen=camera.WorldToViewportPoint(parent.TransformPoint(origin));
        var x=camera.WorldToViewportPoint(parent.TransformPoint(origin+Vector3.right))-screen;
        var z=camera.WorldToViewportPoint(parent.TransformPoint(origin+Vector3.forward))-screen;
        var image=map.illustratedMenu.locomotive;
        float display=map.illustratedMenu.forestTrainDisplayScale;
        var desired=new Vector2((routePoint.x-origin.x)*20/(image.rect.width*display),
            (routePoint.z-origin.z)*20/(image.rect.height*display));
        float determinant=x.x*z.y-x.y*z.x;
        if(Mathf.Abs(determinant)<.00000001f)return routePoint;
        return origin+Vector3.right*((desired.x*z.y-desired.y*z.x)/determinant)
            +Vector3.forward*((x.x*desired.y-x.y*desired.x)/determinant);
    }
    void LateUpdate() => Tick(Time.unscaledDeltaTime);
    public void Tick(float dt)
    {
        Initialize();
        if(collection==null||map.train==null)return;
        foreach(var friend in collection.friends)
            if(AnimalTrainCollection.IsRiding(friend.id)&&!AnimalTrainCollection.IsWaitingToBoard(friend.id)&&!aboard.Contains(friend.id))Add(friend);
        if(view!=null)view.Frame();
        float scale=Mathf.Abs(map.train.localScale.x);
        float previousDistance=map.RouteDistance-map.trainNoseOffset;
        for(int i=0;i<cars.Count;i++){
            float separation=(i==0?4.1f:3.15f)*scale;
            var previousPosition=RenderPosition(Point(previousDistance));
            float low=0,high=separation*3;
            for(int step=0;step<14;step++){
                float middle=(low+high)*.5f;
                float gap=Vector3.Distance(previousPosition,RenderPosition(Point(previousDistance-middle)));
                if(gap<separation)low=middle;else high=middle;
            }
            float d=previousDistance-(low+high)*.5f;
            cars[i].localPosition=RenderPosition(Point(d));
            var delta=RenderPosition(Point(d+.15f))-RenderPosition(Point(d-.15f));
            cars[i].localRotation=Quaternion.Euler(0,-Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg,0);
            float angle=-map.WheelDistanceAt(d)/Mathf.Max(.001f,scale*.425f)*Mathf.Rad2Deg;
            foreach(var wheel in wheels[i])wheel.localRotation=Quaternion.Euler(0,0,angle);
            var leading=i==0?map.train:cars[i-1];
            var a=leading.TransformPoint(new Vector3(i==0?-2.3f:-1.3f,.65f,0));
            var b=cars[i].TransformPoint(new Vector3(1.3f,.65f,0));
            var parent=couplings[i].parent;
            a=parent.InverseTransformPoint(a);b=parent.InverseTransformPoint(b);
            couplings[i].localPosition=(a+b)*.5f;
            couplings[i].localRotation=Quaternion.FromToRotation(Vector3.right,(b-a).normalized);
            couplings[i].localScale=new Vector3(Vector3.Distance(a,b),.12f*scale,.12f*scale);
            if(portraits[i]!=null && passengerObjects[i].activeSelf)portraits[i].FaceCamera();
            previousDistance=d;
        }
        TickBoarding(dt);
    }
    void OnDestroy(){foreach(var car in cars)if(car!=null)Release(car.gameObject);foreach(var coupling in couplings)if(coupling!=null)Release(coupling.gameObject);foreach(var material in materials)Release(material);}
    static void Release(Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
}
