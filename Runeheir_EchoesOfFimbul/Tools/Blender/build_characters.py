"""Builds the Runeheir characters in Blender and exports them to the Unity project.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_characters.py
    ... -- --only warrior,einherjar --no-render

Writes:
  Assets/_Runeheir/Resources/Characters/<outfit>_<m|f>.fbx   rigged body + outfit (+ "Cape" mesh when it has one)
  Assets/_Runeheir/Resources/Characters/hair_<n>.fbx         hair styles, modelled around the Head joint
  Assets/_Runeheir/Resources/Characters/outfits.json         palettes and which jobs wear which outfit
  Tools/Blender/Previews/*.png                                lineup renders (toon preview, not the game shader)
  Tools/Blender/Out/characters.blend                          the built scene, to open and tweak

Everything is generated from code (rh_geo, rh_body, rh_outfits) so the art rebuilds identically and can be reviewed.
"""

import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Euler, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import rh_body  # noqa: E402
import rh_hair  # noqa: E402
import rh_geo  # noqa: E402
import rh_outfits  # noqa: E402
import rh_weapons  # noqa: E402
from rh_body import PARENTS, Proportions, mirror_name  # noqa: E402

PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(PROJECT, "Assets", "_Runeheir", "Resources", "Characters")
PREVIEW_DIR = os.path.join(HERE, "Previews")
BLEND_DIR = os.path.join(HERE, "Out")

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg_value(name, default=None):
    if name in ARGS:
        i = ARGS.index(name)
        return ARGS[i + 1] if i + 1 < len(ARGS) else default
    return default


ONLY = set(arg_value("--only", "").split(",")) - {""}
RENDER = "--no-render" not in ARGS
EXPORT = "--no-export" not in ARGS

BONE_ORDER = ["Hips", "Spine", "Chest", "Neck", "Head",
              "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
              "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
              "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
              "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"]


# ---------------------------------------------------------------------------------------------- mesh builder
class Builder:
    def __init__(self):
        self.verts = []
        self.weights = []
        self.faces = []
        self.face_slots = []
        self.slots = []

    def add(self, part, slot, weigh=None):
        verts, faces = part
        base = len(self.verts)
        for v in verts:
            v = Vector(v)
            self.verts.append(v)
            w = weigh(v) if weigh else {}
            total = sum(w.values()) or 1.0
            self.weights.append({k: x / total for k, x in w.items() if x > 1e-4})
        if slot not in self.slots:
            self.slots.append(slot)
        si = self.slots.index(slot)
        for f in faces:
            idx = [base + i for i in f]
            clean = [i for k, i in enumerate(idx) if i != idx[k - 1]]
            if len(set(clean)) >= 3:
                self.faces.append(clean)
                self.face_slots.append(si)

    @property
    def empty(self):
        return not self.faces

    def build(self, name, rig, materials, collection):
        me = bpy.data.meshes.new(name)
        me.from_pydata([tuple(v) for v in self.verts], [], self.faces)
        me.update()
        for slot in self.slots:
            me.materials.append(materials[slot])
        me.polygons.foreach_set("material_index", self.face_slots)
        me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(me)
        bm.free()
        me.set_sharp_from_angle(angle=math.radians(38.0))
        obj = bpy.data.objects.new(name, me)
        collection.objects.link(obj)
        groups = {}
        for i, w in enumerate(self.weights):
            for bone, value in w.items():
                if bone not in groups:
                    groups[bone] = obj.vertex_groups.new(name=bone)
                groups[bone].add([i], value, "REPLACE")
        if rig is not None:
            obj.parent = rig
            mod = obj.modifiers.new("Armature", "ARMATURE")
            mod.object = rig
        return obj


# ---------------------------------------------------------------------------------------------- materials
def srgb_to_linear(c):
    return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92


