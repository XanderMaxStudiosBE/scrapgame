using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;

namespace Scrapshift
{
    public sealed partial class PrototypeGame : MonoBehaviour
    {
        public PrototypeBalance balance;
        public FirstPersonController player;
        public Transform benchDisplay, machineDisplay, rotor, additionalRoller, feedDisplay;
        public Renderer machineLamp;
        public Texture2D logo;
        public YardBusinessVisual business;
        public FanWorkbenchVisual fanWorkbench;
        public YardLighting lighting;
        public YardModel Model { get; private set; }
        readonly Dictionary<int, GameObject> itemViews = new Dictionary<int, GameObject>();
        readonly List<int> removedViews = new List<int>();
        static readonly ProfilerMarker ViewsMarker = new ProfilerMarker("Scrapshift.SyncViews");
        static readonly ProfilerMarker SaveMarker = new ProfilerMarker("Scrapshift.Save");
        Material lampMaterial;
        WorkbenchJobVisual benchJob;
        GUIStyle wrappedLabel, controlLegend, mapLabel, mapPin;
        readonly YardInterfaceTheme theme = new YardInterfaceTheme();
        bool paused, confirmNew, saveBlocked, mapOpen, ordersOpen, repairOpen, dayOpen, investmentsOpen;
        PlayerInputSettings controls;
        SettingsMenu settings;
        PresentationSettings presentation;
        float frameElapsed;
        int frameSamples;
        string frameReadout = "Measuring gameplay frames…";
        string message;
        float messageUntil, nextStroke, nextAutosave;
        InteractionTarget target;
        YardAudio sounds;
        readonly YardNavigator navigator = new YardNavigator();
        static readonly ProfilerMarker HudMarker = new ProfilerMarker("Scrapshift.RefreshHud");
        StationHint hudHint;
        StationProgress hudProgress;
        InteractionTarget hudTarget;
        string hudObjective, hudHeld, hudLegend, hudArea, hudRoute, hudDay, hudOrder;
        float nextHudRefresh;
        bool hudDirty = true;
        string SavePath { get { return Path.Combine(Application.persistentDataPath, "yard-v1.json"); } }

