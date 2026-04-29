#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SRC = REPO_ROOT / "src"
LOCALIZATION = REPO_ROOT / "assets" / "localization"

TRACKING_PERSISTENT_FIELDS = {
    "_slapProcsThisTurn",
    "_tormentorProcsThisTurn",
    "_courageProcsThisTurn",
    "_bloodPactProcsThisTurn",
    "_clownCollegeProcsThisTurn",
    "_escapePlanTriggered",
    "_escapePlanPending",
    "_repulsorTriggered",
    "_repulsorPending",
    "_dawnTriggered",
    "_speedDemonPending",
    "_devilsDanceTriggeredThisTurn",
    "_feelTheBurnTriggered",
    "_feyMagicPendingNoDrawPlayers",
    "_mikaelsBlessingTriggers",
    "_goliathApplied",
    "_protectiveVeilApplied",
    "_thornmailApplied",
    "_superBrainApplied",
    "_astralBodyApplied",
    "_drawYourSwordApplied",
    "_madScientistApplied",
    "_unmovableMountainApplied",
    "_goldenSpatulaApplied",
    "_tankEngineStacks",
    "_shrinkEngineStacks",
    "_getExcitedPending",
    "_feelTheBurnPending",
    "_mountainSoulHasPreviousTurn",
    "_mountainSoulDamagedSinceLastTurn",
    "_playerAttackCardsPlayedThisCombat",
    "_enemyProtectiveVeilTurnCounter",
}

TRACKING_TRANSIENT_FIELDS = {
    "_monsterDebuffActionProcKeysThisTurn",
    "_groupedPlayerDebuffProcKeys",
    "_lastEnemyThresholdTriggerKey",
    "_handlingMonsterTormentorBurn",
    "_handlingServantMasterIllusion",
    "_handlingGroupedPlayerDebuffs",
}


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


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
        rf"{name}\s*=\s*\[(?P<body>.*?)\];",
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


def extract_monster_hex_list(text: str, name: str) -> list[str]:
    return re.findall(r"MonsterHexKind\.(\w+)", extract_block(text, name))


def extract_monster_hex_icon_pairs(text: str) -> dict[str, str]:
    block = extract_block(text, "MonsterHexIconRelicTypes")
    return dict(re.findall(r"\{\s*MonsterHexKind\.(\w+)\s*,\s*typeof\((\w+)\)\s*\}", block))


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
    registry_text = read(SRC / "HextechContentRegistry.cs")

    enum_values = extract_enum_values(types_text, "MonsterHexKind")
    rarity_values = (
        extract_monster_hex_list(registry_text, "SilverMonsterHexes")
        + extract_monster_hex_list(registry_text, "GoldMonsterHexes")
        + extract_monster_hex_list(registry_text, "PrismaticMonsterHexes")
    )
    disabled_values = extract_monster_hex_list(registry_text, "DisabledMonsterHexes")
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

    icon_pairs = extract_monster_hex_icon_pairs(registry_text)
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
    registry_text = read(SRC / "HextechContentRegistry.cs")
    list_names = [
        "SilverRuneTypes",
        "GoldRuneTypes",
        "PrismaticRuneTypes",
        "SilverForgeTypes",
        "GoldForgeTypes",
        "PrismaticForgeTypes",
        "ShopOnlyRelicTypes",
    ]
    all_types: list[str] = []
    for name in list_names:
        values = extract_type_list(registry_text, name)
        check_duplicates(errors, name, values)
        all_types.extend(values)

    check_duplicates(errors, "all custom relic registries", all_types)

    source_text = "\n".join(read(path) for path in SRC.glob("*.cs"))
    declared_relics = set(re.findall(r"\bclass\s+(\w+)\s*:", source_text))
    missing_declarations = sorted(set(all_types) - declared_relics)
    if missing_declarations:
        fail(errors, f"registered relic types not declared: {', '.join(missing_declarations)}")


def validate_combat_tracking_state(errors: list[str]) -> None:
    state_text = read(SRC / "HextechMayhem.State.cs")
    mayhem_text = "\n".join(read(path) for path in SRC.glob("HextechMayhem*.cs"))
    tracking_decl_match = re.search(
        r"private readonly Dictionary<uint, int> _slapProcsThisTurn = new\(\);(?P<body>.*?)private int _enemyProtectiveVeilTurnCounter;",
        state_text,
        re.S,
    )
    if not tracking_decl_match:
        fail(errors, "combat tracking field block not found")
        return

    declared = {"_slapProcsThisTurn", "_enemyProtectiveVeilTurnCounter"}
    declared.update(re.findall(r"\b(_[A-Za-z0-9]+)\b", tracking_decl_match.group("body")))
    classified = TRACKING_PERSISTENT_FIELDS | TRACKING_TRANSIENT_FIELDS
    unclassified = sorted(declared - classified)
    stale_classification = sorted(classified - declared)
    if unclassified:
        fail(errors, f"combat tracking fields need classification: {', '.join(unclassified)}")
    if stale_classification:
        fail(errors, f"combat tracking classification references missing fields: {', '.join(stale_classification)}")

    for field in sorted(TRACKING_PERSISTENT_FIELDS):
        occurrences = mayhem_text.count(field)
        if field == "_enemyProtectiveVeilTurnCounter":
            minimum = 4
        else:
            minimum = 5
        if occurrences < minimum:
            fail(errors, f"{field} may be missing serialize/restore/has/clear coverage; occurrences={occurrences}")

    for field in sorted(TRACKING_TRANSIENT_FIELDS):
        if mayhem_text.count(field) < 2:
            fail(errors, f"{field} may be missing clear/reset coverage")


def main() -> int:
    errors: list[str] = []
    warnings: list[str] = []
    validate_monster_hex_registry(errors, warnings)
    validate_relic_registry(errors)
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
