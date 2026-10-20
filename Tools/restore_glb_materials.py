"""Keep a rebuilt GLB's mesh and restore exact original material/image bytes."""
import json
import struct
import sys
from pathlib import Path


def read_glb(path):
    data = Path(path).read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    document = json.loads(data[20:20 + length])
    return document, data[28 + length:]


def restore_materials(rebuilt_path, original_path):
    document, binary = read_glb(rebuilt_path)
    original, original_binary = read_glb(original_path)
    binary = bytearray(binary)
    for key in ("materials", "textures", "samplers", "images"):
        if key in original:
            document[key] = original[key]
        else:
            document.pop(key, None)
    for image in document.get("images", []):
        view = original["bufferViews"][image["bufferView"]]
        binary.extend(b"\0" * (-len(binary) % 4))
        offset = len(binary)
        binary.extend(original_binary[view.get("byteOffset", 0):view.get("byteOffset", 0) + view["byteLength"]])
        image["bufferView"] = len(document["bufferViews"])
        document["bufferViews"].append({"buffer": 0, "byteOffset": offset, "byteLength": view["byteLength"]})
    binary.extend(b"\0" * (-len(binary) % 4))
    document["buffers"][0]["byteLength"] = len(binary)
    encoded = json.dumps(document, separators=(",", ":")).encode()
    encoded += b" " * (-len(encoded) % 4)
    output = struct.pack("<III", 0x46546C67, 2, 28 + len(encoded) + len(binary))
    output += struct.pack("<II", len(encoded), 0x4E4F534A) + encoded
    output += struct.pack("<II", len(binary), 0x004E4942) + binary
    Path(rebuilt_path).write_bytes(output)


if __name__ == "__main__":
    restore_materials(sys.argv[1], sys.argv[2])
