# ════════════════════════════════════════════════════════════════════
# RealBuca — Minimal/Clean 3D asset builder
# ════════════════════════════════════════════════════════════════════
# HOW TO RUN:
#   1. In Blender: switch to the "Scripting" workspace (top tab bar)
#   2. Click "Open" → pick this file  (or "New" then paste this whole file)
#   3. Click the ▶ "Run Script" button
#   4. Watch the console (Window → Toggle System Console on Windows) for
#      "[BucaAssets] DONE" — every mesh exports to Assets/Models/*.obj
#
# WHY OBJ (not FBX): Blender 5.1's FBX exporter hangs via remote execution,
# and OBJ is imported natively by Unity with zero extra packages. Static
# props don't need FBX rigging/animation features. Materials are re-authored
# Unity-side anyway, so OBJ+MTL is all we need.
#
# UNITS: every mesh is authored to match Unity's built-in primitive that it
# replaces (cube 1³, sphere Ø1, cylinder Ø1×2, capsule Ø1×2), with the
# Blender-Z→Unity-Y axis baked into the exported vertices. That means the
# game's existing scale math (TubeRadius*2, length*0.5, etc.) is UNCHANGED —
# a straight mesh swap, no re-tuning of the 15 saved level prefabs.
# ════════════════════════════════════════════════════════════════════

import bpy, os, math, mathutils

OUT = r"C:/Users/musok/OneDrive/Desktop/RealBuca/RealBuca/Assets/Models"
os.makedirs(OUT, exist_ok=True)

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

def apply_xforms(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

def export_obj(obj, name):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    fp = os.path.join(OUT, name + ".obj")
    bpy.ops.wm.obj_export(
        filepath=fp,
        export_selected_objects=True,
        forward_axis='NEGATIVE_Z',   # Blender -Z → Unity +Z
        up_axis='Y',                 # Blender  Z → Unity +Y (baked into verts)
        apply_modifiers=True,
        export_materials=True,
        export_triangulated_mesh=True,
    )
    tri = len(obj.data.polygons)
    print(f"[BucaAssets]   exported {name}.obj  ({tri} faces)")

def build(name, make_fn):
    try:
        clear_meshes()
        obj = make_fn()
        obj.name = name
        apply_xforms(obj)
        export_obj(obj, name)
    except Exception as e:
        print(f"[BucaAssets] !! {name} FAILED: {e}")

# ─── asset makers (Blender Z-up; Z becomes Unity Y after export) ──────

def make_puck():
    # Rounded pebble filling Ø1×1 → matches the sphere collider bounds
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.5, depth=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.22, segments=6, angle=30)
    return o

def make_wall():
    # Stadium bar matching Unity capsule (Ø1 cross-section, length 2 along Z→Y).
    # Game scales it to (0.24, len*0.5, 0.24); rounded ends keep the capsule read.
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    o.scale = (0.5, 0.5, 1.0)            # → 1×1×2 box (Ø1 cross, length 2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel_and_smooth(o, width=0.49, segments=8, angle=60)   # round into a capsule
    return o

def make_hole_ring():
    # Flat solid disc with a softly chamfered top rim (game scales y→0.04)
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.5, depth=2.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.08, segments=4, angle=50)
    return o

def make_hole_well():
    # Shallow inverted-cone funnel for a sense of depth (game scales y flat)
    bpy.ops.mesh.primitive_cone_add(vertices=64, radius1=0.5, radius2=0.32, depth=2.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.05, segments=3, angle=50)
    return o

def make_corner():
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.5)
    o = bpy.context.active_object
    for p in o.data.polygons: p.use_smooth = True
    return o

def make_bounce_pad():
    # Rounded launch plate (game scales 1.2×0.08×0.4)
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.16, segments=4, angle=60)
    return o

def make_speed_boost():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.16, segments=4, angle=60)
    return o

def make_pickup():
    # Faceted gem (low-subdiv ico → clean flat facets)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.5)
    o = bpy.context.active_object
    return o   # leave faceted (flat shading) for a crystal look

def make_bumper():
    # Rounded dome bumper (game scales (dia,0.18,dia) on cylinder bounds)
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.5, depth=2.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.45, segments=6, angle=45)
    return o

def make_teleporter():
    # Portal disc with a raised outer rim
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.5, depth=2.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.10, segments=4, angle=50)
    return o

def make_floor():
    # Big slab — gentle rounded top edges only (game scales ~10.4×0.08×15.4)
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.04, segments=3, angle=60)
    return o

def make_table_bevel():
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    o = bpy.context.active_object
    bevel_and_smooth(o, width=0.10, segments=3, angle=60)
    return o

# ─── run all ──────────────────────────────────────────────────────────
print("[BucaAssets] building → " + OUT)
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
print("[BucaAssets] DONE — 12 meshes in Assets/Models/. Re-focus Unity to import.")
