#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SRC = REPO_ROOT / "src"
LOCALIZATION = REPO_ROOT / "assets" / "localization"

def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def source_files(pattern: str = "*.cs") -> list[Path]:
    return [
        path
        for path in SRC.rglob(pattern)
        if "bin" not in path.parts and "obj" not in path.parts
    ]


def source_file_named(name: str) -> Path:
    matches = [path for path in source_files(name) if path.name == name]
    if len(matches) != 1:
        raise ValueError(f"expected exactly one source file named {name}, found {len(matches)}")
    return matches[0]


def registry_source_text() -> str:
    registry_files = list(SRC.glob("HextechContentRegistry*.cs"))
    content_dir = SRC / "Content"
    if content_dir.exists():
        registry_files.extend(content_dir.glob("*.cs"))
    return "\n".join(read(path) for path in sorted(set(registry_files)))


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def lower_first(value: str) -> str:
    return value[:1].lower() + value[1:]


def extract_enum_values(text: str, enum_name: str) -> list[str]:
    match = re.search(rf"enum\s+{enum_name}\s*\{{(?P<body>.*?)\n\}}", text, re.S)
    if not match:
        raise ValueError(f"enum {enum_name} not found")
    values: list[str] = []
    for line in match.group("body").splitlines():
        line = line.split("//", 1)[0].strip().rstrip(",")
        if not line:
            continue
        values.append(line.split("=", 1)[0].strip())
    return values


def extract_block(text: str, name: str) -> str:
    patterns = [
        rf"{name}\s*(?:\{{\s*get;\s*\}}\s*)?=\s*\[(?P<body>.*?)\];",
        rf"{name}\s*=\s*new\s+HashSet<[^>]+>\s*\{{(?P<body>.*?)\}};",
        rf"{name}\s*=\s*new\s+Dictionary<[^>]+>\s*\{{(?P<body>.*?)\}};",
    ]
    for pattern in patterns:
        match = re.search(pattern, text, re.S)
        if match:
            return match.group("body")
    raise ValueError(f"{name} block not found")


def extract_type_list(text: str, name: str) -> list[str]:
    return re.findall(r"typeof\((\w+)\)", extract_block(text, name))


def extract_rune_registrations(text: str) -> list[dict[str, object]]:
    registrations: list[dict[str, object]] = []
    pattern = re.compile(
        r"Rune<(?P<type>\w+)>\(\s*HextechRarityTier\.(?P<rarity>\w+)(?P<args>[^)]*)\)"
    )
    for match in pattern.finditer(text):
        args = match.group("args")
        character_pool_match = re.search(r"characterPool:\s*(?:HextechCharacterPool|PlayerRuneCharacterPool)\.(\w+)", args)
        character_order_match = re.search(r"characterOrder:\s*(\d+)", args)
        registrations.append(
            {
                "type": match.group("type"),
                "rarity": match.group("rarity"),
                "flags": set(re.findall(r"(?:RuneFlags|PlayerRuneFlags)\.(\w+)", args)),
                "character_pool": character_pool_match.group(1) if character_pool_match else None,
                "character_order": int(character_order_match.group(1)) if character_order_match else 0,
                "tag_key": (re.search(r'tagKey:\s*"([^"]+)"', args) or [None, "COMPREHENSIVE"])[1],
            }
        )
    return registrations


def extract_forge_registrations(text: str) -> list[dict[str, str]]:
    pattern = re.compile(r"Forge<(?P<type>\w+)>\(\s*HextechRarityTier\.(?P<rarity>\w+)\s*\)")
    return [
        {
            "type": match.group("type"),
            "rarity": match.group("rarity"),
        }
        for match in pattern.finditer(text)
    ]


def extract_monster_hex_registrations(text: str) -> list[dict[str, object]]:
    registrations: list[dict[str, object]] = []
    pattern = re.compile(
        r"Monster<(?P<type>\w+)>\(\s*MonsterHexKind\.(?P<kind>\w+),\s*HextechRarityTier\.(?P<rarity>\w+)(?P<args>[^)]*)\)"
    )
    for match in pattern.finditer(text):
        args = match.group("args")
        registrations.append(
            {
                "type": match.group("type"),
                "kind": match.group("kind"),
                "rarity": match.group("rarity"),
                "disabled": bool(re.search(r"disabled:\s*true", args)),
            }
        )
    return registrations


def check_duplicates(errors: list[str], label: str, values: list[str]) -> None:
    seen: set[str] = set()
    duplicates: list[str] = []
    for value in values:
        if value in seen:
            duplicates.append(value)
        seen.add(value)
    if duplicates:
        fail(errors, f"{label} has duplicates: {', '.join(sorted(set(duplicates)))}")


