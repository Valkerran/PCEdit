"""Regression coverage for project-version metadata that NuGet locked mode ignores."""
import json
from pathlib import Path
import tempfile
import unittest

from check_lock_versions import check


class LockVersionsTests(unittest.TestCase):
    def test_all_frameworks_and_case_insensitive_project_names(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'Directory.Build.props').write_text('<Project><PropertyGroup><VersionPrefix>1.2.3</VersionPrefix></PropertyGroup></Project>')
            for name in ('App', 'Core'):
                (root / name).mkdir()
                (root / name / f'{name}.csproj').touch()
            graph = {'app': {'type': 'Project', 'dependencies': {'CORE': '[1.2.3, )', 'External': '[9.0.0, )'}}}
            lock = root / 'App/packages.lock.json'
            lock.write_text(json.dumps({'dependencies': {'net10.0': graph, 'net10.0/linux-x64': graph}}))
            self.assertEqual([], check(root))
            graph['app']['dependencies']['CORE'] = '[1.0.0, )'
            lock.write_text(json.dumps({'dependencies': {'net10.0': graph, 'net10.0/linux-x64': graph}}))
            errors = check(root)
            self.assertEqual(2, len(errors))
            self.assertTrue(all('expected [1.2.3, )' in error for error in errors))


if __name__ == '__main__':
    unittest.main()
