"""generate_variant_manifest.py 与 validate_variant_bundle.py 共用的版本/哈希/路径工具。"""
from __future__ import annotations

import hashlib
from pathlib import Path
import re

VERSION_RE = re.compile(r"\d+(?:\.\d+){1,3}")
COMPAT_MARKER_NAME = "compat-target.txt"


def version_key(value: str) -> tuple[int, ...]:
    if not VERSION_RE.fullmatch(value):
        raise ValueError(f"invalid numeric compatibility target: {value!r}")
    return tuple(int(part) for part in value.split("."))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()

