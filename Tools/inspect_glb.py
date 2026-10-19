"""Print a compact JSON report for a GLB imported into Blender.

Usage:
  blender --background --python Tools/inspect_glb.py -- model.glb
"""

import json
import os
import sys

import bpy


def input_path():
    if "--" not in sys.argv:
        raise SystemExit("Expected: -- model.glb")
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 1:
        raise SystemExit("Expected: -- model.glb")
    return os.path.abspath(args[0])


source_path = input_path()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source_path)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
triangles = 0
vertices = 0
for obj in meshes:
    obj.data.calc_loop_triangles()
    triangles += len(obj.data.loop_triangles)
    vertices += len(obj.data.vertices)

all_corners = []
for obj in meshes:
    for corner in obj.bound_box:
        all_corners.append(obj.matrix_world @ __import__("mathutils").Vector(corner))

if all_corners:
    dimensions = [
        max(corner[i] for corner in all_corners)
        - min(corner[i] for corner in all_corners)
        for i in range(3)
    ]
else:
    dimensions = [0.0, 0.0, 0.0]

images = []
for image in bpy.data.images:
    if image.name == "Render Result":
        continue
    images.append(
        {
            "name": image.name,
            "width": image.size[0],
            "height": image.size[1],
            "packed": image.packed_file is not None,
        }
    )

report = {
    "source": source_path,
    "bytes": os.path.getsize(source_path),
    "mesh_objects": len(meshes),
    "vertices": vertices,
    "triangles": triangles,
    "materials": len(bpy.data.materials),
    "images": images,
    "dimensions": [round(value, 4) for value in dimensions],
    "armatures": len([obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]),
    "actions": len(bpy.data.actions),
}
print("GLB_INSPECTION " + json.dumps(report, ensure_ascii=False))
