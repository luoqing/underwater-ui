"""Clean a Tripo GLB for the Unity WebGL benchmark.

Usage:
  blender --background --python Tools/prepare_tripo_asset.py -- source.glb output.glb target_triangles
"""

import json
import os
import sys

import bpy


def cli_args():
    args = sys.argv
    if "--" not in args:
        raise SystemExit("Expected: -- source.glb output.glb target_triangles")
    args = args[args.index("--") + 1 :]
    if len(args) != 3:
        raise SystemExit("Expected: source.glb output.glb target_triangles")
    return os.path.abspath(args[0]), os.path.abspath(args[1]), int(args[2])


def triangle_count(objects):
    count = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        count += len(obj.data.loop_triangles)
    return count


source_path, output_path, target_triangles = cli_args()
os.makedirs(os.path.dirname(output_path), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source_path)

for obj in list(bpy.context.scene.objects):
    if obj.type not in {"MESH", "EMPTY"}:
        bpy.data.objects.remove(obj, do_unlink=True)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise SystemExit(f"No mesh found in {source_path}")

for obj in meshes:
    obj.hide_set(False)
    obj.hide_render = False
    for polygon in obj.data.polygons:
        polygon.use_smooth = True

before_triangles = triangle_count(meshes)
ratio = min(1.0, target_triangles / max(1, before_triangles))
if ratio < 0.995:
    for obj in meshes:
        modifier = obj.modifiers.new(name="WebGL triangle budget", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)

for image in bpy.data.images:
    if image.name == "Render Result" or image.size[0] == 0 or image.size[1] == 0:
        continue
    longest = max(image.size[0], image.size[1])
    if longest > 2048:
        scale = 2048.0 / longest
        image.scale(max(1, round(image.size[0] * scale)), max(1, round(image.size[1] * scale)))

after_triangles = triangle_count(meshes)

bpy.ops.export_scene.gltf(
    filepath=output_path,
    export_format="GLB",
    export_apply=True,
    export_texcoords=True,
    export_normals=True,
    export_materials="EXPORT",
    export_cameras=False,
    export_lights=False,
    export_animations=False,
)

report = {
    "source": source_path,
    "output": output_path,
    "mesh_objects": len(meshes),
    "triangles_before": before_triangles,
    "triangles_after": after_triangles,
    "materials": len(bpy.data.materials),
    "images": [
        {"name": image.name, "width": image.size[0], "height": image.size[1]}
        for image in bpy.data.images
        if image.name != "Render Result"
    ],
    "bytes": os.path.getsize(output_path),
}
print("ASSET_PREPARED " + json.dumps(report, ensure_ascii=False))

