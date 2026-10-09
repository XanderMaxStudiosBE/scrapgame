#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d)"
trap 'rm -rf "$test_dir"' EXIT
python3 "$repo_root/Tests/extract-automation-checkpoint.py" "$test_dir/AutomationCheckpointProduction.cs"
sources=("$repo_root"/Assets/Scrapshift/Core/*.cs
    "$repo_root/Assets/Scrapshift/Runtime/PlayerInputSettings.cs"
    "$test_dir/AutomationCheckpointProduction.cs" "$repo_root/Tests/AutomationCheckpointRunner.cs")
if [[ -n "${SCRAPSHIFT_MONO_ROOT:-}" ]]; then
    export MONO_CFG_DIR="$SCRAPSHIFT_MONO_ROOT/etc"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$SCRAPSHIFT_MONO_ROOT/usr/lib/mono/4.5/mcs.exe" -warnaserror -out:"$test_dir/checkpoint.exe" "${sources[@]}"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$test_dir/checkpoint.exe"
else
    mcs -warnaserror -out:"$test_dir/checkpoint.exe" "${sources[@]}"
    mono "$test_dir/checkpoint.exe"
fi
