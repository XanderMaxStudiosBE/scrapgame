using UnityEngine;

namespace Scrapshift
{
    // IMGUI matches the existing prototype HUD; game coordinator owns pause/cursor state.
    public sealed class SettingsMenu
    {
        readonly PlayerInputSettings input;
        readonly ControlRebind rebind;
        readonly PresentationSettings presentation;
        int page;
        GUIStyle wrap;
        bool captureArmed;
        int captureFrame;
        bool discardCaptureEvents;
        int captureReleasedFrame = -1;
        string pendingMouseCode;
        int pendingMouseReleaseFrame = -1;
        Vector2 scroll;
        public bool IsOpen { get; private set; }
        public bool IsCapturing { get { return rebind.IsCapturing; } }
        public SettingsMenu(PlayerInputSettings input, PresentationSettings presentation = null) { this.input = input; this.presentation = presentation; rebind = new ControlRebind(input.Preferences); }
        public void Open() { IsOpen = true; CancelRebind(); discardCaptureEvents = false; }
        public void Close() { IsOpen = false; CancelRebind(); discardCaptureEvents = false; }
        void CancelRebind()
        {
            rebind.Cancel(); pendingMouseCode = null; pendingMouseReleaseFrame = -1; input.SuppressUntilRelease();
        }
        public bool HandleEscape()
        {
            if (!IsOpen) return false;
            if (rebind.Action.HasValue) CancelRebind();
            else Close();
            return true;
        }
        public void UpdateCapture()
        {
            if (!IsOpen) return;
            if (discardCaptureEvents)
            {
                if (input.AnySupportedHeld) captureReleasedFrame = -1;
                else if (captureReleasedFrame < 0) captureReleasedFrame = Time.frameCount;
                else if (Time.frameCount > captureReleasedFrame) discardCaptureEvents = false;
            }
            if (!rebind.IsCapturing) return;
            // Discard the click/key used to enter capture, including held buttons.
            if (!captureArmed)
            {
                if (Time.frameCount > captureFrame && !input.AnySupportedHeld) captureArmed = true;
                return;
            }
            if (pendingMouseCode != null)
            {
                // Let IMGUI handle the release on Cancel/Back before accepting this mouse input.
                bool held = Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), pendingMouseCode));
                if (held) pendingMouseReleaseFrame = -1;
                else if (pendingMouseReleaseFrame < 0) pendingMouseReleaseFrame = Time.frameCount;
                return;
            }
            string code = input.NextPressedCode();
            if (code == null) return;
            if (code.StartsWith("Mouse", System.StringComparison.Ordinal))
            {
                pendingMouseCode = code; pendingMouseReleaseFrame = -1; input.SuppressUntilRelease();
            }
            else CommitCapture(code);
        }
        void CommitCapture(string code)
        {
            // The captured input belongs to rebinding, including its eventual release.
            // It must not also click another row or activate a newly displayed conflict button.
            discardCaptureEvents = true; captureReleasedFrame = -1;
            if (rebind.Capture(code)) input.Save();
        }
        void Begin(ControlAction action)
        {
            CancelRebind(); rebind.Begin(action); captureArmed = false; captureFrame = Time.frameCount;
        }
        void DrawPresentation()
        {
            var p = presentation.Preferences;
            bool changed = false;
            if (page == 1)
            {
                GUILayout.Label("Graphics preset", wrap);
                int selected = GUILayout.Toolbar((int)p.graphics, new[] { "Laptop", "Balanced", "Detailed" }, GUILayout.Height(36));
                if (selected != (int)p.graphics) { p.graphics = (GraphicsPreset)selected; changed = true; }
                GUILayout.Space(12);
                GUILayout.Label("Laptop: 75% resolution, short sharp shadows, no post processing. Balanced: 90% resolution, soft shadows and a subtle warm grade. Detailed: full resolution and longer shadows.", wrap);
                GUILayout.Space(12);
                GUILayout.Label("Frame limit");
                int index = p.frameLimit == 30 ? 0 : p.frameLimit == 60 ? 1 : 2;
                int next = GUILayout.Toolbar(index, new[] { "30 FPS", "60 FPS", "120 FPS" }, GUILayout.Height(32));
                if (next != index) { p.frameLimit = next == 0 ? 30 : next == 1 ? 60 : 120; changed = true; }
                GUILayout.Space(12);
                GUILayout.Label("Field of view: " + p.fieldOfView.ToString("0") + "°");
                float fov = Mathf.Round(GUILayout.HorizontalSlider(p.fieldOfView, 55, 95));
                if (fov != p.fieldOfView) { p.fieldOfView = fov; changed = true; }
                bool grade = GUILayout.Toggle(p.warmGrade, "Warm colour grade (Balanced / Detailed)");
                if (grade != p.warmGrade) { p.warmGrade = grade; changed = true; }
                GUILayout.Space(10);
                GUILayout.Label("Changes apply immediately. If the Editor feels slow, try Laptop, close Scene view while playing, and compare a standalone build.", wrap);
            }
            else
            {
                changed |= VolumeSlider("Master", ref p.masterVolume);
                changed |= VolumeSlider("Tools and machines", ref p.effectsVolume);
                changed |= VolumeSlider("Yard ambience", ref p.ambienceVolume);
                GUILayout.Label("Tools, footsteps and machines stop while menus are open. Gentle outdoor ambience continues.", wrap);
            }
            if (changed) presentation.Save();
            GUILayout.Space(16);
            if (GUILayout.Button("Restore video and audio defaults", GUILayout.Height(34))) presentation.RestoreDefaults();
            if (!string.IsNullOrEmpty(presentation.Notice)) GUILayout.Label(presentation.Notice, wrap);
        }
        static bool VolumeSlider(string label, ref float value)
        {
            GUILayout.Space(12); GUILayout.Label(label + ": " + (value * 100).ToString("0") + "%");
            float next = Mathf.Round(GUILayout.HorizontalSlider(value, 0, 1) * 100) / 100;
            if (next == value) return false;
            value = next; return true;
        }
        public void Draw(float width, float height)
        {
            if (!IsOpen) return;
            if (discardCaptureEvents && (Event.current.isMouse || Event.current.isKey)) Event.current.Use();
            float panelWidth = Mathf.Min(640, width - 30), panelHeight = Mathf.Min(650, height - 30);
            Rect panel = new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
            GUI.Box(panel, "SETTINGS");
            GUILayout.BeginArea(new Rect(panel.x + 18, panel.y + 36, panel.width - 36, panel.height - 48));
            if (presentation != null)
            {
                GUI.enabled = !rebind.Action.HasValue;
                int nextPage = GUILayout.Toolbar(page, new[] { "Controls", "Video", "Audio" }, GUILayout.Height(32));
                GUI.enabled = true;
                if (nextPage != page) { page = nextPage; scroll = Vector2.zero; input.SuppressUntilRelease(); }
            }
            scroll = GUILayout.BeginScrollView(scroll);
            if (wrap == null) wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (page == 0)
            {
            GUILayout.Label("Select a binding, then press a keyboard key or mouse button. Escape always cancels or goes back.", wrap);
            bool idle = !rebind.Action.HasValue;
            foreach (var action in ControlPreferences.Actions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(ControlPreferences.ActionLabel(action), GUILayout.Width(205));
                GUI.enabled = idle;
                if (GUILayout.Button(input.Label(action), GUILayout.Height(32))) Begin(action);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            if (rebind.IsCapturing)
            {
                string captureStatus = pendingMouseCode == null ? "waiting for input..." : "release " + ControlPreferences.CodeLabel(pendingMouseCode) + " to assign, or cancel";
                GUILayout.Label("Rebind " + ControlPreferences.ActionLabel(rebind.Action.Value) + ": " + captureStatus, wrap);
                if (GUILayout.Button("Cancel rebinding (Escape)")) CancelRebind();
            }
            else if (rebind.Conflict.HasValue)
            {
                GUILayout.Label(ControlPreferences.CodeLabel(rebind.Candidate) + " is already used by " + ControlPreferences.ActionLabel(rebind.Conflict.Value) + ". Swap assigns its current binding to that action; all actions stay bound.", wrap);
                if (GUILayout.Button("Swap bindings")) { if (rebind.Swap()) input.Save(); }
                if (GUILayout.Button("Cancel (keep both bindings)")) CancelRebind();
            }
            GUI.enabled = idle;
            GUILayout.Space(10);
            GUILayout.Label("Mouse sensitivity: " + input.Sensitivity.ToString("0.00"));
            float sensitivity = GUILayout.HorizontalSlider(input.Sensitivity, .1f, 10);
            bool invert = GUILayout.Toggle(input.InvertY, "Invert mouse Y");
            if (sensitivity != input.Sensitivity || invert != input.InvertY) input.SetLook(sensitivity, invert);
            GUILayout.Space(8);
            if (GUILayout.Button("Restore Defaults", GUILayout.Height(32))) input.RestoreDefaults();
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(input.Notice)) GUILayout.Label(input.Notice, wrap);
            GUILayout.Label("Keyboard, mouse buttons 1–7, and arrow keys are supported. Escape is reserved. Defaults include movement arrow aliases.", wrap);
            }
            else DrawPresentation();
            GUILayout.EndScrollView();
            if (GUILayout.Button(rebind.Action.HasValue ? "Cancel rebinding / Back (Escape)" : "Back to pause menu (Escape)", GUILayout.Height(36))) HandleEscape();
            GUILayout.EndArea();
            // Give every MouseUp/Cancel event a complete release frame before committing
            // on a later Repaint. A Cancel/Back click clears the pending candidate above;
            // an ordinary click commits here without activating a newly enabled control.
            // Polling also supports side buttons without corresponding IMGUI events.
            if (Event.current.type == EventType.Repaint && pendingMouseReleaseFrame >= 0 &&
                Time.frameCount > pendingMouseReleaseFrame && pendingMouseCode != null && rebind.IsCapturing)
            {
                string code = pendingMouseCode; pendingMouseCode = null; pendingMouseReleaseFrame = -1;
                CommitCapture(code);
            }
        }
    }
}
