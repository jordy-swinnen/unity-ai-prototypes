"""Medieval Chess pieces: six articulated figures made of named parts.

Run headless:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/MedievalChess/make_pieces.py

Writes pawn.glb, knight.glb, bishop.glb, rook.glb, queen.glb and king.glb into
Assets/Prototypes/MedievalChess/Art/Exports/. Each file is a hierarchy of parts whose origins sit
at their joints, so Unity can rotate them by code (no skeleton). Exits with a non-zero status
if a part is missing, a piece is the wrong height, or a piece is over the triangle budget.

Parts (the same names on every piece, so one animation system drives them all):
  Base, Body, Head, ArmR > Weapon, ArmL > Shield   (knight also Horse and HorseHead, bishop and
  queen and king also have a Mitre or Crown as a child of Head)
  Face (iron, a child of Head), ShoulderBone and ShoulderRed (children of Body, Unity shows one pair)
Coordinates are written in Unity axes: x to the piece's right, y up, z forward (the piece faces +z).
Blender is Z-up and the glTF import mirrors x, so a Unity point (x, y, z) is written to Blender as
(-x, -z, y). Weapons and arms rest pointing straight up or down.

Materials are neutral and named, and Unity replaces them at runtime:
  "body" (the side colour), "iron" (weapons, eyes) and "trim" (gold: crowns, belts, rims, bosses).

Shapes (all built here from nothing, never copied):
  box     a bevelled box, optionally tapered or rotated
  lathe   a profile of (radius, height) points turned round the y axis (robes, domes, towers)
  ball    an ellipsoid
  limb    a tapered tube between two points (legs, necks, tails, arms)
Ambient occlusion is baked into a vertex colour attribute ("AO", the red channel, 1 open and lower in crevices)
by casting rays from every vertex against the whole piece in its rest pose. It needs no UV maps, and the
game's shader (Shaders/MedievalLit.shader) multiplies it into the light. The two shoulder variants do not
shade each other, because only one is shown.
Faces are smooth-shaded, and edges sharper than SHARP_DEGREES stay crisp, so a bevelled box keeps its
corners and a robe stays round. A shape marked flat=True keeps every edge crisp (a stone tower).
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Vector
from mathutils.bvhtree import BVHTree

REPO_ROOT = Path(__file__).resolve().parents[3]
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "MedievalChess" / "Art" / "Exports"
MAX_TRIANGLES = 6000
HEIGHT_TOLERANCE = 0.06
MAX_FOOTPRINT = 0.9
REQUIRED = ["Base", "Body", "Head", "ArmR", "Weapon", "ArmL", "Shield"]
SHARP_DEGREES = 52.0


def to_blender(v):
    x, y, z = v
    return Vector((-x, -z, y))


# ---- shapes: each returns a dict with Unity-space vertices and faces ----

def _shape(verts, faces, offset, rot, bevel=0.0, flat=False):
    m = Euler([math.radians(a) for a in rot], "XYZ").to_matrix()
    moved = [m @ Vector(v) + Vector(offset) for v in verts]
    return {"verts": moved, "faces": faces, "bevel": bevel, "flat": flat}


def box(size, offset=(0, 0, 0), bevel=None, rot=(0, 0, 0), taper=(1.0, 1.0), flat=False):
    """A bevelled box. taper is the x and z scale of the top and of the bottom (1, 1 is straight)."""
    sx, sy, sz = size
    if bevel is None:
        bevel = min(0.02, min(size) * 0.22)
    verts = []
    for iy, y in enumerate((-sy / 2, sy / 2)):
        k = taper[1] if iy == 0 else taper[0]
        for x in (-sx / 2, sx / 2):
            for z in (-sz / 2, sz / 2):
                verts.append((x * k, y, z * k))
    faces = [(0, 1, 3, 2), (4, 5, 7, 6), (0, 1, 5, 4), (2, 3, 7, 6), (0, 2, 6, 4), (1, 3, 7, 5)]
    return _shape(verts, faces, offset, rot, bevel, flat)


def lathe(profile, offset=(0, 0, 0), segs=16, squash=(1.0, 1.0), rot=(0, 0, 0), closed=False, flat=False, spin=0.0):
    """
    Turns a list of (radius, height) points round the y axis. A point at radius 0 becomes one vertex (a pole).
    The ends are capped, unless closed=True, which joins the last point back to the first (a ring or a rim).
    """
    verts, rings = [], []
    for r, y in profile:
        if r < 1e-6:
            verts.append((0.0, y, 0.0))
            rings.append([len(verts) - 1])
        else:
            ring = []
            for k in range(segs):
                a = spin + 2 * math.pi * k / segs
                verts.append((r * math.cos(a) * squash[0], y, r * math.sin(a) * squash[1]))
                ring.append(len(verts) - 1)
            rings.append(ring)
    faces = []

    def join(a, b):
        if len(a) == 1 and len(b) == 1:
            return
        if len(a) == 1:
            for k in range(segs):
                faces.append((a[0], b[k], b[(k + 1) % segs]))
        elif len(b) == 1:
            for k in range(segs):
                faces.append((a[k], a[(k + 1) % segs], b[0]))
        else:
            for k in range(segs):
                faces.append((a[k], a[(k + 1) % segs], b[(k + 1) % segs], b[k]))

    for i in range(len(rings) - 1):
        join(rings[i], rings[i + 1])
    if closed:
        join(rings[-1], rings[0])
    else:
        for ring in (rings[0], rings[-1]):
            if len(ring) > 1:
                faces.append(tuple(ring))
    return _shape(verts, faces, offset, rot, 0.0, flat)


def ball(rx, ry=None, rz=None, offset=(0, 0, 0), segs=14, rings=8, rot=(0, 0, 0)):
    """An ellipsoid with radii rx, ry, rz (a sphere if only rx is given)."""
    ry = rx if ry is None else ry
    rz = rx if rz is None else rz
    profile = []
    for i in range(rings + 1):
        t = -math.pi / 2 + math.pi * i / rings
        profile.append((max(0.0, rx * math.cos(t)) if 0 < i < rings else 0.0, ry * math.sin(t)))
    return lathe(profile, offset, segs, (1.0, rz / rx), rot)


def limb(p0, p1, r0, r1, segs=10, offset=(0, 0, 0)):
    """A tapered tube from p0 to p1 (Unity points) with radius r0 at p0 and r1 at p1."""
    a, b = Vector(p0), Vector(p1)
    d = b - a
    length = d.length
    q = Vector((0, 1, 0)).rotation_difference(d.normalized())
    shape = lathe([(r0, 0.0), (r1, length)], (0, 0, 0), segs)
    shape["verts"] = [q @ v + a + Vector(offset) for v in shape["verts"]]
    return shape


def slab(r, y0, y1, offset=(0, 0, 0), segs=16, squash=(1.0, 1.0)):
    """A thin round plate: a belt, a collar, a brim."""
    return lathe([(0, y0), (r, y0), (r, y1), (0, y1)], offset, segs, squash)


def plinth(r, height=0.09):
    """A stepped, rounded foot."""
    h = height
    return lathe([(0, 0), (r, 0), (r, h * 0.28), (r * 0.93, h * 0.5), (r * 0.8, h * 0.62), (r * 0.8, h * 0.74), (r * 0.7, h), (0, h)], segs=22)


def add_shape(bm, shape, slot):
    """Adds one shape to the mesh, bevels and shades it, and gives its faces a material slot."""
    start = len(bm.faces)
    verts = [bm.verts.new(to_blender(v)) for v in shape["verts"]]
    new_faces = []
    for f in shape["faces"]:
        try:
            new_faces.append(bm.faces.new([verts[i] for i in f]))
        except ValueError:
            pass  # a duplicate face at a pole
    bmesh.ops.recalc_face_normals(bm, faces=new_faces)
    if shape["bevel"] > 0.0:
        edges = {e for f in new_faces for e in f.edges}
        segments = 2 if shape["bevel"] >= 0.01 else 1
        bmesh.ops.bevel(bm, geom=list(edges), offset=shape["bevel"], segments=segments, affect="EDGES")
    bm.faces.ensure_lookup_table()
    mine = [f for f in bm.faces if f.is_valid][start:]
    limit = math.radians(SHARP_DEGREES)
    for f in mine:
        f.material_index = slot
        f.smooth = not shape["flat"]
    for e in {e for f in mine for e in f.edges}:
        if shape["flat"]:
            e.smooth = False
        elif len(e.link_faces) == 2 and e.calc_face_angle(0.0) > limit:
            e.smooth = False


def make_material(name, rgba, metallic=0.0, roughness=0.6):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


MATERIALS = {}


def new_part(name, parent, origin, content, material):
    """
    origin is the joint position relative to the parent's origin (Unity axes). content is a list of shapes in one
    material, or a dict of material name to a list of shapes (a part with more than one material).
    """
    groups = content if isinstance(content, dict) else ({material: content} if content else {})
    obj = bpy.data.objects.new(name, bpy.data.meshes.new(name) if groups else None)
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = to_blender(origin)
    if groups:
        bm = bmesh.new()
        for slot, (mat_name, shapes) in enumerate(groups.items()):
            obj.data.materials.append(MATERIALS[mat_name])
            for shape in shapes:
                add_shape(bm, shape, slot)
        bm.to_mesh(obj.data)
        bm.free()
    return obj


# ---- the pieces ----
# "head" is (radius, z offset of the face) of the Head part. "shoulder" is (x, y, size) of the arm joints, where the
# round (bone side) or pointed (cinnabar side) pauldrons sit. Heights are measured without the arms and what they hold.

def head_shapes(r, nose=True, chin=True):
    """A head: a slightly tall ball with a nose and a chin."""
    shapes = [ball(r * 0.96, r * 1.05, r)]
    if nose:
        shapes.append(box((r * 0.26, r * 0.36, r * 0.4), (0, -r * 0.08, r * 0.96), rot=(-12, 0, 0)))
    if chin:
        shapes.append(ball(r * 0.34, r * 0.24, r * 0.3, (0, -r * 0.82, r * 0.5)))
    return shapes


def helmet(r, base_y, brim=True):
    """A round helmet over the top of a head of radius r, starting at base_y, with an optional brim."""
    shapes = [lathe([(r * 1.08, base_y), (r * 1.06, base_y + r * 0.3), (r * 0.86, base_y + r * 0.7), (r * 0.5, base_y + r * 0.95),
                     (r * 0.2, base_y + r * 1.06), (0, base_y + r * 1.08)], segs=18)]
    if brim:
        shapes.append(slab(r * 1.28, base_y - 0.004, base_y + 0.022, segs=18))
    return shapes


PIECES = {
    "pawn": {
        "height": 0.5,
        "head": (0.1, 0.0),
        "shoulder": (0.18, 0.3, 0.055),
        "parts": [
            ("Base", "Root", (0, 0, 0), [plinth(0.26)], "body"),
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.085), (0.2, 0.09), (0.205, 0.12), (0.18, 0.2), (0.155, 0.3), (0.165, 0.345), (0.1, 0.37), (0, 0.37)], segs=20),
                         ball(0.1, 0.05, 0.1, (0, 0.34, 0)),
                         box((0.12, 0.14, 0.03), (0, 0.12, 0.185), rot=(8, 0, 0))],
                "trim": [slab(0.19, 0.205, 0.235, segs=20)],
            }, None),
            # a footman: a head with a nose and a helmet with a nasal bar
            ("Head", "Root", (0, 0.4, 0), {
                "body": head_shapes(0.1, chin=False) + helmet(0.1, 0.035),
                "trim": [box((0.018, 0.075, 0.014), (0, 0.0, 0.108))],
            }, None),
            ("ArmR", "Root", (0.19, 0.28, 0), [ball(0.045, 0.05, 0.045)], "body"),
            ("Weapon", "ArmR", (0, 0, 0), {
                "iron": [limb((0, -0.13, 0), (0, 0.4, 0), 0.02, 0.016, 8),
                         lathe([(0.0, 0.4), (0.032, 0.4), (0.045, 0.43), (0.03, 0.48), (0, 0.53)], segs=8, squash=(1.0, 0.45))],
                "trim": [slab(0.032, 0.385, 0.41, segs=10)],
            }, None),
            ("ArmL", "Root", (-0.19, 0.28, 0), [ball(0.045, 0.05, 0.045)], "body"),
            # a round shield with a rim and a boss
            ("Shield", "ArmL", (0, 0, 0.08), {
                "body": [lathe([(0, -0.022), (0.145, -0.022), (0.145, 0.02), (0.06, 0.028), (0, 0.032)], segs=22, rot=(90, 0, 0))],
                "trim": [lathe([(0.138, -0.028), (0.158, -0.028), (0.158, 0.028), (0.138, 0.028)], segs=22, rot=(90, 0, 0), closed=True),
                         lathe([(0.0, 0.03), (0.055, 0.03), (0.045, 0.055), (0.0, 0.07)], segs=14, rot=(90, 0, 0))],
            }, None),
        ],
    },
    "bishop": {
        "height": 0.8,
        "head": (0.11, 0.0),
        "shoulder": (0.22, 0.46, 0.06),
        "parts": [
            ("Base", "Root", (0, 0, 0), [plinth(0.28)], "body"),
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.085), (0.25, 0.09), (0.26, 0.13), (0.225, 0.24), (0.185, 0.36), (0.15, 0.46), (0.145, 0.53), (0.1, 0.56), (0, 0.56)], segs=22),
                         ball(0.17, 0.07, 0.15, (0, 0.5, 0)),   # a cape over the shoulders
                         box((0.2, 0.22, 0.03), (0, 0.18, 0.225), rot=(10, 0, 0), taper=(0.7, 1.0))],
                "trim": [slab(0.2, 0.28, 0.305, segs=20), slab(0.16, 0.455, 0.48, segs=20),
                         box((0.07, 0.3, 0.025), (0, 0.33, 0.185), rot=(8, 0, 0))],   # the stole
            }, None),
            ("Head", "Root", (0, 0.52, 0), head_shapes(0.11), "body"),
            ("Mitre", "Head", (0, 0.1, 0), {
                "body": [lathe([(0.1, 0.0), (0.105, 0.04), (0.08, 0.12), (0.04, 0.19), (0, 0.22)], segs=14, squash=(1.0, 0.55)),
                         box((0.035, 0.1, 0.012), (0.04, -0.01, -0.1), rot=(-15, 0, 10)), box((0.035, 0.1, 0.012), (-0.04, -0.01, -0.1), rot=(-15, 0, -10))],
                "trim": [slab(0.11, 0.0, 0.035, segs=14, squash=(1.0, 0.6)), box((0.035, 0.17, 0.012), (0, 0.1, 0.06), rot=(-18, 0, 0))],
            }, None),
            ("ArmR", "Root", (0.23, 0.44, 0), [ball(0.05, 0.055, 0.05)], "body"),
            ("Weapon", "ArmR", (0, 0, 0), {
                "iron": [limb((0, -0.1, 0), (0, 0.3, 0), 0.022, 0.018, 8), ball(0.065, 0.065, 0.065, (0, 0.35, 0), segs=12, rings=6)]
                        + [box((0.016, 0.075, 0.06), (0, 0.35, 0), rot=(0, 0, a)) for a in (0, 60, 120)],
                "trim": [slab(0.035, 0.28, 0.305, segs=10), slab(0.03, -0.12, -0.09, segs=10)],
            }, None),
            ("ArmL", "Root", (-0.23, 0.44, 0), [ball(0.05, 0.055, 0.05)], "body"),
            # a heater shield with a cross
            ("Shield", "ArmL", (0, 0, 0.08), {
                "body": [box((0.2, 0.28, 0.05), (0, -0.01, 0), taper=(1.0, 0.45))],
                "trim": [box((0.03, 0.2, 0.012), (0, 0.0, 0.03)), box((0.12, 0.03, 0.012), (0, 0.05, 0.03))],
            }, None),
        ],
    },
    "rook": {
        "height": 0.75,
        "head": (0.0, 0.0),
        "shoulder": (0.3, 0.46, 0.07),
        "parts": [
            ("Base", "Root", (0, 0, 0), [plinth(0.3)], "body"),
            # a stone warden: an eight-sided tower with a door and arrow slits
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.085), (0.31, 0.09), (0.29, 0.18), (0.27, 0.49), (0, 0.49)], segs=8, flat=True, spin=math.pi / 8)],
                "iron": [box((0.15, 0.2, 0.03), (0, 0.2, 0.275), bevel=0.0), box((0.035, 0.12, 0.03), (0.2, 0.34, 0.205), rot=(0, 40, 0), bevel=0.0),
                         box((0.035, 0.12, 0.03), (-0.2, 0.34, 0.205), rot=(0, -40, 0), bevel=0.0)],
                "trim": [box((0.17, 0.035, 0.02), (0, 0.31, 0.29), bevel=0.0), box((0.04, 0.035, 0.03), (0.06, 0.2, 0.295), bevel=0.0),
                         box((0.04, 0.035, 0.03), (-0.06, 0.2, 0.295), bevel=0.0)],
            }, None),
            ("Head", "Root", (0, 0.48, 0), {
                "body": [lathe([(0, 0.0), (0.27, 0.0), (0.34, 0.06), (0.34, 0.17), (0, 0.17)], segs=8, flat=True, spin=math.pi / 8)]
                        + [box((0.13, 0.1, 0.13), (0.275 * math.sin(a), 0.22, 0.275 * math.cos(a)), rot=(0, math.degrees(a), 0), bevel=0.012, flat=True)
                           for a in [k * math.pi / 4 for k in range(8)]],
                "trim": [slab(0.345, 0.075, 0.1, segs=8)],
            }, None),
            ("ArmR", "Root", (0.33, 0.42, 0), [limb((0, 0, 0), (0, -0.3, 0), 0.075, 0.06, 8)], "body"),
            ("Weapon", "ArmR", (0, -0.3, 0), {
                "iron": [ball(0.115, 0.105, 0.115, (0, -0.085, 0), segs=12, rings=6)],
                "trim": [ball(0.02, 0.02, 0.02, (0.0, -0.085, 0.11), segs=6, rings=4), ball(0.02, 0.02, 0.02, (0.07, -0.05, 0.09), segs=6, rings=4),
                         ball(0.02, 0.02, 0.02, (-0.07, -0.05, 0.09), segs=6, rings=4)],
            }, None),
            ("ArmL", "Root", (-0.33, 0.42, 0), [limb((0, 0, 0), (0, -0.3, 0), 0.075, 0.06, 8)], "body"),
            ("Shield", "ArmL", (0, -0.3, 0), {
                "iron": [ball(0.115, 0.105, 0.115, (0, -0.085, 0), segs=12, rings=6)],
                "trim": [ball(0.02, 0.02, 0.02, (0.0, -0.085, 0.11), segs=6, rings=4), ball(0.02, 0.02, 0.02, (0.07, -0.05, 0.09), segs=6, rings=4),
                         ball(0.02, 0.02, 0.02, (-0.07, -0.05, 0.09), segs=6, rings=4)],
            }, None),
        ],
    },
    "knight": {
        "height": 0.9,
        "head": (0.085, 0.0),
        "shoulder": (0.14, 0.7, 0.045),
        "parts": [
            ("Base", "Root", (0, 0, 0), [box((0.62, 0.085, 0.82), (0, 0.0425, 0), bevel=0.03)], "body"),
            # a stout pony: a barrel body, a chest, a rump, four legs with hooves, a tail and a trimmed saddle cloth
            ("Horse", "Root", (0, 0, 0), {
                "body": [ball(0.19, 0.16, 0.32, (0, 0.4, 0.0), segs=16, rings=9), ball(0.17, 0.17, 0.17, (0, 0.42, 0.2)), ball(0.18, 0.17, 0.17, (0, 0.41, -0.2)),
                         limb((0.1, 0.34, 0.22), (0.105, 0.14, 0.25), 0.06, 0.036), limb((-0.1, 0.34, 0.22), (-0.105, 0.14, 0.25), 0.06, 0.036),
                         limb((0.11, 0.38, -0.2), (0.125, 0.22, -0.3), 0.07, 0.042), limb((0.125, 0.22, -0.3), (0.105, 0.14, -0.26), 0.042, 0.036),
                         limb((-0.11, 0.38, -0.2), (-0.125, 0.22, -0.3), 0.07, 0.042), limb((-0.125, 0.22, -0.3), (-0.105, 0.14, -0.26), 0.042, 0.036),
                         limb((0, 0.5, -0.34), (0, 0.3, -0.5), 0.04, 0.012)],
                "iron": [limb((0.105, 0.14, 0.25), (0.105, 0.085, 0.255), 0.045, 0.05), limb((-0.105, 0.14, 0.25), (-0.105, 0.085, 0.255), 0.045, 0.05),
                         limb((0.105, 0.14, -0.26), (0.105, 0.085, -0.255), 0.045, 0.05), limb((-0.105, 0.14, -0.26), (-0.105, 0.085, -0.255), 0.045, 0.05)],
                "trim": [box((0.4, 0.04, 0.38), (0, 0.555, -0.02), rot=(0, 0, 0), bevel=0.012), box((0.02, 0.15, 0.36), (0.205, 0.46, -0.02), bevel=0.006),
                         box((0.02, 0.15, 0.36), (-0.205, 0.46, -0.02), bevel=0.006)],
            }, None),
            ("HorseHead", "Root", (0, 0.46, 0.26), {
                "body": [limb((0, 0.0, -0.1), (0, 0.27, 0.08), 0.1, 0.07), ball(0.075, 0.085, 0.1, (0, 0.31, 0.1)), limb((0, 0.32, 0.12), (0, 0.25, 0.3), 0.06, 0.04),
                         ball(0.042, 0.04, 0.045, (0, 0.235, 0.31)),
                         limb((0.045, 0.36, 0.05), (0.055, 0.42, 0.04), 0.022, 0.004, 6), limb((-0.045, 0.36, 0.05), (-0.055, 0.42, 0.04), 0.022, 0.004, 6)]
                        + [box((0.035, 0.07, 0.05), (0, 0.04 + 0.055 * i, -0.095 + 0.03 * i), rot=(25, 0, 0), bevel=0.008) for i in range(5)],
                "iron": [ball(0.012, 0.012, 0.012, (0.062, 0.33, 0.13), segs=6, rings=4), ball(0.012, 0.012, 0.012, (-0.062, 0.33, 0.13), segs=6, rings=4),
                         ball(0.01, 0.01, 0.01, (0.018, 0.24, 0.345), segs=6, rings=4), ball(0.01, 0.01, 0.01, (-0.018, 0.24, 0.345), segs=6, rings=4)],
                "trim": [slab(0.045, 0.2, 0.225, (0, 0, 0.12), segs=10), box((0.035, 0.03, 0.2), (0, 0.345, 0.21), rot=(-24, 0, 0), bevel=0.006)],
            }, None),
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.5), (0.12, 0.5), (0.135, 0.6), (0.115, 0.7), (0.085, 0.75), (0, 0.75)], offset=(0, 0, -0.06), segs=14),
                         limb((0.1, 0.56, -0.04), (0.19, 0.42, 0.07), 0.05, 0.035), limb((-0.1, 0.56, -0.04), (-0.19, 0.42, 0.07), 0.05, 0.035),
                         box((0.2, 0.2, 0.03), (0, 0.6, -0.2), rot=(-14, 0, 0), taper=(1.0, 1.3))],
                "trim": [slab(0.14, 0.58, 0.605, (0, 0, -0.06), segs=14)],
            }, None),
            ("Head", "Root", (0, 0.78, -0.06), {
                "body": head_shapes(0.085, chin=False) + helmet(0.085, 0.02, brim=False),
                "trim": [box((0.015, 0.06, 0.012), (0, 0.0, 0.092))],
            }, None),
            ("ArmR", "Root", (0.15, 0.68, -0.06), [ball(0.042, 0.048, 0.042)], "body"),
            ("Weapon", "ArmR", (0, 0, 0), {
                "iron": [limb((0, -0.2, 0), (0, 0.5, 0), 0.022, 0.014, 8), lathe([(0.0, 0.5), (0.03, 0.5), (0.04, 0.53), (0, 0.62)], segs=8, squash=(1.0, 0.5))],
                "trim": [slab(0.04, 0.0, 0.03, segs=10), slab(0.03, 0.47, 0.5, segs=10)],
                "body": [box((0.14, 0.07, 0.008), (0.08, 0.43, 0), taper=(0.3, 1.0))],   # a pennant
            }, None),
            ("ArmL", "Root", (-0.15, 0.68, -0.06), [ball(0.042, 0.048, 0.042)], "body"),
            ("Shield", "ArmL", (0, 0, 0.06), {
                "body": [box((0.05, 0.32, 0.22), (0, -0.05, 0), taper=(1.0, 0.4), bevel=0.012)],
                "trim": [box((0.012, 0.22, 0.03), (-0.03, -0.03, 0), bevel=0.0), ball(0.03, 0.03, 0.03, (-0.032, 0.03, 0), segs=8, rings=4)],
            }, None),
        ],
    },
    "queen": {
        "height": 1.0,
        "head": (0.11, 0.0),
        "shoulder": (0.2, 0.62, 0.06),
        "parts": [
            ("Base", "Root", (0, 0, 0), [plinth(0.3)], "body"),
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.085), (0.27, 0.09), (0.28, 0.14), (0.24, 0.3), (0.19, 0.45), (0.14, 0.58), (0.13, 0.66), (0.15, 0.7), (0.1, 0.735), (0, 0.735)], segs=22),
                         ball(0.17, 0.06, 0.14, (0, 0.67, 0)),
                         box((0.26, 0.45, 0.025), (0, 0.4, -0.2), rot=(-14, 0, 0), taper=(1.0, 1.5))],   # a cloak behind
                "trim": [slab(0.2, 0.41, 0.435, segs=22), slab(0.165, 0.665, 0.69, segs=22), ball(0.03, 0.03, 0.03, (0, 0.43, 0.2), segs=8, rings=4)],
            }, None),
            ("Head", "Root", (0, 0.78, 0), head_shapes(0.11) + [box((0.22, 0.26, 0.05), (0, -0.06, -0.105), bevel=0.012), ball(0.12, 0.1, 0.11, (0, 0.03, -0.02))], "body"),
            ("Crown", "Head", (0, 0.08, 0), {
                "trim": [lathe([(0.115, 0.0), (0.12, 0.045), (0.11, 0.055)], segs=16, closed=False)]
                        + [lathe([(0.03, 0.0), (0.0, 0.1)], offset=(0.095 * math.sin(a), 0.045, 0.095 * math.cos(a)), segs=6)
                           for a in [k * 2 * math.pi / 5 for k in range(5)]]
                        + [ball(0.016, 0.016, 0.016, (0.095 * math.sin(a), 0.145, 0.095 * math.cos(a)), segs=6, rings=4) for a in [k * 2 * math.pi / 5 for k in range(5)]],
            }, None),
            ("ArmR", "Root", (0.21, 0.6, 0), [ball(0.047, 0.052, 0.047)], "body"),
            # a sword: a tapering blade, a crossguard, a grip and a pommel
            ("Weapon", "ArmR", (0, 0, 0), {
                "iron": [box((0.05, 0.62, 0.016), (0, 0.33, 0), taper=(0.25, 1.0), bevel=0.004)],
                "trim": [box((0.2, 0.03, 0.045), (0, 0.015, 0), bevel=0.008), ball(0.028, 0.028, 0.028, (0, -0.11, 0), segs=10, rings=5),
                         limb((0, -0.09, 0), (0, 0.0, 0), 0.016, 0.016, 8)],
            }, None),
            ("ArmL", "Root", (-0.21, 0.6, 0), [ball(0.047, 0.052, 0.047)], "body"),
            ("Shield", "ArmL", (0, 0, 0.07), {
                "body": [lathe([(0, -0.02), (0.125, -0.02), (0.125, 0.018), (0.05, 0.026), (0, 0.03)], segs=20, rot=(90, 0, 0))],
                "trim": [lathe([(0.118, -0.026), (0.135, -0.026), (0.135, 0.026), (0.118, 0.026)], segs=20, rot=(90, 0, 0), closed=True),
                         lathe([(0.0, 0.028), (0.045, 0.028), (0.035, 0.05), (0.0, 0.06)], segs=12, rot=(90, 0, 0))],
            }, None),
        ],
    },
    "king": {
        "height": 1.1,
        "head": (0.12, 0.0),
        "shoulder": (0.24, 0.64, 0.07),
        "parts": [
            ("Base", "Root", (0, 0, 0), [plinth(0.32)], "body"),
            ("Body", "Root", (0, 0, 0), {
                "body": [lathe([(0, 0.085), (0.29, 0.09), (0.3, 0.14), (0.26, 0.3), (0.21, 0.46), (0.165, 0.6), (0.16, 0.7), (0.18, 0.74), (0.12, 0.78), (0, 0.78)], segs=24),
                         ball(0.2, 0.07, 0.16, (0, 0.71, 0)),
                         box((0.36, 0.66, 0.05), (0, 0.42, -0.2), rot=(-12, 0, 0), taper=(1.0, 1.5), bevel=0.014)],   # a heavy cape
                "trim": [slab(0.225, 0.43, 0.46, segs=24), slab(0.19, 0.7, 0.73, segs=24), ball(0.035, 0.035, 0.035, (0, 0.445, 0.225), segs=8, rings=4),
                         box((0.09, 0.55, 0.02), (0, 0.4, 0.2), rot=(10, 0, 0), bevel=0.004)],
            }, None),
            # a heavy beard under the face
            ("Head", "Root", (0, 0.8, 0), head_shapes(0.12) + [box((0.17, 0.15, 0.07), (0, -0.1, 0.085), rot=(10, 0, 0), taper=(1.0, 0.45)), ball(0.13, 0.1, 0.12, (0, 0.03, -0.02))], "body"),
            ("Crown", "Head", (0, 0.08, 0), {
                "trim": [lathe([(0.13, 0.0), (0.14, 0.05), (0.13, 0.085)], segs=16)]
                        + [lathe([(0.04, 0.0), (0.0, 0.08)], offset=(0.11 * math.sin(a), 0.085, 0.11 * math.cos(a)), segs=6) for a in [k * math.pi / 2 for k in range(4)]]
                        + [box((0.035, 0.15, 0.035), (0, 0.15, 0)), box((0.12, 0.035, 0.035), (0, 0.19, 0))],
            }, None),
            ("ArmR", "Root", (0.25, 0.62, 0), [ball(0.052, 0.058, 0.052)], "body"),
            ("Weapon", "ArmR", (0, 0, 0), {
                "iron": [box((0.075, 0.72, 0.026), (0, 0.38, 0), taper=(0.3, 1.0), bevel=0.006)],
                "trim": [box((0.26, 0.04, 0.055), (0, 0.02, 0), bevel=0.01), ball(0.034, 0.034, 0.034, (0, -0.13, 0), segs=10, rings=5),
                         limb((0, -0.11, 0), (0, 0.0, 0), 0.019, 0.019, 8)],
            }, None),
            ("ArmL", "Root", (-0.25, 0.62, 0), [ball(0.052, 0.058, 0.052)], "body"),
            ("Shield", "ArmL", (0, 0, 0.08), {
                "body": [box((0.28, 0.38, 0.05), (0, -0.02, 0), taper=(1.0, 0.4), bevel=0.014)],
                "trim": [box((0.04, 0.26, 0.012), (0, 0.0, 0.03), bevel=0.0), box((0.17, 0.04, 0.012), (0, 0.06, 0.03), bevel=0.0),
                         box((0.29, 0.02, 0.055), (0, 0.17, 0), bevel=0.004)],
            }, None),
        ],
    },
}


def face_shapes(radius, dz):
    """Two eyes and a heavy brow on the front of a head, in the Head's own axes. Grave, not smiling."""
    if radius <= 0:
        # the rook has no face: two arrow slits in its front
        return [box((0.04, 0.1, 0.03), (0.13, 0.085, 0.272), bevel=0.0), box((0.04, 0.1, 0.03), (-0.13, 0.085, 0.272), bevel=0.0)]
    r = radius
    z = r * 0.88 + dz
    return [
        box((0.34 * r, 0.2 * r, 0.3 * r), (0.4 * r, 0.1 * r, z), bevel=0.0),
        box((0.34 * r, 0.2 * r, 0.3 * r), (-0.4 * r, 0.1 * r, z), bevel=0.0),
        box((1.2 * r, 0.14 * r, 0.3 * r), (0.0, 0.34 * r, z - 0.02 * r), bevel=0.0),
    ]