def hex_rgba(hexstr):
    h = hexstr.lstrip("#")
    r, g, b = (int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    return (srgb_to_linear(r), srgb_to_linear(g), srgb_to_linear(b), 1.0)


def darker(hexstr, k=0.62):
    h = hexstr.lstrip("#")
    r, g, b = (int(int(h[i:i + 2], 16) * k) for i in (0, 2, 4))
    return "#%02X%02X%02X" % (r, g, b)


def toon_material(name, hexstr, glow=False):
    """Preview stand-in for Runeheir/Toon: one hard light band with a cool shadow tint."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    color = hex_rgba(hexstr)
    if glow:
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = color
        em.inputs["Strength"].default_value = 2.5
        nt.links.new(em.outputs[0], out.inputs["Surface"])
    else:
        diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
        diff.inputs["Color"].default_value = (1, 1, 1, 1)
        s2r = nt.nodes.new("ShaderNodeShaderToRGB")
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "CONSTANT"
        ramp.color_ramp.elements[0].position = 0.0
        ramp.color_ramp.elements[0].color = (0.45, 0.48, 0.66, 1.0)
        ramp.color_ramp.elements[1].position = 0.22
        ramp.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        mul.inputs[6].default_value = color
        em = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(diff.outputs[0], s2r.inputs[0])
        nt.links.new(s2r.outputs["Color"], ramp.inputs["Fac"])
        nt.links.new(ramp.outputs["Color"], mul.inputs[7])
        nt.links.new(mul.outputs[2], em.inputs["Color"])
        nt.links.new(em.outputs[0], out.inputs["Surface"])
    mat.diffuse_color = color
    return mat


def outline_material():
    mat = bpy.data.materials.get("RH_Outline")
    if mat:
        return mat
    mat = bpy.data.materials.new("RH_Outline")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (0.012, 0.008, 0.01, 1.0)
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    mat.use_backface_culling = True
    return mat


NO_OUTLINE = {"EyeWhite", "Eye", "Lash", "Highlight", "Brow", "Mouth", "Glow"}


def add_outline(obj, width=0.0045):
    mats = obj.data.materials
    mats.append(outline_material())
    mod = obj.modifiers.new("Outline", "SOLIDIFY")
    mod.thickness = width
    mod.offset = 1.0
    mod.use_flip_normals = True
    mod.use_rim = False
    mod.material_offset = len(mats) - 1
    # face features get no ink (they are the ink)
    vg = obj.vertex_groups.new(name="_ink")
    ink = set()
    for poly in obj.data.polygons:
        if mats[poly.material_index].name.split("@")[0] not in NO_OUTLINE:
            ink.update(poly.vertices)
    vg.add(list(ink), 1.0, "REPLACE")
    mod.vertex_group = "_ink"
    mod.thickness_vertex_group = 0.0


# ---------------------------------------------------------------------------------------------- rig
def build_rig(P, name, collection):
    data = bpy.data.armatures.new(name)
    rig = bpy.data.objects.new(name, data)
    collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    deselect_all()
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in BONE_ORDER:
        h, t = P.joints[bone]
        eb = data.edit_bones.new(bone)
        eb.head = h
        eb.tail = t
        eb.roll = 0.0
    for bone in BONE_ORDER:
        parent = PARENTS.get(bone) or PARENTS.get(bone.replace("Right", "Left"))
        if parent:
            data.edit_bones[bone].parent = data.edit_bones[mirror_name(parent) if bone.startswith("Right") else parent]
            data.edit_bones[bone].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


# ---------------------------------------------------------------------------------------------- scene
def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for block in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item)


def palette_for(key):
    pal = dict(rh_outfits.PALETTES[key])
    job_hex = "#9C8D74"  # the Initiate, for the preview of the common outfit
    for slot, value in list(pal.items()):
        if value == "$outfit":
            pal[slot] = job_hex
        elif value == "$outfitDark":
            pal[slot] = darker(job_hex)
    return pal


def slot_materials(key, gender, character_colors):
    """Materials named exactly after their slots (that is what the FBX carries to Unity)."""
    pal = palette_for(key)
    pal.update(character_colors)
    mats = {}
    for slot, hexstr in pal.items():
        old = bpy.data.materials.get(slot)
        if old:
            old.name = slot + "@old"
        mats[slot] = toon_material(slot, hexstr, glow=(slot in ("Glow", "Highlight")))
    return mats


def deselect_all():
    for o in list(bpy.context.view_layer.objects):
        if o is not None:
            o.select_set(False)


def export_fbx(path, objects, turn=True):
    """Unity reads Blender's -Y front as -Z; the game faces +Z, so the top-level objects turn 180 degrees for the export
    (their children follow) and turn back after."""
    deselect_all()
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    roots = [o for o in objects if o.parent is None or o.parent not in objects] if turn else []
    for o in roots:
        o.rotation_euler.z += math.pi
    bpy.context.view_layer.update()
    try:
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
            axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="OFF", use_mesh_modifiers=True,
            use_armature_deform_only=True, armature_nodetype="NULL", primary_bone_axis="Y", secondary_bone_axis="X",
            use_custom_props=False, path_mode="AUTO", embed_textures=False)
    finally:
        for o in roots:
            o.rotation_euler.z -= math.pi
        bpy.context.view_layer.update()


PREVIEW_HAIR = {  # (style, hair color, brow) per lineup slot, to show the range
    "m": [(1, "#E8D7A0", "#B89B60"), (7, "#7A4A2A", "#5A3520"), (0, "#1E1A1E", "#141014"), (1, "#D9DDE4", "#9AA0AA"),
          (6, "#A8321F", "#7A2416"), (2, "#E8D7A0", "#B89B60")],
    "f": [(3, "#E8D7A0", "#B89B60"), (4, "#A8321F", "#7A2416"), (2, "#7A4A2A", "#5A3520"), (5, "#1E1A1E", "#141014"),
          (2, "#D9DDE4", "#9AA0AA"), (3, "#F2E6C2", "#C9AE76")],
}


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    os.makedirs(BLEND_DIR, exist_ok=True)
    clear_scene()
    scene = bpy.context.scene
    root = scene.collection

    keys = [k for k in rh_outfits.OUTFITS if not ONLY or k in ONLY]
    built = []  # (key, gender, rig, meshes)

    # hair styles: exported once, then copied onto each preview character
    hair_meshes = {}
    for i in range(len(rh_hair.HAIR_STYLES)):
        b = Builder()
        b.add(rh_hair.hair_style(i), "Hair", lambda p: {"Head": 1.0})
        mats = slot_materials("common", "m", {"Hair": "#E8D7A0"})
        obj = b.build("Hair", None, mats, root)
        if EXPORT:
            export_fbx(os.path.join(OUT_DIR, "hair_%d.fbx" % i), [obj])
        obj.name = "HairStyle_%d" % i
        hair_meshes[i] = obj
        for m in obj.data.materials:
            m.name = "Hair@style%d" % i
        obj.hide_render = True

    build_weapons(root)

    for gender in ("m", "f"):
        P = Proportions(gender == "f")
        for idx, key in enumerate(keys):
            coll = bpy.data.collections.new("%s_%s" % (key, gender))
            root.children.link(coll)
            style, hair, brow = PREVIEW_HAIR[gender][idx % 6]
            mats = slot_materials(key, gender, {**rh_outfits.CHARACTER_SLOTS, **rh_outfits.GARMENT_PREVIEW, "Hair": hair, "Brow": brow})
            rig = build_rig(P, "Rig", coll)
            B, C = Builder(), Builder()
            rh_outfits.OUTFITS[key](B, C, P)
            body = B.build("Body", rig, mats, coll)
            meshes = [body]
            if not C.empty:
                meshes.append(C.build("Cape", rig, mats, coll))
            G = Builder()
            rh_outfits.garment_cape(G, P, key)
            garment = G.build("GarmentCape", rig, mats, coll)
            garment.hide_render = True  # shown in game only while a cloak is worn
            if EXPORT:
                export_fbx(os.path.join(OUT_DIR, "%s_%s.fbx" % (key, gender)), [rig] + meshes + [garment])
            garment.name = "GarmentCape_%s_%s" % (key, gender)
            # unique names from here on, so every character keeps its own colors in the lineup
            rig.name = "Rig_%s_%s" % (key, gender)
            for o in meshes:
                o.name = "%s_%s_%s" % (o.name, key, gender)
            for slot, m in mats.items():
                m.name = "%s@%s_%s" % (slot, key, gender)
            # preview hair
            h = hair_meshes[style].copy()
            h.data = hair_meshes[style].data.copy()
            h.data.materials.clear()
            h.data.materials.append(mats["Hair"])
            h.hide_render = False
            h.name = "Hair_%s_%s" % (key, gender)
            h.location = P.head_origin
            h.parent = rig
            coll.objects.link(h)
            meshes.append(h)
            built.append((key, gender, rig, meshes, idx))
            print("RH built %s_%s: %d verts" % (key, gender, sum(len(o.data.vertices) for o in meshes)))

    write_manifest()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, "characters.blend"))
    if RENDER:
        render_previews(built, len(keys))




def weapon_materials():
    mats = {}
    for slot, hexstr in rh_weapons.PALETTE.items():
        old = bpy.data.materials.get(slot)
        if old:
            old.name = slot + "@old"
        mats[slot] = toon_material(slot, hexstr, glow=(slot == "Gem"))
    return mats


def build_weapons(root):
    """One model per weapon type, gripped at the origin (Characters/Weapons/<Type>.fbx); kept out of the lineup renders."""
    folder = os.path.join(OUT_DIR, "Weapons")
    os.makedirs(folder, exist_ok=True)
    static = lambda p: {}
    for name, build in rh_weapons.WEAPON_BUILDERS.items():
        mats = weapon_materials()
        W = Builder()
        build(W)
        obj = W.build(name, None, mats, root)
        objects = [obj]
        if EXPORT:
            export_fbx(os.path.join(folder, name + ".fbx"), objects, turn=False)
        finish_weapon(objects, name)
    mats = weapon_materials()
    W, S, A = Builder(), Builder(), Builder()
    rh_weapons.w_bow(W, S, A)
    objects = [W.build("Bow", None, mats, root), S.build("String", None, mats, root), A.build("Arrow", None, mats, root)]
    if EXPORT:
        export_fbx(os.path.join(folder, "Bow.fbx"), objects, turn=False)
    finish_weapon(objects, "Bow")


def finish_weapon(objects, name):
    for o in objects:
        o.name = "Weapon_%s_%s" % (name, o.name)
        o.hide_render = True
        for m in o.data.materials:
            if "@" not in m.name:
                m.name = "%s@%s" % (m.name, name)


def write_manifest():
    if not EXPORT:
        return
    outfits = []
    for key, pal in rh_outfits.PALETTES.items():
        outfits.append({
            "key": key,
            "jobs": rh_outfits.OUTFIT_JOBS.get(key, []),
            "slots": [{"slot": s, "color": c} for s, c in pal.items()],
        })
    weapons = [{"slot": s, "color": c} for s, c in rh_weapons.PALETTE.items()]
    data = {"outfits": outfits, "hairStyles": len(rh_hair.HAIR_STYLES), "weaponSlots": weapons}
    with open(os.path.join(OUT_DIR, "outfits.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.write("\n")


# ---------------------------------------------------------------------------------------------- previews
def render_previews(built, count):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False
    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.62, 0.68, 0.78, 1.0)
    bg.inputs["Strength"].default_value = 0.6

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.0
    sun_data.angle = math.radians(2)
    sun = bpy.data.objects.new("Sun", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = Euler((math.radians(50), 0.0, math.radians(-35)))

    # ground
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.data.materials.append(toon_material("Ground", "#E9EEF3"))

    spacing = 0.85
    for key, gender, rig, meshes, idx in built:
        row = 0 if gender == "m" else 1
        rig.location = Vector(((idx - (count - 1) / 2) * spacing, row * 1.6, 0.0))
        for o in meshes:
            if o.type == "MESH":
                add_outline(o)

    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def shoot(name, loc, rot, lens=None, ortho=None, rows=(0, 1)):
        for key, gender, rig, meshes, idx in built:
            show = (0 if gender == "m" else 1) in rows
            for o in meshes:
                o.hide_render = not show
        cam.location = loc
        cam.rotation_euler = Euler([math.radians(a) for a in rot])
        if ortho:
            cam_data.type = "ORTHO"
            cam_data.ortho_scale = ortho
        else:
            cam_data.type = "PERSP"
            cam_data.lens = lens or 50
        scene.render.filepath = os.path.join(PREVIEW_DIR, name)
        bpy.ops.render.render(write_still=True)
        print("RH preview", scene.render.filepath)

    width = count * spacing
    for row, tag in ((0, "male"), (1, "female")):
        y = row * 1.6
        shoot("lineup_%s_front.png" % tag, (0.75, y - 6.6, 1.25), (87, 0, 6.5), lens=47, rows=(row,))
        shoot("lineup_%s_back.png" % tag, (-0.75, y + 6.6, 1.25), (87, 0, 186.5), lens=47, rows=(row,))
    # the game's camera: pitch 45, yaw 45, far away
    d = 9.0
    shoot("lineup_game_angle.png", (d * 0.5, -d * 0.5 + 0.8, d * 0.72 + 0.5), (45, 0, 45), ortho=width * 1.15)


main()
