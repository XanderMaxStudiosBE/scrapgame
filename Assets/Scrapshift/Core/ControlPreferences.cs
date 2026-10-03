using System;
using System.Collections.Generic;

namespace Scrapshift
{
    public enum ControlAction { MoveForward, MoveBackward, MoveLeft, MoveRight, Interact, Drop, ManualWork, BuildToggle, BuildRotate, Journal }

    [Serializable]
    public sealed class ControlPreferences
    {
        public int version = 1;
        public string[] bindings = { "W", "S", "A", "D", "E", "Q", "Mouse0", "B", "R", "J" };
        public float sensitivity = 2;
        public bool invertY;
        static readonly string[] Defaults = { "W", "S", "A", "D", "E", "Q", "Mouse0", "B", "R", "J" };
        static readonly string[] Arrows = { "UpArrow", "DownArrow", "LeftArrow", "RightArrow" };
        public static readonly ControlAction[] Actions = (ControlAction[])Enum.GetValues(typeof(ControlAction));
        public static readonly string[] SupportedCodes = CreateSupportedCodes();
        static readonly HashSet<string> Supported = new HashSet<string>(SupportedCodes);
        static string[] CreateSupportedCodes()
        {
            var codes = new List<string>();
            for (char c = 'A'; c <= 'Z'; c++) codes.Add(c.ToString());
            for (int i = 0; i < 10; i++) { codes.Add("Alpha" + i); codes.Add("Keypad" + i); }
            for (int i = 1; i <= 12; i++) codes.Add("F" + i);
            for (int i = 0; i <= 6; i++) codes.Add("Mouse" + i);
            codes.AddRange(new[] { "UpArrow", "DownArrow", "LeftArrow", "RightArrow", "Space", "Tab", "Return", "Backspace", "Delete", "Insert", "Home", "End", "PageUp", "PageDown", "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt", "BackQuote", "Minus", "Equals", "LeftBracket", "RightBracket", "Backslash", "Semicolon", "Quote", "Comma", "Period", "Slash", "KeypadPeriod", "KeypadDivide", "KeypadMultiply", "KeypadMinus", "KeypadPlus", "KeypadEnter", "KeypadEquals", "CapsLock", "Numlock", "ScrollLock", "Pause", "Print" });
            return codes.ToArray();
        }
        public static bool IsSupported(string code) { return code != null && Supported.Contains(code); }
        public string Binding(ControlAction action) { return bindings[(int)action]; }
        public string Alias(ControlAction action)
        {
            int i = (int)action;
            if (i >= 4 || bindings[i] != Defaults[i]) return null;
            // A customized primary owns its key; never recreate an arrow alias over it.
            foreach (var other in Actions) if (other != action && Binding(other) == Arrows[i]) return null;
            return Arrows[i];
        }
        public ControlAction? Conflict(ControlAction action, string code)
        {
            foreach (var other in Actions)
                if (other != action && (Binding(other) == code || Alias(other) == code)) return other;
            return null;
        }
        public void SetBinding(ControlAction action, string code, bool swap)
        {
            if (!IsSupported(code)) throw new ArgumentException("Unsupported or reserved binding.");
            var conflict = Conflict(action, code);
            if (conflict.HasValue && !swap) throw new ArgumentException("Binding already used.");
            string old = Binding(action);
            if (conflict.HasValue) bindings[(int)conflict.Value] = old;
            bindings[(int)action] = code;
            Validate();
        }
        public void RestoreDefaults()
        {
            bindings = (string[])Defaults.Clone(); sensitivity = 2; invertY = false;
        }
        // Version one remains additive: never replace a player's seven or nine existing choices.
        // Validate a copy before committing; malformed legacy arrays still recover via backup.
        public bool UpgradeLegacyBindings()
        {
            if (version != 1 || bindings == null || (bindings.Length != 7 && bindings.Length != 9)) return false;
            int originalLength = bindings.Length;
            var upgraded = new ControlPreferences { sensitivity = sensitivity, invertY = invertY };
            Array.Copy(bindings, upgraded.bindings, originalLength);
            if (originalLength == 7)
            {
                upgraded.bindings[7] = UnusedBuildCode("B", upgraded, 7);
                upgraded.bindings[8] = UnusedBuildCode("R", upgraded, 8);
            }
            upgraded.bindings[9] = UnusedBuildCode("J", upgraded, 9);
            upgraded.Validate();
            bindings = upgraded.bindings;
            return true;
        }
        static string UnusedBuildCode(string preferred, ControlPreferences p, int populated)
        {
            if (Unused(preferred, p, populated)) return preferred;
            // Function keys make stable, accessible fallbacks without stealing movement aliases.
            for (int i = 2; i <= 12; i++) if (Unused("F" + i, p, populated)) return "F" + i;
            foreach (string code in SupportedCodes) if (Unused(code, p, populated)) return code;
            throw new ArgumentException("No supported binding available for construction.");
        }
        static bool Unused(string code, ControlPreferences p, int populated)
        {
            for (int i = 0; i < populated; i++)
                if (p.bindings[i] == code || (i < 4 && p.Alias((ControlAction)i) == code)) return false;
            return true;
        }
        public void Validate()
        {
            if (version != 1 || bindings == null || bindings.Length != Actions.Length ||
                float.IsNaN(sensitivity) || float.IsInfinity(sensitivity) || sensitivity < .1f || sensitivity > 10)
                throw new ArgumentException("Invalid controls preferences.");
            foreach (var action in Actions)
                if (!IsSupported(Binding(action)) || Conflict(action, Binding(action)).HasValue ||
                    (Alias(action) != null && Conflict(action, Alias(action)).HasValue))
                    throw new ArgumentException("Invalid or conflicting control bindings.");
        }
        public static string ActionLabel(ControlAction action)
        {
            switch (action)
            {
                case ControlAction.MoveForward: return "Move forward";
                case ControlAction.MoveBackward: return "Move backward";
                case ControlAction.MoveLeft: return "Move left";
                case ControlAction.MoveRight: return "Move right";
                case ControlAction.Interact: return "Interact";
                case ControlAction.Drop: return "Drop carried item";
                case ControlAction.BuildToggle: return "Build catalogue";
                case ControlAction.BuildRotate: return "Rotate construction";
                case ControlAction.Journal: return "Yard journal";
                default: return "Manual work";
            }
        }
        public static string CodeLabel(string code)
        {
            if (code == "Mouse0") return "Left mouse";
            if (code == "Mouse1") return "Right mouse";
            if (code == "Mouse2") return "Middle mouse";
            if (code.StartsWith("Mouse", StringComparison.Ordinal)) return "Mouse " + (int.Parse(code.Substring(5)) + 1);
            if (code.StartsWith("Alpha", StringComparison.Ordinal)) return code.Substring(5);
            if (code.EndsWith("Arrow", StringComparison.Ordinal)) return code.Substring(0, code.Length - 5) + " arrow";
            return code;
        }
    }

    // Capture and conflicts do not alter preferences until a choice is committed.
    public sealed class ControlRebind
    {
        readonly ControlPreferences preferences;
        public ControlAction? Action { get; private set; }
        public string Candidate { get; private set; }
        public ControlAction? Conflict { get; private set; }
        public bool IsCapturing { get { return Action.HasValue && Candidate == null; } }
        public ControlRebind(ControlPreferences preferences) { this.preferences = preferences; }
        public void Begin(ControlAction action) { Cancel(); Action = action; }
        public bool Capture(string code)
        {
            if (!IsCapturing || !ControlPreferences.IsSupported(code)) return false;
            Candidate = code; Conflict = preferences.Conflict(Action.Value, code);
            if (Conflict.HasValue) return false;
            preferences.SetBinding(Action.Value, code, false); Cancel(); return true;
        }
        public bool Swap()
        {
            if (!Action.HasValue || Candidate == null || !Conflict.HasValue) return false;
            preferences.SetBinding(Action.Value, Candidate, true); Cancel(); return true;
        }
        public void Cancel() { Action = null; Candidate = null; Conflict = null; }
    }
}
