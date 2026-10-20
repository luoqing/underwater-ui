"""Rebuild a damaged reduced mesh from the original, retaining current materials.

blender -b --python Tools/rebuild_closed_model.py -- source.glb current.glb output.glb triangles
"""
import json
import os
import sys
import tempfile
from pathlib import Path

import bmesh
import bpy


source_path, current_path, output_path, budget = sys.argv[sys.argv.index("--") + 1:]
material_copy = tempfile.NamedTemporaryFile(suffix=".glb")
material_copy.write(Path(current_path).read_bytes())
material_copy.flush()
budget = int(budget)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(current_path), merge_vertices=True)
current_objects = list(bpy.context.scene.objects)
materials = [slot.material for obj in current_objects if obj.type == "MESH" for slot in obj.material_slots]
for obj in current_objects:
    bpy.data.objects.remove(obj, do_unlink=True)

bpy.ops.import_scene.gltf(filepath=os.path.abspath(source_path), merge_vertices=True)
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
before = sum(len(obj.data.polygons) for obj in meshes)
for obj in meshes:
    # glTF duplicates vertices for UV/normal seams. Weld geometric positions
    # before reduction; Blender retains UVs on face corners rather than cutting
    # the surface open at every texture seam.
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=0.000001)
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if materials:
        obj.data.materials.clear()
        for material in materials:
            obj.data.materials.append(material)
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new("Topology preserving reduction", "DECIMATE")
    modifier.ratio = min(1.0, budget / before)
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    boundary_edges = [edge for edge in mesh.edges if edge.is_boundary]
    if boundary_edges:
        bmesh.ops.holes_fill(mesh, edges=boundary_edges, sides=32)
        mesh.normal_update()
        mesh.to_mesh(obj.data)
    boundary = sum(edge.is_boundary for edge in mesh.edges)
    mesh.free()
    print("MODEL_TOPOLOGY " + json.dumps({"name": obj.name, "triangles": len(obj.data.polygons), "boundary_edges": boundary}))

os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
bpy.ops.export_scene.gltf(filepath=os.path.abspath(output_path), export_format="GLB", export_animations=False, export_cameras=False, export_lights=False)
sys.path.insert(0, os.path.dirname(__file__))
from restore_glb_materials import restore_materials
restore_materials(output_path, material_copy.name)
material_copy.close()
