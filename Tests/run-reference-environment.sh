#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d)"
trap 'rm -rf "$test_dir"' EXIT
sources=("$repo_root"/Assets/Scrapshift/Core/*.cs
  "$repo_root/Assets/Scrapshift/Runtime/CompactEquipmentVisuals.cs"
  "$repo_root/Assets/Scrapshift/Runtime/CompactAutomationVisuals.cs"
  "$repo_root/Assets/Scrapshift/Runtime/CompactIndustryVisuals.cs"
  "$repo_root/Assets/Scrapshift/Runtime/CompactConveyorPortTarget.cs"
  "$repo_root/Assets/Scrapshift/Runtime/CompactYardWorld.cs"
  "$repo_root/Assets/Scrapshift/Runtime/YardGeometry.cs"
  "$repo_root/Assets/Scrapshift/Runtime/ProceduralMeshOwner.cs"
  "$repo_root/Tests/MachineryPresentationRunner.cs"
  "$repo_root/Tests/ReferenceEnvironmentRunner.cs")
if [[ -n "${SCRAPSHIFT_MONO_ROOT:-}" ]]; then
  export MONO_CFG_DIR="$SCRAPSHIFT_MONO_ROOT/etc"
  "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$SCRAPSHIFT_MONO_ROOT/usr/lib/mono/4.5/mcs.exe" -warnaserror -main:ReferenceEnvironmentRunner -out:"$test_dir/environment.exe" "${sources[@]}"
  "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$test_dir/environment.exe"
else
  mcs -warnaserror -main:ReferenceEnvironmentRunner -out:"$test_dir/environment.exe" "${sources[@]}"
  mono "$test_dir/environment.exe"
fi
