"""Add a lightweight procedural swim rig to the static Tripo whale GLB.

Usage:
  blender --background --python Tools/rig_whale.py -- input.glb output.glb
"""

import os
import sys

import bpy


def arguments():
    if "--" not in sys.argv:
        raise SystemExit("Expected: -- input.glb output.glb")
    values = sys.argv[sys.argv.index("--") + 1 :]
    if len(values) != 2:
        raise SystemExit("Expected: -- input.glb output.glb")
    return os.path.abspath(values[0]), os.path.abspath(values[1])


source_path, output_path = arguments()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source_path)

mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if len(mesh_objects) != 1:
    raise SystemExit(f"Expected one whale mesh, found {len(mesh_objects)}")

mesh_object = mesh_objects[0]
min_y = min(vertex.co.y for vertex in mesh_object.data.vertices)
max_y = max(vertex.co.y for vertex in mesh_object.data.vertices)

armature_data = bpy.data.armatures.new("WhaleSwimRig")
armature_object = bpy.data.objects.new("WhaleSwimRig", armature_data)
bpy.context.collection.objects.link(armature_object)
bpy.context.view_layer.objects.active = armature_object
armature_object.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

segments = [
    ("WhaleRoot", min_y - 0.02, -0.12),
    ("WhaleBody", -0.12, 0.16),
    ("WhaleTailBase", 0.16, 0.36),
    ("WhaleTailTip", 0.36, max_y + 0.02),
]

previous = None
for name, start_y, end_y in segments:
    bone = armature_data.edit_bones.new(name)
    bone.head = (0.0, start_y, 0.0)
    bone.tail = (0.0, end_y, 0.0)
    if previous is not None:
        bone.parent = previous
        bone.use_connect = True
    previous = bone

bpy.ops.object.mode_set(mode="OBJECT")

groups = {name: mesh_object.vertex_groups.new(name=name) for name, _, _ in segments}
centres = [-0.31, 0.04, 0.26, 0.45]

for vertex in mesh_object.data.vertices:
    y = vertex.co.y
    if y <= centres[0]:
        groups[segments[0][0]].add([vertex.index], 1.0, "REPLACE")
        continue
    if y >= centres[-1]:
        groups[segments[-1][0]].add([vertex.index], 1.0, "REPLACE")
        continue

    for index in range(len(centres) - 1):
        if centres[index] <= y <= centres[index + 1]:
            blend = (y - centres[index]) / (centres[index + 1] - centres[index])
            smooth = blend * blend * (3.0 - 2.0 * blend)
            groups[segments[index][0]].add([vertex.index], 1.0 - smooth, "REPLACE")
            groups[segments[index + 1][0]].add([vertex.index], smooth, "REPLACE")
            break

modifier = mesh_object.modifiers.new(name="WhaleSwimRig", type="ARMATURE")
modifier.object = armature_object
mesh_object.parent = armature_object

os.makedirs(os.path.dirname(output_path), exist_ok=True)
bpy.ops.export_scene.gltf(
    filepath=output_path,
    export_format="GLB",
    export_animations=False,
    export_skins=True,
    export_morph=False,
    export_yup=True,
)
print(f"RIGGED_WHALE {output_path}")
