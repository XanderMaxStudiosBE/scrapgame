#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d)"
trap 'rm -rf "$test_dir"' EXIT
python3 "$repo_root/Tests/extract-port-interaction.py" "$test_dir/PortInteractionProduction.cs"
sources=("$repo_root"/Assets/Scrapshift/Core/*.cs
    "$repo_root/Assets/Scrapshift/Runtime/CompactBuildMode.cs"
    "$repo_root/Assets/Scrapshift/Runtime/CompactInteractionTarget.cs"
    "$repo_root/Assets/Scrapshift/Runtime/CompactConveyorPortTarget.cs"
    "$repo_root/Assets/Scrapshift/Runtime/PlayerInputSettings.cs"
    "$test_dir/PortInteractionProduction.cs" "$repo_root/Tests/PortInteractionRunner.cs")
if [[ -n "${SCRAPSHIFT_MONO_ROOT:-}" ]]; then
    export MONO_CFG_DIR="$SCRAPSHIFT_MONO_ROOT/etc"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$SCRAPSHIFT_MONO_ROOT/usr/lib/mono/4.5/mcs.exe" -warnaserror -out:"$test_dir/port-interaction.exe" "${sources[@]}"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$test_dir/port-interaction.exe"
else
    mcs -warnaserror -out:"$test_dir/port-interaction.exe" "${sources[@]}"
    mono "$test_dir/port-interaction.exe"
fi