def shoulder_shapes(x, y, size, pointed):
    """A pair of pauldrons at the arm joints: round on the bone side, a pointed horn on the cinnabar side."""
    shapes = []
    for sx in (1, -1):
        if pointed:
            shapes.append(lathe([(0, 0), (size * 0.95, 0), (size * 0.6, size * 1.6), (0, size * 3.4)], offset=(sx * (x + size * 0.4), y + size * 0.9, 0), segs=8))
        else:
            shapes.append(ball(size * 1.1, size * 0.85, size * 1.1, (sx * (x + size * 0.3), y + size * 0.6, 0), segs=10, rings=6))
    return shapes


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)


def build(name, spec):
    clear_scene()
    root = bpy.data.objects.new("Root", None)
    bpy.context.scene.collection.objects.link(root)
    parts = {"Root": root}
    head_radius, face_dz = spec["head"]
    sx, sy, size = spec["shoulder"]
    extra = [
        ("Face", "Head", (0, 0, 0), face_shapes(head_radius, face_dz), "iron"),
        ("ShoulderBone", "Body", (0, 0, 0), shoulder_shapes(sx, sy, size, False), "body"),
        ("ShoulderRed", "Body", (0, 0, 0), shoulder_shapes(sx, sy, size, True), "body"),
    ]
    for pname, parent, origin, content, material in spec["parts"] + extra:
        parts[pname] = new_part(pname, parts[parent], origin, content, material)
    bpy.context.view_layer.update()
    return parts


