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
Coordinates are written in Unity axes: x to the piece's right, y up, z forward (the piece faces +z).
Blender is Z-up and the glTF import mirrors x, so a Unity point (x, y, z) is written to Blender as
(-x, -z, y). Weapons and arms rest pointing straight up or down. Materials are neutral and named:
"body" (the side colour is set in Unity) and "iron".

The look pass: boxes are bevelled, every piece has a stepped base and a Face (brow and eyes, iron) on its
Head, and a pair of ShoulderBone parts (round pauldrons) and a pair of ShoulderRed parts (pointed horns) as
children of Body. Unity shows one of the two pairs, so the sides differ by shape and not only by colour.
Part names and joints are unchanged, so animations keep working.
"""
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

REPO_ROOT = Path(__file__).resolve().parents[3]
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "MedievalChess" / "Art" / "Exports"
MAX_TRIANGLES = 2500
HEIGHT_TOLERANCE = 0.06
MAX_FOOTPRINT = 0.9
REQUIRED = ["Base", "Body", "Head", "ArmR", "Weapon", "ArmL", "Shield"]


def to_blender(v):
    x, y, z = v
    return Vector((-x, -z, y))


# A shape is (kind, size, offset). Sizes and offsets are in Unity axes and relative to the part origin.
# box: size = (x, y, z). cyl: size = (radius, height). cone: size = (radius at bottom, radius at top, height).
# sphere: size = (radius,).
def box(size, offset=(0, 0, 0), bevel=None):
    """A box with softened edges. The bevel defaults to a fraction of the thinnest side."""
    if bevel is None:
        bevel = min(0.02, min(size) * 0.22)
    return ("box", size, offset, bevel)


def disc_z(radius, thickness, offset=(0, 0, 0)):
    """A round disc whose flat face looks along the piece's z axis (a round shield)."""
    return ("disc_z", (radius, thickness), offset)


def cyl(radius, height, offset=(0, 0, 0)):
    return ("cone", (radius, radius, height), offset)


def cone(r_bottom, r_top, height, offset=(0, 0, 0)):
    return ("cone", (r_bottom, r_top, height), offset)


def sphere(radius, offset=(0, 0, 0)):
    return ("sphere", (radius,), offset)


def add_shape(bm, kind, size, offset, bevel=0.0):
    off = Vector(offset)
    if kind == "box":
        res = bmesh.ops.create_cube(bm, size=1.0)
        for v in res["verts"]:
            # unit cube corners are +-0.5 in Blender axes; place them in Unity axes first
            ux, uy, uz = -v.co.x * size[0], v.co.z * size[1], -v.co.y * size[2]
            v.co = to_blender((ux, uy, uz)) + to_blender(off)
        if bevel > 0.0:
            edges = {e for v in res["verts"] for e in v.link_edges}
            bmesh.ops.bevel(bm, geom=list(edges), offset=bevel, segments=1, affect="EDGES")
    elif kind == "disc_z":
        radius, thickness = size
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=radius, radius2=radius, depth=thickness)
        # the cone's axis is Blender z (Unity y). Turn it to Unity z, which is Blender -y.
        bmesh.ops.rotate(bm, verts=res["verts"], cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
        for v in res["verts"]:
            v.co = v.co + to_blender(off)
    elif kind == "cone":
        r1, r2, h = size
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=r1, radius2=r2, depth=h)
        for v in res["verts"]:
            v.co = v.co + to_blender(off)
    elif kind == "sphere":
        res = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=size[0])
        for v in res["verts"]:
            v.co = v.co + to_blender(off)
    else:
        raise ValueError(kind)


def make_material(name, rgba):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = rgba
    return mat


MATERIALS = {}


def new_part(name, parent, origin, shapes, material):
    """origin is the joint position relative to the parent's origin (Unity axes)."""
    obj = bpy.data.objects.new(name, bpy.data.meshes.new(name) if shapes else None)
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = to_blender(origin)
    if shapes:
        bm = bmesh.new()
        for shape in shapes:
            add_shape(bm, *shape)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.materials.append(MATERIALS[material])
    return obj


