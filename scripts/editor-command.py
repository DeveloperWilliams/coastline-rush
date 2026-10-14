#!/usr/bin/env python3
"""Queue one fixed operation for the open Unity editor, using an atomic file write."""
import argparse
import os
import tempfile
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('command', choices=(
    'refresh', 'stop', 'play', 'prepare', 'build', 'rules',
    'validate', 'validate-wet', 'validate-nova', 'assets', 'capture'))
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
request = root / 'Temp/coastline-command.txt'
request.parent.mkdir(parents=True, exist_ok=True)
if request.exists():
    parser.error('A command is already queued; wait for the editor to consume it.')
with tempfile.NamedTemporaryFile(mode='w', dir=request.parent, delete=False) as stream:
    pending = Path(stream.name)
    stream.write(args.command + '\n')
    stream.flush()
    os.fsync(stream.fileno())
try:
    pending.replace(request)
finally:
    pending.unlink(missing_ok=True)
print('Editor command queued:', args.command)
