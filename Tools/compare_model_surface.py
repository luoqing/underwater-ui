"""Sample original/reduced surface deviation in both directions using Blender BVH.

blender -b --python Tools/compare_model_surface.py -- original.glb candidate.glb ...
"""
import json
import os
import sys

import bpy
from mathutils.bvhtree import BVHTree


def load_mesh(path):
    previous = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.abspath(path), merge_vertices=True)
    return next(obj for obj in bpy.data.objects if obj not in previous and obj.type == "MESH")


def tree(mesh):
    return BVHTree.FromPolygons([vertex.co for vertex in mesh.vertices], [tuple(face.vertices) for face in mesh.polygons])


def distances(mesh, target):
    stride = max(1, len(mesh.vertices) // 5000)
    return [target.find_nearest(mesh.vertices[index].co)[3] for index in range(0, len(mesh.vertices), stride)]


bpy.ops.wm.read_factory_settings(use_empty=True)
paths = sys.argv[sys.argv.index("--") + 1:]
source = load_mesh(paths[0]).data
source_tree = tree(source)
for path in paths[1:]:
    candidate = load_mesh(path).data
    candidate_tree = tree(candidate)
    samples = sorted(distances(source, candidate_tree) + distances(candidate, source_tree))
    print("MODEL_SURFACE_DEVIATION " + json.dumps({"path": path, "samples": len(samples), "median": samples[len(samples) // 2], "p95": samples[int(len(samples) * 0.95)], "sample_max": max(samples)}))
