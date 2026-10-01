using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;

namespace Scrapshift
{
    public sealed class PrototypeGame : MonoBehaviour
    {
        public PrototypeBalance balance;
        public FirstPersonController player;
        public Transform benchDisplay, machineDisplay, rotor, additionalRoller, feedDisplay;
        public Renderer machineLamp;
        public Texture2D logo;
        public YardModel Model { get; private set; }
        readonly Dictionary<int, GameObject> itemViews = new Dictionary<int, GameObject>();
        readonly List<int> removedViews = new List<int>();
        static readonly ProfilerMarker ViewsMarker = new ProfilerMarker("Scrapshift.SyncViews");
        static readonly ProfilerMarker SaveMarker = new ProfilerMarker("Scrapshift.Save");
        Material benchMaterial, lampMaterial;
        GUIStyle wrappedLabel, controlLegend, mapLabel;
        bool paused, confirmNew, saveBlocked, mapOpen;
        PlayerInputSettings controls;
        SettingsMenu settings;
        Vector3 benchRestPosition;
        float workPulseUntil;
        string message;
        float messageUntil, nextStroke, nextAutosave;
        InteractionTarget target;
        AudioSource audioSource;
        AudioClip feedback;
        string SavePath { get { return Path.Combine(Application.persistentDataPath, "yard-v1.json"); } }

