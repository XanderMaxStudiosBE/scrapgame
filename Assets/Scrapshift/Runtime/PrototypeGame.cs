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
        public YardBusinessVisual business;
        public FanWorkbenchVisual fanWorkbench;
        public YardModel Model { get; private set; }
        readonly Dictionary<int, GameObject> itemViews = new Dictionary<int, GameObject>();
        readonly List<int> removedViews = new List<int>();
        static readonly ProfilerMarker ViewsMarker = new ProfilerMarker("Scrapshift.SyncViews");
        static readonly ProfilerMarker SaveMarker = new ProfilerMarker("Scrapshift.Save");
        Material benchMaterial, lampMaterial;
        GUIStyle wrappedLabel, controlLegend, mapLabel;
        bool paused, confirmNew, saveBlocked, mapOpen, ordersOpen, repairOpen, dayOpen, investmentsOpen;
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
            if (!value) { mapOpen = false; ordersOpen = false; repairOpen = false; dayOpen = false; }
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
                if (repairOpen) repairOpen = false;
                else if (dayOpen) dayOpen = false;
                else if (ordersOpen) ordersOpen = false;
                else if (mapOpen) mapOpen = false;
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
            // A station can open a menu during this frame; stop before any simultaneous tool input.
            if (paused || settings.IsOpen) return;
            if (inputReady && target != null && (target.kind == TargetKind.Bench || target.kind == TargetKind.FanBench) && controls.Pressed(ControlAction.ManualWork) && Time.time >= nextStroke)
            {
                if (target.kind == TargetKind.FanBench ? Model.WorkFan() : Model.WorkBench())
                {
                    nextStroke = Time.time + .22f; workPulseUntil = Time.time + .18f;
                    Beep(1.25f);
                    Tell(target.kind == TargetKind.FanBench ?
                        (Model.State.fanStage == FanStage.ReadyToTest ? "Motor fitted. Power on and test the fan." : Model.State.fanStage == FanStage.CopperReady ? "Fan dismantled. Collect the recovered copper." : "Working on the fan • " + Model.State.fanStrokes + " steps complete") :
                        Model.State.benchOutput > 0 ? "Insulation removed. Collect the copper." : "Stripping stroke " + Model.State.benchStrokes + "/" + Model.WireWorkSteps);
                    SyncViews(); Save();
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
            if (fanWorkbench != null) fanWorkbench.Step(Model,Time.deltaTime);
            if (Time.unscaledTime >= nextAutosave) { Save(); nextAutosave = Time.unscaledTime + 15; }
        }
        void Interact()
        {
            var before = CurrentHint();
            if (!before.canUse) { Tell(before.text); return; }
            int oldMoney = Model.State.money;
            MaterialKind oldKind = Model.Carried == null ? MaterialKind.Wire : Model.Carried.kind;
            bool ownedBefore = Model.State.machineOwned;
            int oldOrder = Model.State.orderIndex;
            bool orderAcceptedBefore = Model.State.orderAccepted;
            bool changed = false;
            switch (target.kind)
            {
                case TargetKind.DayBoard: SetPaused(true); dayOpen = true; investmentsOpen = false; Save(); return;
                case TargetKind.FanSupply: changed = Model.AcquireFan(); break;
                case TargetKind.FanBench:
                    if (Model.State.fanStage == FanStage.Empty) changed = Model.LoadFan();
                    else if (Model.State.fanStage == FanStage.ReadyToTest) changed = Model.TestFan();
                    else if (Model.State.fanStage == FanStage.Tested || Model.State.fanStage == FanStage.CopperReady) changed = Model.CollectFan();
                    else { SetPaused(true); repairOpen = true; Save(); return; }
                    break;
                case TargetKind.WireStorage: changed = Model.Carried == null ? Model.Retrieve(MaterialKind.Wire) : Model.Store(MaterialKind.Wire); break;
                case TargetKind.CopperStorage: changed = Model.Carried == null ? Model.Retrieve(MaterialKind.Copper) : Model.Store(MaterialKind.Copper); break;
                case TargetKind.OrderBoard: changed = Model.State.orderAccepted ? Model.DeliverOrder() : Model.AcceptOrder(); break;
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
                if (Model.State.orderIndex > oldOrder) { Tell("Order complete • +€" + (Model.State.money - oldMoney) + ". A new customer request is on the board."); Beep(1.6f); }
                else if (!orderAcceptedBefore && Model.State.orderAccepted) { Tell(Model.CurrentOrder.customer + " order accepted. Deliver " + Model.CurrentOrder.copper + " copper for €" + Model.CurrentOrder.reward + ". No rush."); Beep(); }
                else if (target.kind == TargetKind.OrderBoard) { Tell("Copper delivered • " + Model.State.orderDelivered + "/" + Model.CurrentOrder.copper + ". The order pays when complete."); Beep(1.2f); }
                else if (target.kind == TargetKind.WireStorage || target.kind == TargetKind.CopperStorage) { Tell(Model.Carried == null ? "Bundle stored safely. Use this bin with empty hands to retrieve it." : "Bundle retrieved from storage."); Beep(); }
                else if (Model.State.money > oldMoney) { Tell("Sold " + (oldKind == MaterialKind.RestoredFan ? "tested desk fan" : "copper") + " • +€" + (Model.State.money - oldMoney)); Beep(1.6f); }
                else if (target.kind == TargetKind.FanBench && Model.State.fanStage == FanStage.Tested) { Tell("The fan runs smoothly. Collect it and sell it for €" + Model.Rules.fanSalePrice + "."); Beep(1.4f); }
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
                point.y = floor.point.y + ((Model.Carried.kind == MaterialKind.BrokenFan || Model.Carried.kind == MaterialKind.RestoredFan) ? .02f : .12f);
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
                foreach (var pair in itemViews) if (Model.Find(pair.Key) == null || Model.Find(pair.Key).storage != StorageSlot.None) { Destroy(pair.Value); removedViews.Add(pair.Key); }
                foreach (int id in removedViews) itemViews.Remove(id);
                foreach (var item in Model.State.items)
                {
                    if (item.storage != StorageSlot.None) continue;
                    if (!itemViews.TryGetValue(item.id, out GameObject view))
                    {
                        view = YardItemVisual.Create(item.kind, transform);
                        var interaction = view.AddComponent<InteractionTarget>(); interaction.kind = TargetKind.LooseItem; interaction.itemId = item.id;
                        itemViews.Add(item.id, view);
                    }
                    bool held = item.id == Model.State.carriedId;
                    Transform owner = held ? player.view.transform : transform;
                    if (view.transform.parent != owner) view.transform.SetParent(owner, false);
                    bool appliance = item.kind == MaterialKind.BrokenFan || item.kind == MaterialKind.RestoredFan;
                    view.transform.localPosition = held ? (appliance ? new Vector3(.55f,-.65f,1.05f) : new Vector3(.4f,-.35f,.85f)) : new Vector3(item.x, item.y, item.z);
                    view.transform.localRotation = Quaternion.Euler(0, 0, held ? -15 : 0);
                    foreach (var collider in view.GetComponentsInChildren<Collider>()) collider.enabled = !held;
                }
                benchDisplay.gameObject.SetActive(Model.State.benchLoaded || Model.State.benchOutput > 0);
                float progress = Model.State.benchLoaded ? (float)Model.State.benchStrokes / Model.WireWorkSteps : 1;
                benchDisplay.localScale = new Vector3(.75f, .12f + .15f * progress, .35f);
                benchMaterial.color = Model.State.benchOutput > 0 ? YardGeometry.Copper : Color.Lerp(YardGeometry.Charcoal, YardGeometry.Copper, progress);
                benchDisplay.localPosition = benchRestPosition + (Time.time < workPulseUntil ? Vector3.up * .025f : Vector3.zero);
                machineDisplay.gameObject.SetActive(Model.State.machineOutput > 0);
                if (feedDisplay != null) feedDisplay.gameObject.SetActive(Model.State.machineRemaining > 0);
                if (business != null) business.Refresh(Model);
                if (fanWorkbench != null) fanWorkbench.Refresh(Model);
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
            if (ordersOpen) { DrawOrders(width, height); return; }
            if (repairOpen) { DrawRepair(width, height); return; }
            if (dayOpen) { DrawDay(width, height); return; }
            GUI.Box(new Rect(20, 20, Mathf.Min(width - 40, 490), 140), "");
            GUI.Label(new Rect(35, 28, 440, 30), "DAY " + ((long)Model.State.dayIndex+1) + "   /   €" + Model.State.money);
            GUI.Label(new Rect(25, 162, width - 50, 28), YardWorldLayout.Area(player.transform.position.x, player.transform.position.z));
            string objective = YardGuidance.Objective(Model, controls.Label(ControlAction.Interact), controls.Label(ControlAction.ManualWork), controls.Label(ControlAction.Drop));
            GUI.Label(new Rect(35, 64, Mathf.Min(width - 70, 455), 85), objective, wrappedLabel);
            if (Model.State.orderAccepted && width > 980)
            {
                GUI.Box(new Rect(width - 320, 20, 300, 105), "CUSTOMER ORDER");
                GUI.Label(new Rect(width - 305, 48, 270, 65), Model.CurrentOrder.customer + "\n" + Model.State.orderDelivered + " / " + Model.CurrentOrder.copper + " copper • €" + Model.CurrentOrder.reward, controlLegend);
            }
            string held = Model.Carried == null ? "Hands empty" : "Carrying " + YardItemVisual.Label(Model.Carried.kind) + " ×" + Model.Carried.quantity + "   /   " + controls.Label(ControlAction.Drop) + ": drop";
            GUI.Label(new Rect(25, height - 144, width - 50, 28), held);
            var hint = CurrentHint();
            GUI.Label(new Rect(25, height - 108, width - 50, 55), hint.text, wrappedLabel);
            GUI.Label(new Rect(25, height - 53, width - 50, 48), ControlHints(), controlLegend);
            GUI.color = hint.canUse ? new Color(.65f, 1f, .55f) : YardGeometry.Ivory;
            GUI.Label(new Rect(width / 2 - 5, height / 2 - 15, 20, 30), "+");
            GUI.color = YardGeometry.Ivory;
            if (Time.unscaledTime < messageUntil) GUI.Label(new Rect(25, 198, width - 50, 85), message, wrappedLabel);
            if (!paused) return;
            GUI.Box(new Rect(width / 2 - 235, height / 2 - 215, 470, 495), "PAUSED");
            if (logo != null) GUI.DrawTexture(new Rect(width / 2 - 140, height / 2 - 155, 280, 85), logo, ScaleMode.ScaleToFit);
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 - 50, 360, 40), "Resume")) SetPaused(false);
            if (GUI.Button(new Rect(width / 2 - 180, height / 2, 360, 40), "Save yard")) { Save(); }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 50, 360, 40), "Settings")) { confirmNew = false; messageUntil = 0; settings.Open(); }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 100, 360, 40), confirmNew ? "Confirm: reset yard (old save archived)" : "New game"))
            { if (confirmNew) NewGame(); else confirmNew = true; }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 150, 360, 40), "Yard map")) { mapOpen = true; confirmNew = false; }
            if (GUI.Button(new Rect(width / 2 - 180, height / 2 + 198, 360, 40), "Orders & storage")) { ordersOpen = true; confirmNew = false; }
            if (saveBlocked) GUI.Label(new Rect(width / 2 - 215, height / 2 - 210, 430, 65), "Saving blocked to protect unreadable data.\nChoose New game to archive it and restart.");
        }
        void DrawRepair(float width, float height)
        {
            float w = Mathf.Min(width-30,640),h = Mathf.Min(height-30,510);
            var panel = new Rect((width-w)/2,(height-h)/2,w,h);
            GUI.Box(panel,"RESTORATION / DESK FAN");
            GUI.Label(new Rect(panel.x+20,panel.y+45,w-40,85),"A salvaged desk fan. Recover its useful material or put it back into service.",wrappedLabel);
            if (Model.State.fanStage == FanStage.AwaitingInspection)
            {
                GUI.Label(new Rect(panel.x+20,panel.y+145,w-40,90),"Inspect the housing, wiring and motor before choosing a repair.",wrappedLabel);
                if (GUI.Button(new Rect(panel.x+20,panel.y+255,w-40,45),"Inspect fan"))
                { if (Model.InspectFan()) { SyncViews(); Save(); Beep(); } }
            }
            else if (Model.State.fanStage == FanStage.Diagnosed)
            {
                GUI.Label(new Rect(panel.x+20,panel.y+135,w-40,100),"FAULT / Seized motor\nThe guard, base and wiring are reusable. A replacement motor costs €"+Model.Rules.fanPartsPrice+". A tested fan sells for €"+Model.Rules.fanSalePrice+".",wrappedLabel);
                GUI.enabled = Model.State.money >= Model.Rules.fanPartsPrice;
                if (GUI.Button(new Rect(panel.x+20,panel.y+255,w-40,45),"Fit replacement motor / €"+Model.Rules.fanPartsPrice))
                {
                    if (Model.BeginFanRepair()) { SyncViews();Save();SetPaused(false);Tell("Replacement motor supplied. Use ["+controls.Label(ControlAction.ManualWork)+"] at the bench to fit it, then test the fan."); }
                }
                GUI.enabled = true;
                if (GUI.Button(new Rect(panel.x+20,panel.y+315,w-40,45),"Dismantle instead / recover "+Model.Rules.fanCopperYield+" copper"))
                {
                    if (Model.BeginFanDismantle()) { SyncViews();Save();SetPaused(false);Tell("Salvage selected. Use ["+controls.Label(ControlAction.ManualWork)+"] at the bench to recover copper."); }
                }
            }
            else
                GUI.Label(new Rect(panel.x+20,panel.y+150,w-40,150),"Current job: "+(Model.State.fanStage==FanStage.Repairing?"fitting a replacement motor":"recovering copper")+"\n"+Model.State.fanStrokes+" hand-work steps complete.\nReturn to the bench and use ["+controls.Label(ControlAction.ManualWork)+"].",wrappedLabel);
            if (GUI.Button(new Rect(panel.x+20,panel.yMax-55,w-40,40),"Back / Escape")) repairOpen=false;
        }
        void DrawDay(float width,float height)
        {
            float w=Mathf.Min(width-30,640),h=Mathf.Min(height-30,620);
            var panel=new Rect((width-w)/2,(height-h)/2,w,h);
            GUI.Box(panel,"YARD DIARY / DAY "+((long)Model.State.dayIndex+1));
            if(GUI.Button(new Rect(panel.x+20,panel.y+42,(w-50)/2,35),"Day review"))investmentsOpen=false;
            if(GUI.Button(new Rect(panel.center.x+5,panel.y+42,(w-50)/2,35),"Invest in the yard"))investmentsOpen=true;
            if(investmentsOpen){DrawInvestments(panel);return;}
            GUI.Label(new Rect(panel.x+20,panel.y+100,w-40,190),"Sales and contract income today: €"+Model.State.incomeToday+"\nCash on hand: €"+Model.State.money+"\nCompleted customer orders: "+Model.State.orderIndex+"\nFans restored: "+Model.State.fansRepaired+" / dismantled: "+Model.State.fansDismantled+"\nSalvage fans left today: "+Mathf.Max(0,Model.Rules.fanDailyLimit-Model.State.fansTakenToday),wrappedLabel);
            GUI.Label(new Rect(panel.x+20,panel.y+315,w-40,110),"Return tomorrow for a fresh appliance-salvage delivery. Inventory, repairs and customer orders are kept; the powered stripper finishes its current load overnight. There are no deadlines or daily fees.",wrappedLabel);
            GUI.enabled=Model.State.dayIndex<int.MaxValue;
            if (GUI.Button(new Rect(panel.x+20,panel.y+475,w-40,45),"Finish day / return on day "+((long)Model.State.dayIndex+2)))
            {
                if (Model.AdvanceDay()) { SyncViews();Save();SetPaused(false);Tell("A fresh day at the yard. New salvage fans have arrived.");Beep(1.3f); }
            }
            GUI.enabled=true;
            if (GUI.Button(new Rect(panel.x+20,panel.yMax-55,w-40,40),"Keep working / Back (Escape)")) dayOpen=false;
        }
        void DrawInvestments(Rect panel)
        {
            GUI.Label(new Rect(panel.x+20,panel.y+87,panel.width-40,30),"Cash available: €"+Model.State.money);
            DrawInvestment(panel,120,YardUpgrade.StorageRack,"Storage rack","Add up to 12 yard bundle slots. Stored material stays in its existing bins.");
            DrawInvestment(panel,245,YardUpgrade.HandTools,"Better hand tools","One fewer hand-work step for wire stripping, fan repair and fan dismantling; material yields stay the same.");
            DrawInvestment(panel,370,YardUpgrade.MachineTuning,"Stripper tune-up","New wire loads finish 40% faster. Requires your powered stripper; its current load stays consistent.");
            if(GUI.Button(new Rect(panel.x+20,panel.yMax-55,panel.width-40,40),"Back / Escape"))dayOpen=false;
        }
        void DrawInvestment(Rect panel,float y,YardUpgrade upgrade,string title,string description)
        {
            GUI.Label(new Rect(panel.x+20,panel.y+y,panel.width-40,55),title+" • €"+Model.UpgradePrice(upgrade)+"\n"+description,controlLegend);
            bool owned=Model.Owns(upgrade);
            GUI.enabled=Model.CanBuyUpgrade(upgrade);
            string caption=owned?"Installed":upgrade==YardUpgrade.MachineTuning&&!Model.State.machineOwned?"Buy a powered stripper first":"Buy "+title.ToLowerInvariant();
            if(GUI.Button(new Rect(panel.x+20,panel.y+y+66,panel.width-40,38),caption))
            {
                if(Model.BuyUpgrade(upgrade)){SyncViews();Save();Beep(1.2f);Tell(title+" installed.");}
            }
            GUI.enabled=true;
        }
        void DrawOrders(float width, float height)
        {
            float w = Mathf.Min(width - 30, 640), h = Mathf.Min(height - 30, 530);
            var panel = new Rect((width - w) / 2, (height - h) / 2, w, h);
            GUI.Box(panel, "ORDERS & STORAGE");
            var order = Model.CurrentOrder;
            GUI.Label(new Rect(panel.x + 20, panel.y + 40, w - 40, 75),
                (Model.State.orderAccepted ? "ACTIVE ORDER" : "AVAILABLE AT THE CUSTOMER BOARD") + "\n" + order.customer + " / " + order.title, wrappedLabel);
            GUI.Label(new Rect(panel.x + 20, panel.y + 125, w - 40, 100), order.note + "\n" + Model.State.orderDelivered + " / " + order.copper + " copper • €" + order.reward + " on completion • No deadline", wrappedLabel);
            GUI.Label(new Rect(panel.x + 20, panel.y + 240, w - 40, 105),
                "Completed orders: " + Model.State.orderIndex + " / Yard occupancy: " + Model.OccupiedBundles + "/" + Model.Capacity + "\nWire storage: " + Model.StoredBundles(MaterialKind.Wire) + " bundles\nCopper storage: " + Model.StoredQuantity(MaterialKind.Copper) + " units in " + Model.StoredBundles(MaterialKind.Copper) + " bundles", wrappedLabel);
            GUI.Label(new Rect(panel.x + 20, panel.y + 355, w - 40, 80),
                "Use the CUSTOMER BOARD north of the workshop to accept and deliver. Storage bins east of the workshop store your carried bundle; use them with empty hands to retrieve one.", controlLegend);
            if (GUI.Button(new Rect(panel.x + 20, panel.yMax - 55, w - 40, 40), "Back / Escape")) ordersOpen = false;
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
            MapLabel(map, 7, 12, "ORDERS");
            MapLabel(map, -7, 12, "RESTORATION");
            MapLabel(map, -33, -19, "FANS");
            MapLabel(map, 6, -27, "DIARY");
            MapLabel(map, 14, -1, "STORAGE");
            foreach (var site in YardWorldLayout.SalvageSites) MapLabel(map, site.x, site.z, "WIRE");
            GUI.color = new Color(.6f, 1, .55f);
            MapLabel(map, player.transform.position.x, player.transform.position.z, "+ YOU");
            GUI.color = YardGeometry.Ivory;
            GUI.Label(new Rect(panel.x + 20, map.yMax + 10, panel.width - 40, 70),
                "Explore salvage crates for wire. Store bundles near the workshop or deliver copper to the customer board. " +
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
