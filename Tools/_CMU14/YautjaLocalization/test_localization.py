from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from .audit import (
    derive_entity_keys,
    extract_placeholders,
    parse_fluent,
)


class YautjaLocalizationAuditTests(unittest.TestCase):
    def test_parse_fluent_messages_and_attributes(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "sample.ftl"
            path.write_text(
                "sample = {$user} has {$count} trophies.\n"
                "    .desc = {$user} owns them.\n",
                encoding="utf-8",
            )

            messages = parse_fluent(path)

        self.assertIn("sample", messages)
        self.assertIn("sample.desc", messages)
        self.assertEqual({"user", "count"}, messages["sample"].placeholders)
        self.assertEqual({"user"}, messages["sample.desc"].placeholders)

    def test_extract_placeholders_supports_selectors(self) -> None:
        self.assertEqual(
            {"target", "seconds"},
            extract_placeholders("{$target} ({$seconds ->[one] second *[other] seconds})"),
        )

    def test_derive_entity_keys_from_yaml_fields(self) -> None:
        self.assertEqual(
            {"ent-CMUTest", "ent-CMUTest.desc"},
            derive_entity_keys({"type": "entity", "id": "CMUTest", "name": "Test", "description": "Desc"}),
        )

    def test_repository_has_complete_yautja_localization(self) -> None:
        from .audit import audit_repository

        root = Path(__file__).resolve().parents[3]
        result = audit_repository(root)

        self.assertEqual([], result.errors, "\n".join(result.errors))


if __name__ == "__main__":
    unittest.main()
