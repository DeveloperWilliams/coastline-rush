#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Set UNITY_EDITOR to the executable of Unity 6000.3.25f1." >&2
  exit 1
fi
mkdir -p "$PROJECT_ROOT/Logs"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PROJECT_ROOT" -executeMethod BuildRace.Build -logFile "$PROJECT_ROOT/Logs/build.log"
