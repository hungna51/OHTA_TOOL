"""OHTA AI wrapper around kev.serve:
Exposes GET /v1/rules so client CAD machines in the company LAN automatically sync
the latest .md rule files (05_AI/data/kev/*.md and 05_AI/data/huong_dan_ai.md) from
the Kev Server machine without manual file copying.
"""
import os
import socket
import kev.serve as ks

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DATA_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "data"))


@ks.app.get("/v1/rules")
def get_ohta_rules():
    data_dir = os.environ.get("OHTA_AI_DATA_DIR") or DEFAULT_DATA_DIR
    kev_dir = os.path.join(data_dir, "kev")
    kev_files = {}
    if os.path.isdir(kev_dir):
        for fn in sorted(os.listdir(kev_dir)):
            if fn.lower().endswith(".md"):
                full_path = os.path.join(kev_dir, fn)
                if os.path.isfile(full_path):
                    try:
                        with open(full_path, "r", encoding="utf-8") as f:
                            kev_files[fn] = f.read()
                    except Exception:
                        pass

    huong_dan_ai = ""
    claude_path = os.path.join(data_dir, "huong_dan_ai.md")
    if os.path.isfile(claude_path):
        try:
            with open(claude_path, "r", encoding="utf-8") as f:
                huong_dan_ai = f.read()
        except Exception:
            pass

    return {
        "server_host": socket.gethostname(),
        "kev_files": kev_files,
        "huong_dan_ai": huong_dan_ai,
    }


if __name__ == "__main__":
    ks.main()
