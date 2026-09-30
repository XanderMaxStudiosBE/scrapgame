using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Scrapshift
{
    public static class ControlSettingsStore
    {
        public static ControlPreferences Read(string path, out string notice)
        {
            notice = "";
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    if (!json.Contains("\"version\"") || !json.Contains("\"bindings\"") || !json.Contains("\"sensitivity\"") || !json.Contains("\"invertY\""))
                        throw new ArgumentException("Missing preference fields.");
                    var preferences = JsonUtility.FromJson<ControlPreferences>(json);
                    if (preferences == null) throw new ArgumentException("Empty preferences.");
                    preferences.Validate();
                    if (candidate != path) notice = "Controls restored from backup; latest preferences were unreadable.";
                    return preferences;
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
                { notice = "Could not load controls; defaults are active. " + ex.Message; }
            }
            return new ControlPreferences();
        }
        public static void Write(string path, ControlPreferences preferences)
        {
            preferences.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(preferences, true));
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
    }

    public sealed class PlayerInputSettings
    {
        readonly string path;
        readonly KeyCode[] supportedKeys;
        readonly Dictionary<string, KeyCode> keyCodes = new Dictionary<string, KeyCode>();
        readonly string[] labelCodes = new string[ControlPreferences.Actions.Length];
        readonly string[] labelAliases = new string[ControlPreferences.Actions.Length];
        readonly string[] labels = new string[ControlPreferences.Actions.Length];
        bool awaitingRelease;
        int suppressedFrame;
        public ControlPreferences Preferences { get; private set; }
        public string Notice { get; private set; }
        public float Sensitivity { get { return Preferences.sensitivity; } }
        public bool InvertY { get { return Preferences.invertY; } }
        public PlayerInputSettings() : this(Path.Combine(Application.persistentDataPath, "controls-v1.json")) { }
        public PlayerInputSettings(string path)
        {
            this.path = path;
            string notice;
            Preferences = ControlSettingsStore.Read(path, out notice); Notice = notice;
            supportedKeys = new KeyCode[ControlPreferences.SupportedCodes.Length];
            for (int i = 0; i < supportedKeys.Length; i++)
            {
                string code = ControlPreferences.SupportedCodes[i];
                supportedKeys[i] = (KeyCode)Enum.Parse(typeof(KeyCode), code);
                keyCodes.Add(code, supportedKeys[i]);
            }
        }
        KeyCode Parse(string code) { return keyCodes[code]; }
        public string Label(ControlAction action)
        {
            int index = (int)action;
            string code = Preferences.Binding(action), alias = Preferences.Alias(action);
            if (labels[index] != null && labelCodes[index] == code && labelAliases[index] == alias) return labels[index];
            labelCodes[index] = code; labelAliases[index] = alias;
            string label = ControlPreferences.CodeLabel(code);
            labels[index] = alias == null ? label : label + " / " + ControlPreferences.CodeLabel(alias);
            return labels[index];
        }
        public bool Held(ControlAction action)
        {
            if (!GameplayReady) return false;
            string alias = Preferences.Alias(action);
            return Input.GetKey(Parse(Preferences.Binding(action))) || (alias != null && Input.GetKey(Parse(alias)));
        }
        public bool Pressed(ControlAction action)
        {
            if (!GameplayReady) return false;
            string alias = Preferences.Alias(action);
            return Input.GetKeyDown(Parse(Preferences.Binding(action))) || (alias != null && Input.GetKeyDown(Parse(alias)));
        }
        public Vector2 Movement
        {
            get { return new Vector2((Held(ControlAction.MoveRight) ? 1 : 0) - (Held(ControlAction.MoveLeft) ? 1 : 0),
                (Held(ControlAction.MoveForward) ? 1 : 0) - (Held(ControlAction.MoveBackward) ? 1 : 0)); }
        }
        public bool AnySupportedHeld
        {
            get
            {
                if (Input.GetKey(KeyCode.Escape)) return true;
                foreach (var key in supportedKeys) if (Input.GetKey(key)) return true;
                return false;
            }
        }
        public string NextPressedCode()
        {
            for (int i = 0; i < supportedKeys.Length; i++) if (Input.GetKeyDown(supportedKeys[i])) return ControlPreferences.SupportedCodes[i];
            return null;
        }
        public void SuppressUntilRelease() { awaitingRelease = true; suppressedFrame = Time.frameCount; }
        public bool GameplayReady
        {
            get
            {
                if (awaitingRelease)
                {
                    if (Time.frameCount <= suppressedFrame || AnySupportedHeld) return false;
                    awaitingRelease = false; suppressedFrame = Time.frameCount;
                }
                return Time.frameCount > suppressedFrame;
            }
        }
        public void Save()
        {
            try { ControlSettingsStore.Write(path, Preferences); Notice = "Settings saved."; }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            { Notice = "Settings applied, but could not save: " + ex.Message; }
            SuppressUntilRelease();
        }
        public void RestoreDefaults() { Preferences.RestoreDefaults(); Save(); }
        public void SetLook(float sensitivity, bool invertY)
        {
            Preferences.sensitivity = Mathf.Clamp(sensitivity, .1f, 10); Preferences.invertY = invertY; Save();
        }
    }
}