# Each piece: height of the body (without the weapon), and a list of
# (name, parent, origin, shapes, material).
def plinth(radius, height=0.08):
    """A stepped base: a wide slab and a narrower collar above it."""
    return [cyl(radius, height * 0.6, (0, height * 0.3, 0)), cyl(radius * 0.82, height * 0.5, (0, height * 0.6 + height * 0.25, 0))]


# "head" is (radius, z offset of the face) of the Head part. "shoulder" is (x, y, size) of the arm joints, where the
# round (bone side) or pointed (cinnabar side) pauldrons sit.
PIECES = {
    "pawn": {
        "height": 0.5,
        "head": (0.1, 0.0),
        "shoulder": (0.18, 0.3, 0.055),
        "parts": [
            ("Base", "Root", (0, 0, 0), plinth(0.26, 0.09), "body"),
            ("Body", "Root", (0, 0, 0), [cyl(0.17, 0.26, (0, 0.21, 0)), cyl(0.2, 0.05, (0, 0.1, 0))], "body"),
            # a footman's helmet: the head with a brim
            ("Head", "Root", (0, 0.4, 0), [sphere(0.1), cyl(0.125, 0.025, (0, 0.025, 0))], "body"),
            ("ArmR", "Root", (0.19, 0.28, 0), [box((0.07, 0.07, 0.07))], "body"),
            ("Weapon", "ArmR", (0, 0, 0), [box((0.04, 0.5, 0.04), (0, 0.12, 0)), cone(0.05, 0.0, 0.1, (0, 0.42, 0))], "iron"),
            ("ArmL", "Root", (-0.19, 0.28, 0), [box((0.07, 0.07, 0.07))], "body"),
            # a round shield
            ("Shield", "ArmL", (0, 0, 0.08), [disc_z(0.15, 0.05)], "body"),
        ],
    },
    "bishop": {
        "height": 0.8,
        "head": (0.11, 0.0),
        "shoulder": (0.22, 0.46, 0.06),
        "parts": [
            ("Base", "Root", (0, 0, 0), plinth(0.28), "body"),
            ("Body", "Root", (0, 0, 0), [cone(0.24, 0.14, 0.46, (0, 0.31, 0)), cyl(0.15, 0.05, (0, 0.545, 0))], "body"),
            ("Head", "Root", (0, 0.52, 0), [sphere(0.11)], "body"),
            ("Mitre", "Head", (0, 0.1, 0), [cone(0.1, 0.0, 0.2, (0, 0.1, 0)), box((0.2, 0.03, 0.03), (0, 0.04, 0.08))], "body"),
            ("ArmR", "Root", (0.23, 0.44, 0), [box((0.08, 0.08, 0.08))], "body"),
            ("Weapon", "ArmR", (0, 0, 0), [box((0.04, 0.4, 0.04), (0, 0.1, 0)), sphere(0.08, (0, 0.34, 0)),
                                           box((0.02, 0.02, 0.2), (0, 0.34, 0)), box((0.2, 0.02, 0.02), (0, 0.34, 0))], "iron"),
            ("ArmL", "Root", (-0.23, 0.44, 0), [box((0.08, 0.08, 0.08))], "body"),
            ("Shield", "ArmL", (0, 0, 0.08), [box((0.2, 0.26, 0.05), (0, 0, 0))], "body"),
        ],
    },
    "rook": {
        "height": 0.75,
        "head": (0.0, 0.0),
        "shoulder": (0.3, 0.46, 0.07),
        "parts": [
            ("Base", "Root", (0, 0, 0), plinth(0.3), "body"),
            ("Body", "Root", (0, 0, 0), [box((0.52, 0.4, 0.46), (0, 0.28, 0)), box((0.4, 0.06, 0.04), (0, 0.18, 0.24))], "body"),
            ("Head", "Root", (0, 0.48, 0), [
                box((0.6, 0.18, 0.54), (0, 0.09, 0)),
                box((0.14, 0.09, 0.14), (0.2, 0.225, 0.18)), box((0.14, 0.09, 0.14), (-0.2, 0.225, 0.18)),
                box((0.14, 0.09, 0.14), (0.2, 0.225, -0.18)), box((0.14, 0.09, 0.14), (-0.2, 0.225, -0.18)),
            ], "body"),
            ("ArmR", "Root", (0.33, 0.42, 0), [box((0.13, 0.3, 0.13), (0, -0.15, 0))], "body"),
            ("Weapon", "ArmR", (0, -0.3, 0), [box((0.2, 0.2, 0.2), (0, -0.08, 0))], "iron"),
            ("ArmL", "Root", (-0.33, 0.42, 0), [box((0.13, 0.3, 0.13), (0, -0.15, 0))], "body"),
            ("Shield", "ArmL", (0, -0.3, 0), [box((0.2, 0.2, 0.2), (0, -0.08, 0))], "iron"),
        ],
    },
    "knight": {
        "height": 0.9,
        "head": (0.085, 0.0),
        "shoulder": (0.14, 0.7, 0.045),
        "parts": [
            ("Base", "Root", (0, 0, 0), [box((0.6, 0.08, 0.8), (0, 0.04, 0), 0.02)], "body"),
            ("Horse", "Root", (0, 0, 0), [
                box((0.34, 0.24, 0.62), (0, 0.38, 0)),
                box((0.07, 0.22, 0.07), (0.12, 0.19, 0.24)), box((0.07, 0.22, 0.07), (-0.12, 0.19, 0.24)),
                box((0.07, 0.22, 0.07), (0.12, 0.19, -0.24)), box((0.07, 0.22, 0.07), (-0.12, 0.19, -0.24)),
                box((0.08, 0.2, 0.08), (0, 0.34, -0.34)),                     # the tail
                box((0.36, 0.05, 0.42), (0, 0.5, 0.0)),                        # a saddle cloth
            ], "body"),
            ("HorseHead", "Root", (0, 0.46, 0.26), [
                box((0.16, 0.34, 0.14), (0, 0.14, 0.03)), box((0.14, 0.14, 0.28), (0, 0.3, 0.14)),
                box((0.04, 0.12, 0.04), (0.05, 0.36, 0.0)), box((0.04, 0.12, 0.04), (-0.05, 0.36, 0.0)),   # ears
                box((0.05, 0.28, 0.06), (0, 0.16, -0.06)),                                                  # the mane
            ], "body"),
            ("Body", "Root", (0, 0, 0), [box((0.24, 0.24, 0.2), (0, 0.62, -0.06))], "body"),
            ("Head", "Root", (0, 0.78, -0.06), [sphere(0.085), cyl(0.1, 0.02, (0, 0.02, 0))], "body"),
            ("ArmR", "Root", (0.15, 0.68, -0.06), [box((0.07, 0.07, 0.07))], "body"),
            ("Weapon", "ArmR", (0, 0, 0), [box((0.04, 0.7, 0.04), (0, 0.15, 0)), cone(0.05, 0.0, 0.1, (0, 0.55, 0))], "iron"),
            ("ArmL", "Root", (-0.15, 0.68, -0.06), [box((0.07, 0.07, 0.07))], "body"),
            ("Shield", "ArmL", (0, 0, 0.06), [box((0.05, 0.3, 0.2), (0, -0.05, 0))], "body"),
        ],
    },
    "queen": {
        "height": 1.0,
        "head": (0.11, 0.0),
        "shoulder": (0.2, 0.62, 0.06),
        "parts": [
            ("Base", "Root", (0, 0, 0), plinth(0.3), "body"),
            ("Body", "Root", (0, 0, 0), [cone(0.26, 0.14, 0.62, (0, 0.39, 0)), cyl(0.16, 0.05, (0, 0.7, 0))], "body"),
            # a veil falling behind the head
            ("Head", "Root", (0, 0.78, 0), [sphere(0.11), box((0.22, 0.24, 0.05), (0, -0.07, -0.1))], "body"),
            ("Crown", "Head", (0, 0.08, 0), [cyl(0.115, 0.05, (0, 0.03, 0)),
                                             cone(0.035, 0.0, 0.09, (0.08, 0.1, 0)), cone(0.035, 0.0, 0.09, (-0.08, 0.1, 0)),
                                             cone(0.035, 0.0, 0.09, (0, 0.1, 0.08)), cone(0.035, 0.0, 0.09, (0, 0.1, -0.08))], "body"),
            ("ArmR", "Root", (0.21, 0.6, 0), [box((0.08, 0.08, 0.08))], "body"),
            ("Weapon", "ArmR", (0, 0, 0), [box((0.05, 0.7, 0.02), (0, 0.25, 0)), box((0.16, 0.03, 0.04), (0, 0.02, 0))], "iron"),
            ("ArmL", "Root", (-0.21, 0.6, 0), [box((0.08, 0.08, 0.08))], "body"),
            ("Shield", "ArmL", (0, 0, 0.07), [box((0.2, 0.2, 0.04), (0, 0, 0))], "body"),
        ],
    },
    "king": {
        "height": 1.1,
        "head": (0.12, 0.0),
        "shoulder": (0.24, 0.64, 0.07),
        "parts": [
            ("Base", "Root", (0, 0, 0), plinth(0.32), "body"),
            # a cape falls behind the body
            ("Body", "Root", (0, 0, 0), [cone(0.28, 0.18, 0.66, (0, 0.41, 0)), cyl(0.2, 0.05, (0, 0.74, 0)), box((0.34, 0.6, 0.05), (0, 0.4, -0.2))], "body"),
            # a heavy beard under the face
            ("Head", "Root", (0, 0.8, 0), [sphere(0.12), box((0.14, 0.12, 0.06), (0, -0.09, 0.08))], "body"),
            ("Crown", "Head", (0, 0.08, 0), [box((0.2, 0.08, 0.2), (0, 0.06, 0)), box((0.04, 0.14, 0.04), (0, 0.17, 0)), box((0.12, 0.04, 0.04), (0, 0.19, 0))], "body"),
            ("ArmR", "Root", (0.25, 0.62, 0), [box((0.09, 0.09, 0.09))], "body"),
            ("Weapon", "ArmR", (0, 0, 0), [box((0.07, 0.8, 0.03), (0, 0.3, 0)), box((0.22, 0.04, 0.05), (0, 0.04, 0))], "iron"),
            ("ArmL", "Root", (-0.25, 0.62, 0), [box((0.09, 0.09, 0.09))], "body"),
            ("Shield", "ArmL", (0, 0, 0.08), [box((0.26, 0.34, 0.05), (0, 0, 0))], "body"),
        ],
    },
}


