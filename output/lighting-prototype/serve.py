"""Local-only server for the saved Unity WebGL lighting comparison."""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Builds" / "BucaLightingPrototype"

class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def guess_type(self, path):
        raw = path.removesuffix(".unityweb").removesuffix(".gz")
        if raw.endswith(".wasm"):
            return "application/wasm"
        if raw.endswith(".js"):
            return "application/javascript"
        return super().guess_type(raw)

    def end_headers(self):
        if self.path.split("?", 1)[0].endswith((".unityweb", ".gz")):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()

    def do_POST(self):
        # Only captures from the local comparison, never arbitrary paths/files.
        names = {f"/capture/{mode}-{view}" for mode in ("current", "prototype") for view in ("close", "board")}
        size = int(self.headers.get("Content-Length", "0"))
        if self.path not in names or not 0 < size < 8_000_000:
            self.send_error(400)
            return
        data = self.rfile.read(size)
        if not data.startswith(b"\x89PNG\r\n\x1a\n"):
            self.send_error(400)
            return
        destination = Path(__file__).resolve().parent / ("browser-" + self.path.rsplit("/", 1)[1] + ".png")
        destination.write_bytes(data)
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"Capture saved")

if __name__ == "__main__":
    print("Buca lighting comparison: http://127.0.0.1:8766", flush=True)
    ThreadingHTTPServer(("127.0.0.1", 8766), Handler).serve_forever()