def validate_monster_hex_registry(errors: list[str], warnings: list[str]) -> None:
    types_text = read(SRC / "HextechTypes.cs")
    registry_text = registry_source_text()

    enum_values = extract_enum_values(types_text, "MonsterHexKind")
    monster_regs = extract_monster_hex_registrations(registry_text)
    if not monster_regs:
        fail(errors, "MonsterHexRegistrations block not found")
        return

    registry_values = [str(reg["kind"]) for reg in monster_regs]
    rarity_values = [str(reg["kind"]) for reg in monster_regs if not reg["disabled"]]
    disabled_values = [str(reg["kind"]) for reg in monster_regs if reg["disabled"]]
    check_duplicates(errors, "monster hex registry", registry_values)
    check_duplicates(errors, "monster hex rarity registry", rarity_values)
    check_duplicates(errors, "disabled monster hex registry", disabled_values)

    missing_from_rarity = sorted(set(enum_values) - set(rarity_values) - set(disabled_values))
    unknown_in_rarity = sorted(set(rarity_values) - set(enum_values))
    unknown_disabled = sorted(set(disabled_values) - set(enum_values))
    if missing_from_rarity:
        fail(errors, f"MonsterHexKind missing from rarity or disabled registry: {', '.join(missing_from_rarity)}")
    if unknown_in_rarity:
        fail(errors, f"Unknown MonsterHexKind in rarity registry: {', '.join(unknown_in_rarity)}")
    if unknown_disabled:
        fail(errors, f"Unknown MonsterHexKind in disabled registry: {', '.join(unknown_disabled)}")

    icon_pairs = {str(reg["kind"]): str(reg["type"]) for reg in monster_regs}
    missing_from_icons = sorted(set(enum_values) - set(icon_pairs))
    if missing_from_icons:
        fail(errors, f"MonsterHexKind missing from MonsterHexIconRelicTypes: {', '.join(missing_from_icons)}")

    for locale in ("zhs", "eng"):
        loc = json.loads(read(LOCALIZATION / locale / "relics.json"))
        missing: list[str] = []
        for hex_name in enum_values:
            relic_type = icon_pairs.get(hex_name)
            if relic_type is None:
                continue
            key = f"{lower_first(relic_type)}.enemyDescription"
            if key not in loc:
                missing.append(key)
        if missing:
            fail(errors, f"{locale} relics.json missing enemy descriptions: {', '.join(missing)}")


def validate_relic_registry(errors: list[str]) -> None:
    registry_text = registry_source_text()

    rune_regs = extract_rune_registrations(registry_text)
    forge_regs = extract_forge_registrations(registry_text)
    if not rune_regs:
        fail(errors, "RuneRegistrations block not found")
        return
    if not forge_regs:
        fail(errors, "ForgeRegistrations block not found")
        return

    all_types: list[str] = []

    for rarity in ("Silver", "Gold", "Prismatic"):
        values = [str(reg["type"]) for reg in rune_regs if reg["rarity"] == rarity]
        check_duplicates(errors, f"{rarity}RuneTypes", values)
        all_types.extend(values)

    for rarity in ("Silver", "Gold", "Prismatic"):
        values = [str(reg["type"]) for reg in forge_regs if reg["rarity"] == rarity]
        check_duplicates(errors, f"{rarity}ForgeTypes", values)
        all_types.extend(values)

    values = extract_type_list(registry_text, "ShopOnlyRelicTypes")
    check_duplicates(errors, "ShopOnlyRelicTypes", values)
    all_types.extend(values)

    for character_pool in ("Ironclad", "Silent", "Regent", "Defect", "Necrobinder"):
        values = [str(reg["type"]) for reg in rune_regs if reg["character_pool"] == character_pool]
        orders = [str(reg["character_order"]) for reg in rune_regs if reg["character_pool"] == character_pool]
        check_duplicates(errors, f"{character_pool}RuneTypes", values)
        check_duplicates(errors, f"{character_pool}RuneTypes character order", orders)
        missing_order = [str(reg["type"]) for reg in rune_regs if reg["character_pool"] == character_pool and reg["character_order"] == 0]
        if missing_order:
            fail(errors, f"{character_pool}RuneTypes missing character order: {', '.join(missing_order)}")

    for flag in ("Disabled", "AttributeConversionExclusive", "FirstActExcluded", "ThirdActExcluded"):
        values = [str(reg["type"]) for reg in rune_regs if flag in reg["flags"]]
        check_duplicates(errors, f"{flag} rune registry", values)

    tag_keys = sorted({str(reg["tag_key"]) for reg in rune_regs})
    for locale in ("zhs", "eng"):
        loc = json.loads(read(LOCALIZATION / locale / "relic_collection.json"))
        missing_tags = [tag_key for tag_key in tag_keys if f"HEXTECH_TAG.{tag_key}" not in loc]
        if missing_tags:
            fail(errors, f"{locale} relic_collection.json missing rune tag localization: {', '.join(missing_tags)}")

    check_duplicates(errors, "all custom relic registries", all_types)

    source_text = "\n".join(read(path) for path in source_files())
    declared_relics = set(re.findall(r"\bclass\s+(\w+)\s*:", source_text))
    missing_declarations = sorted(set(all_types) - declared_relics)
    if missing_declarations:
        fail(errors, f"registered relic types not declared: {', '.join(missing_declarations)}")


