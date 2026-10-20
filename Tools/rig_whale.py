"""Add a lightweight procedural swim rig to the static Tripo whale GLB.

Usage:
  blender --background --python Tools/rig_whale.py -- input.glb output.glb
"""

import os
import sys
import math
import json

import bpy


def centreline(value):
    # A single smooth curve fitted to the source pose. Independent smoothstep
    # segments introduce a flattened derivative at every knot, corrugating skin.
    return -0.1610551525 + 0.2872986240 * value + 1.4299077869 * value * value


def slope(value):
    return 0.2872986240 + 2.8598155738 * value


def arc_length(value):
    # Exact integral of sqrt(1 + (a + b*t)^2).
    u = slope(value)
    return (u * math.sqrt(1.0 + u * u) + math.asinh(u)) / (2.0 * 2.8598155738)


def unbend_point(longitudinal, height):
    # Project onto the source curve before flattening its arc. Moving height
    # alone shears the steep tail and stretches triangle edges almost 3x.
    nearest = longitudinal
    for _ in range(12):
        derivative = slope(nearest)
        residual = centreline(nearest) - height
        numerator = nearest - longitudinal + residual * derivative
        denominator = 1.0 + derivative * derivative + residual * 2.8598155738
        if abs(denominator) < 0.00001:
            break
        nearest -= max(-0.08, min(0.08, numerator / denominator))
    derivative = slope(nearest)
    normal_distance = (height - centreline(nearest) - (longitudinal - nearest) * derivative) / math.sqrt(1.0 + derivative * derivative)
    straight_y = arc_length(nearest) - arc_length(0.0)
    # Retain a gentle natural curvature rather than over-flattening anatomy.
    return longitudinal * 0.20 + straight_y * 0.80, height * 0.20 + normal_distance * 0.80


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
before_vertices = [vertex.co.copy() for vertex in mesh_object.data.vertices]
for vertex in mesh_object.data.vertices:
    vertex.co.y, vertex.co.z = unbend_point(vertex.co.y, vertex.co.z)
mesh_object.data.update()
mesh_object.data.normals_split_custom_set_from_vertices([vertex.normal for vertex in mesh_object.data.vertices])
ratios = []
for edge in mesh_object.data.edges:
    a, b = edge.vertices
    before = (before_vertices[a] - before_vertices[b]).length
    if before > 0.00000001:
        ratios.append((mesh_object.data.vertices[a].co - mesh_object.data.vertices[b].co).length / before)
ratios.sort()
print("WHALE_EDGE_QUALITY " + json.dumps({"edges": len(ratios), "stretch_p99": ratios[int(len(ratios) * 0.99)], "stretch_max": ratios[-1], "edges_over_2x": sum(value > 2.0 for value in ratios)}))
min_y = min(vertex.co.y for vertex in mesh_object.data.vertices)
max_y = max(vertex.co.y for vertex in mesh_object.data.vertices)

armature_data = bpy.data.armatures.new("WhaleSwimRig")
armature_object = bpy.data.objects.new("WhaleSwimRig", armature_data)
bpy.context.collection.objects.link(armature_object)
bpy.context.view_layer.objects.active = armature_object
armature_object.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

segments = [
    ("WhaleRoot", min_y - 0.02, unbend_point(-0.12, centreline(-0.12))[0]),
    ("WhaleBody", unbend_point(-0.12, centreline(-0.12))[0], unbend_point(0.16, centreline(0.16))[0]),
    ("WhaleTailBase", unbend_point(0.16, centreline(0.16))[0], unbend_point(0.36, centreline(0.36))[0]),
    ("WhaleTailTip", unbend_point(0.36, centreline(0.36))[0], max_y + 0.02),
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
centres = [unbend_point(value, centreline(value))[0] for value in [-0.31, 0.04, 0.26, 0.45]]

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
