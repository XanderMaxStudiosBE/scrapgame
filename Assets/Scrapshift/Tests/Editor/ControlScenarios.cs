using System;

namespace Scrapshift.Tests
{
    public static class ControlScenarios
    {
        public static readonly string[] Names = { "ControlDefaults", "ControlRebindAndCancel", "ControlConflicts", "ControlArrowConflicts", "ControlRestoreDefaults", "ControlInvalidPreferences", "ControlLabels", "JournalControlsMigration", "JournalControlsMalformedMigration" };
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; }
            throw new Exception("Invalid preferences accepted.");
        }
        public static void Run(string name)
        {
            var p = new ControlPreferences(); var r = new ControlRebind(p);
            switch (name)
            {
                case "ControlDefaults":
                    Check(p.Binding(ControlAction.MoveForward) == "W" && p.Binding(ControlAction.ManualWork) == "Mouse0", "original controls");
                    Check(p.Alias(ControlAction.MoveForward) == "UpArrow" && p.Alias(ControlAction.MoveLeft) == "LeftArrow", "arrow aliases");
                    Check(p.sensitivity == 2 && !p.invertY, "original look"); break;
                case "ControlRebindAndCancel":
                    r.Begin(ControlAction.Interact); Check(!r.Capture("Escape") && r.IsCapturing && p.Binding(ControlAction.Interact) == "E", "escape reserved");
                    r.Cancel(); Check(!r.Action.HasValue && p.Binding(ControlAction.Interact) == "E", "cancel no changes");
                    r.Begin(ControlAction.Interact); Check(r.Capture("F") && p.Binding(ControlAction.Interact) == "F", "keyboard immediate");
                    r.Begin(ControlAction.ManualWork); Check(r.Capture("Mouse1") && p.Binding(ControlAction.ManualWork) == "Mouse1", "mouse immediate"); break;
                case "ControlConflicts":
                    r.Begin(ControlAction.Interact); Check(!r.Capture("Q") && r.Conflict == ControlAction.Drop, "detect conflict");
                    Check(p.Binding(ControlAction.Interact) == "E" && p.Binding(ControlAction.Drop) == "Q", "pending conflict changes nothing");
                    r.Cancel(); Check(p.Binding(ControlAction.Drop) == "Q", "cancel conflict");
                    r.Begin(ControlAction.Interact); r.Capture("Q"); Check(r.Swap(), "swap resolves");
                    Check(p.Binding(ControlAction.Interact) == "Q" && p.Binding(ControlAction.Drop) == "E", "both remain bound");
                    r.Begin(ControlAction.ManualWork); Check(!r.Capture("Q") && r.Swap(), "mouse keyboard swap");
                    Check(p.Binding(ControlAction.Interact) == "Mouse0", "mouse assigned other action"); break;
                case "ControlArrowConflicts":
                    r.Begin(ControlAction.Interact); Check(!r.Capture("UpArrow") && r.Conflict == ControlAction.MoveForward, "alias conflict");
                    Check(r.Swap() && p.Binding(ControlAction.MoveForward) == "E" && p.Alias(ControlAction.MoveForward) == null, "alias swap");
                    p.SetBinding(ControlAction.MoveForward, "W", false);
                    Check(p.Alias(ControlAction.MoveForward) == null && p.Binding(ControlAction.Interact) == "UpArrow", "restored primary never steals occupied alias"); break;
                case "ControlRestoreDefaults":
                    p.SetBinding(ControlAction.ManualWork, "Space", false); p.sensitivity = 5; p.invertY = true; p.RestoreDefaults();
                    Check(p.Binding(ControlAction.ManualWork) == "Mouse0" && p.Alias(ControlAction.MoveBackward) == "DownArrow" && p.sensitivity == 2 && !p.invertY, "complete restore"); break;
                case "ControlInvalidPreferences":
                    Reject(() => p.SetBinding(ControlAction.Interact, "Escape", false));
                    Reject(() => p.SetBinding(ControlAction.Interact, "Q", false));
                    p.bindings[4] = "Mouse0"; Reject(p.Validate);
                    p.RestoreDefaults(); p.sensitivity = float.NaN; Reject(p.Validate);
                    p.RestoreDefaults(); p.version = 99; Reject(p.Validate);
                    p.version = 1; p.RestoreDefaults(); p.bindings = new string[0]; Reject(p.Validate);
                    p.RestoreDefaults(); Check(!r.Capture("JoystickButton0"), "unsupported input ignored"); break;
                case "ControlLabels":
                    Check(ControlPreferences.CodeLabel("Mouse0") == "Left mouse" && ControlPreferences.CodeLabel("Alpha8") == "8", "readable labels");
                    p.SetBinding(ControlAction.Interact, "F", false); Check(ControlPreferences.CodeLabel(p.Binding(ControlAction.Interact)) == "F", "dynamic label"); break;
                case "JournalControlsMigration":
                    p.bindings = new[] { "W", "S", "A", "D", "J", "Q", "Mouse1", "B", "R" };
                    p.sensitivity=4;p.invertY=true;
                    Check(p.UpgradeLegacyBindings(), "nine bindings upgraded");
                    Check(p.Binding(ControlAction.Interact)=="J" && p.Binding(ControlAction.Journal)=="F2", "journal does not steal existing J");
                    Check(p.Binding(ControlAction.ManualWork)=="Mouse1" && p.sensitivity==4 && p.invertY, "look and mouse preferences retained");
                    Check(!p.UpgradeLegacyBindings(), "migration once only");
                    p.bindings = new[] { "W", "S", "A", "D", "E", "Q", "Mouse0" };
                    Check(p.UpgradeLegacyBindings() && p.Binding(ControlAction.BuildToggle)=="B" && p.Binding(ControlAction.Journal)=="J", "seven actions upgraded together");
                    r.Begin(ControlAction.Journal);Check(!r.Capture("E") && r.Conflict==ControlAction.Interact && r.Swap(), "journal conflict swap");
                    Check(p.Binding(ControlAction.Journal)=="E" && p.Binding(ControlAction.Interact)=="J", "journal dynamic binding");break;
                case "JournalControlsMalformedMigration":
                    p.bindings = new[] { "W", "S", "A", "D", "Escape", "Q", "Mouse0", "B", "R" };
                    string[] original=p.bindings;
                    Reject(()=>p.UpgradeLegacyBindings());Check(ReferenceEquals(original,p.bindings) && p.bindings.Length==9, "bad old preferences unchanged for recovery");
                    p.RestoreDefaults();break;
                default: throw new Exception("Unknown control scenario: " + name);
            }
            p.Validate();
        }
    }
}