def validate_rune_file_layout(errors: list[str]) -> None:
    for path in source_files():
        text = read(path)
        rune_classes = re.findall(r"^public\s+sealed\s+class\s+(\w+Rune)\b", text, re.M)
        if len(rune_classes) > 1:
            fail(errors, f"{path.relative_to(REPO_ROOT)} contains multiple rune classes: {', '.join(rune_classes)}")
            continue

        if len(rune_classes) == 1 and path.stem != rune_classes[0]:
            fail(errors, f"{path.relative_to(REPO_ROOT)} should be named {rune_classes[0]}.cs")


def validate_enemy_hex_effect_layout(errors: list[str]) -> None:
    registry_text = registry_source_text()
    effect_registry_text = read(SRC / "EnemyHexes" / "HextechEnemyHexEffects.cs")
    monster_regs = extract_monster_hex_registrations(registry_text)
    if not monster_regs:
        fail(errors, "MonsterHexRegistrations block not found for enemy hex effect layout")
        return

    for reg in monster_regs:
        kind = str(reg["kind"])
        expected_class = f"{kind}EnemyHex"
        expected_path = SRC / "EnemyHexes" / f"{expected_class}.cs"
        if not expected_path.exists():
            fail(errors, f"enemy hex effect file missing: {expected_path.relative_to(REPO_ROOT)}")
            continue

        text = read(expected_path)
        class_pattern = rf"\binternal\s+sealed\s+class\s+{expected_class}\s*:\s*HextechEnemyHexEffect\b"
        if not re.search(class_pattern, text):
            fail(errors, f"{expected_path.relative_to(REPO_ROOT)} should declare {expected_class} : HextechEnemyHexEffect")

        kind_pattern = rf"\bKind\s*=>\s*MonsterHexKind\.{kind}\b"
        if not re.search(kind_pattern, text):
            fail(errors, f"{expected_path.relative_to(REPO_ROOT)} should bind Kind to MonsterHexKind.{kind}")

        if f"new {expected_class}()" not in effect_registry_text:
            fail(errors, f"enemy hex effect registry missing {expected_class}")


def validate_combat_tracking_state(errors: list[str]) -> None:
    state_text = read(source_file_named("HextechMayhemCombatTrackingState.cs"))
    snapshot_text = read(source_file_named("HextechMayhemCombatTrackingSnapshot.cs"))
    state_fields = {
        name: field_type
        for field_type, name in re.findall(
            r"\bpublic\s+(?:readonly\s+)?(Dictionary<[^>]+>|HashSet<[^>]+>|string\?|bool|int)\s+([A-Za-z0-9]+)",
            state_text,
        )
    }
    declared = set(state_fields)
    snapshot_fields = {
        name: field_type
        for field_type, name in re.findall(
            r"\bpublic\s+(Dictionary<[^>]+>|List<[^>]+>|int)\s+([A-Za-z0-9]+)\s*\{\s*get;\s*set;\s*\}",
            snapshot_text,
        )
    }
    persistent = set(snapshot_fields)
    transient = set(
        name
        for _, name in re.findall(
            r"\[CombatTrackingTransient\]\s*\n\s*public\s+(?:readonly\s+)?(Dictionary<[^>]+>|HashSet<[^>]+>|string\?|bool|int)\s+([A-Za-z0-9]+)",
            state_text,
        )
    )
    if not declared:
        fail(errors, "combat tracking field block not found")
        return

    if not persistent:
        fail(errors, "combat tracking snapshot properties not found")
        return

    classified = persistent | transient
    unclassified = sorted(declared - classified)
    snapshot_without_state = sorted(persistent - declared)
    transient_and_persistent = sorted(transient & persistent)
    if unclassified:
        fail(errors, f"combat tracking fields need classification: {', '.join(unclassified)}")
    if snapshot_without_state:
        fail(errors, f"combat tracking snapshot properties missing state fields: {', '.join(snapshot_without_state)}")
    if transient_and_persistent:
        fail(errors, f"combat tracking fields marked both persistent and transient: {', '.join(transient_and_persistent)}")

    for field in sorted(persistent & declared):
        state_type = state_fields[field]
        snapshot_type = snapshot_fields[field]
        if state_type.startswith("Dictionary<"):
            expected = state_type
        elif state_type.startswith("HashSet<"):
            expected = "List<" + state_type.removeprefix("HashSet<")
        else:
            expected = state_type

        if snapshot_type != expected:
            fail(errors, f"combat tracking snapshot type mismatch for {field}: expected {expected}, got {snapshot_type}")


def main() -> int:
    errors: list[str] = []
    warnings: list[str] = []
    validate_monster_hex_registry(errors, warnings)
    validate_relic_registry(errors)
    validate_rune_file_layout(errors)
    validate_enemy_hex_effect_layout(errors)
    validate_combat_tracking_state(errors)

    if errors:
        print("Hextech content validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    if warnings:
        print("Hextech content validation warnings:")
        for warning in warnings:
            print(f"- {warning}")
    print("Hextech content validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
