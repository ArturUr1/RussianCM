from pathlib import Path
import unittest

from .audit import audit_repository


class YautjaLocalizationAuditTests(unittest.TestCase):
    def test_repository_has_complete_yautja_localization(self) -> None:
        root = Path(__file__).resolve().parents[3]
        result = audit_repository(root)

        self.assertEqual([], result.errors, "\n".join(result.errors))


if __name__ == "__main__":
    unittest.main()
