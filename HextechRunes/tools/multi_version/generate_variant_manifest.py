#!/usr/bin/env python3
import argparse
import json
import os
from pathlib import Path
import tempfile

from variant_common import COMPAT_MARKER_NAME, sha256, version_key


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate a validated STS2 multi-version variant manifest."
    )
    parser.add_argument("--dist", type=Path, required=True)
    parser.add_argument("--mod-id", required=True)
    parser.add_argument("--manifest-name", required=True)
    parser.add_argument("--target", action="append", required=True)
    args = parser.parse_args()

    # 目录、DLL、标记与哈希的一致性由随后运行的 validate_variant_bundle.py 统一校验。
    dist = args.dist.resolve()
    assembly = f"{args.mod_id}.dll"
    targets = sorted(set(args.target), key=version_key)
    if len(targets) != len(args.target):
        raise ValueError("duplicate --target value")

    variants = []
    for target in targets:
        directory = dist / "lib" / target
        dll = directory / assembly
        marker = directory / COMPAT_MARKER_NAME
        marker.write_text(f"{target}\n", encoding="utf-8")
        variants.append(
            {
                "compatTarget": target,
                "directory": f"lib/{target}",
                "assembly": assembly,
                "sha256": sha256(dll),
            }
        )

    output = dist / args.manifest_name
    manifest = {"schema": 1, "variants": variants}
    with tempfile.NamedTemporaryFile(
        "w", encoding="utf-8", dir=dist, delete=False
    ) as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
        temporary = Path(handle.name)
    os.replace(temporary, output)
    print(f"generated {output} with {len(variants)} variant(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
