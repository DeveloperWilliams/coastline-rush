#!/usr/bin/env python3
"""Check Unity checkout integrity without downloading or launching the editor."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors = []
assets = ROOT / 'Assets'
guids = {}
for path in sorted(assets.rglob('*')):
    if path.name.startswith('.') or path.suffix == '.meta':
        continue
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        errors.append(f'Missing meta: {path.relative_to(ROOT)}')
        continue
    match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.MULTILINE)
    if not match:
        errors.append(f'Missing GUID: {meta.relative_to(ROOT)}')
    elif match.group(1) in guids:
        errors.append(f'Duplicate GUID: {meta.relative_to(ROOT)} and {guids[match.group(1)]}')
    else:
        guids[match.group(1)] = str(meta.relative_to(ROOT))
    if path.is_file() and path.stat().st_size > 50 * 1024 * 1024:
        errors.append(f'Asset exceeds 50 MB: {path.relative_to(ROOT)}')

manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text())
for name, version in manifest['dependencies'].items():
    if lock['dependencies'].get(name, {}).get('version') != version:
        errors.append(f'Package lock mismatch: {name}')

for pack in ('car-kit', 'racing-kit', 'nature-kit'):
    license_file = assets / 'ThirdParty/Kenney' / pack / 'License.txt'
    if not license_file.exists() or 'CC0' not in license_file.read_text():
        errors.append(f'Missing original CC0 license: {pack}')

font_license = assets / 'ThirdParty/JosefinSans/OFL.txt'
if not font_license.exists() or 'SIL OPEN FONT LICENSE Version 1.1' not in font_license.read_text():
    errors.append('Missing original Josefin Sans OFL license')
for weight in ('regular', 'semibold', 'bold'):
    if not (assets / f'ThirdParty/JosefinSans/Resources/Fonts/JosefinSans-{weight}.ttf').exists():
        errors.append(f'Missing Josefin Sans font: {weight}')
for sound in ('engine-idle', 'engine-load', 'tyre-scrub', 'road-wind', 'nitro-air', 'impact', 'countdown', 'go', 'lap', 'finish', 'click'):
    if not (assets / f'Resources/Audio/{sound}.wav').exists():
        errors.append(f'Missing original sound asset: {sound}')

if (assets / 'StreamingAssets/Licenses/JosefinSans-OFL.txt').read_bytes() != font_license.read_bytes():
    errors.append('Standalone font license differs from original OFL')

for scene in assets.rglob('*.unity'):
    for guid in re.findall(r'guid: ([a-f0-9]{32})', scene.read_text()):
        if guid not in guids and not guid.startswith('0000000000000000'):
            errors.append(f'Unresolved scene GUID: {scene.relative_to(ROOT)} / {guid}')

for asset in assets.rglob('*.asmdef'):
    json.loads(asset.read_text())

if errors:
    print('\n'.join(errors), file=sys.stderr)
    sys.exit(1)
print(f'PASS: {len(guids)} unique asset GUIDs, package lock, scene references, artwork/font licenses and audio assets')
