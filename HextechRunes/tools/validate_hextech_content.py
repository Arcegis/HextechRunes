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
    "_playerCardsDrawnThisCombat",
    "_eightPennyGatePlayersTriggeredThisTurn",
    "_enemyProtectiveVeilTurnCounter",
}

TRACKING_TRANSIENT_FIELDS = {
    "_monsterDebuffActionProcKeysThisTurn",
    "_groupedPlayerDebuffProcKeys",
    "_eightPennyGatePendingCardHashes",
    "_lastEnemyThresholdTriggerKey",
    "_handlingMonsterTormentorBurn",
    "_handlingServantMasterIllusion",
    "_handlingGroupedPlayerDebuffs",
}


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def source_files(pattern: str = "*.cs") -> list[Path]:
    return [
        path
        for path in SRC.rglob(pattern)
        if "bin" not in path.parts and "obj" not in path.parts
    ]


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


def extract_rune_registrations(text: str) -> list[dict[str, object]]:
    registrations: list[dict[str, object]] = []
    pattern = re.compile(
        r"Rune<(?P<type>\w+)>\(\s*HextechRarityTier\.(?P<rarity>\w+)(?P<args>[^)]*)\)"
    )
    for match in pattern.finditer(text):
        args = match.group("args")
        character_pool_match = re.search(r"characterPool:\s*HextechCharacterPool\.(\w+)", args)
        character_order_match = re.search(r"characterOrder:\s*(\d+)", args)
        registrations.append(
            {
                "type": match.group("type"),
                "rarity": match.group("rarity"),
                "flags": set(re.findall(r"RuneFlags\.(\w+)", args)),
                "character_pool": character_pool_match.group(1) if character_pool_match else None,
                "character_order": int(character_order_match.group(1)) if character_order_match else 0,
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
    registry_text = read(SRC / "HextechContentRegistry.cs")

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
    registry_text = read(SRC / "HextechContentRegistry.cs")

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

    check_duplicates(errors, "all custom relic registries", all_types)

    source_text = "\n".join(read(path) for path in source_files())
    declared_relics = set(re.findall(r"\bclass\s+(\w+)\s*:", source_text))
    missing_declarations = sorted(set(all_types) - declared_relics)
    if missing_declarations:
        fail(errors, f"registered relic types not declared: {', '.join(missing_declarations)}")


def validate_combat_tracking_state(errors: list[str]) -> None:
    state_text = read(SRC / "HextechMayhem.State.cs")
    mayhem_text = "\n".join(read(path) for path in source_files("HextechMayhem*.cs"))
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
