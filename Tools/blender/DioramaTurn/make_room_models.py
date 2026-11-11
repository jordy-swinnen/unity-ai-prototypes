"""DioramaTurn room models: the wooden plinth tray, the rounded block, the pillar and the goal pedestal.

Run headless:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/DioramaTurn/make_room_models.py

Writes Art/Exports/{plinth,block,pillar,goal_pedestal}.glb and Art/source/DioramaTurn/room_models.blend.
Shapes are built from nothing here. Ambient occlusion is baked into a FLOAT_COLOR point attribute named
"AO", one value per vertex (1 open, lower in the crevices), and exported as the active vertex colour so
glTFast imports it into Mesh.colors. The game's shader multiplies it into the light.

Unity axes: x right, y up, z forward. Blender is Z-up and glTF mirrors x, so a Unity point (x, y, z) is
written to Blender as (-x, -z, y).
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
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "DioramaTurn" / "Art" / "Exports"
SOURCE_DIR = REPO_ROOT / "Art" / "source" / "DioramaTurn"
MAX_TRIANGLES = 5000
SHARP_DEGREES = 52.0


def to_blender(v):
    x, y, z = v
    return Vector((-x, -z, y))


def box(size, offset=(0, 0, 0), bevel=None, rot=(0, 0, 0), flat=False):
    sx, sy, sz = size
    if bevel is None:
        bevel = min(0.02, min(size) * 0.22)
    verts = []
    for y in (-sy / 2, sy / 2):
        for x in (-sx / 2, sx / 2):
            for z in (-sz / 2, sz / 2):
                verts.append((x, y, z))
    faces = [(0, 1, 3, 2), (4, 5, 7, 6), (0, 1, 5, 4), (2, 3, 7, 6), (0, 2, 6, 4), (1, 3, 7, 5)]
    m = Euler([math.radians(a) for a in rot], "XYZ").to_matrix()
    return {"verts": [m @ Vector(v) + Vector(offset) for v in verts], "faces": faces, "bevel": bevel, "flat": flat}


def lathe(profile, offset=(0, 0, 0), segs=24, closed=False, flat=False):
    """Turns (radius, height) points round the y axis. A radius of 0 is a pole."""
    verts, rings = [], []
    for r, y in profile:
        if r < 1e-6:
            verts.append((0.0, y, 0.0))
            rings.append([len(verts) - 1])
        else:
            ring = []
            for k in range(segs):
                a = 2 * math.pi * k / segs
                verts.append((r * math.cos(a), y, r * math.sin(a)))
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
    return {"verts": [Vector(v) + Vector(offset) for v in verts], "faces": faces, "bevel": 0.0, "flat": flat}


def add_shape(bm, shape):
    start = len(bm.faces)
    verts = [bm.verts.new(to_blender(v)) for v in shape["verts"]]
    new_faces = []
    for f in shape["faces"]:
        try:
            new_faces.append(bm.faces.new([verts[i] for i in f]))
        except ValueError:
            pass
    bmesh.ops.recalc_face_normals(bm, faces=new_faces)
    if shape["bevel"] > 0.0:
        edges = {e for f in new_faces for e in f.edges}
        segments = 2 if shape["bevel"] >= 0.01 else 1
        bmesh.ops.bevel(bm, geom=list(edges), offset=shape["bevel"], segments=segments, affect="EDGES")
    bm.faces.ensure_lookup_table()
    mine = [f for f in bm.faces if f.is_valid][start:]
    limit = math.radians(SHARP_DEGREES)
    for f in mine:
        f.smooth = not shape["flat"]
    for e in {e for f in mine for e in f.edges}:
        if shape["flat"]:
            e.smooth = False
        elif len(e.link_faces) == 2 and e.calc_face_angle(0.0) > limit:
            e.smooth = False


def make_material(name="surface"):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.85, 0.8, 0.72, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.6
    return mat


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)


def build(name, shapes):
    clear_scene()
    root = bpy.data.objects.new("Root", None)
    bpy.context.scene.collection.objects.link(root)
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    obj.parent = root
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(make_material())
    bm = bmesh.new()
    for shape in shapes:
        add_shape(bm, shape)
    bm.to_mesh(mesh)
    bm.free()
    bpy.context.view_layer.update()
    return {"Root": root, name: obj}


AO_SAMPLES = 48
AO_DISTANCE = 0.35
AO_FLOOR = 0.25
AO_CONTRAST = 1.0


def bake_occlusion(parts):
    rng = random.Random(11)
    verts, polys = [], []
    for obj in parts.values():
        if obj.type != "MESH":
            continue
        base = len(verts)
        verts.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        polys.extend(tuple(base + i for i in poly.vertices) for poly in obj.data.polygons)
    tree = BVHTree.FromPolygons(verts, polys, epsilon=0.0)
    for obj in parts.values():
        if obj.type != "MESH":
            continue
        mesh = obj.data
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        attr = mesh.color_attributes.new("AO", "FLOAT_COLOR", "POINT")
        for v in mesh.vertices:
            p = obj.matrix_world @ v.co
            n = (normal_matrix @ v.normal).normalized()
            t = n.orthogonal().normalized()
            b = n.cross(t)
            spin = rng.random() * math.tau
            origin = p + n * 0.004
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


MODELS = {
    # origin at the bottom centre; 7 x 0.6 x 7, with a rim round the top
    "plinth": [
        box((7.0, 0.6, 7.0), (0, 0.3, 0), bevel=0.14),
        box((6.6, 0.26, 0.25), (0, 0.6, 3.375), bevel=0.06),
        box((6.6, 0.26, 0.25), (0, 0.6, -3.375), bevel=0.06),
        box((0.25, 0.26, 6.25), (3.375, 0.6, 0), bevel=0.06),
        box((0.25, 0.26, 6.25), (-3.375, 0.6, 0), bevel=0.06),
    ],
    # 1 x 1 x 1, centred on its cell
    "block": [box((1.0, 1.0, 1.0), (0, 0, 0), bevel=0.12)],
    # 0.56 x 1 x 0.56 support column with top and bottom caps, placed under elevated blocks
    "pillar": [
        box((0.40, 0.84, 0.40), (0, 0.0, 0), bevel=0.06),
        box((0.56, 0.08, 0.56), (0, 0.46, 0), bevel=0.03),
        box((0.56, 0.08, 0.56), (0, -0.46, 0), bevel=0.03),
    ],
    # a round pedestal top on a 1 x 1 x 1 block
    "goal_pedestal": [
        box((1.0, 0.9, 1.0), (0, -0.05, 0), bevel=0.1),
        lathe([(0.0, 0.4), (0.42, 0.4), (0.42, 0.46), (0.36, 0.5), (0.0, 0.5)], segs=28),
    ],
    # a 3D wooden plank bridge (1.0 along x, 0.66 along z, walkable top at y = +0.5)
    "bridge": [
        box((0.30, 0.12, 0.66), (-0.33, 0.44, 0), bevel=0.025),
        box((0.30, 0.12, 0.66), (0.0, 0.44, 0), bevel=0.025),
        box((0.30, 0.12, 0.66), (0.33, 0.44, 0), bevel=0.025),
        box((0.98, 0.08, 0.08), (0, 0.36, 0.24), bevel=0.02),
        box((0.98, 0.08, 0.08), (0, 0.36, -0.24), bevel=0.02),
        box((0.96, 0.07, 0.07), (0, 0.535, 0.28), bevel=0.02),
        box((0.96, 0.07, 0.07), (0, 0.535, -0.28), bevel=0.02),
    ],
}


def check(name, parts):
    problems = []
    obj = parts[name]
    obj.data.calc_loop_triangles()
    tris = len(obj.data.loop_triangles)
    if tris > MAX_TRIANGLES:
        problems.append(f"{tris} triangles, budget is {MAX_TRIANGLES}")
    if not any(c.name == "AO" for c in obj.data.color_attributes):
        problems.append("no AO vertex colour")
    if tuple(round(s, 4) for s in obj.scale) != (1.0, 1.0, 1.0):
        problems.append(f"scale {tuple(obj.scale)}")
    xs = [(obj.matrix_world @ v.co).x for v in obj.data.vertices]
    ys = [(obj.matrix_world @ v.co).y for v in obj.data.vertices]
    zs = [(obj.matrix_world @ v.co).z for v in obj.data.vertices]
    size = (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))  # Blender: x, y, z(=Unity y)
    print(f"{name}: {tris} triangles, size x={size[0]:.2f} depth={size[1]:.2f} height={size[2]:.2f}")
    return problems


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
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    failed = False
    for name, shapes in MODELS.items():
        parts = build(name, shapes)
        bake_occlusion(parts)
        problems = check(name, parts)
        for p in problems:
            print(f"  PROBLEM: {p}")
            failed = True
        if not problems:
            export(name, parts)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_DIR / "room_models.blend"))
    print("wrote " + str(SOURCE_DIR / "room_models.blend"))
    if failed:
        print("Model check failed; fix the problems above.")
        sys.exit(1)
    print("All room models exported.")


main()