AO_SAMPLES = 40
AO_DISTANCE = 0.28   # metres: nothing further away than this shades a vertex
AO_FLOOR = 0.12      # the darkest a vertex can get
AO_CONTRAST = 1.15   # bends the result: above 1 darkens the crevices a little more


def _tree(parts, skip):
    """One BVH of every part in its rest pose, leaving out the parts named in skip."""
    verts, polys = [], []
    for pname, obj in parts.items():
        if obj.type != "MESH" or pname in skip:
            continue
        base = len(verts)
        verts.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        polys.extend(tuple(base + i for i in poly.vertices) for poly in obj.data.polygons)
    return BVHTree.FromPolygons(verts, polys, epsilon=0.0)


def bake_occlusion(parts):
    """Write the "AO" vertex colour on every mesh: the share of the hemisphere above each vertex that is open."""
    rng = random.Random(7)
    trees = {"bone": _tree(parts, {"ShoulderRed"}), "red": _tree(parts, {"ShoulderBone"})}
    for pname, obj in parts.items():
        if obj.type != "MESH":
            continue
        tree = trees["red" if pname == "ShoulderRed" else "bone"]
        mesh = obj.data
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        attr = mesh.color_attributes.new("AO", "FLOAT_COLOR", "POINT")
        for v in mesh.vertices:
            p = obj.matrix_world @ v.co
            n = (normal_matrix @ v.normal).normalized()
            t = n.orthogonal().normalized()
            b = n.cross(t)
            spin = rng.random() * math.tau
            origin = p + n * 0.006
            shade = 0.0
            for i in range(AO_SAMPLES):
                u = (i + 0.5) / AO_SAMPLES
                r = math.sqrt(u)
                a = i * 2.399963 + spin
                d = (t * (r * math.cos(a)) + b * (r * math.sin(a)) + n * math.sqrt(1.0 - u)).normalized()
                hit = tree.ray_cast(origin, d, AO_DISTANCE)
                if hit[0] is not None:
                    shade += 1.0 - hit[3] / AO_DISTANCE
            ao = 1.0 - shade / AO_SAMPLES
            ao = max(AO_FLOOR, min(1.0, ao)) ** AO_CONTRAST
            attr.data[v.index].color = (ao, ao, ao, 1.0)
        mesh.color_attributes.active_color = attr
        mesh.color_attributes.render_color_index = mesh.color_attributes.find("AO")


