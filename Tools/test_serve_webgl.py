"""Exercise WebGL response headers against an isolated local HTTP server."""

import functools
import gzip
import http.client
from pathlib import Path
import tempfile
import threading
import unittest
from http.server import ThreadingHTTPServer

from serve_webgl import UnityWebGLHandler


class QuietHandler(UnityWebGLHandler):
    def log_message(self, format, *args):
        pass


class WebGLServerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.directory = tempfile.TemporaryDirectory()
        cls.root = Path(cls.directory.name)
        cls.server = ThreadingHTTPServer(
            ("127.0.0.1", 0),
            functools.partial(QuietHandler, directory=cls.directory.name),
        )
        cls.thread = threading.Thread(target=cls.server.serve_forever, daemon=True)
        cls.thread.start()

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown()
        cls.server.server_close()
        cls.thread.join()
        cls.directory.cleanup()

    def request(self, path, headers=None, method="GET"):
        connection = http.client.HTTPConnection(*self.server.server_address, timeout=5)
        try:
            connection.request(method, path, headers=headers or {})
            response = connection.getresponse()
            return response.status, response.headers, response.read()
        finally:
            connection.close()

    def test_plain_files_and_html_revalidate(self):
        for filename, content_type in (
            ("index.html", "text/html"),
            ("notes.txt", "text/plain"),
            ("WebGL.loader.js", "application/javascript"),
            ("WebGL.wasm", "application/wasm"),
        ):
            with self.subTest(filename=filename):
                (self.root / filename).write_bytes(b"plain fixture")
                status, headers, body = self.request("/" + filename + "?release=20261019")
                self.assertEqual(status, 200)
                self.assertEqual(headers["Content-Type"], content_type)
                self.assertEqual(headers["Cache-Control"], "no-cache")
                self.assertNotIn("Content-Encoding", headers)
                self.assertEqual(body, b"plain fixture")

    def test_compressed_unity_assets_and_conditional_requests(self):
        content = b"Unity build fixture"
        for stem in ("WebGL", "0123456789abcdef0123456789abcdef"):
            cache = "no-cache" if stem == "WebGL" else "public, max-age=31536000, immutable"
            for asset, content_type in (
                ("data", "application/octet-stream"),
                ("wasm", "application/wasm"),
                ("framework.js", "application/javascript"),
            ):
                for suffix in (".unityweb", ".gz"):
                    filename = stem + "." + asset + suffix
                    with self.subTest(filename=filename):
                        compressed = gzip.compress(content)
                        (self.root / filename).write_bytes(compressed)
                        path = "/" + filename + "?release=20261019"
                        status, headers, body = self.request(path)
                        self.assertEqual(status, 200)
                        self.assertEqual(headers["Content-Type"], content_type)
                        self.assertEqual(headers["Content-Encoding"], "gzip")
                        self.assertEqual(headers["Content-Length"], str(len(compressed)))
                        self.assertEqual(headers["Cache-Control"], cache)
                        self.assertEqual(gzip.decompress(body), content)

                        status, conditional_headers, body = self.request(
                            path, {"If-Modified-Since": headers["Last-Modified"]}
                        )
                        self.assertEqual(status, 304)
                        self.assertEqual(conditional_headers["Cache-Control"], cache)
                        self.assertNotIn("Content-Encoding", conditional_headers)
                        self.assertEqual(body, b"")

                        status, head_headers, body = self.request(path, method="HEAD")
                        self.assertEqual(status, 200)
                        self.assertEqual(head_headers["Content-Encoding"], "gzip")
                        self.assertEqual(body, b"")

    def test_only_recognized_hash_names_are_immutable(self):
        for filename, immutable in (
            ("0123456789abcdef0123456789abcdef.loader.js", True),
            ("0123456789abcdef0123456789abcdef.html", False),
            ("01234567.loader.js", False),
            ("release-0123456789abcdef0123456789abcdef.loader.js", False),
            ("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz.loader.js", False),
        ):
            with self.subTest(filename=filename):
                (self.root / filename).write_bytes(b"fixture")
                status, headers, _ = self.request("/" + filename)
                self.assertEqual(status, 200)
                self.assertEqual("immutable" in headers["Cache-Control"], immutable)
                self.assertNotIn("Content-Encoding", headers)

    def test_missing_compressed_assets_return_readable_error(self):
        for filename in (
            "missing.wasm.unityweb",
            "missing.data.gz",
            "abcdef0123456789abcdef0123456789.wasm.gz",
        ):
            with self.subTest(filename=filename):
                status, headers, body = self.request("/" + filename)
                self.assertEqual(status, 404)
                self.assertTrue(headers["Content-Type"].startswith("text/html"))
                self.assertNotIn("Content-Encoding", headers)
                self.assertEqual(headers["Cache-Control"], "no-cache")
                self.assertIn(b"Error response", body)


if __name__ == "__main__":
    unittest.main()