def face_shapes(radius, dz):
    """Two eyes and a heavy brow on the front of a head, in the Head's own axes. Grave, not smiling."""
    if radius <= 0:
        # the rook has no face: two arrow slits in its front
        return [box((0.08, 0.02, 0.03), (0.14, 0.09, 0.28), 0.0), box((0.08, 0.02, 0.03), (-0.14, 0.09, 0.28), 0.0)]
    r = radius
    z = r * 0.88 + dz
    return [
        box((0.34 * r, 0.2 * r, 0.3 * r), (0.4 * r, 0.1 * r, z), 0.0),
        box((0.34 * r, 0.2 * r, 0.3 * r), (-0.4 * r, 0.1 * r, z), 0.0),
        box((1.2 * r, 0.14 * r, 0.3 * r), (0.0, 0.34 * r, z - 0.02 * r), 0.0),
    ]


def shoulder_shapes(x, y, size, pointed):
    """A pair of pauldrons at the arm joints: round on the bone side, a pointed horn on the cinnabar side."""
    shapes = []
    for sx in (1, -1):
        if pointed:
            shapes.append(cone(size * 0.9, 0.0, size * 3.2, (sx * (x + size * 0.4), y + size * 1.4, 0)))
        else:
            shapes.append(sphere(size, (sx * (x + size * 0.3), y + size * 0.6, 0)))
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
    for pname, parent, origin, shapes, material in spec["parts"] + extra:
        parts[pname] = new_part(pname, parts[parent], origin, shapes, material)
    bpy.context.view_layer.update()
    return parts


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
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=True, export_yup=True
    )
    return path


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    MATERIALS["body"] = make_material("body", (0.9, 0.86, 0.75, 1.0))
    MATERIALS["iron"] = make_material("iron", (0.35, 0.37, 0.39, 1.0))

    failed = False
    built = {}
    for name, spec in PIECES.items():
        parts = build(name, spec)
        line, problems = check(name, spec, parts)
        print(line)
        for p in problems:
            print(f"  PROBLEM: {p}")
            failed = True
        built[name] = (spec, parts)
        if not problems:
            export(name, parts)
    if failed:
        print("Piece check failed. Fix the problems above (nothing was exported for a piece with a problem).")
        sys.exit(1)
    print("All pieces exported.")


main()
