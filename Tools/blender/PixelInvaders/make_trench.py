"""PixelInvaders 3D command trench models: trench_deck, trench_rail, trench_markers.

Run headless:
    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/PixelInvaders/make_trench.py

Writes Art/Exports/{trench_deck,trench_rail,trench_markers}.glb and Art/source/PixelInvaders/trench.blend.
Ambient occlusion is baked into a FLOAT_COLOR point attribute named "AO" via BVHTree ray-casting and exported
with export_vertex_color="ACTIVE" so glTFast imports it into Mesh.colors.
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
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "PixelInvaders" / "Art" / "Exports"
SOURCE_DIR = REPO_ROOT / "Art" / "source" / "PixelInvaders"
MAX_TRIANGLES_TOTAL = 4000
SHARP_DEGREES = 50.0


def to_blender(v):
    x, y, z = v
    return Vector((-x, -z, y))


def box(size, offset=(0.0, 0.0, 0.0), bevel=0.02, rot=(0.0, 0.0, 0.0), flat=False):
    sx, sy, sz = size
    verts = []
    for y in (-sy / 2.0, sy / 2.0):
        for x in (-sx / 2.0, sx / 2.0):
            for z in (-sz / 2.0, sz / 2.0):
                verts.append((x, y, z))
    faces = [(0, 1, 3, 2), (4, 5, 7, 6), (0, 1, 5, 4), (2, 3, 7, 6), (0, 2, 6, 4), (1, 3, 7, 5)]
    m = Euler([math.radians(a) for a in rot], "XYZ").to_matrix()
    return {"verts": [m @ Vector(v) + Vector(offset) for v in verts], "faces": faces, "bevel": bevel, "flat": flat}


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
        bmesh.ops.bevel(bm, geom=list(edges), offset=shape["bevel"], segments=1, affect="EDGES")
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


def make_material(name="trench_surface"):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.15, 0.20, 0.28, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.55
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
    obj.data.materials.append(make_material(name))
    bm = bmesh.new()
    for shape in shapes:
        add_shape(bm, shape)
    bm.to_mesh(mesh)
    bm.free()
    bpy.context.view_layer.update()
    return {"Root": root, name: obj}


AO_SAMPLES = 44
AO_DISTANCE = 0.85
AO_FLOOR = 0.22


def bake_occlusion(parts, extra_occluder_shapes=None):
    rng = random.Random(31)
    verts, polys = [], []
    for obj in parts.values():
        if obj.type != "MESH":
            continue
        base = len(verts)
        verts.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        polys.extend(tuple(base + i for i in poly.vertices) for poly in obj.data.polygons)

    if extra_occluder_shapes:
        bm_occ = bmesh.new()
        for s in extra_occluder_shapes:
            add_shape(bm_occ, s)
        bm_occ.verts.ensure_lookup_table()
        bm_occ.faces.ensure_lookup_table()
        base = len(verts)
        verts.extend(v.co.copy() for v in bm_occ.verts)
        polys.extend(tuple(base + v.index for v in f.verts) for f in bm_occ.faces)
        bm_occ.free()

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
            ao = max(AO_FLOOR, min(1.0, ao))
            attr.data[v.index].color = (ao, ao, ao, 1.0)
        mesh.color_attributes.active_color = attr
        mesh.color_attributes.render_color_index = mesh.color_attributes.find("AO")


# 1. trench_deck: top surface at y = 0, with side gutter curbs and top/bottom bulkheads so crevices get rich AO
DECK_SHAPES = [
    box((15.2, 0.36, 13.2), (0.0, -0.18, 0.0), bevel=0.04),
    # Side gutter curbs along x = +/- 6.55
    box((0.28, 0.14, 13.0), (-6.55, 0.07, 0.0), bevel=0.025),
    box((0.28, 0.14, 13.0), (6.55, 0.07, 0.0), bevel=0.025),
    # Top and bottom trench lip sills
    box((13.4, 0.18, 0.32), (0.0, 0.09, 6.25), bevel=0.03),
    box((13.4, 0.16, 0.32), (0.0, 0.08, -6.25), bevel=0.03),
]
for z_rib in (-5.0, -3.5, -1.5, 0.5, 2.5, 4.5):
    DECK_SHAPES.append(box((0.42, 0.12, 0.24), (-6.35, 0.06, z_rib), bevel=0.02))
    DECK_SHAPES.append(box((0.42, 0.12, 0.24), (6.35, 0.06, z_rib), bevel=0.02))

# 2. trench_rail: single side containment wall (instanced on left x=-7.1 and rotated 180 on right x=+7.1)
RAIL_SHAPES = [
    box((0.82, 0.72, 13.2), (0.0, 0.36, 0.0), bevel=0.05),
    box((0.52, 0.18, 12.8), (-0.10, 0.78, 0.0), bevel=0.03),
]
for z_buttress in (-5.2, -3.4, -1.6, 0.2, 2.0, 3.8, 5.4):
    RAIL_SHAPES.append(box((0.26, 0.64, 0.34), (0.38, 0.32, z_buttress), bevel=0.025))

# 3. trench_markers: defense line bar at z = -3.5 + side lane ticks along x = +/- 6.25
MARKER_SHAPES = [
    box((12.8, 0.025, 0.065), (0.0, 0.012, -3.5), bevel=0.006),
    box((0.35, 0.045, 0.14), (-6.25, 0.022, -3.5), bevel=0.008),
    box((0.35, 0.045, 0.14), (6.25, 0.022, -3.5), bevel=0.008),
]
for z_tick in (0.6, 1.35, 2.10, 2.85, 3.60, 4.80):
    MARKER_SHAPES.append(box((0.24, 0.03, 0.06), (-6.25, 0.015, z_tick), bevel=0.005))
    MARKER_SHAPES.append(box((0.24, 0.03, 0.06), (6.25, 0.015, z_tick), bevel=0.005))

MODELS = {
    "trench_deck": (
        DECK_SHAPES,
        [box((0.82, 0.72, 13.2), (-7.1, 0.36, 0.0), bevel=0.02),
         box((0.82, 0.72, 13.2), (7.1, 0.36, 0.0), bevel=0.02)],
    ),
    "trench_rail": (
        RAIL_SHAPES,
        [box((4.0, 0.36, 13.2), (1.8, -0.18, 0.0), bevel=0.02)],
    ),
    "trench_markers": (
        MARKER_SHAPES,
        [box((15.0, 0.36, 13.2), (0.0, -0.18, 0.0), bevel=0.02)],
    ),
}


def export(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts.values():
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts["Root"]
    path = EXPORT_DIR / f"{name}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_vertex_color="ACTIVE",
    )
    print("wrote " + str(path))


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)

    total_tris = 0
    for name, (shapes, extra_occ) in MODELS.items():
        parts = build(name, shapes)
        bake_occlusion(parts, extra_occ)
        obj = parts[name]
        obj.data.calc_loop_triangles()
        tris = len(obj.data.loop_triangles)
        total_tris += tris
        aos = [c.color[0] for c in obj.data.color_attributes["AO"].data]
        print(f"{name}: {tris} tris, AO min={min(aos):.3f} max={max(aos):.3f}")
        assert min(aos) < max(aos) - 0.05, f"{name} AO has insufficient variation"
        export(name, parts)

    assert total_tris <= MAX_TRIANGLES_TOTAL, f"Total triangles {total_tris} exceeds budget {MAX_TRIANGLES_TOTAL}"
    blend_path = SOURCE_DIR / "trench.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    print(f"wrote {blend_path} (total tris: {total_tris})")


main()
sys.exit(0)
