#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d)"
trap 'rm -rf "$test_dir"' EXIT
sources=("$repo_root"/Assets/Scrapshift/Core/*.cs "$repo_root"/Assets/Scrapshift/Tests/Editor/*Scenarios.cs "$repo_root/Tests/CoreRunner.cs")
run_checks() {
    local checks_output="$1"
    shift
    if [[ -n "${SCRAPSHIFT_MONO_ROOT:-}" ]]; then
        export MONO_CFG_DIR="$SCRAPSHIFT_MONO_ROOT/etc"
        "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$SCRAPSHIFT_MONO_ROOT/usr/lib/mono/4.5/mcs.exe" -warnaserror -out:"$checks_output" "$@"
        "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$checks_output"
    else
        mcs -warnaserror -out:"$checks_output" "$@"
        mono "$checks_output"
    fi
}
run_checks "$test_dir/core.exe" "${sources[@]}"
# Real SaveStore recovery/filesystem flow with a narrow adapter after field checks, not Unity JSON.
run_checks "$test_dir/recovery.exe" "$repo_root"/Assets/Scrapshift/Core/*.cs \
    "$repo_root/Assets/Scrapshift/Runtime/SaveStore.cs" "$repo_root/Tests/SaveRecoveryRunner.cs"
