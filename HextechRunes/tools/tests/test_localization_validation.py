#!/usr/bin/env python3
"""本地化校验的边界：拓展包纳入键/格式检查、与 eng 相同的疑似漏译、白名单。只用临时目录。"""
from __future__ import annotations

import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import validate_hextech_content as validation

LANGUAGES = ("zhs", "eng", "esp", "spa", "jpn", "kor", "ptb", "rus", "tha")


def write_tables(root: Path, table: str, per_language: dict[str, dict[str, str]]) -> None:
    for language in LANGUAGES:
        directory = root / language
        directory.mkdir(parents=True, exist_ok=True)
        entries = per_language.get(language, per_language["eng"])
        (directory / table).write_text(json.dumps(entries, ensure_ascii=False), encoding="utf-8")


class UntranslatedValueTests(unittest.TestCase):
    def test_markup_is_stripped_before_counting_latin_letters(self):
        self.assertEqual(
            validation.strip_localization_markup("[gold]{Cards:plural:{IfUpgraded:show:a|b}|cards}[/gold]x").strip(),
            "x",
        )
        # 只剩占位符/数字/少于 4 个拉丁字母时不算漏译。
        self.assertFalse(validation.looks_untranslated("[blue]{Damage}[/blue] HP", "[blue]{Damage}[/blue] HP"))
        self.assertFalse(validation.looks_untranslated("Hextech+", "Hextech"))
        self.assertTrue(validation.looks_untranslated("Enable mod", "Enable mod"))
        self.assertFalse(validation.looks_untranslated("Activar mod", "Enable mod"))
        self.assertFalse(validation.looks_untranslated("Enable mod", None))

    def test_main_pack_reports_errors_and_honours_allowlist(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            write_tables(root, "relics.json", {
                "eng": {"A.title": "Omega", "B.title": "Enable mod", "C.title": "Slow"},
                "zhs": {"A.title": "欧米伽", "B.title": "启用模组", "C.title": "缓慢"},
                "esp": {"A.title": "Omega", "B.title": "Enable mod", "C.title": "Lentitud"},
            })
            allowlist = {
                (validation.MOD_ID, "esp", "relics.json", "A.title"): "same word",
                (validation.MOD_ID, "esp", "relics.json", "C.title"): "stale",
            }
            errors: list[str] = []
            warnings: list[str] = []
            validation.validate_untranslated_values(errors, warnings, root, validation.MOD_ID, allowlist)
            self.assertEqual(warnings, [])
            # jpn/kor/... 默认抄了 eng，也都要报；esp 的 A 被白名单豁免，C 已翻译所以白名单条目过期。
            self.assertTrue(any("esp/relics.json:B.title" in error for error in errors))
            self.assertFalse(any("esp/relics.json:A.title" in error for error in errors))
            self.assertTrue(any("tha/relics.json:A.title" in error for error in errors))
            self.assertTrue(any("stale entry" in error and "esp/relics.json/C.title" in error for error in errors))
            self.assertFalse(any("zhs/" in error for error in errors))

    def test_sponsor_pack_only_warns_and_groups_by_file(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            write_tables(root, "events.json", {
                "eng": {"E.title": "Miracle", "E.description": "Pick one."},
                "zhs": {"E.title": "奇迹", "E.description": "选一个。"},
            })
            errors: list[str] = []
            warnings: list[str] = []
            with patch.dict(validation.UNTRANSLATED_SEVERITY, {validation.SPONSOR_PACK: "warning"}):
                validation.validate_untranslated_values(errors, warnings, root, validation.SPONSOR_PACK, {})
            self.assertEqual(errors, [])
            tha = [warning for warning in warnings if "tha/events.json" in warning]
            self.assertEqual(len(tha), 1)
            self.assertIn(validation.SPONSOR_PACK, tha[0])
            self.assertIn("2 value(s)", tha[0])
            # 合并后把严重级别切到 error，同样的数据就会逐条报错。
            with patch.dict(validation.UNTRANSLATED_SEVERITY, {validation.SPONSOR_PACK: "error"}):
                errors, warnings = [], []
                validation.validate_untranslated_values(errors, warnings, root, validation.SPONSOR_PACK, {})
            self.assertEqual(warnings, [])
            self.assertTrue(any("HextechRunesSponsorPack:tha/events.json:E.title" in error for error in errors))

    def test_allowlist_requires_reason_and_rejects_duplicates(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "allow.json"
            entry = {"pack": validation.MOD_ID, "language": "tha", "table": "powers.json", "key": "X.title", "reason": "fallback"}
            path.write_text(json.dumps({"entries": [entry, dict(entry), dict(entry, key="Y.title", reason=" ")]}), encoding="utf-8")
            errors: list[str] = []
            allowlist = validation.load_untranslated_allowlist(errors, path)
            self.assertEqual(set(allowlist), {(validation.MOD_ID, "tha", "powers.json", "X.title")})
            self.assertTrue(any("duplicate entry" in error for error in errors))
            self.assertTrue(any("needs pack, language, table, key and reason" in error for error in errors))

    def test_repository_allowlist_entries_all_have_reasons(self):
        errors: list[str] = []
        allowlist = validation.load_untranslated_allowlist(errors)
        self.assertEqual(errors, [])
        self.assertTrue(allowlist)
        self.assertTrue(all(pack in validation.UNTRANSLATED_SEVERITY for pack, *_ in allowlist))


class SponsorPackLocalizationTests(unittest.TestCase):
    def test_sponsor_pack_joins_key_and_format_checks_with_prefixed_labels(self):
        with tempfile.TemporaryDirectory() as folder:
            main_root = Path(folder) / "main"
            sponsor_root = Path(folder) / "sponsor"
            write_tables(main_root, "relics.json", {"eng": {"A.title": "A"}})
            write_tables(sponsor_root, "relics.json", {
                "eng": {"S.description": "Gain [blue]{Amount}[/blue]."},
                "zhs": {"S.description": "获得[blue]{Amount}[/blue]。"},
                "esp": {"S.description": "Obtén [blue]{Cantidad}[/blue]."},
                "tha": {"S.description": "ได้รับ [blue]{Amount}", "S.extra": "x"},
            })
            with patch.object(validation, "LOCALIZATION", main_root), \
                 patch.object(validation, "SPONSOR_LOCALIZATION", sponsor_root):
                roots = validation.localization_roots()
                self.assertEqual([pack for pack, _ in roots], [validation.MOD_ID, validation.SPONSOR_PACK])
                errors: list[str] = []
                for pack, root in roots:
                    validation.validate_localization_key_parity(errors, root, pack)
                    validation.validate_localization_format_parity(errors, root, pack)
            self.assertTrue(any(error.startswith("HextechRunesSponsorPack:tha/relics.json extra keys") for error in errors))
            self.assertTrue(any("HextechRunesSponsorPack:esp/relics.json:S.description: placeholders" in error for error in errors))
            self.assertTrue(any("HextechRunesSponsorPack:tha/relics.json:S.description: BBCode unbalanced" in error for error in errors))
            self.assertFalse(any(error.startswith("tha/") or error.startswith("esp/") for error in errors))

    def test_missing_sponsor_directory_is_skipped(self):
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(validation, "SPONSOR_LOCALIZATION", Path(folder) / "absent"):
                self.assertEqual([pack for pack, _ in validation.localization_roots()], [validation.MOD_ID])


if __name__ == "__main__":
    unittest.main()
