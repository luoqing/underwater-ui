"""Read-only GLB geometry and material checks; accepts one or more GLB paths."""
import hashlib
import json
import math
import struct
import sys
from collections import Counter
from pathlib import Path

from restore_glb_materials import read_glb


def accessor(document, binary, index):
    item = document["accessors"][index]
    view = document["bufferViews"][item["bufferView"]]
    columns = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[item["type"]]
    code = {5121: "B", 5123: "H", 5125: "I", 5126: "f"}[item["componentType"]]
    row = struct.Struct("<" + code * columns)
    start = view.get("byteOffset", 0) + item.get("byteOffset", 0)
    stride = view.get("byteStride", row.size)
    values = [row.unpack_from(binary, start + i * stride) for i in range(item["count"])]
    return [value[0] for value in values] if columns == 1 else values


def audit(path):
    document, binary = read_glb(path)
    primitive = document["meshes"][0]["primitives"][0]
    positions = accessor(document, binary, primitive["attributes"]["POSITION"])
    normals = accessor(document, binary, primitive["attributes"]["NORMAL"])
    indices = accessor(document, binary, primitive["indices"])
    ids = {}
    welded_positions = []
    welded = []
    for point in positions:
        key = tuple(round(value * 1000000) for value in point)
        if key not in ids:
            ids[key] = len(ids)
            welded_positions.append(point)
        welded.append(ids[key])
    edges = Counter()
    for offset in range(0, len(indices), 3):
        triangle = [welded[index] for index in indices[offset:offset + 3]]
        for a, b in zip(triangle, triangle[1:] + triangle[:1]):
            if a != b:
                edges[tuple(sorted((a, b)))] += 1
    boundary_lengths = sorted(math.dist(welded_positions[a], welded_positions[b]) for (a, b), count in edges.items() if count == 1)
    images = {}
    for index, image in enumerate(document.get("images", [])):
        view = document["bufferViews"][image["bufferView"]]
        start = view.get("byteOffset", 0)
        images[image.get("name", str(index))] = hashlib.sha256(binary[start:start + view["byteLength"]]).hexdigest()
    return {"path": str(path), "vertices": len(positions), "triangles": len(indices) // 3,
            "boundary_edges": len(boundary_lengths), "longest_boundary_edge": max(boundary_lengths, default=0),
            "finite_geometry": all(math.isfinite(value) for row in positions + normals for value in row),
            "normal_length_error": max(abs(math.sqrt(sum(v * v for v in normal)) - 1) for normal in normals),
            "images_sha256": images, "materials": document.get("materials", [])}


if __name__ == "__main__":
    for model_path in sys.argv[1:]:
        print(json.dumps(audit(Path(model_path))))
