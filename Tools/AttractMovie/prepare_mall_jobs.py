"""Write reproducible, scene-specific motion jobs. Does not start generation."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"output/buca-mall-film/jobs"
OUT.mkdir(parents=True,exist_ok=True)
opening=json.loads((OUT.parent/"opening-motion-test.json").read_text())["prompt"]
common=" Photorealistic live-action advertisement in the same bright warm shopping mall. Preserve the same boy's face, age, brown hair, navy hoodie, natural anatomy and the exact cabinet shape and button layout from the input image. Natural human movement and breathing. No talking, no titles, no cuts within the shot."
scenes=[
    ("01-walk",6,"01-walking",opening,None),
    ("02-approach",5,"02-approach","The boy takes two natural small steps forward toward the black tabletop arcade on its separate stand. He eagerly raises his left hand toward its left-side joystick. The camera gently tracks forward behind his left shoulder, revealing the physical controls and screen. The cabinet remains stationary, rigid, and consistently proportioned. His hands move naturally with his arms.",None),
    ("03-select",5,"03-controls","Locked-off over-the-left-shoulder shot of the boy selecting a game on the arcade. His left hand gently nudges the joystick sideways and returns it to center. His right index finger then presses the BLACK button immediately below the green button once, then releases it. His other right fingers stay naturally curled over the control deck. The monitor and cabinet remain rigid and stationary. His head leans slightly forward with interest. Both hands remain anatomically intact and connected to the same arms.",None),
    ("04-play",6,"03-controls","Locked-off over-the-left-shoulder shot. The boy's left hand gently pulls the physical black joystick toward himself to aim, then holds it steady. His right index finger presses the black front-left button below the green button. He HOLDS this button down while concentrating, then lifts the fingertip and RELEASES it near the end of the shot. The right hand stays on the control deck. The boy subtly shifts his shoulders naturally. Preserve the stationary rigid cabinet and screen corners. Believable hands, small precise finger movement, no extra fingers.",None),
    ("05-focus",3,"06-focus","A close view of the same boy's face as he watches his shot in the arcade. His eyes follow the game slightly downward and to his right. He leans forward a little with hopeful concentration, naturally blinks once, and begins to smile. Subtle natural head and shoulder movement. Camera makes an almost imperceptible smooth push-in. His hands stay out of frame.",None),
    ("06-win",5,"03-controls","Locked-off view over the boy's left shoulder at the arcade. The boy has released the black button and his right index finger rests relaxed just beside it. His left hand loosely holds the joystick without moving it. He watches his shot, leans forward slightly, then lifts his shoulders a little in delight when he wins near the end of the shot. Keep the cabinet rigid and all four screen corners stationary, with realistic small head and body movement.",None),
    ("07-celebrate",6,"04-reaction","The boy has just won his arcade game. Looking at the screen, his smile grows into a huge joyful smile and a natural joyful laugh. He turns toward the camera, raises BOTH hands from his waist into a spontaneous two-fist celebration near his chest, and gives one happy small fist pump. His hands are anatomically natural. His eyes are bright and excited. The compact black cabinet remains fixed beside him. Camera stays still; background shoppers move naturally. End with the same smiling two-fist pose as the final reference. Sound: a brief happy child laugh, subdued mall ambience, no spoken words and no music.","05-final"),
    ("08-invite",4,"05-final","The delighted boy stands beside the winning arcade machine, looking toward the camera with a huge warm inviting smile. His raised fists relax slightly while he gives a small joyful nod and naturally breathes and blinks. Subtle continuous human motion throughout, a friendly candid end to a real advertisement. The cabinet remains fixed. Camera stays still. Leave the upper-right background clear for the final title added in postproduction.","05-final"),
]
for index,(name,duration,anchor,prompt,end) in enumerate(scenes):
    # LTX conditions movement on frame rate. 16 fps reduces memory/attention
    # cost on the M2; optical flow produces the uniform 24 fps edit afterward.
    fps=24 if index==0 else 16
    frames=duration*fps+1
    job={"name":name,"duration":duration,"prompt":prompt+("" if index==0 else common),
         "anchor":f"anchors/{anchor}.png","output":f"motion/{name}.mp4",
         "width":1024,"height":576,"frames":frames,"frame_rate":fps,"seed":92+index*7}
    if end:job["end_anchor"]=f"anchors/{end}.png"
    if index>=2:job["stage2_steps"]=2
    if name=="06-win":
        # The win is a screen close-up, using an already filmed moving cabinet
        # plate. This keeps the real puck/goal and result card large and clear.
        job["reuse_motion"]="motion/03-select.mp4"
    if name=="07-celebrate":job["audio"]=True
    (OUT/f"{name}.json").write_text(json.dumps(job,indent=2)+"\n")
print("Prepared",len(scenes),"motion jobs totaling",sum(s[1] for s in scenes),"seconds.")
