# ════════════════════════════════════════════════════════════════════
# RealBuca — Minimal/Clean 3D asset builder  (v3 — headless, colored)
# ════════════════════════════════════════════════════════════════════
# Run headlessly:
#   blender --background --factory-startup --python BlenderAssetBuilder.py
#
# v3 changes over v2:
#   • Two-material submeshes (Body + Accent) on Puck / Bumper /
#     Teleporter / BouncePad — Unity imports them as 2 material slots
#     so the accent parts can be tinted separately in-game.
#   • Baked MTL diffuse colors (matte-neon palette) so the OBJs look
#     right in Unity previews instead of default grey.
#   • Bounds fixes: puck ring lowered (was poking 0.025 above the Ø1×1
#     bounds), bumper band shrunk (was 8% wider than Ø1).
#   • Pickup: capless 6-sided bipyramid gem (no interior faces).
#   • Per-asset bounds printout for verification.
# ════════════════════════════════════════════════════════════════════

import bpy, os, math

OUT = r"C:/Users/musok/OneDrive/Desktop/RealBuca/RealBuca/Assets/Models"
os.makedirs(OUT, exist_ok=True)

# ─── matte-neon preview palette (Kd baked into MTL) ───────────────────
COL = {
    "puck":    (1.00, 0.82, 0.30, 1.0),   # warm amber
    "wall":    (0.92, 0.92, 0.96, 1.0),   # soft white
    "ring":    (1.00, 0.85, 0.35, 1.0),   # hole-ring gold
    "well":    (0.06, 0.05, 0.10, 1.0),   # near-black
    "pad":     (0.30, 0.90, 0.45, 1.0),   # pad green
    "boost":   (0.35, 0.80, 1.00, 1.0),   # boost cyan
    "gem":     (1.00, 0.90, 0.35, 1.0),   # pickup gold
    "bumper":  (1.00, 0.45, 0.75, 1.0),   # bumper pink
    "portal":  (0.65, 0.40, 1.00, 1.0),   # teleport purple
    "floor":   (0.55, 0.30, 0.85, 1.0),   # purple slab
    "bevel":   (0.10, 0.08, 0.16, 1.0),   # dark frame
    "accent":  (0.13, 0.12, 0.18, 1.0),   # shared dark accent
}

def make_mat(name, rgba):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = 0.55
    m.diffuse_color = rgba
    return m

def paint(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)

# ─── helpers ──────────────────────────────────────────────────────────
def clear_meshes():
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            bpy.data.objects.remove(o, do_unlink=True)

def bevel_and_smooth(obj, width=0.06, segments=3, smooth=True, angle=40):
    bpy.context.view_layer.objects.active = obj
    m = obj.modifiers.new("bev", 'BEVEL')
    m.width = width; m.segments = segments
    m.limit_method = 'ANGLE'; m.angle_limit = math.radians(angle)
    bpy.ops.object.modifier_apply(modifier=m.name)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True

def join(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    return bpy.context.active_object

def apply_xforms(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    # location=True is load-bearing: join() keeps the FIRST part's origin,
    # so multi-part assets whose first part isn't at (0,0,0) — HoleRing's
    # low tier, Pickup's top cone — would otherwise export off-center
    # (caught by the bounds printout: HoleRing Z[-0.6,+1.4] instead of
    # [-1,+1]). Baking location makes vertex coords == world coords.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def print_bounds(obj, name):
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    print(f"[BucaAssets]   {name} bounds X[{min(xs):+.3f},{max(xs):+.3f}] "
          f"Y[{min(ys):+.3f},{max(ys):+.3f}] Z[{min(zs):+.3f},{max(zs):+.3f}] "
          f"mats={len(obj.data.materials)}")

def export_obj(obj, name):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    fp = os.path.join(OUT, name + ".obj")
    bpy.ops.wm.obj_export(
        filepath=fp,
        export_selected_objects=True,
        forward_axis='NEGATIVE_Z',
        up_axis='Y',
        apply_modifiers=True,
        export_materials=True,
        export_triangulated_mesh=True,
    )
    print(f"[BucaAssets]   exported {name}.obj  ({len(obj.data.polygons)} faces)")

def build(name, make_fn):
    try:
        clear_meshes()
        obj = make_fn()
        obj.name = name
        apply_xforms(obj)
        print_bounds(obj, name)
        export_obj(obj, name)
    except Exception as e:
        import traceback; traceback.print_exc()
        print(f"[BucaAssets] !! {name} FAILED: {e}")

ACCENT = None  # initialized in main

# ─── asset makers (Blender Z-up; Z becomes Unity Y on export) ─────────

def make_puck():
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.5, depth=1.0)
    body = bpy.context.active_object
    bevel_and_smooth(body, width=0.20, segments=6, angle=30)
    paint(body, make_mat("Puck_Body", COL["puck"]))
    # Inset top ring — z lowered to 0.42 so top (0.42+0.045) stays inside
    # the Ø1×1 bounds (v2 poked 0.025 above).
    bpy.ops.mesh.primitive_torus_add(major_radius=0.30, minor_radius=0.045,
                                     major_segments=48, minor_segments=12,
                                     location=(0, 0, 0.42))
    ring = bpy.context.active_object
    for p in ring.data.polygons: p.use_smooth = True
    paint(ring, ACCENT)
    return join([body, ring])

def make_wall():
    # MUST fill Ø1 × length-2 — Unity's capsule primitive bounds. The game
    # scales walls as (TubeRadius*2, len*0.5, TubeRadius*2) against those
    # bounds; v1-v3 were authored at HALF size (Ø0.5×1) which would have
    # rendered every wall at half thickness. Caught by the bounds check.
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    o.scale = (1.0, 1.0, 2.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel_and_smooth(o, width=0.49, segments=8, angle=60)
    paint(o, make_mat("Wall_Body", COL["wall"]))
    return o

def make_hole_ring():
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.5, depth=1.2,
                                        location=(0, 0, -0.4))
    low = bpy.context.active_object
    bevel_and_smooth(low, width=0.07, segments=4, angle=50)
    paint(low, make_mat("HoleRing_Body", COL["ring"]))
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.42, depth=0.8,
                                        location=(0, 0, 0.6))
    high = bpy.context.active_object
    bevel_and_smooth(high, width=0.06, segments=4, angle=50)
    paint(high, make_mat("HoleRing_Body", COL["ring"]))
    return join([low, high])