        void Start()
        {
            controls = new PlayerInputSettings();
            presentation = new PresentationSettings(player.view, transform);
            settings = new SettingsMenu(controls, presentation);
            player.controls = controls;
            benchJob = new WorkbenchJobVisual(benchDisplay);
            lampMaterial = machineLamp.material;
            sounds = new YardAudio(transform, player, presentation);
            try { Model = new YardModel(balance.PreparedRules, SaveStore.Read(SavePath, out message)); }
            catch (Exception ex)
            {
                Model = new YardModel(balance.PreparedRules); saveBlocked = true;
                message = "Save could not load: " + ex.Message; Debug.LogWarning(message);
            }
            player.Restore(Model.State); messageUntil = Time.unscaledTime + 10;
            InitializeFrontEnd(); SyncViews(); nextAutosave = Time.unscaledTime + 15;
        }
        void SetPaused(bool value)
        {
            hudDirty = true;
            if (sounds != null) sounds.Pause(value);
            paused = value; Time.timeScale = value ? 0 : 1;
            if (!value && settings != null) settings.Close();
            if (!value) { mapOpen = false; ordersOpen = false; repairOpen = false; dayOpen = false; titleOpen=false;helpOpen=false;introOpen=false;creditsOpen=false; }
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
            sounds.ApplyVolumes();
            // Escape is permanently reserved for cancellation/back. Never process gameplay on a menu transition frame.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if(settings.IsOpen)settings.HandleEscape();
                else if(introOpen)FinishIntroduction();
                else if(helpOpen)helpOpen=false;
                else if(creditsOpen)creditsOpen=false;
                else if(titleOpen)confirmNew=false;
                else if(repairOpen)repairOpen=false;
                else if(dayOpen)dayOpen=false;
                else if(ordersOpen)ordersOpen=false;
                else if(mapOpen)mapOpen=false;
                else { SetPaused(!paused); confirmNew=false; if(paused)Save(); }
                return;
            }
            if (settings.IsOpen) settings.UpdateCapture();
            if (paused || settings.IsOpen) return;
            frameElapsed += Time.unscaledDeltaTime; frameSamples++;
            if (frameElapsed >= .5f)
            {
                frameReadout = (frameSamples / frameElapsed).ToString("0") + " FPS • " + (frameElapsed * 1000 / frameSamples).ToString("0.0") + " ms";
                frameElapsed = 0; frameSamples = 0;
            }
            bool inputReady = controls.GameplayReady;
            if (inputReady) player.Step();
            bool machineWasRunning = Model.State.machineRemaining > 0;
            Model.Tick(Time.deltaTime);
            if (machineWasRunning && Model.State.machineRemaining == 0) { SyncViews(); Tell("Stripping finished. Copper is ready in the output tray."); }
            sounds.Step(Model);
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
                    nextStroke = Time.time + .22f;
                    if (target.kind == TargetKind.Bench) benchJob.Pulse();
                    else if (fanWorkbench != null) fanWorkbench.Pulse();
                    sounds.Play(YardSound.Tool);
                    Tell(target.kind == TargetKind.FanBench ?
                        (Model.State.fanStage == FanStage.ReadyToTest ? "Repair complete. Power on and test the "+Model.CurrentRepair.name+"." : Model.State.fanStage == FanStage.CopperReady ? "Appliance dismantled. Collect the recovered copper." : "Working on the "+Model.CurrentRepair.name+" • " + Model.State.fanStrokes + " steps complete") :
                        Model.State.benchOutput > 0 ? "Insulation removed. Collect the copper." : "Stripping stroke " + Model.State.benchStrokes + "/" + Model.WireWorkSteps);
                    SyncViews(); Save();
                }
                else Tell(CurrentHint().text);
            }
            // Inventory presentation changes only on transactions/load/completion. Carried objects follow the camera by parenting.
            benchJob.Step(Time.deltaTime);
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
                case TargetKind.RadioSupply: changed = Model.AcquireRadio(); break;
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
                else if (Model.State.money > oldMoney) { Tell("Sold " + YardItemVisual.Label(oldKind) + " • +€" + (Model.State.money - oldMoney)); Beep(1.6f); }
                else if (target.kind == TargetKind.FanBench && Model.State.fanStage == FanStage.Tested) { Tell(Model.CurrentRepair.testResult+" Collect it and sell it for €"+Model.CurrentRepair.salePrice+"."); Beep(1.4f); }
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
                point.y = floor.point.y + (ApplianceRecipe.IsAppliance(Model.Carried.kind) ? .02f : .12f);
            else point.y = .3f;
            if (Model.Drop(point.x, point.y, point.z)) { SyncViews(); Save(); }
        }
        void Beep(float pitch = 1f) { sounds.Play(pitch >= 1.5f ? YardSound.Sale : YardSound.Pickup); }
        void Tell(string text) { message = text; messageUntil = Time.unscaledTime + 5; }
        void SyncViews()
        {
            using (ViewsMarker.Auto())
            {
                hudDirty = true;
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
                    bool appliance = ApplianceRecipe.IsAppliance(item.kind);
                    view.transform.localPosition = held ? (appliance ? (item.kind==MaterialKind.BrokenRadio || item.kind==MaterialKind.RestoredRadio?new Vector3(.45f,-.42f,.9f):new Vector3(.55f,-.65f,1.05f)) : new Vector3(.4f,-.35f,.85f)) : new Vector3(item.x, item.y, item.z);
                    view.transform.localRotation = Quaternion.Euler(0, 0, held ? -15 : 0);
                    foreach (var collider in view.GetComponentsInChildren<Collider>()) collider.enabled = !held;
                }
                benchDisplay.gameObject.SetActive(Model.State.benchLoaded || Model.State.benchOutput > 0);
                benchJob.Refresh(Model);
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
        bool Save()
        {
            using (SaveMarker.Auto())
            {
                if(Model==null || saveBlocked || (!sessionStarted && !hasYardSave))return false;
                CapturePlayer();
                try { SaveStore.Write(SavePath,Model.State);hasYardSave=true;return true; }
                catch (Exception ex) { Tell("SAVE FAILED: " + ex.Message); Debug.LogWarning(ex);return false; }
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
            Model = new YardModel(balance.PreparedRules); navigator.Select(YardLandmark.Automatic); saveBlocked = false; player.Restore(Model.State);
            hasYardSave=false;sessionStarted=true;SyncViews();confirmNew=false;BeginYard();
        }
        void OnApplicationQuit() { Save(); }
        void OnDestroy() { theme.Dispose(); if (presentation != null) presentation.Dispose(); if(lighting!=null)lighting.Dispose(); Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (sounds != null) sounds.Dispose(); if (lampMaterial != null) Destroy(lampMaterial); }

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
                controls.Label(ControlAction.Interact) + " interact • " + controls.Label(ControlAction.ManualWork) + " work • " +
                controls.Label(ControlAction.Drop) + " drop • Esc pause";
        }
        void LateUpdate()
        {
            if (Model == null) return;
            // Refresh on a transaction/menu transition/new target, otherwise at ten Hz for countdowns/navigation.
            // IMGUI may invoke OnGUI several times per frame; drawing does not rebuild gameplay strings.
            if (hudDirty || hudTarget != target || (!paused && Time.unscaledTime >= nextHudRefresh)) RefreshHud();
        }
        void RefreshHud()
        {
            using (HudMarker.Auto())
            {
                hudDirty = false; hudTarget = target; nextHudRefresh = Time.unscaledTime + .1f;
                hudHint = CurrentHint();
                hudProgress = target == null ? default(StationProgress) : StationProgress.Read(Model,target.kind);
                hudObjective = YardGuidance.Objective(Model,controls.Label(ControlAction.Interact),controls.Label(ControlAction.ManualWork),controls.Label(ControlAction.Drop));
                hudHeld = Model.Carried == null ? "Hands empty" : "Carrying " + YardItemVisual.Label(Model.Carried.kind) + " ×" + Model.Carried.quantity + "   /   " + controls.Label(ControlAction.Drop) + ": drop";
                hudLegend = ControlHints();
                hudDay = "DAY " + ((long)Model.State.dayIndex+1) + "    •    €" + Model.State.money;
                hudOrder = Model.State.orderAccepted ? Model.CurrentOrder.customer+"\n"+Model.State.orderDelivered+" / "+Model.CurrentOrder.copper+" copper • €"+Model.CurrentOrder.reward : "";
                var p = player.transform.position;
                hudArea = YardWorldLayout.Area(p.x,p.z);
                var destination = navigator.Resolve(Model,p.x,p.z);
                hudRoute = destination.Direction(p.x,p.z,player.Yaw) + "  /  " + destination.name + "  /  " + destination.Distance(p.x,p.z).ToString("0") + "m";
            }
        }
        void OnGUI()
        {
            if (Model == null) return;
            if (hudHint == null) RefreshHud();
            float scale = Mathf.Clamp(Mathf.Min(Screen.height / 800f, Screen.width / 960f), .25f, 2f);
            using (theme.Begin(scale))
            {
                float width = Screen.width / scale, height = Screen.height / scale;
                if (wrappedLabel == null)
                {
                    wrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
                    controlLegend = new GUIStyle(wrappedLabel) { fontSize = 15 };
                    mapLabel = new GUIStyle(controlLegend) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
                    mapPin = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0,0,0,0) };
                }
                if (paused)
                {
                    GUI.color = new Color(0, 0, 0, .42f);
                    GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                if (settings.IsOpen) { settings.Draw(width, height); return; }
                if(introOpen){DrawIntroduction(width,height);return;}
                if(helpOpen){DrawHelp(width,height);return;}
                if(creditsOpen){DrawCredits(width,height);return;}
                if(titleOpen){DrawTitle(width,height);return;}
                if (mapOpen) { DrawYardMap(width, height); return; }
                if (ordersOpen) { DrawOrders(width, height); return; }
                if (repairOpen) { DrawRepair(width, height); return; }
                if (dayOpen) { DrawDay(width, height); return; }
                float objectiveWidth = Mathf.Min(width - 40, 420);
                GUI.Box(new Rect(20,20,objectiveWidth,152), "");
                GUI.Label(new Rect(34,27,objectiveWidth-28,26), hudDay);
                GUI.Label(new Rect(34,58,objectiveWidth-28,68),hudObjective,controlLegend);
                GUI.Label(new Rect(34,137,objectiveWidth-28,28),hudRoute,controlLegend);
                GUI.Label(new Rect(24,178,objectiveWidth,24),hudArea,controlLegend);
                if (Model.State.orderAccepted && width > 900)
                {
                    GUI.Box(new Rect(width-300,20,280,95),"CUSTOMER ORDER");
                    GUI.Label(new Rect(width-286,48,252,54),hudOrder,controlLegend);
                }
                if (presentation.Preferences.showFrameRate)
                    GUI.Label(new Rect(width-250,height-175,230,28),frameReadout,controlLegend);
                float promptWidth=Mathf.Min(width-40,720);
                var hint=hudHint;
                GUI.Box(new Rect((width-promptWidth)/2,height-139,promptWidth,93),"");
                GUI.Label(new Rect((width-promptWidth)/2+14,height-134,promptWidth-28,25),hudHeld,controlLegend);
                GUI.Label(new Rect((width-promptWidth)/2+14,height-106,promptWidth-28,55),hint.text,wrappedLabel);
                if (!paused && target != null)
                {
                    var progress = hudProgress;
                    if (progress.Visible)
                    {
                        float progressWidth = Mathf.Min(promptWidth,460);
                        float x = (width-progressWidth)/2, y = height-205;
                        GUI.Box(new Rect(x,y,progressWidth,56), "");
                        GUI.Label(new Rect(x+12,y+3,progressWidth-24,25),progress.label,controlLegend);
                        GUI.color = new Color(.2f,.22f,.2f);
                        GUI.DrawTexture(new Rect(x+12,y+34,progressWidth-24,8),Texture2D.whiteTexture);
                        GUI.color = new Color(.78f,.64f,.37f);
                        GUI.DrawTexture(new Rect(x+12,y+34,(progressWidth-24)*progress.fraction,8),Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                }
                GUI.Label(new Rect(24,height-40,width-48,35),hudLegend,controlLegend);
                GUI.color=hint.canUse?new Color(.77f,.86f,.62f):YardGeometry.Ivory;
                GUI.DrawTexture(new Rect(width/2-2,height/2-2,4,4),Texture2D.whiteTexture);
                GUI.color=Color.white;
                if(Time.unscaledTime<messageUntil)
                {
                    GUI.Box(new Rect((width-promptWidth)/2,218,promptWidth,76),"");
                    GUI.Label(new Rect((width-promptWidth)/2+14,228,promptWidth-28,56),message,wrappedLabel);
                }
                if (!paused) return;
                DrawPause(width,height);
            }
        }
        void DrawRepair(float width, float height)
        {
            float w=Mathf.Min(width-30,680),h=Mathf.Min(height-30,580);
            var panel=new Rect((width-w)/2,(height-h)/2,w,h);
            var recipe=Model.CurrentRepair;
            GUI.Box(panel,"RESTORATION / "+recipe.name.ToUpperInvariant());
            GUI.Label(new Rect(panel.x+20,panel.y+45,w-40,70),"Give this salvaged "+recipe.name+" another life, or recover its useful copper.",wrappedLabel);
            if(Model.State.fanStage==FanStage.AwaitingInspection)
            {
                GUI.Label(new Rect(panel.x+20,panel.y+135,w-40,105),"Inspect the housing, wiring and moving parts before choosing a repair. The fault stays with this appliance until the job is finished.",wrappedLabel);
                if(GUI.Button(new Rect(panel.x+20,panel.y+275,w-40,45),"Inspect "+recipe.name))
                { if(Model.InspectFan()){SyncViews();Save();Beep();} }
            }
            else if(Model.State.fanStage==FanStage.Diagnosed)
            {
                GUI.Label(new Rect(panel.x+20,panel.y+120,w-40,100),"FAULT / "+recipe.fault+"\n"+recipe.observation,wrappedLabel);
                GUI.Label(new Rect(panel.x+20,panel.y+225,w-40,90),
                    "Repair: "+Model.FanRepairSteps+" work steps + power-on test • €"+recipe.partsPrice+" parts\n"+
                    "Tested resale €"+recipe.salePrice+" / after parts €"+(recipe.salePrice-recipe.partsPrice)+"\n"+
                    "Salvage: "+Model.FanSalvageSteps+" work steps / "+recipe.copperYield+" copper worth €"+(recipe.copperYield*Model.Rules.copperUnitPrice),controlLegend);
                GUI.enabled=Model.State.money>=recipe.partsPrice;
                if(GUI.Button(new Rect(panel.x+20,panel.y+330,w-40,45),recipe.repairAction+" / "+(recipe.partsPrice==0?"no parts needed":"€"+recipe.partsPrice)))
                {
                    if(Model.BeginFanRepair()){SyncViews();Save();SetPaused(false);Tell(recipe.repairAction+". Use ["+controls.Label(ControlAction.ManualWork)+"] at the bench, then test the "+recipe.name+".");}
                }
                GUI.enabled=true;
                if(Model.State.money<recipe.partsPrice)GUI.Label(new Rect(panel.x+20,panel.y+379,w-40,25),"Need €"+(recipe.partsPrice-Model.State.money)+" more for parts. The appliance is kept.",controlLegend);
                if(GUI.Button(new Rect(panel.x+20,panel.y+417,w-40,45),"Dismantle instead / recover "+recipe.copperYield+" copper"))
                {
                    if(Model.BeginFanDismantle()){SyncViews();Save();SetPaused(false);Tell("Salvage selected. Use ["+controls.Label(ControlAction.ManualWork)+"] at the bench to recover copper.");}
                }
            }
            else
                GUI.Label(new Rect(panel.x+20,panel.y+150,w-40,150),"Current job: "+(Model.State.fanStage==FanStage.Repairing?recipe.workAction:"recovering copper")+"\n"+Model.State.fanStrokes+" hand-work steps complete.\nReturn to the bench and use ["+controls.Label(ControlAction.ManualWork)+"].",wrappedLabel);
            if(GUI.Button(new Rect(panel.x+20,panel.yMax-55,w-40,40),"Back / Escape"))repairOpen=false;
        }
        void DrawDay(float width,float height)
        {
            float w=Mathf.Min(width-30,640),h=Mathf.Min(height-30,620);
            var panel=new Rect((width-w)/2,(height-h)/2,w,h);
            GUI.Box(panel,"YARD DIARY / DAY "+((long)Model.State.dayIndex+1));
            if(GUI.Button(new Rect(panel.x+20,panel.y+42,(w-50)/2,35),"Day review"))investmentsOpen=false;
            if(GUI.Button(new Rect(panel.center.x+5,panel.y+42,(w-50)/2,35),"Invest in the yard"))investmentsOpen=true;
            if(investmentsOpen){DrawInvestments(panel);return;}
            GUI.Label(new Rect(panel.x+20,panel.y+100,w-40,190),"Sales and contract income today: €"+Model.State.incomeToday+"\nCash on hand: €"+Model.State.money+"\nCompleted customer orders: "+Model.State.orderIndex+"\nFans restored: "+Model.State.fansRepaired+" / dismantled: "+Model.State.fansDismantled+"\nRadios restored: "+Model.State.radiosRepaired+" / dismantled: "+Model.State.radiosDismantled+"\nSalvage fans left today: "+Mathf.Max(0,Model.Rules.fanDailyLimit-Model.State.fansTakenToday)+" / radios: "+Mathf.Max(0,Model.Rules.radioDailyLimit-Model.State.radiosTakenToday),wrappedLabel);
            GUI.Label(new Rect(panel.x+20,panel.y+315,w-40,110),"Return tomorrow for a fresh appliance-salvage delivery. Inventory, repairs and customer orders are kept; the powered stripper finishes its current load overnight. There are no deadlines or daily fees.",wrappedLabel);
            GUI.enabled=Model.State.dayIndex<int.MaxValue;
            if (GUI.Button(new Rect(panel.x+20,panel.y+475,w-40,45),"Finish day / return on day "+((long)Model.State.dayIndex+2)))
            {
                if (Model.AdvanceDay()) { SyncViews();Save();SetPaused(false);Tell("A fresh day at the yard. Fresh fans and radios have arrived at salvage.");Beep(1.3f); }
            }
            GUI.enabled=true;
            if (GUI.Button(new Rect(panel.x+20,panel.yMax-55,w-40,40),"Keep working / Back (Escape)")) dayOpen=false;
        }
        void DrawInvestments(Rect panel)
        {
            GUI.Label(new Rect(panel.x+20,panel.y+87,panel.width-40,30),"Cash available: €"+Model.State.money);
            DrawInvestment(panel,120,YardUpgrade.StorageRack,"Storage rack","Add up to 12 yard bundle slots. Stored material stays in its existing bins.");
            DrawInvestment(panel,245,YardUpgrade.HandTools,"Better hand tools","One fewer hand-work step for wire stripping, appliance repair and dismantling; material yields stay the same.");
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
            float panelWidth=Mathf.Min(width-30,900),panelHeight=Mathf.Min(height-30,710);
            var panel=new Rect((width-panelWidth)/2,(height-panelHeight)/2,panelWidth,panelHeight);
            GUI.Box(panel,"SCRAPSHIFT / YARD MAP");
            float aspect=YardWorldLayout.HalfWidth/YardWorldLayout.HalfDepth;
            float mapWidth=Mathf.Min(panel.width-285,(panel.height-175)*aspect);
            var map=new Rect(panel.x+20,panel.y+48,mapWidth,mapWidth/aspect);
            GUI.Box(map,"NORTH / LOADING & STORAGE");
            GUI.color=new Color(.75f,.70f,.55f);
            GUI.DrawTexture(new Rect(map.center.x-3,map.y+25,6,map.height-30),Texture2D.whiteTexture);
            float laneY=map.y+(YardWorldLayout.HalfDepth+13)/(YardWorldLayout.HalfDepth*2)*map.height;
            GUI.DrawTexture(new Rect(map.x+10,laneY,map.width-20,6),Texture2D.whiteTexture);
            GUI.color=YardGeometry.Ivory;
            MapLabel(map,-29,9,"VEHICLE SALVAGE"); MapLabel(map,30,9,"METAL SORTING");
            MapLabel(map,-16,-30,"OFFICE"); MapLabel(map,0,-37,"ENTRY GATE");
            var p=player.transform.position;
            var route=navigator.Resolve(Model,p.x,p.z);
            float listX=map.xMax+15,listWidth=panel.xMax-listX-15;
            for(int i=0;i<YardNavigation.Destinations.Length;i++)
            {
                var destination=YardNavigation.Destinations[i];
                bool active=destination.landmark==route.landmark;
                GUI.color=active?new Color(.93f,.76f,.45f):Color.white;
                float px=map.x+(destination.x+YardWorldLayout.HalfWidth)/(YardWorldLayout.HalfWidth*2)*map.width;
                float py=map.y+(YardWorldLayout.HalfDepth-destination.z)/(YardWorldLayout.HalfDepth*2)*map.height;
                // Compact numbered pins remain distinct around the dense workshop hub.
                if(GUI.Button(new Rect(px-11,py-10,22,20),((int)destination.landmark).ToString("00"),mapPin) ||
                    GUI.Button(new Rect(listX,panel.y+48+i*34,listWidth,30),destination.mapLabel))
                { navigator.Select(destination.landmark);SetPaused(false);return; }
            }
            GUI.color=new Color(.6f,1,.55f);
            float playerX=map.x+(p.x+YardWorldLayout.HalfWidth)/(YardWorldLayout.HalfWidth*2)*map.width;
            float playerY=map.y+(YardWorldLayout.HalfDepth-p.z)/(YardWorldLayout.HalfDepth*2)*map.height;
            GUI.DrawTexture(new Rect(playerX-3,playerY-3,6,6),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(map.x,map.yMax+8,map.width,65),"Green dot: you. Select a station to track it and resume. Follow the gravel lanes around obstacles.\n"+hudRoute,controlLegend);
            if(GUI.Button(new Rect(listX,panel.y+48+YardNavigation.Destinations.Length*34+10,listWidth,40),"Follow objective"))
            { navigator.Select(YardLandmark.Automatic);SetPaused(false);return; }
            if(GUI.Button(new Rect(panel.x+20,panel.yMax-55,panel.width-40,40),"Back / Escape"))mapOpen=false;
        }
        void MapLabel(Rect map, float x, float z, string text)
        {
            float px = map.x + (x + YardWorldLayout.HalfWidth) / (YardWorldLayout.HalfWidth * 2) * map.width;
            float py = map.y + (YardWorldLayout.HalfDepth - z) / (YardWorldLayout.HalfDepth * 2) * map.height;
            GUI.Label(new Rect(px - 80, py - 20, 160, 40), text, mapLabel);
        }
    }
}