        void Start()
        {
            controls = new PlayerInputSettings(); settings = new SettingsMenu(controls);
            player.controls = controls; benchRestPosition = benchDisplay.localPosition;
            benchMaterial = benchDisplay.GetComponent<Renderer>().material;
            lampMaterial = machineLamp.material;
            audioSource = gameObject.AddComponent<AudioSource>();
            feedback = AudioClip.Create("Tool click", 2205, 1, 22050, false);
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .19f) * .12f * (1f - i / 2205f);
            feedback.SetData(samples, 0);
            try { Model = new YardModel(balance.rules, SaveStore.Read(SavePath, out message)); }
            catch (Exception ex)
            {
                Model = new YardModel(balance.rules); saveBlocked = true;
                message = "Save could not load: " + ex.Message; Debug.LogWarning(message);
            }
            player.Restore(Model.State); messageUntil = Time.unscaledTime + 10;
            SetPaused(saveBlocked); SyncViews(); nextAutosave = Time.unscaledTime + 15;
        }
        void SetPaused(bool value)
        {
            paused = value; Time.timeScale = value ? 0 : 1;
            if (!value && settings != null) settings.Close();
            if (!value) mapOpen = false;
            if (controls != null) controls.SuppressUntilRelease();
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && Model != null) { settings.Close(); SetPaused(true); Save(); }
        }
        void Update()
        {
            if (Model == null) return;
            // Escape is permanently reserved for cancellation/back. Never process gameplay on a menu transition frame.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (mapOpen) mapOpen = false;
                else if (settings.IsOpen) settings.HandleEscape();
                else { SetPaused(!paused); confirmNew = false; if (paused) Save(); }
                return;
            }
            if (settings.IsOpen) settings.UpdateCapture();
            if (paused || settings.IsOpen) return;
            bool inputReady = controls.GameplayReady;
            if (inputReady) player.Step();
            bool machineWasRunning = Model.State.machineRemaining > 0;
            Model.Tick(Time.deltaTime);
            if (machineWasRunning && Model.State.machineRemaining == 0) SyncViews();
            target = null;
            if (Physics.Raycast(player.view.transform.position, player.view.transform.forward, out RaycastHit hit, 3.2f, ~(1 << 2)))
                target = hit.collider.GetComponentInParent<InteractionTarget>();
            if (inputReady && controls.Pressed(ControlAction.Drop)) Drop();
            if (inputReady && target != null && controls.Pressed(ControlAction.Interact)) Interact();
            if (inputReady && target != null && target.kind == TargetKind.Bench && controls.Pressed(ControlAction.ManualWork) && Time.time >= nextStroke)
            {
                if (Model.WorkBench())
                {
                    nextStroke = Time.time + .22f; workPulseUntil = Time.time + .18f;
                    Beep(1.25f); Tell(Model.State.benchOutput > 0 ? "Insulation removed. Collect the copper." : "Stripping stroke " + Model.State.benchStrokes + "/" + Model.Rules.manualStrokes); SyncViews(); Save();
                }
                else Tell(CurrentHint().text);
            }
            // Inventory presentation changes only on transactions/load/completion. Carried objects follow the camera by parenting.
            benchDisplay.localPosition = benchRestPosition + (Time.time < workPulseUntil ? Vector3.up * .025f : Vector3.zero);
            if (Model.State.machineRemaining > 0)
            {
                rotor.Rotate(0, 0, 240 * Time.deltaTime, Space.Self);
                if (additionalRoller != null) additionalRoller.Rotate(0, 0, -240 * Time.deltaTime, Space.Self);
            }
            if (Time.unscaledTime >= nextAutosave) { Save(); nextAutosave = Time.unscaledTime + 15; }
        }
        void Interact()
        {
            var before = CurrentHint();
            if (!before.canUse) { Tell(before.text); return; }
            int oldMoney = Model.State.money;
            bool ownedBefore = Model.State.machineOwned;
            bool changed = false;
            switch (target.kind)
            {
                case TargetKind.Supply: changed = Model.AcquireWire(); break;
                case TargetKind.LooseItem: changed = Model.PickUp(target.itemId); break;
                case TargetKind.Sell: changed = Model.Sell(); break;
                case TargetKind.Bench:
                    changed = Model.State.benchOutput > 0 ? Model.CollectBench() : Model.LoadBench(); break;
                case TargetKind.Machine:
                    changed = !Model.State.machineOwned ? Model.BuyMachine() :
                        Model.State.machineOutput > 0 ? Model.CollectMachine() : Model.FeedMachine(); break;
            }
            if (changed)
            {
                if (Model.State.money > oldMoney) { Tell("Sold copper • +€" + (Model.State.money - oldMoney)); Beep(1.6f); }
                else if (!ownedBefore && Model.State.machineOwned) { Tell("Powered stripper installed • -€" + Model.Rules.machinePrice + ". Feed wire into the front opening."); Beep(.8f); }
                else if (target.kind == TargetKind.Machine && Model.State.machineRemaining > 0) { Tell("Wire accepted. Rollers are stripping; collect copper from the output tray when ready."); Beep(.9f); }
                else { Tell(YardGuidance.Objective(Model, controls.Label(ControlAction.Interact), controls.Label(ControlAction.ManualWork), controls.Label(ControlAction.Drop))); Beep(); }
                SyncViews(); Save();
            }
            else Tell(CurrentHint().text);
        }
        void Drop()
        {
            if (Model.Carried == null) return;
            Vector3 direction = player.transform.forward;
            Vector3 origin = player.transform.position;
            Vector3 point = origin + direction * 1.4f;
            if (Physics.SphereCast(origin, .3f, direction, out RaycastHit wall, 1.4f, ~(1 << 2)))
                point = origin + direction * Mathf.Max(.1f, wall.distance - .15f);
            point.x = YardWorldLayout.ClampX(point.x); point.z = YardWorldLayout.ClampZ(point.z);
            if (Physics.Raycast(new Vector3(point.x, origin.y + .5f, point.z), Vector3.down, out RaycastHit floor, 3, ~(1 << 2)))
                point.y = floor.point.y + .25f;
            else point.y = .3f;
            if (Model.Drop(point.x, point.y, point.z)) { SyncViews(); Save(); }
        }
        void Beep(float pitch = 1f) { audioSource.pitch = pitch; audioSource.PlayOneShot(feedback); }
        void Tell(string text) { message = text; messageUntil = Time.unscaledTime + 5; }
        void SyncViews()
        {
            using (ViewsMarker.Auto())
            {
                removedViews.Clear();
                foreach (var pair in itemViews) if (Model.Find(pair.Key) == null) { Destroy(pair.Value); removedViews.Add(pair.Key); }
                foreach (int id in removedViews) itemViews.Remove(id);
                foreach (var item in Model.State.items)
                {
                    if (!itemViews.TryGetValue(item.id, out GameObject view))
                    {
                        view = YardGeometry.Bundle(item.kind, transform);
                        var interaction = view.AddComponent<InteractionTarget>(); interaction.kind = TargetKind.LooseItem; interaction.itemId = item.id;
                        itemViews.Add(item.id, view);
                    }
                    bool held = item.id == Model.State.carriedId;
                    Transform owner = held ? player.view.transform : transform;
                    if (view.transform.parent != owner) view.transform.SetParent(owner, false);
                    view.transform.localPosition = held ? new Vector3(.4f, -.35f, .85f) : new Vector3(item.x, item.y, item.z);
                    view.transform.localRotation = Quaternion.Euler(0, 0, held ? -15 : 0);
                    foreach (var collider in view.GetComponentsInChildren<Collider>()) collider.enabled = !held;
                }
                benchDisplay.gameObject.SetActive(Model.State.benchLoaded || Model.State.benchOutput > 0);
                float progress = Model.State.benchLoaded ? (float)Model.State.benchStrokes / Model.Rules.manualStrokes : 1;
                benchDisplay.localScale = new Vector3(.75f, .12f + .15f * progress, .35f);
                benchMaterial.color = Model.State.benchOutput > 0 ? YardGeometry.Copper : Color.Lerp(YardGeometry.Charcoal, YardGeometry.Copper, progress);
                benchDisplay.localPosition = benchRestPosition + (Time.time < workPulseUntil ? Vector3.up * .025f : Vector3.zero);
                machineDisplay.gameObject.SetActive(Model.State.machineOutput > 0);
                if (feedDisplay != null) feedDisplay.gameObject.SetActive(Model.State.machineRemaining > 0);
                lampMaterial.color = !Model.State.machineOwned ? Color.gray : Model.State.machineRemaining > 0 ? YardGeometry.Rust : Model.State.machineOutput > 0 ? Color.green : YardGeometry.Ivory;
            }
        }
        void CapturePlayer()
        {
            Vector3 p = player.transform.position;
            Model.State.playerX = p.x; Model.State.playerY = p.y; Model.State.playerZ = p.z;
            Model.State.yaw = player.Yaw; Model.State.pitch = player.Pitch;
        }
        void Save()
        {
            using (SaveMarker.Auto())
            {
                if (Model == null || saveBlocked) return;
                CapturePlayer();
                try { SaveStore.Write(SavePath, Model.State); }
                catch (Exception ex) { Tell("SAVE FAILED: " + ex.Message); Debug.LogWarning(ex); }
            }
        }
        void NewGame()
        {
            // Retain the previous save for manual recovery; no destructive deletion.
            try
            {
                string suffix = ".archived-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + suffix, false);
                if (File.Exists(SavePath + ".bak")) File.Copy(SavePath + ".bak", SavePath + suffix + ".bak", false);
            }
            catch (Exception ex) { Tell("Could not archive old save: " + ex.Message); return; }
            Model = new YardModel(balance.rules); saveBlocked = false; player.Restore(Model.State);
            SyncViews(); Save(); confirmNew = false; SetPaused(false); Tell("New yard. Take wire from the delivery crate.");
        }
        void OnApplicationQuit() { Save(); }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (feedback != null) Destroy(feedback); if (benchMaterial != null) Destroy(benchMaterial); if (lampMaterial != null) Destroy(lampMaterial); }

        StationHint CurrentHint()
        {
            if (target == null) return new StationHint(false, "Aim at a station or bundle within 3.2 metres");
            var hint = YardGuidance.Hint(Model, target.kind, target.itemId, controls.Label(ControlAction.Interact), controls.Label(ControlAction.ManualWork));
            if (target.kind == TargetKind.Supply && !string.IsNullOrEmpty(target.displayName))
                return new StationHint(hint.canUse, hint.text.Replace("DELIVERY", target.displayName));
            return hint;
        }
        string PrimaryLabel(ControlAction action) { return ControlPreferences.CodeLabel(controls.Preferences.Binding(action)); }
        string ControlHints()
        {
            return "Move " + PrimaryLabel(ControlAction.MoveForward) + "/" + PrimaryLabel(ControlAction.MoveBackward) + "/" +
                PrimaryLabel(ControlAction.MoveLeft) + "/" + PrimaryLabel(ControlAction.MoveRight) + " • Mouse look • " +
                controls.Label(ControlAction.Interact) + " interact • " + controls.Label(ControlAction.ManualWork) + " strip • " +
                controls.Label(ControlAction.Drop) + " drop • Esc pause";
        }
        void OnGUI()
        {
            if (Model == null) return;
            float scale = Mathf.Clamp(Screen.height / 800f, .65f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.box.fontSize = 18;
            if (wrappedLabel == null)
            {
                wrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
                controlLegend = new GUIStyle(wrappedLabel) { fontSize = 15 };
                mapLabel = new GUIStyle(controlLegend) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            }
            GUI.color = YardGeometry.Ivory;
            if (settings.IsOpen) { settings.Draw(width, height); return; }
            if (mapOpen) { DrawYardMap(width, height); return; }
            GUI.Box(new Rect(20, 20, Mathf.Min(width - 40, 490), 140), "");
            GUI.Label(new Rect(35, 28, 440, 30), "SCRAPSHIFT   /   €" + Model.State.money);
            GUI.Label(new Rect(25, 162, width - 50, 28), YardWorldLayout.Area(player.transform.position.x, player.transform.position.z));
            string objective = YardGuidance.Objective(Model, controls.Label(ControlAction.Interact), controls.Label(ControlAction.ManualWork), controls.Label(ControlAction.Drop));
            GUI.Label(new Rect(35, 64, Mathf.Min(width - 70, 455), 85), objective, wrappedLabel);
            string held = Model.Carried == null ? "Hands empty" : "Carrying " + Model.Carried.kind + " ×" + Model.Carried.quantity + "   /   " + controls.Label(ControlAction.Drop) + ": drop";
            GUI.Label(new Rect(25, height - 144, width - 50, 28), held);
            var hint = CurrentHint();
            GUI.Label(new Rect(25, height - 108, width - 50, 55), hint.text, wrappedLabel);
            GUI.Label(new Rect(25, height - 53, width - 50, 48), ControlHints(), controlLegend);
            GUI.color = hint.canUse ? new Color(.65f, 1f, .55f) : YardGeometry.Ivory;
            GUI.Label(new Rect(width / 2 - 5, height / 2 - 15, 20, 30), "+");
            GUI.color = YardGeometry.Ivory;
            if (Time.unscaledTime < messageUntil) GUI.Label(new Rect(25, 198, width - 50, 85), message, wrappedLabel);
            if (!paused) return;
            GUI.Box(new Rect(width / 2 - 235, height / 2 - 190, 470, 470), "PAUSED");
            if (logo != null) GUI.DrawTexture(new Rect(width / 2 - 140, height / 2 - 155, 280, 85), logo, ScaleMode.ScaleToFit);
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 - 50, 360, 40), "Resume")) SetPaused(false);
            if (GUI.Button(new Rect(width / 2 - 180, height / 2, 360, 40), "Save yard")) { Save(); }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 50, 360, 40), "Settings")) { confirmNew = false; messageUntil = 0; settings.Open(); }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 100, 360, 40), confirmNew ? "Confirm: reset yard (old save archived)" : "New game"))
            { if (confirmNew) NewGame(); else confirmNew = true; }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 150, 360, 40), "Yard map")) { mapOpen = true; confirmNew = false; }
            if (saveBlocked) GUI.Label(new Rect(width / 2 - 215, height / 2 + 198, 430, 65), "Saving blocked to protect unreadable data.\nChoose New game to archive it and restart.");
        }
        void DrawYardMap(float width, float height)
        {
            float panelWidth = Mathf.Min(width - 30, 760), panelHeight = Mathf.Min(height - 30, 650);
            var panel = new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
            GUI.Box(panel, "SCRAPSHIFT / YARD MAP");
            float aspect = YardWorldLayout.HalfWidth / YardWorldLayout.HalfDepth;
            float mapWidth = Mathf.Min(panel.width - 40, (panel.height - 200) * aspect);
            var map = new Rect(panel.center.x - mapWidth / 2, panel.y + 45, mapWidth, mapWidth / aspect);
            GUI.Box(map, "NORTH / LOADING & STORAGE");
            GUI.color = new Color(.75f, .70f, .55f);
            GUI.DrawTexture(new Rect(map.center.x - 3, map.y + 25, 6, map.height - 30), Texture2D.whiteTexture);
            float laneY = map.y + (YardWorldLayout.HalfDepth + 13) / (YardWorldLayout.HalfDepth * 2) * map.height;
            GUI.DrawTexture(new Rect(map.x + 10, laneY, map.width - 20, 6), Texture2D.whiteTexture);
            GUI.color = YardGeometry.Ivory;
            MapLabel(map, -29, 9, "VEHICLE SALVAGE");
            MapLabel(map, 30, 9, "METAL SORTING");
            MapLabel(map, 0, 4, "WORKSHOP\nWIRE / BUYER");
            MapLabel(map, -16, -30, "OFFICE");
            MapLabel(map, 0, -37, "ENTRY GATE");
            foreach (var site in YardWorldLayout.SalvageSites) MapLabel(map, site.x, site.z, "WIRE");
            GUI.color = new Color(.6f, 1, .55f);
            MapLabel(map, player.transform.position.x, player.transform.position.z, "+ YOU");
            GUI.color = YardGeometry.Ivory;
            GUI.Label(new Rect(panel.x + 20, map.yMax + 10, panel.width - 40, 70),
                "Explore the salvage crates for free wire. Bring it back to the workshop to strip and sell. " +
                "Pale gravel lanes connect the districts.\nCurrent area: " + YardWorldLayout.Area(player.transform.position.x, player.transform.position.z), controlLegend);
            if (GUI.Button(new Rect(panel.x + 20, panel.yMax - 55, panel.width - 40, 40), "Back / Escape")) mapOpen = false;
        }
        void MapLabel(Rect map, float x, float z, string text)
        {
            float px = map.x + (x + YardWorldLayout.HalfWidth) / (YardWorldLayout.HalfWidth * 2) * map.width;
            float py = map.y + (YardWorldLayout.HalfDepth - z) / (YardWorldLayout.HalfDepth * 2) * map.height;
            GUI.Label(new Rect(px - 80, py - 20, 160, 40), text, mapLabel);
        }
    }
}
