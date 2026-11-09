"""Neon Pong models: table, rail, end line, paddle and ball.

Run headless:
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P Tools/blender/NeonPong/make_neon_pong_assets.py

Writes one .glb per model into Assets/Prototypes/NeonPong/Art/Exports/ and the source file
Art/source/NeonPong/neon_pong.blend. Exits with a non-zero status if a size, pivot or triangle
count is outside the plan (docs/prototypes/neon-pong-plan.md, "Units and layout").

Sizes are written in Unity axes (x length, y up, z width). Blender is Z-up, so Unity (x, y, z)
becomes Blender (x, -z, y), and the glTF exporter turns that back into Unity axes.
Materials are neutral and named. Colours and emission are set in Unity from the palette.
"""
import sys
from pathlib import Path

import bmesh
import bpy

REPO_ROOT = Path(__file__).resolve().parents[3]
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "NeonPong" / "Art" / "Exports"
BLEND_PATH = REPO_ROOT / "Art" / "source" / "NeonPong" / "neon_pong.blend"
MAX_TRIANGLES = 1500
TOLERANCE = 0.001

# name -> (size in Unity axes, min corner y, expected centre x and z are 0)
# "base_y" is the lowest y of the mesh in Unity axes (the pivot is at y = 0).
EXPECTED = {
    # 0.4 slab plus 0.01 grid lines standing on its top face
    "Table": {"size": (17.0, 0.41, 11.0), "base_y": -0.4},
    "Rail": {"size": (16.4, 0.3, 0.3), "base_y": 0.0},
    "EndLine": {"size": (0.1, 0.02, 10.0), "base_y": 0.0},
    "Paddle": {"size": (0.4, 0.3, 2.0), "base_y": 0.0},
    "Ball": {"size": (0.5, 0.5, 0.5), "base_y": -0.25},
}


def to_blender(x, y, z):
    """Unity axes to Blender axes."""
    return (x, -z, y)


def add_box(bm, size, center):
    """Add an axis-aligned box. size and center are in Unity axes."""
    res = bmesh.ops.create_cube(bm, size=1.0)
    verts = res["verts"]
    sx, sy, sz = size
    cx, cy, cz = center
    for v in verts:
        ux, uy, uz = v.co.x * sx + cx, v.co.z * sy + cy, -v.co.y * sz + cz
        v.co = to_blender(ux, uy, uz)
    return verts


def make_material(name, rgba):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Roughness"].default_value = 0.6
    return mat


def new_object(name, bm, materials):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for mat in materials:
        mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def build_table(mat_base, mat_grid):
    bm = bmesh.new()
    add_box(bm, (17.0, 0.4, 11.0), (0.0, -0.2, 0.0))
    for f in bm.faces:
        f.material_index = 0
    # Thin raised grid lines on the top face. They use the second material so Unity can make
    # them glow faintly. Lines every 2 units, inside the 16 x 10 play area.
    line, height = 0.04, 0.01
    grid_faces_start = len(bm.faces)
    for gx in range(-6, 7, 2):
        add_box(bm, (line, height, 10.0), (float(gx), height / 2.0, 0.0))
    for gz in range(-4, 5, 2):
        add_box(bm, (16.0, height, line), (0.0, height / 2.0, float(gz)))
    bm.faces.ensure_lookup_table()
    for f in bm.faces[grid_faces_start:]:
        f.material_index = 1
    return new_object("Table", bm, [mat_base, mat_grid])


def build_rail(mat):
    bm = bmesh.new()
    add_box(bm, (16.4, 0.3, 0.3), (0.0, 0.15, 0.0))
    return new_object("Rail", bm, [mat])


def build_end_line(mat):
    bm = bmesh.new()
    add_box(bm, (0.1, 0.02, 10.0), (0.0, 0.01, 0.0))
    return new_object("EndLine", bm, [mat])


def build_paddle(mat):
    bm = bmesh.new()
    add_box(bm, (0.4, 0.3, 2.0), (0.0, 0.15, 0.0))
    bmesh.ops.bevel(
        bm, geom=list(bm.edges), offset=0.08, segments=3, profile=0.5, affect="EDGES"
    )
    for f in bm.faces:
        f.smooth = True
    return new_object("Paddle", bm, [mat])


def build_ball(mat):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=0.25)
    for f in bm.faces:
        f.smooth = True
    return new_object("Ball", bm, [mat])


def measure(obj):
    """Return (size, min_y, centre_x, centre_z, triangles) in Unity axes."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    xs = [v.co.x for v in mesh.vertices]
    ys = [v.co.y for v in mesh.vertices]  # Blender y = Unity -z
    zs = [v.co.z for v in mesh.vertices]  # Blender z = Unity y
    size = (max(xs) - min(xs), max(zs) - min(zs), max(ys) - min(ys))
    centre_x = (max(xs) + min(xs)) / 2.0
    centre_z = -(max(ys) + min(ys)) / 2.0
    return size, min(zs), centre_x, centre_z, len(mesh.loop_triangles)


def check(obj):
    spec = EXPECTED[obj.name]
    size, base_y, cx, cz, tris = measure(obj)
    problems = []
    for axis, got, want in zip("xyz", size, spec["size"]):
        if abs(got - want) > TOLERANCE:
            problems.append(f"size {axis} is {got:.4f}, expected {want}")
    if abs(base_y - spec["base_y"]) > TOLERANCE:
        problems.append(f"lowest y is {base_y:.4f}, expected {spec['base_y']}")
    if abs(cx) > TOLERANCE or abs(cz) > TOLERANCE:
        problems.append(f"centre is off the pivot: x {cx:.4f}, z {cz:.4f}")
    if tris >= MAX_TRIANGLES:
        problems.append(f"{tris} triangles, limit is {MAX_TRIANGLES}")
    line = (
        f"{obj.name}: size ({size[0]:.3f}, {size[1]:.3f}, {size[2]:.3f}), "
        f"lowest y {base_y:.3f}, {tris} triangles"
    )
    return line, problems


def export(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = EXPORT_DIR / f"{obj.name.lower()}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
    )
    return path


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)

    mat_body = make_material("body", (0.08, 0.09, 0.14, 1.0))
    mat_grid = make_material("grid", (0.07, 0.19, 0.35, 1.0))
    mat_rail = make_material("rail", (0.07, 0.19, 0.35, 1.0))
    mat_paddle = make_material("paddle", (0.9, 0.9, 0.95, 1.0))
    mat_ball = make_material("ball", (1.0, 0.89, 0.48, 1.0))

    objects = [
        build_table(mat_body, mat_grid),
        build_rail(mat_rail),
        build_end_line(mat_rail),
        build_paddle(mat_paddle),
        build_ball(mat_ball),
    ]

    failed = False
    for obj in objects:
        line, problems = check(obj)
        print(line)
        for p in problems:
            print(f"  PROBLEM: {p}")
            failed = True
    if failed:
        print("Model check failed. Nothing was exported.")
        sys.exit(1)

    for obj in objects:
        print(f"Exported {export(obj)}")
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"Saved {BLEND_PATH}")


main()