def make_hole_well():
    bpy.ops.mesh.primitive_cone_add(vertices=64, radius1=0.5, radius2=0.32, depth=2.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.05, segments=3, angle=50)
    paint(o, make_mat("HoleWell_Body", COL["well"]))
    return o

def make_corner():
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.5)
    o = bpy.context.active_object
    for p in o.data.polygons: p.use_smooth = True
    paint(o, make_mat("Wall_Body", COL["wall"]))
    return o

def make_bounce_pad():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    plate = bpy.context.active_object
    bevel_and_smooth(plate, width=0.14, segments=4, angle=60)
    paint(plate, make_mat("Pad_Body", COL["pad"]))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.30))
    bar = bpy.context.active_object
    bar.scale = (0.55, 0.18, 0.25)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel_and_smooth(bar, width=0.07, segments=3, angle=60)
    paint(bar, ACCENT)
    return join([plate, bar])

def make_speed_boost():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.16, segments=4, angle=60)
    paint(o, make_mat("Boost_Body", COL["boost"]))
    return o

def make_pickup():
    # Capless 6-sided bipyramid gem — no interior faces (v2 had two
    # buried base quads from the capped cones).
    top_kwargs = dict(vertices=6, radius1=0.42, radius2=0.0, depth=0.62)
    bpy.ops.mesh.primitive_cone_add(end_fill_type='NOTHING',
                                    location=(0, 0, 0.31), **top_kwargs)
    top = bpy.context.active_object
    bpy.ops.mesh.primitive_cone_add(end_fill_type='NOTHING',
                                    location=(0, 0, -0.31),
                                    rotation=(math.pi, 0, 0), **top_kwargs)
    bot = bpy.context.active_object
    gem = join([top, bot])
    paint(gem, make_mat("Gem_Body", COL["gem"]))
    return gem

def make_bumper():
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.46, depth=2.0)
    dome = bpy.context.active_object
    bevel_and_smooth(dome, width=0.42, segments=6, angle=45)
    paint(dome, make_mat("Bumper_Body", COL["bumper"]))
    # Band shrunk: 0.44 + 0.055 = 0.495 ≤ 0.5 (v2 was 0.54 — 8% outside Ø1)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.44, minor_radius=0.055,
                                     major_segments=48, minor_segments=12)
    band = bpy.context.active_object
    for p in band.data.polygons: p.use_smooth = True
    paint(band, ACCENT)
    return join([dome, band])

def make_teleporter():
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.5, depth=2.0)
    rim = bpy.context.active_object
    bevel_and_smooth(rim, width=0.10, segments=4, angle=50)
    paint(rim, make_mat("Portal_Body", COL["portal"]))
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.36, depth=1.0)
    inner = bpy.context.active_object
    bevel_and_smooth(inner, width=0.05, segments=3, angle=50)
    paint(inner, ACCENT)
    return join([rim, inner])

def make_floor():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.04, segments=3, angle=60)
    paint(o, make_mat("Floor_Body", COL["floor"]))
    return o

def make_table_bevel():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.10, segments=3, angle=60)
    paint(o, make_mat("Frame_Body", COL["bevel"]))
    return o

# ─── run all ──────────────────────────────────────────────────────────
ACCENT = make_mat("Accent", COL["accent"])
print("[BucaAssets] v3 building -> " + OUT)
build("Puck",        make_puck)
build("Wall",        make_wall)
build("HoleRing",    make_hole_ring)
build("HoleWell",    make_hole_well)
build("Corner",      make_corner)
build("BouncePad",   make_bounce_pad)
build("SpeedBoost",  make_speed_boost)
build("Pickup",      make_pickup)
build("Bumper",      make_bumper)
build("Teleporter",  make_teleporter)
build("Floor",       make_floor)
build("TableBevel",  make_table_bevel)
print("[BucaAssets] DONE - 12 colored meshes in Assets/Models/")
