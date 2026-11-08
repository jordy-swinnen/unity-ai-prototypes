import bpy
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
EXPORT_PATH = REPO_ROOT / "Assets" / "Art" / "Exports" / "crate.glb"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.5))
bpy.context.active_object.name = "Crate"

EXPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.gltf(filepath=str(EXPORT_PATH), export_format="GLB", use_selection=False)
print(f"Exported {EXPORT_PATH}")
