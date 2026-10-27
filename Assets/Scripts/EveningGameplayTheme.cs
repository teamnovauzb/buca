using System.Collections.Generic;
using UnityEngine;

/// <summary>World 2 presentation only; never changes board geometry or colliders.</summary>
public sealed class EveningGameplayTheme : MonoBehaviour
{
    public LevelManager manager;
    public GameObject room, station;
    public Camera gameplayCamera;
    public GameObject rivetPrefab;
    readonly List<GameObject> ornaments=new List<GameObject>();
    public Material navy, brass, ivory, goldPuck, goalBlue, blackCap, bumper;
    readonly Dictionary<Renderer,bool> originalVisibility=new Dictionary<Renderer,bool>();
    Color sky, equator, ground;
    readonly Dictionary<Renderer,Material[]> original=new Dictionary<Renderer,Material[]>();
    Vector3 cameraPosition; Quaternion cameraRotation;
    bool initialized, evening;
    GameObject lastLevel;
    Material originalTimerInk;
    void Awake(){Capture();}
    void Capture(){if(initialized)return;initialized=true;sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;if(gameplayCamera!=null){cameraPosition=gameplayCamera.transform.position;cameraRotation=gameplayCamera.transform.rotation;}}
    void LateUpdate()
    {
        if(manager==null)return;
        bool next=manager.CurrentLevelIndex>=30;
        if(next!=evening||lastLevel!=manager.CurrentLevelRoot){Apply(next,manager.CurrentLevelRoot);lastLevel=manager.CurrentLevelRoot;}
    }
    public void Apply(bool enabled,GameObject level)
    {
        Capture();evening=enabled;
        if(room!=null)room.SetActive(!enabled);if(station!=null)station.SetActive(enabled);
        foreach(var pair in original)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
        original.Clear();
        foreach(var ornament in ornaments)if(ornament!=null){if(Application.isPlaying)Destroy(ornament);else DestroyImmediate(ornament);}ornaments.Clear();
        foreach(var pair in originalVisibility)if(pair.Key!=null)pair.Key.enabled=pair.Value;originalVisibility.Clear();
        RenderSettings.ambientSkyColor=enabled?new Color(.32f,.40f,.52f):sky;
        RenderSettings.ambientEquatorColor=enabled?new Color(.25f,.29f,.35f):equator;
        RenderSettings.ambientGroundColor=enabled?new Color(.09f,.085f,.075f):ground;
        if(gameplayCamera!=null)
        {
            gameplayCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            if(enabled){gameplayCamera.transform.position=new Vector3(0,13.3f,-19.8f);gameplayCamera.transform.LookAt(new Vector3(0,0,-.3f));}
        }
        if(manager!=null&&gameplayCamera!=null)manager.SetGameplayCameraRestPose(gameplayCamera.transform.position,gameplayCamera.transform.rotation);
        var hud=FindAnyObjectByType<ToyBoxGameplayHud>();
        if(hud!=null){if(originalTimerInk==null)originalTimerInk=hud.normalTime;hud.normalTime=enabled?ivory:originalTimerInk;}
        if(!enabled)return;
        if(level!=null)Tint(level);
        if(manager!=null&&manager.puck!=null)Tint(manager.puck.gameObject);
        if(hud!=null)Tint(hud.gameObject);
    }
    void Tint(GameObject root)
    {
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n=r.name;Material material=null;
            // World 2 uses larger, individually modeled instruction plaques.
            bool instruction=false;
            for(var p=r.transform.parent;p!=null&&p!=root.transform;p=p.parent)
                if(p.name=="ControlInstructionCards"){instruction=true;break;}
            if(instruction){originalVisibility[r]=r.enabled;r.enabled=false;continue;}
            if(n.Contains("Rail")||n.Contains("Cabinet")||n.Contains("Deck")||n.Contains("Bumper")||n.EndsWith("_SolidWood")||n.StartsWith("BonusBank")||n=="BirchPlaque"||n=="AllowancePanel"||n=="MoveCountTile"||n=="CreamSwallowtail")material=navy;
            if(n.Contains("CornerCap")||n.Contains("Rivet")||n.Contains("Fastener")||n.Contains("Trim"))material=brass;
            if(n=="MapleWithCutout")material=ivory;
            if(n.StartsWith("BonusBank")&&bumper!=null)material=bumper;
            if(n.StartsWith("BonusBank")&&rivetPrefab!=null)
            {
                var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                foreach(int side in new[]{-1,1})
                {
                    var detail=Instantiate(rivetPrefab,r.transform);detail.transform.localPosition=new Vector3(side*(mesh.bounds.extents.x-.15f),0,-mesh.bounds.extents.z-.025f);ornaments.Add(detail);
                    var top=Instantiate(rivetPrefab,r.transform);top.transform.localPosition=new Vector3(side*(mesh.bounds.extents.x-.18f),mesh.bounds.extents.y+.012f,0);top.transform.localRotation=Quaternion.Euler(90,0,0);ornaments.Add(top);
                }
            }
            if(n=="NavyPod"||n=="TealFoot"){originalVisibility[r]=r.enabled;r.enabled=false;}
            if(n=="SilverButtonBezel"||n=="CyanPerimeter"||n=="TealRim"||n=="WoodPole"||n=="BirchSideInlay"||n=="BUCA")material=brass;
            if(n=="CreamInset")material=navy;
            if(n=="SHOOTButton"||n=="DarkSocketInset")material=blackCap;
            if(n=="BeveledGoalRim")material=goalBlue;
            string old=r.sharedMaterial!=null?r.sharedMaterial.name:"";
            if(old=="NavyLettering"||old=="ControlInstructionInk")material=ivory;
            if(manager!=null&&manager.puck!=null&&root==manager.puck.gameObject&&r.enabled&&!n.Contains("Shadow"))material=goldPuck;
            if(material==null)continue;
            original[r]=r.sharedMaterials;var replacement=new Material[r.sharedMaterials.Length];for(int i=0;i<replacement.Length;i++)replacement[i]=material;r.sharedMaterials=replacement;
        }
    }
}
