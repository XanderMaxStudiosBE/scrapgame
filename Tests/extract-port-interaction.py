#!/usr/bin/env python3
"""Copy production coordinator methods unchanged into the portable API harness.

The runner supplies controlled Unity/presentation APIs. This is a source/API
check, not a Unity compile, physics, rendering, input-backend or build result.
Extraction fails if a requested method is missing or ambiguous.
"""
import pathlib
import re
import sys


def method(source, name):
    matches = list(re.finditer(
        r"^        (?:public )?(?:static )?(?:void|bool|string|int|Vector3|LineRenderer|CompactPowerStatus) "
        + re.escape(name) + r"\([^\n]*\)\s*\{", source, re.MULTILINE))
    if len(matches) != 1:
        raise SystemExit(f"Expected exactly one production method {name}; found {len(matches)}")
    start = matches[0].start()
    i = source.index("{", matches[0].start())
    depth, state = 0, "code"
    while i < len(source):
        c = source[i]
        n = source[i:i + 2]
        if state == "code":
            if n == "//":
                state = "line"; i += 2; continue
            if n == "/*":
                state = "comment"; i += 2; continue
            if c in '\"\'':
                state = c
            elif c == "{":
                depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    return source[start:i + 1]
        elif state == "line":
            if c == "\n":
                state = "code"
        elif state == "comment":
            if n == "*/":
                state = "code"; i += 2; continue
        elif c == "\\":
            i += 2; continue
        elif c == state:
            state = "code"
        i += 1
    raise SystemExit(f"Unclosed production method {name}")


root = pathlib.Path(__file__).resolve().parent.parent
runtime = root / "Assets/Scrapshift/Runtime"
methods = {
    "CompactYardGame.cs": ["Update", "UpdateTarget", "Interact", "Act", "Pause", "Show", "Back", "FinishWelcome", "Tell", "PowerStatus", "QueuePassiveProgress", "RefreshPassiveCareer", "UpdateWorkCheckpoint", "SavePendingWork"],
    "CompactYardGame.Views.cs": ["Hint", "EquipmentBayStatus", "ConveyorEndpointLabel", "ConveyorRouteLabel", "ConnectedPortStatus", "ScrapStage", "CancelBuild", "DestroyOwnedView", "SyncCables", "PowerPort"],
    "CompactYardGame.Automation.cs": ["BeginBelt", "UpdateBeltBuild", "CreateBeltPreviewLine", "PreviewBelt", "PreviewBeltSocket", "NearestPort", "BeltAvoidsWorld", "DestroyBeltPreview", "AutomationAct", "DrawAutomation"],
}
output = ["// Generated directly from production; do not edit.", "using System;", "using System.Collections.Generic;", "using UnityEngine;", "using UnityEngine.Rendering;", "using Object = UnityEngine.Object;", "namespace Scrapshift.Compact { public sealed partial class CompactYardGame {"]
for filename, names in methods.items():
    source = (runtime / filename).read_text()
    for name in names:
        output.append(f"// Source: Assets/Scrapshift/Runtime/{filename} / {name}")
        output.append(method(source, name))
output.append("} }")
helpers = {
    "CompactEquipmentVisuals": ["GeneratorPowerSocket", "Tier1PowerSocket"],
    "CompactAutomationVisuals": ["Tier2PowerSocket"],
    "CompactIndustryVisuals": ["PrimaryPowerSocket", "ExportPowerSocket"],
}
for class_name, names in helpers.items():
    source = (runtime / (class_name + ".cs")).read_text()
    output.append("namespace Scrapshift.Compact { public static partial class " + class_name + " {")
    if class_name == "CompactIndustryVisuals":
        constants = re.search(r"^        const float PrimaryWidth[^\n]*", source, re.MULTILINE)
        if constants is None:
            raise SystemExit("Missing production industry socket dimensions")
        output.append(constants.group())
    for name in names:
        output.append("// Source: Assets/Scrapshift/Runtime/" + class_name + ".cs / " + name)
        output.append(method(source, name))
    output.append("} }")
pathlib.Path(sys.argv[1]).write_text("\n".join(output) + "\n")
print("Extracted " + str(sum(map(len, methods.values()))) + " coordinator methods and " + str(sum(map(len, helpers.values()))) + " power-socket helpers unchanged from production.")
