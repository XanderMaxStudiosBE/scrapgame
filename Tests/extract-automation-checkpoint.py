#!/usr/bin/env python3
"""Extract unchanged coordinator scheduling/persistence methods for a controlled API check.

Unity's player, views, input/clock APIs and the durable writer are explicit seams.
This does not compile or execute the Unity engine or its JSON serializer.
"""
import pathlib
import re
import sys


def method(source, name):
    matches = list(re.finditer(
        r"^        (?:public )?(?:static )?(?:void|bool|string|int|CompactPowerStatus) "
        + re.escape(name) + r"\([^\n]*\)\s*\{", source, re.MULTILINE))
    if len(matches) != 1:
        raise SystemExit(f"Expected one production method {name}; found {len(matches)}")
    start = matches[0].start()
    i = source.index("{", start)
    depth, state = 0, "code"
    while i < len(source):
        c, pair = source[i], source[i:i + 2]
        if state == "code":
            if pair == "//":
                state = "line"; i += 2; continue
            if pair == "/*":
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
            if pair == "*/":
                state = "code"; i += 2; continue
        elif c == "\\":
            i += 2; continue
        elif c == state:
            state = "code"
        i += 1
    raise SystemExit(f"Unclosed method {name}")


root = pathlib.Path(__file__).resolve().parent.parent
# Reuse the repository's declared controlled API values without its game shell
# or its tests. The scheduling methods below always come from current runtime.
api_source = (root / "Tests/PortInteractionRunner.cs").read_text()
api = api_source.split("namespace Scrapshift.Compact\n{", 1)[0]
if "namespace UnityEngine\n{" not in api or "public sealed class FirstPersonController" not in api:
    raise SystemExit("Controlled API definitions changed; review extraction")
api = api.replace("public sealed class PresentationSettings {public void UpdatePending(float now){}}",
                  "public sealed class PresentationSettings {public void UpdatePending(float now){}public bool FlushPending(){return true;}}")
api = api.replace("public void UpdateCapture(){}", "public void UpdateCapture(){}public void Close(){IsOpen=false;}")
output = ["// Generated source/API fixture; Unity execution remains unverified.", api,
          "namespace Scrapshift.Compact { public sealed partial class CompactYardGame {"]
names = ["Update", "Act", "IndustryAct", "ManualAct", "Pause", "Show", "FinishWelcome", "Tell",
         "QueuePassiveProgress", "RefreshPassiveCareer", "UpdateWorkCheckpoint", "SavePendingWork",
         "SaveNow", "OnApplicationFocus", "OnApplicationQuit", "RebuildPower", "PowerStatus"]
source = (root / "Assets/Scrapshift/Runtime/CompactYardGame.cs").read_text()
for name in names:
    output.append("// Source: Assets/Scrapshift/Runtime/CompactYardGame.cs / " + name)
    output.append(method(source, name))
guidance = (root / "Assets/Scrapshift/Runtime/CompactYardGame.Guidance.cs").read_text()
output.append(method(guidance, "RefreshCareer"))
output.append("} }")
pathlib.Path(sys.argv[1]).write_text("\n".join(output) + "\n")
print(f"Extracted {len(names) + 1} actual coordinator scheduling/save methods; controlled Unity and writer seams.")
