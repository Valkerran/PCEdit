#!/usr/bin/env python3
"""Reject stale in-solution project dependency versions in NuGet lock files."""
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def check(root: Path) -> list[str]:
    version = ET.parse(root / 'Directory.Build.props').findtext('.//VersionPrefix')
    projects = {p.stem.casefold() for p in root.glob('*/*.csproj')}
    errors = []
    for path in sorted(root.glob('*/packages.lock.json')):
        for framework, graph in json.loads(path.read_text()).get('dependencies', {}).items():
            for name, entry in graph.items():
                if entry.get('type', '').casefold() != 'project':
                    continue
                for dependency, recorded in entry.get('dependencies', {}).items():
                    if dependency.casefold() in projects and recorded != f'[{version}, )':
                        errors.append(f'{path.relative_to(root)} ({framework}): '
                                      f'{name} -> {dependency}: {recorded}, expected [{version}, )')
    return errors


if __name__ == '__main__':
    errors = check(ROOT)
    if errors:
        print('\n'.join(errors), file=sys.stderr)
        sys.exit('Run dotnet restore PCEdit.slnx --force-evaluate and commit the lock files.')
    print('Lock-file project references match VersionPrefix.')
