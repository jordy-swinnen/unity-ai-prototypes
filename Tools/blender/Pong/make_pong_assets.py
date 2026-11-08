"""Builds the Pong models and exports each one as a .glb file.

Run headless from the repository root:
    blender -b -P Tools/blender/Pong/make_pong_assets.py

Sizes are in metres and written as Unity sees them (x, y, z). Blender is Z-up and
Unity is Y-up, so Blender's Y axis becomes Unity's -Z on export. Every shape is
symmetric, so that flip changes nothing. Each model is exported at the origin with
its pivot at its centre.
"""
import bpy
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
EXPORT_DIR = REPO_ROOT / "Assets" / "Prototypes" / "Pong" / "Art" / "Exports"
SOURCE_BLEND = REPO_ROOT / "Art" / "source" / "Pong" / "pong.blend"

# name: (size in Unity x, y, z, flat colour as linear RGBA)
BOXES = {
    "table": ((16.0, 0.4, 10.0), (0.05, 0.25, 0.10, 1.0)),
    "wall": ((16.0, 0.6, 0.5), (0.50, 0.50, 0.50, 1.0)),
    "paddle": ((0.4, 0.5, 2.0), (0.90, 0.90, 0.90, 1.0)),
}
BALL_RADIUS = 0.25
BALL_COLOUR = (1.0, 0.45, 0.05, 1.0)

# Where each model sits in the saved .blend, so a person can see them side by side.
# The exports ignore this: they are written at the origin.
PREVIEW_OFFSETS = {
    "table": (0.0, 0.0, 0.0),
    "wall": (0.0, 9.0, 0.0),
    "paddle": (0.0, -9.0, 0.0),
    "ball": (0.0, -13.0, 0.0),
}


def make_material(name, colour):
    material = bpy.data.materials.new(name=f"{name}_mat")
    if material.node_tree is None:
        material.use_nodes = True
    principled = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    principled.inputs["Base Color"].default_value = colour
    principled.inputs["Roughness"].default_value = 0.6
    material.diffuse_color = colour
    return material


def select_only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def make_box(name, unity_size, colour):
    size_x, size_y, size_z = unity_size
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    # Blender (x, y, z) = Unity (x, -z, y)
    obj.scale = (size_x, size_z, size_y)
    select_only(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(make_material(name, colour))
    return obj


def make_ball(name, radius, colour):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=32, ring_count=16, location=(0.0, 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    select_only(obj)
    bpy.ops.object.shade_smooth()
    obj.data.materials.append(make_material(name, colour))
    return obj


def export_glb(obj):
    select_only(obj)
    obj.location = (0.0, 0.0, 0.0)
    path = EXPORT_DIR / f"{obj.name}.glb"
    bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True)
    print(f"Exported {path}")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    SOURCE_BLEND.parent.mkdir(parents=True, exist_ok=True)

    objects = [make_box(name, size, colour) for name, (size, colour) in BOXES.items()]
    objects.append(make_ball("ball", BALL_RADIUS, BALL_COLOUR))

    for obj in objects:
        export_glb(obj)

    for obj in objects:
        obj.location = PREVIEW_OFFSETS[obj.name]
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_BLEND), check_existing=False)
    print(f"Saved {SOURCE_BLEND}")


main()
