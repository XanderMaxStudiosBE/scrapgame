#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
test_dir="$(mktemp -d)"
trap 'rm -rf "$test_dir"' EXIT

# Compile the actual declaration code, omitting the Unity object builders.
# Extract fixed reservations from Core so this check cannot silently retain obsolete bounds.
python3 - "$repo_root" "$test_dir" <<'PY'
from pathlib import Path
import re
import sys

repo_root, test_dir = map(Path, sys.argv[1:])
stock = (repo_root / 'Assets/Scrapshift/Runtime/CompactYardClutter.cs').read_text()
declarations = stock[:stock.index('        public static bool IsBlocked')]
declarations = declarations.replace('using UnityEngine.Rendering;', '') + '    }\n}\n'
(test_dir / 'StockDeclarations.cs').write_text(declarations)

construction = (repo_root / 'Assets/Scrapshift/Core/CompactConstruction.cs').read_text()
protected = construction[construction.index('        internal static bool Protected('):construction.index('        public bool Place(')]
footprints = re.findall(r'new\s+Footprint\(([^()]*)\)', protected)
if not footprints:
    raise RuntimeError('No protected construction footprints found; update the extraction before running.')
for footprint in footprints:
    if len(footprint.split(',')) != 5:
        raise RuntimeError('Protected footprint signature changed; update the extraction before running.')
fixed = '\n'.join('            new StarterFloorRect(' + footprint + ',0),' for footprint in footprints)
(test_dir / 'FixedReservations.cs').write_text('''internal static class StarterFloorFixedReservations
{
    internal static StarterFloorRect[] Describe()
    {
        return new[]
        {
''' + fixed + '''
        };
    }
}
''')
PY

sources=("$repo_root"/Assets/Scrapshift/Core/*.cs "$repo_root/Tests/StarterFloorRunner.cs" "$test_dir/StockDeclarations.cs" "$test_dir/FixedReservations.cs")
if [[ -n "${SCRAPSHIFT_MONO_ROOT:-}" ]]; then
    export MONO_CFG_DIR="$SCRAPSHIFT_MONO_ROOT/etc"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$SCRAPSHIFT_MONO_ROOT/usr/lib/mono/4.5/mcs.exe" -warnaserror -out:"$test_dir/floor.exe" "${sources[@]}"
    "$SCRAPSHIFT_MONO_ROOT/usr/bin/mono-sgen" "$test_dir/floor.exe"
else
    mcs -warnaserror -out:"$test_dir/floor.exe" "${sources[@]}"
    mono "$test_dir/floor.exe"
fi
