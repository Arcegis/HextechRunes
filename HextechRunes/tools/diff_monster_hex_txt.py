#!/usr/bin/env python3
"""Diff the MonsterHex registry against the 怪物 section of hextech_relics_summary.txt.

Prints which registered monster hexes are NOT documented in the 怪物 section,
so they can be added (additively, without touching existing lines).
"""
import json
import re
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
REG = os.path.join(ROOT, "src", "Content", "HextechMonsterHexRegistry.cs")
LOC = os.path.join(ROOT, "assets", "localization", "zhs", "relics.json")
SUMMARY = os.path.join(ROOT, "hextech_relics_summary.txt")


def camel_to_key(cls: str) -> str:
    # 我方借壳的图标 relic 类名以 Rune 结尾(loc key 为 {SNAKE}_RUNE);
    # 敌方专属图标 relic 类名以 Hex 结尾(loc key 直接为 {SNAKE},含 HEX 尾缀)。
    if cls.endswith("Hex"):
        s = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", cls)
        s = re.sub(r"(?<=[A-Z])(?=[A-Z][a-z])", "_", s)
        return s.upper()
    name = cls[:-4] if cls.endswith("Rune") else cls
    s = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", name)
    s = re.sub(r"(?<=[A-Z])(?=[A-Z][a-z])", "_", s)
    return s.upper() + "_RUNE"


def main() -> None:
    reg_text = open(REG, encoding="utf-8").read()
    loc = json.load(open(LOC, encoding="utf-8"))
    summary = open(SUMMARY, encoding="utf-8").read()
    # 怪物 section = from "怪物：" to the next top-level section.
    start = summary.index("怪物：")
    rest = summary[start + len("怪物："):]
    nxt = re.search(r"\n[^\n#白黄棱]{0,8}：\n", rest)
    section = rest[: nxt.start()] if nxt else rest

    entries = re.findall(
        r"Monster<(\w+)>\(\s*MonsterHexKind\.(\w+),\s*HextechRarityTier\.(\w+)([^)]*)\)",
        reg_text,
    )

    missing = []
    unresolved = []
    for cls, kind, rarity, args in entries:
        disabled = "disabled: true" in args
        key = camel_to_key(cls)
        title = loc.get(key + ".title")
        if title is None:
            unresolved.append((rarity, kind, cls, key))
            continue
        present = title in section
        if not present:
            missing.append((rarity, kind, cls, title, disabled))

    print(f"registry entries: {len(entries)}")
    print(f"unresolved loc keys: {len(unresolved)}")
    for r, k, c, key in unresolved:
        print(f"  UNRESOLVED {r} {k} ({c}) key={key}")
    print(f"missing from 怪物 section: {len(missing)}")
    order = {"Silver": 0, "Gold": 1, "Prismatic": 2}
    for r, k, c, title, dis in sorted(missing, key=lambda x: order.get(x[0], 9)):
        flag = " [disabled]" if dis else ""
        print(f"  MISSING {r}: {title}  <{k}/{c}>{flag}")


if __name__ == "__main__":
    main()
