"""Serve a Unity WebGL build with the headers required for gzip assets."""

import argparse
import functools
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer


class UnityWebGLHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        if self.path.split("?", 1)[0].endswith(".unityweb"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm.unityweb"):
            return "application/wasm"
        if path.endswith(".framework.js.unityweb"):
            return "application/javascript"
        if path.endswith(".data.unityweb"):
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