def world_top_and_size(parts):
    """Return (highest y of the body, base width x, base depth z), in Unity axes."""
    top = 0.0
    for pname, obj in parts.items():
        if obj.type != "MESH":
            continue
        # arms and what they hold are not part of the piece's height
        ancestor, skip = obj, False
        while ancestor is not None:
            if ancestor.name in ("ArmR", "ArmL"):
                skip = True
            ancestor = ancestor.parent
        if skip:
            continue
        for v in obj.data.vertices:
            top = max(top, (obj.matrix_world @ v.co).z)  # Blender z is Unity y
    base = parts["Base"]
    xs = [(base.matrix_world @ v.co).x for v in base.data.vertices]
    ys = [(base.matrix_world @ v.co).y for v in base.data.vertices]
    return top, max(xs) - min(xs), max(ys) - min(ys)


def check(name, spec, parts):
    problems = []
    for required in REQUIRED:
        if required not in parts:
            problems.append(f"missing part {required}")
    if problems:
        return "", problems
    for pname, obj in parts.items():
        if tuple(round(s, 4) for s in obj.scale) != (1.0, 1.0, 1.0):
            problems.append(f"{pname} has scale {tuple(obj.scale)}")
    # The right arm is on Unity +x, which is Blender -x.
    if not parts["ArmR"].location.x < 0 < parts["ArmL"].location.x:
        problems.append("ArmR must be on the piece's right (Unity +x) and ArmL on its left")
    top, width, depth = world_top_and_size(parts)
    if abs(top - spec["height"]) > HEIGHT_TOLERANCE:
        problems.append(f"height {top:.3f}, expected {spec['height']} +- {HEIGHT_TOLERANCE}")
    if width > MAX_FOOTPRINT or depth > MAX_FOOTPRINT:
        problems.append(f"base {width:.2f} x {depth:.2f} is over {MAX_FOOTPRINT}")
    tris = 0
    for obj in parts.values():
        if obj.type == "MESH":
            obj.data.calc_loop_triangles()
            tris += len(obj.data.loop_triangles)
            for mat in obj.data.materials:
                if mat.name not in ("body", "iron", "trim"):
                    problems.append(f"{obj.name} has an unknown material {mat.name}")
    if tris > MAX_TRIANGLES:
        problems.append(f"{tris} triangles, budget is {MAX_TRIANGLES}")
    return f"{name}: height {top:.2f}, base {width:.2f} x {depth:.2f}, {tris} triangles, {len(parts) - 1} parts", problems


def export(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts.values():
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts["Root"]
    path = EXPORT_DIR / f"{name}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
        export_vertex_color="ACTIVE",
    )
    return path


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    MATERIALS["body"] = make_material("body", (0.9, 0.86, 0.75, 1.0), 0.0, 0.7)
    MATERIALS["iron"] = make_material("iron", (0.35, 0.37, 0.39, 1.0), 0.6, 0.4)
    MATERIALS["trim"] = make_material("trim", (0.78, 0.6, 0.25, 1.0), 0.7, 0.35)

    failed = False
    for name, spec in PIECES.items():
        parts = build(name, spec)
        line, problems = check(name, spec, parts)
        print(line)
        for p in problems:
            print(f"  PROBLEM: {p}")
            failed = True
        if not problems:
            bake_occlusion(parts)
            export(name, parts)
    if failed:
        print("Piece check failed. Fix the problems above (nothing was exported for a piece with a problem).")
        sys.exit(1)
    print("All pieces exported.")


main()
