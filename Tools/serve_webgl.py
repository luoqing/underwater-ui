"""Serve a Unity WebGL build with the headers required for gzip assets."""

import argparse
import functools
import posixpath
import re
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote, urlsplit


HASHED_BUILD_ASSET = re.compile(
    r"[0-9a-fA-F]{32}\.(?:(?:data|wasm|framework\.js)(?:\.gz|\.unityweb)?|loader\.js)"
)


class UnityWebGLHandler(SimpleHTTPRequestHandler):
    def send_response(self, code, message=None):
        self.response_status = code
        super().send_response(code, message)

    def end_headers(self):
        path = unquote(urlsplit(self.path).path)
        if self.response_status == 200 and path.endswith((".unityweb", ".gz")):
            self.send_header("Content-Encoding", "gzip")
        immutable = self.response_status in (200, 304) and HASHED_BUILD_ASSET.fullmatch(
            posixpath.basename(path)
        )
        self.send_header("Cache-Control", "public, max-age=31536000, immutable" if immutable else "no-cache")
        super().end_headers()

    def guess_type(self, path):
        for suffix in (".unityweb", ".gz"):
            if path.endswith(suffix):
                path = path[:-len(suffix)]
                break
        if path.endswith(".wasm"):
            return "application/wasm"
        if path.endswith(".js"):
            return "application/javascript"
        if path.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8878)
    parser.add_argument("--bind", default="0.0.0.0")
    parser.add_argument("--directory", required=True)
    args = parser.parse_args()

    handler = functools.partial(UnityWebGLHandler, directory=args.directory)
    server = ThreadingHTTPServer((args.bind, args.port), handler)
    print(f"Serving Unity WebGL on http://{args.bind}:{args.port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
