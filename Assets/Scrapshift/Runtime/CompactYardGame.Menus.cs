using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        GUIStyle wrap,small,hudBody,hudHeading,menuHeading,eyebrow,card,primaryButton;
        bool showLegacyMenus;
        Page drawnPage=Page.None;
        Vector2 menuScroll;
        readonly GUIContent measuredText=new GUIContent();
        float MeasuredHeight(GUIStyle style,string text,float width)
        {measuredText.text=text??"";return style.CalcHeight(measuredText,width);}
        void OnGUI()
        {
            if(Model==null)return;
            if(page!=drawnPage||settings.IsOpen){ResetRequestReview();ResetIndustryReview();drawnPage=page;}
            float scale=Mathf.Clamp(Mathf.Min(Screen.height/800f,Screen.width/960f),.25f,2);
            using(theme.Begin(scale))
            {
                float width=Screen.width/scale,height=Screen.height/scale;
                if(wrap==null)
                {
                    wrap=new GUIStyle(GUI.skin.label){wordWrap=true};small=new GUIStyle(wrap){fontSize=14};
                    hudBody=new GUIStyle(wrap){fontSize=16};hudHeading=new GUIStyle(wrap){fontSize=19,fontStyle=FontStyle.Bold};
                    menuHeading=new GUIStyle(wrap){fontSize=23,fontStyle=FontStyle.Bold};
                    eyebrow=new GUIStyle(small){fontSize=13,fontStyle=FontStyle.Bold};
                    eyebrow.normal.textColor=new Color(.79f,.71f,.49f);
                    card=new GUIStyle(GUI.skin.box){padding=new RectOffset(16,16,12,12)};
                    primaryButton=new GUIStyle(GUI.skin.button){fontSize=19,fontStyle=FontStyle.Bold};
                }
                if(paused){GUI.color=new Color(0,0,0,.46f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;}
                if(settings.IsOpen){settings.Draw(width,height);return;}
                if(page==Page.None){DrawHud(width,height);return;}
                if(page==Page.Title){DrawTitle(width,height);return;}
                if(page==Page.Pause){DrawPause(width,height);return;}
                var panel=Panel(width,height,PageTitle(),780,720);
                GUILayout.BeginArea(new Rect(panel.x+22,panel.y+66,panel.width-44,panel.height-171));
                menuScroll=GUILayout.BeginScrollView(menuScroll);
                switch(page)
                {
                    case Page.Welcome:DrawWelcome();break;
                    case Page.Help:DrawHelp();break;
                    case Page.Catalogue:DrawCatalogue();break;
                    case Page.Equipment:DrawEquipment();break;
                    case Page.LargeScrap:DrawLargeScrap();break;
                    case Page.Sales:DrawSales();break;
                    case Page.Delivery:DrawDelivery();break;
                    case Page.Import:DrawImport();break;
                    case Page.Journal:DrawJournal();break;
                    case Page.Contracts:DrawContracts();break;
                    case Page.Credits:DrawCredits();break;
                }
                GUILayout.EndScrollView();GUILayout.EndArea();
                if(Time.unscaledTime<messageUntil)GUI.Label(new Rect(panel.x+22,panel.yMax-99,panel.width-44,43),message,small);
                if(GUI.Button(new Rect(panel.x+22,panel.yMax-54,panel.width-44,38),page==Page.Welcome?"Open the yard / Escape":"Back / Escape"))
                {if(page==Page.Welcome)FinishWelcome();else Back();}
            }
        }
        Rect Panel(float width,float height,string title,float desiredWidth=700,float desiredHeight=680)
        {
            float w=Mathf.Min(desiredWidth,width-40),h=Mathf.Min(desiredHeight,height-40);
            var r=new Rect((width-w)*.5f,(height-h)*.5f,w,h);GUI.Box(r,"");
            Fill(new Rect(r.x+1,r.y+1,r.width-2,5),new Color(.66f,.53f,.31f));
            GUI.Label(new Rect(r.x+22,r.y+18,r.width-44,37),title,menuHeading);return r;
        }
        string PageTitle()
        {
            switch(page)
            {
                case Page.Welcome:return "Welcome to your yard";
                case Page.Help:return "The yard handbook";
                case Page.Catalogue:return "Equipment catalogue  /  €"+Model.State.money;
                case Page.Equipment:return "Equipment & connections";
                case Page.LargeScrap:return "Scrap inspection";
                case Page.Sales:return "Material sales";
                case Page.Delivery:return "The receiving yard";
                case Page.Import:return "Bring progress from your earlier yard";
                case Page.Journal:return "Your workshop journal";
                case Page.Contracts:return "Neighbourhood requests";
                case Page.Credits:return "Made for a quieter kind of shift";
                default:return "SCRAPSHIFT";
            }
        }
        void Text(string text){GUILayout.Label(text,wrap);GUILayout.Space(8);}
        bool Button(string text,bool enabled=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool clicked=GUILayout.Button(text,GUILayout.MinHeight(36));GUI.enabled=previous;return clicked;
        }
        void Section(string title,string body=null)
        {
            GUILayout.Space(10);GUILayout.Label(title,eyebrow);
            if(!string.IsNullOrEmpty(body))Text(body);
        }
        void BeginCard(string heading)
        {GUILayout.BeginVertical(card);if(!string.IsNullOrEmpty(heading))GUILayout.Label(heading,hudHeading);}
        void EndCard(){GUILayout.EndVertical();GUILayout.Space(10);}
        bool Primary(string text,bool enabled=true)
        {
            bool before=GUI.enabled;GUI.enabled=before&&enabled;
            bool clicked=GUILayout.Button(text,primaryButton,GUILayout.MinHeight(44));GUI.enabled=before;return clicked;
        }
        void Progress(float fraction,string label)
        {
            GUILayout.Label(label,small);
            Rect line=GUILayoutUtility.GetRect(1,8,GUILayout.ExpandWidth(true));
            Fill(line,new Color(.24f,.29f,.25f));
            Fill(new Rect(line.x,line.y,line.width*Mathf.Clamp01(fraction),line.height),new Color(.70f,.62f,.38f));
            GUILayout.Space(8);
        }
        string YardSummary()
        {
            return "€"+Model.State.money+" in the till  •  Level "+Model.Level+"  •  "+Model.State.equipment.Count+" equipment"+
                (Model.State.belts.Count>0?"  •  "+Model.State.belts.Count+" conveyor lines":"");
        }
        void DrawTitle(float width,float height)
        {
            var r=Panel(width,height,"SCRAPSHIFT",730,740);
            GUILayout.BeginArea(new Rect(r.x+24,r.y+62,r.width-48,r.height-91));
            menuScroll=GUILayout.BeginScrollView(menuScroll);
            if(logo!=null)
            {
                Rect logoRect=GUILayoutUtility.GetRect(1,86,GUILayout.ExpandWidth(true));
                GUI.DrawTexture(logoRect,logo,ScaleMode.ScaleToFit,true);
            }
            GUILayout.Label("Start with your bare hands.",menuHeading);
            Text("Turn forgotten machines into something useful. Build a scrapyard that works for you.");
            BeginCard(hasSave?"Your yard is waiting":"A small yard. A fresh start.");
            if(hasSave)
            {
                Text(YardSummary());
                if(Model.Career.CurrentGoal!=null)GUILayout.Label("Next in your journal: "+Model.Career.CurrentGoal.title,small);
                else GUILayout.Label("Starter chapter complete • Continue building your own production lines.",small);
            }
            else Text("A manual bench, a scrap car and a refrigerator are ready for your first shift. Take your time; there are no daily fees or deadlines.");
            if(Primary(hasSave?"Continue your yard":"Start your first shift",!saveBlocked))BeginYard();
            if(saveBlocked)Text("Your existing save could not be opened and is protected. Start a new yard below to keep the old files in an archive.");
            else if(!string.IsNullOrEmpty(saveStatus))GUILayout.Label(saveStatus,small);
            EndCard();
            if(Button("Settings"))settings.Open();
            if(Button("How to play"))Show(Page.Help);
            if(Button("Credits"))Show(Page.Credits);
            GUILayout.Space(8);
            if(Button(confirmNew?"Confirm new yard — archive current progress":"Start a new yard…"))
            {if(confirmNew)NewYard();else confirmNew=true;}
            if(confirmNew)
            {
                Text("Current compact-yard saves will be archived before a fresh yard is created. Your separate Settings and earlier yard remain available.");
                if(Button("Keep my current yard"))confirmNew=false;
            }
            if(Button(showLegacyMenus?"Hide earlier yard options":"Earlier yard options…"))showLegacyMenus=!showLegacyMenus;
            if(showLegacyMenus)
            {
                BeginCard("Your earlier yard");
                Text("Revisit the original workshop and its restoration jobs, or bring compatible progress into the compact yard.");
                if(File.Exists(LegacyPath)||File.Exists(LegacyPath+".bak"))
                    if(Button("Import earlier progress…"))Show(Page.Import);
                if(Button("Open earlier yard"))OpenLegacy();EndCard();
            }
            if(Button("Quit"))Quit();
            if(saveBlocked&&!string.IsNullOrEmpty(saveNotice))GUILayout.Label(saveNotice,small);
            if(Time.unscaledTime<messageUntil&&!string.IsNullOrEmpty(message))GUILayout.Label(message,small);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void DrawPause(float width,float height)
        {
            var r=Panel(width,height,"Shift paused",650,740);
            GUILayout.BeginArea(new Rect(r.x+24,r.y+64,r.width-48,r.height-92));
            menuScroll=GUILayout.BeginScrollView(menuScroll);
            Text(YardSummary());
            if(Primary("Back to the yard / Escape")){page=Page.None;Pause(false);}
            if(Button("Workshop journal ["+controls.Label(ControlAction.Journal)+"]"))Show(Page.Journal);
            if(Button("Neighbourhood requests"))Show(Page.Contracts);
            if(Button("Buy and place equipment ["+controls.Label(ControlAction.BuildToggle)+"]"))Show(Page.Catalogue);
            if(Button("Settings"))settings.Open();
            if(Button("How to play / controls"))Show(Page.Help);
            Section("YOUR PROGRESS");
            if(!string.IsNullOrEmpty(saveStatus))GUILayout.Label(saveStatus,small);
            if(Button("Save yard")){if(Save())Tell("Your yard is saved.");}
            if(Button("Save and return to title")){if(Save()){page=Page.Title;Pause(true);}}
            if(Button("Save and quit"))Quit();
            if(Time.unscaledTime<messageUntil&&!string.IsNullOrEmpty(message))Text(message);
            else GUILayout.Label("Machines and conveyors take a break with you. Escape always returns you from a menu.",small);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void DrawWelcome()
        {
            Section("YOUR FIRST SHIFT",Model.State.importedLegacy?
                "Your earlier work has a new home. The original workshop and its full progress are still available from the title.":
                "This little yard is yours now. Start with the car or refrigerator at the receiving area; your first bench is ready nearby.");
            BeginCard("01  /  Find something worth saving");
            Text("Look at delivered scrap and press ["+controls.Label(ControlAction.Interact)+"] to inspect it. Close its inspection, then use ["+controls.Label(ControlAction.ManualWork)+"] for each dismantling stroke with empty hands.");EndCard();
            BeginCard("02  /  Work it down");
            Text("Collect a motor, compressor, wiring or casing. Carry it to the manual bench: ["+controls.Label(ControlAction.Interact)+"] loads it, ["+controls.Label(ControlAction.ManualWork)+"] does the work. Collect each recovered material.");EndCard();
            BeginCard("03  /  Make your first sale");
            Text("Bring recovered materials to the office counter. Sales earn money and experience. Neighbours also have small requests, with an extra bonus when you finish their order.");EndCard();
            Section("A LITTLE HELP WHEN YOU NEED IT");
            Text("Press ["+controls.Label(ControlAction.Journal)+"] for your next step. Put down items with ["+controls.Label(ControlAction.Drop)+"]. Free wiring at delivery keeps you earning when money is low. Escape opens Pause and Settings.");
            Text("Use ["+controls.Label(ControlAction.BuildToggle)+"] to buy and place equipment as you earn the money. Your first powered line uses a generator, a Tier 1 scrapper, storage and a conveyor. Storage and belts unlock at level "+FirstLineLevel()+"; your manual bench also has conveyor ports, but you still do its tool strokes.");
            if(Button("Adjust my controls & settings"))settings.Open();
        }
        void DrawHelp()
        {
            Section("CONTROLS THAT FIT YOUR HANDS");
            Text("Walk: ["+controls.Label(ControlAction.MoveForward)+"] forward, ["+controls.Label(ControlAction.MoveBackward)+"] back, ["+controls.Label(ControlAction.MoveLeft)+"] left, ["+controls.Label(ControlAction.MoveRight)+"] right. Mouse look.\n["+controls.Label(ControlAction.Interact)+"] inspect/use/pick up; ["+controls.Label(ControlAction.ManualWork)+"] one work stroke; ["+controls.Label(ControlAction.Drop)+"] put down.\n["+controls.Label(ControlAction.Journal)+"] workshop journal. ["+controls.Label(ControlAction.BuildToggle)+"] catalogue/cancel construction; ["+controls.Label(ControlAction.BuildRotate)+"] rotate preview. Escape cancels construction or returns from menus.");
            Text("CAR → MOTOR / WIRING / BODY METAL\nREFRIGERATOR → COMPRESSOR / WIRING / CASING / PLASTIC\nInspect, finish its individual dismantling steps, then remove the components. Manual work keeps whole objects at receiving. A primary dismantler can instead load intact, uninspected scrap directly. Hands must be empty for manual work.");
            foreach(var recipe in Model.Rules.recipes)
                Text(Model.Rules.Part(recipe.input).name+" → "+YieldText(recipe.yields)+" / "+recipe.strokes+" strokes per "+recipe.inputQuantity+" input");
            Text("Each processing batch reserves its inputs once. Finished outputs stay safe until collected or taken by an outgoing belt. Component machines have separate IN and OUT bays, each with the listed capacity, so a full input queue does not block the current batch. The next batch starts when the previous outputs clear. Power cuts and overloaded networks preserve inputs and progress. Pause/Settings stop every processing clock.");
            Text("POWER & BUILDING\nSelect a catalogue item, aim at nearby gravel and rotate its ghost. Green is valid; red explains the blockage. Toggle optional grid snap in the catalogue. Confirm with ["+controls.Label(ControlAction.Interact)+"]. Cancel is free. Keep the entrance and receiving area clear. Empty equipment and disconnect its cables and conveyors before moving or dismantling it. Keep at least one manual bench.");
            Text("Inspect powered equipment to connect or disconnect a cable to another nearby port. A generator supplies its connected network; if combined machine demand exceeds supply every consumer pauses. Generators have no fuel cost.");
            Text("PROGRESSION\nOnly completed sales of eligible recovered materials earn XP. Collection, moving, purchases and repeated button presses do not. Levels increase the editable material-sale bonus. Replacement cars and refrigerators are bought at delivery; renewable free wiring protects the basic earning loop.");
            Text("YOUR FIRST PRODUCTION LINE\nBuy a generator and Tier 1 scrapper, connect their power, then add storage and conveyors when you can afford them. Basic storage and belts unlock at level "+FirstLineLevel()+". Deposit components into the scrapper's IN buffer or feed it from storage. It reserves one buffered recipe batch, processes with power and sends finished materials through OUT. Tier 2 is a faster, larger upgrade at level "+Model.Rules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel+"; Tier 1 already works with belts.");
            Text("CONVEYORS & STORAGE\nSelect a marked OUT, then aim at a different station's IN to snap a conveyor preview. ["+controls.Label(ControlAction.BuildRotate)+"] switches the corner; ["+controls.Label(ControlAction.Interact)+"] confirms. Inspect a station to manage its buffers and routes. Processing recipe filters select which components may enter; storage's output filter chooses which contents leave. Changing a filter retains existing stock. Splitters rotate between free outputs; mergers combine incoming lines. A full destination holds items on the belt and pauses the upstream line when its output fills. No transport grants XP.");
            Text("The manual bench also accepts components through IN and sends recovered materials through OUT. It prepares the next buffered recipe, but you must finish its strokes with ["+controls.Label(ControlAction.ManualWork)+"] and empty hands. Generators provide cable power, primary dismantlers receive whole objects, and dispatch stations take materials: their ports follow those roles.");
            Section("WHOLE-OBJECT AUTOMATION");
            Text("At level "+Model.Rules.Equipment(EquipmentKind.PrimaryScrapper).unlockLevel+", buy and place a primary dismantler. Feed an intact owned car or refrigerator without paying again, or review its standing-delivery terms. Recurring purchases start off; each delivery needs money, power and room. The delivery clock advances only while the machine is ready. Turning purchases off lets an already paid object finish.");
            Text("PRIMARY → COMPONENT STORAGE → POWERED SCRAPPER → MATERIAL STORAGE / DISPATCH. Tier 1 and Tier 2 both accept component belts and output recovered materials. Connect a material belt to a powered dispatch station, review a shipment, or explicitly turn automatic export on. Dispatch starts off and uses ordinary material prices and sale XP. It never pays customer-request bonuses. Fridges also yield ready plastic; use a sorting branch to route that material directly to dispatch while components go through a scrapper.");
            Section("YOUR OWN PACE");
            Text("Your journal follows your first dismantling job through sales, powered work and a connected storage line. Neighbourhood requests pay for matching recovered materials, with an extra bonus on the last delivery. Partial deliveries count and there are no deadlines.");
            Text("There are no daily fees. Free wiring remains available if your till is empty. Pause and Settings stop all work, and your yard saves automatically as you go.");
            if(Model.State.importedLegacy)Text(Model.State.legacyNotice);
            if(Button("Settings"))settings.Open();
        }
        string YieldText(PartAmount[] yields)
        {
            string result="";foreach(var output in yields)
                if(output.quantity>0)result+=(result.Length>0?", ":"")+Model.Rules.Part(output.kind).name+" ×"+output.quantity;
            return result.Length==0?"Collected":result;
        }
        void DrawCatalogue()
        {
            Text("Select equipment, then choose its position. No money is spent until you confirm a valid preview. Empty hands required. Move owned equipment by inspecting its body in the yard.");
            Text("FIRST POWERED LINE / Generator + Tier 1 scrapper. Add storage and connect OUT → IN with conveyors. Every purchased Tier 1 works with belts; compare later upgrades' cycle time and capacity below.");
            build.GridSnap=GUILayout.Toggle(build.GridSnap,"Snap previews to a 0.5 metre grid");GUILayout.Space(12);
            for(int group=0;group<2;group++)
            {
                bool heading=false;
                foreach(var d in Model.Rules.equipment)
                {
                    if(!d.available||(Model.Level>=d.unlockLevel)!=(group==0))continue;
                    if(!heading){Section(group==0?"AVAILABLE TO BUILD":"LATER UPGRADES");heading=true;}
                    string requirement=Model.Level<d.unlockLevel?"Unlocks at level "+d.unlockLevel:Model.State.money<d.price?"Save €"+(d.price-Model.State.money)+" more":"Ready to place";
                    string size=d.kind==EquipmentKind.Conveyor?" per 2m section / choose OUT then IN": " / "+d.width.ToString("0.#")+" × "+d.depth.ToString("0.#")+"m";
                    BeginCard(d.name);
                    Text("€"+d.price+size+"\n"+requirement+(d.powerOutput>0?" / supplies "+d.powerOutput+" kW":"")+(d.powerDemand>0?" / draws "+d.powerDemand+" kW":""));
                    Text(EquipmentRole(d));
                    if(Button("Choose "+d.name,Model.Level>=d.unlockLevel&&Model.State.money>=d.price))BeginBuild(d.kind);
                    EndCard();
                }
            }
        }
        int FirstLineLevel()
        {return Math.Max(Model.Rules.Equipment(EquipmentKind.Storage).unlockLevel,Model.Rules.Equipment(EquipmentKind.Conveyor).unlockLevel);}
        string EquipmentRole(EquipmentDefinition definition)
        {
            string capacity=" / "+definition.outputCapacity+" units "+(ScrappingModel.IsComponentProcessor(definition.kind)?"per IN and OUT bay":"total buffer capacity");
            switch(definition.kind)
            {
                case EquipmentKind.Workbench:return "Components IN → manual tool strokes → materials OUT"+capacity+". Belts supply and collect; you do the work.";
                case EquipmentKind.Generator:return "Cable power for connected machines. Its network must cover total demand; no material belts or fuel purchases.";
                case EquipmentKind.Tier1Scrapper:return "Components IN → powered recovery → materials OUT"+capacity+" / base cycle "+definition.processingSeconds.ToString("0.#")+"s. Automatic buffered batches and both conveyor ports work immediately; recipe times vary.";
                case EquipmentKind.Tier2Scrapper:return "Components IN → powered recovery → materials OUT"+capacity+" / base cycle "+definition.processingSeconds.ToString("0.#")+"s. Compare capacity, cycle time and power demand with your current line; recipe times vary.";
                case EquipmentKind.Storage:return "Items IN → stock buffer → items OUT"+capacity+". An output filter selects which stored kind leaves.";
                case EquipmentKind.Conveyor:return "Transfers single items automatically from a station's OUT to another station's IN. Price follows route length; blocked items wait safely.";
                case EquipmentKind.Splitter:return "One IN → three OUT. Sends matching items to the next free route; an output filter selects the item kind.";
                case EquipmentKind.Merger:return "Three IN → one OUT. Combines incoming lines; an output filter selects the item kind.";
                case EquipmentKind.PrimaryScrapper:return "Whole cars or fridges → components OUT"+capacity+" / "+definition.processingSeconds.ToString("0.#")+"s work. Recurring purchases start off; review their terms before enabling them.";
                case EquipmentKind.ExportStation:return "Materials IN → ordinary sales"+capacity+" / "+definition.processingSeconds.ToString("0.#")+"s dispatch interval. Automatic dispatch starts off.";
                default:return "Inspect placed equipment to manage its contents and connections.";
            }
        }
        void DrawLargeScrap()
        {
            var scrap=Model.FindScrap(selectedId);if(scrap==null){Text("All components removed. The receiving space is clear.");return;}
            var recipe=Model.Rules.LargeRecipe(scrap.kind);
            if(!scrap.inspected)
            {
                Text(recipe.name+" / not yet inspected. Put down any carried item before inspecting.");
                Text("Possible components: "+YieldText(recipe.yields));
                if(Button("Inspect this scrap",Model.State.carriedId==0))Act(()=>Model.InspectScrap(selectedId));
                return;
            }
            Section(recipe.name.ToUpperInvariant());
            Progress(scrap.requiredStrokes>0?(float)scrap.strokes/scrap.requiredStrokes:0,
                scrap.strokes+" / "+Model.RequiredScrapStrokes(scrap.id)+" dismantling steps");
            BeginCard(scrap.strokes<scrap.requiredStrokes?Model.ScrapWorkStage(scrap.id):"Ready to recover");
            Text("Components: "+YieldText(scrap.remaining??recipe.yields));EndCard();
            if(scrap.strokes<scrap.requiredStrokes)
                Text("Close this page and use ["+controls.Label(ControlAction.ManualWork)+"] at the object with empty hands. Each stroke finishes one stage.");
            else
                for(int i=0;i<scrap.remaining.Length;i++)
                {
                    int slot=i;var output=scrap.remaining[i];if(output.quantity<=0)continue;
                    if(Button("Remove "+Model.Rules.Part(output.kind).name+" ×"+output.quantity,Model.State.carriedId==0))
                        if(Act(()=>Model.CollectScrap(selectedId,slot))){page=Page.None;Pause(false);}
                }
        }
        void DrawEquipment()
        {
            var equipment=Model.FindEquipment(selectedId);if(equipment==null){Text("This equipment has been removed.");return;}
            var definition=Model.Rules.Equipment(equipment.kind);
            Section(definition.name.ToUpperInvariant(),definition.width.ToString("0.#")+" × "+definition.depth.ToString("0.#")+" metre footprint");
            BeginCard(equipment.job!=null&&equipment.job.ready?"Your recovered materials are ready":"At the work station");
            if(equipment.job!=null)
            {
                var job=equipment.job;
                Text("Loaded: "+Model.Rules.Part(job.input).name+" ×"+job.inputQuantity+"\nRecover: "+YieldText(job.yields));
                float fraction=job.ready?1:equipment.kind==EquipmentKind.Workbench?(job.requiredStrokes>0?(float)job.strokes/job.requiredStrokes:0):
                    job.duration>0?1-job.remaining/job.duration:0;
                string progress=job.ready?"Ready to collect":equipment.kind==EquipmentKind.Workbench?
                    job.strokes+" / "+job.requiredStrokes+" work strokes":job.remaining.ToString("0.0")+"s processing left";
                Progress(fraction,progress);
                if(job.ready)Text("An outgoing OUT belt takes these materials automatically when you close the menu. You can also collect a bundle by hand below. A full destination holds the output safely.");
                if(!job.ready)
                    Text(equipment.kind==EquipmentKind.Workbench?
                        "Return to the yard and use ["+controls.Label(ControlAction.ManualWork)+"] with empty hands to keep working.":
                        Model.ProcessingBlockReason(equipment.id)+". Menus pause processing; close when you are ready.");
                for(int i=0;i<job.yields.Length;i++)
                {
                    int slot=i;var output=job.yields[i];if(output.quantity<=0)continue;
                    if(Button("Collect "+Model.Rules.Part(output.kind).name+" ×"+output.quantity,job.ready&&Model.State.carriedId==0))
                        if(Act(()=>Model.CollectOutput(selectedId,slot))){page=Page.None;Pause(false);EndCard();return;}
                }
                if(Model.State.carriedId!=0)GUILayout.Label("Put down your carried item before collecting another bundle.",small);
            }
            else if(IsIndustryEquipment(equipment))Text(equipment.kind==EquipmentKind.PrimaryScrapper?
                "Load an intact owned car or refrigerator below. Standing deliveries stay off until you approve recurring purchases.":
                "Connect a material belt or deposit saleable material below. Automatic export stays off until you approve it.");
            else if(equipment.kind==EquipmentKind.Generator)Text("Ready to supply its connected network. Link enough generators to cover your machines' combined power demand.");
            else if(equipment.kind==EquipmentKind.Workbench)Text("Components arrive through IN or your carried bundle. The next buffered recipe prepares automatically; finish its strokes in the yard with ["+controls.Label(ControlAction.ManualWork)+"]. Finished materials leave through OUT or manual collection.");
            else if(equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper)Text("Components arrive through IN or your carried bundle. With power, the machine reserves each buffered recipe batch automatically and sends its finished materials through OUT. Each separate IN and OUT bay has the listed capacity; a full input queue leaves the current batch working, while a blocked OUT waits safely before the next batch.");
            else if(equipment.kind==EquipmentKind.Storage||equipment.kind==EquipmentKind.Splitter||equipment.kind==EquipmentKind.Merger)Text("Deposit or withdraw bundles below. Connected conveyors take care of moving them between stations.");
            else Text("Ready for the next load. Carry a component here and use ["+controls.Label(ControlAction.Interact)+"] to place it inside.");
            EndCard();
            DrawIndustry(equipment);
            if(AutomationModel.PortCount(equipment.kind,false)>0||AutomationModel.PortCount(equipment.kind,true)>0)
            {Section("CONTENTS & CONVEYOR ROUTES");DrawAutomation(equipment);}
            if(definition.powerOutput>0||definition.powerDemand>0)
            {
                var power=Construction.PowerFor(selectedId);
                BeginCard("Power network / "+power.reason);
                Text("Supply "+power.supply+" kW  •  Demand "+power.demand+" kW\nCable reach: "+Model.Rules.CableRange+" metres");
                foreach(var other in Model.State.equipment)
                {
                    if(other.id==selectedId)continue;var d=Model.Rules.Equipment(other.kind);if(d.powerOutput<=0&&d.powerDemand<=0)continue;
                    int partner=other.id;bool linked=Construction.CanDisconnect(selectedId,partner,out string reason);
                    bool allowed=linked||Construction.CanConnect(selectedId,partner,out reason);
                    float dx=equipment.x-other.x,dz=equipment.z-other.z;
                    string label=d.name+" / "+Mathf.Sqrt(dx*dx+dz*dz).ToString("0.0")+"m away";
                    if(Button((linked?"Disconnect ":"Connect ")+label,allowed))
                        ConstructionAct(()=>linked?Construction.Disconnect(selectedId,partner):Construction.Connect(selectedId,partner));
                    if(!allowed)GUILayout.Label(reason,small);
                }
                EndCard();
            }
            Section("MAKE IT FIT YOUR YARD");
            bool canMove=Construction.CanMove(selectedId,out string moveReason);
            Text(moveReason);
            if(Button("Move this equipment",canMove))BeginBuild(equipment.kind,selectedId);
            bool canRemove=Construction.CanRemove(selectedId,out string removeReason);
            if(Button(confirmRemove?"Confirm dismantle / refund €"+Construction.RefundFor(selectedId):"Dismantle / refund €"+Construction.RefundFor(selectedId),canRemove))
            {
                if(confirmRemove){if(ConstructionAct(()=>Construction.Remove(selectedId))){page=Page.None;Pause(false);}}
                else confirmRemove=true;
            }
            if(confirmRemove&&Button("Keep this equipment"))confirmRemove=false;
            if(!canRemove)Text(removeReason);
        }
        bool ConstructionAct(Func<bool> action)
        {
            bool changed=action();Tell(Construction.LastMessage);
            if(changed){sounds.Play(YardSound.Tool);SyncViews();RefreshCareer();Save();}return changed;
        }
        void DrawSales()
        {
            Section("TODAY'S MATERIAL RATE","Level "+Model.Level+" gives +"+Model.SaleBonusPercent+"% on material sales. "+
                (Model.XPToNextLevel>0?Model.XPToNextLevel+" XP until your next level.":"You have reached the current maximum level."));
            var quote=Model.SaleQuote();
            BeginCard(Model.Carried==null?"Bring your recovered materials":"On the counter / "+quote.name+" ×"+quote.quantity);
            if(Model.Carried==null)Text("Carry a material bundle here to see its value. You can sell it directly or deliver matching material to a neighbour below.");
            else
            {
                Text("€"+quote.unitPrice+" per unit  •  Base €"+quote.baseTotal+"  •  Level bonus €"+quote.bonusTotal);
                GUILayout.Label("Sale total  €"+quote.total,hudHeading);
                Text(quote.experience+" sale XP  /  "+quote.reason);
                if(Primary("Sell carried bundle / €"+quote.total,quote.allowed))
                    if(Act(()=>Model.Sell(),YardSound.Sale)){page=Page.None;Pause(false);EndCard();return;}
            }
            EndCard();
            DrawCustomerOffer(false);
            if(Button("Open neighbourhood requests"))Show(Page.Contracts);
            Section("PRICE BOARD","Prices per unit before your level bonus. Process components at a bench or scrapper to recover their valuable materials.");
            foreach(var part in Model.Rules.parts)if(part.unitPrice>0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(part.name,wrap,GUILayout.ExpandWidth(true));
                GUILayout.Label("€"+part.unitPrice+" / unit",small,GUILayout.Width(120));
                GUILayout.Label(part.isMaterial?part.saleXp+" XP":"—",small,GUILayout.Width(75));
                GUILayout.EndHorizontal();
            }
        }
        void DrawDelivery()
        {
            Text("Buy another full-size object when its receiving space is clear. These objects stay in the yard while you dismantle them. The free wiring crate remains available if cash is low.");
            foreach(var recipe in Model.Rules.largeRecipes)
            {
                var kind=recipe.kind;BeginCard(recipe.name);
                Text("€"+recipe.purchasePrice+" / "+recipe.strokes+" manual stages\nComponents: "+YieldText(recipe.yields));
                if(Button("Buy "+recipe.name+" / €"+recipe.purchasePrice,Model.State.money>=recipe.purchasePrice))Act(()=>Model.BuyScrap(kind));
                EndCard();
            }
        }
        void DrawImport()
        {
            Text(CompactLegacyImport.Explanation);
            Text("This creates a compact yard without the new-yard gifted car/fridge. Your original yard files remain untouched. Existing compact saves are archived before replacement. Imported quantities use current compact-yard prices; historical recovery XP is not guessed.");
            Text("Use Open legacy yard from the title for the complete original restoration/customer-order continuation. Nothing is silently reset or discarded.");
            if(Button(confirmNew?"Confirm import / archive current compact files":"Import compatible progress…"))
            {if(confirmNew)ImportLegacy();else confirmNew=true;}
        }
        void NewYard()
        {
            try
            {
                CompactSaveStore.Archive(SavePath);DestroyGhost();BindModel(null);saveBlocked=false;hasSave=false;sessionStarted=true;
                RestorePlayer();SyncViews();confirmNew=false;BeginYard();
            }
            catch(Exception ex){Tell("New yard could not start: "+ex.Message);}
        }
        void ImportLegacy()
        {
            try
            {
                var legacy=SaveStore.Read(LegacyPath,out string notice);
                string sourcePath=notice.StartsWith("Restored backup",StringComparison.Ordinal)?LegacyPath+".bak":LegacyPath;
                string snapshot=File.ReadAllText(sourcePath);
                var rules=legacyBalance!=null?legacyBalance.PreparedRules:new YardRules();
                var state=CompactLegacyImport.Convert(legacy,rules,Model.Rules,snapshot);
                // Validate the complete proposed conversion before archiving any compact save.
                CompactSaveStore.Validate(state,Model.Rules);CompactSaveStore.Archive(SavePath);
                DestroyGhost();BindModel(state);saveBlocked=false;hasSave=false;sessionStarted=true;
                RestorePlayer();SyncViews();bool saved=Save();page=Page.Welcome;menuScroll=Vector2.zero;Pause(true);
                if(saved)Tell("Compatible progress imported and saved; full legacy yard retained.");
            }
            catch(Exception ex){Tell("Import did not complete: "+ex.Message);}
        }
        void OpenLegacy()
        {
            if(!Application.CanStreamedLevelBeLoaded("Scrapyard")){Tell("Use Scrapshift → Create or Open Compact Yard to register the preserved legacy scene.");return;}
            if((hasSave||sessionStarted)&&!saveBlocked&&!Save())return;
            // Restore scene globals before the legacy bootstrap captures its own previous settings.
            if(lighting!=null)lighting.Dispose();if(presentation!=null)presentation.Dispose();
            SceneManager.LoadScene("Scrapyard");
        }
        void Quit()
        {
            controls.FlushPending();presentation.FlushPending();
            if((sessionStarted||hasSave)&&!saveBlocked&&!Save())return;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
        }
        void DrawHud(float width,float height)
        {
            float statusWidth=Mathf.Min(450,width-40),objectiveWidth=Mathf.Min(520,width*.52f);
            float titleHeight=Mathf.Max(28,MeasuredHeight(hudHeading,hudTitle,statusWidth-30));
            float statusHeight=titleHeight+46;
            GUI.Box(new Rect(18,18,statusWidth,statusHeight),"");
            Fill(new Rect(18,18,4,statusHeight),new Color(.68f,.57f,.34f));
            GUI.Label(new Rect(33,26,statusWidth-30,titleHeight),hudTitle,hudHeading);
            string levelText=Model.XPToNextLevel>0?Model.XPToNextLevel+" XP to level "+(Model.Level+1):"Highest level reached";
            GUI.Label(new Rect(33,28+titleHeight,statusWidth-30,24),levelText,small);
            Fill(new Rect(33,statusHeight+10,statusWidth-30,4),new Color(.25f,.29f,.24f));
            Fill(new Rect(33,statusHeight+10,(statusWidth-30)*Model.LevelFraction,4),new Color(.69f,.62f,.34f));
            if(presentation.Preferences.showFrameRate)
            {GUI.Box(new Rect(width-232,18,215,35),"");GUI.Label(new Rect(width-220,23,191,25),fpsLabel,small);}
            float objectiveTop=28+statusHeight;
            float objectiveTextHeight=MeasuredHeight(hudBody,hudObjective,objectiveWidth-26);
            float directionHeight=string.IsNullOrEmpty(hudDirection)?0:MeasuredHeight(small,hudDirection,objectiveWidth-26)+5;
            GUI.Box(new Rect(18,objectiveTop,objectiveWidth,objectiveTextHeight+46+directionHeight),"");
            GUI.Label(new Rect(31,objectiveTop+9,objectiveWidth-26,19),Model.Career.Completed?"YOUR YARD, YOUR WAY":"NEXT IN YOUR JOURNAL",eyebrow);
            GUI.Label(new Rect(31,objectiveTop+34,objectiveWidth-26,objectiveTextHeight+3),hudObjective,hudBody);
            if(directionHeight>0)GUI.Label(new Rect(31,objectiveTop+39+objectiveTextHeight,objectiveWidth-26,directionHeight),hudDirection,small);
            float contextWidth=Mathf.Min(690,width-40),contextLeft=(width-contextWidth)*.5f;
            string footer="["+controls.Label(ControlAction.Journal)+"] Journal   ["+controls.Label(ControlAction.BuildToggle)+"] Equipment   Esc Pause / Settings";
            float footerHeight=Mathf.Max(25,MeasuredHeight(small,footer,Mathf.Min(700,width-40)-24)+6);
            float contextBottom=height-footerHeight-24;
            float hintHeight=MeasuredHeight(hudBody,hudHint,contextWidth-28);
            float progressHeight=hudProgress>=0?MeasuredHeight(small,hudProgressLabel,contextWidth-28)+20:0;
            float contextHeight=hintHeight+24+progressHeight;
            if(hudHeld.Length>0)
            {
                float heldWidth=Mathf.Min(650,width-60);
                float heldHeight=MeasuredHeight(hudBody,hudHeld,heldWidth-26)+18;
                float heldTop=contextBottom-(IsBuilding?108:contextHeight+8)-heldHeight;
                GUI.Box(new Rect((width-heldWidth)*.5f,heldTop,heldWidth,heldHeight),"");
                GUI.Label(new Rect((width-heldWidth)*.5f+13,heldTop+8,heldWidth-26,heldHeight-15),hudHeld,hudBody);
            }
            if(IsBuilding)
            {
                Fill(new Rect(width*.5f-2,height*.5f-2,4,4),previewClear?new Color(.48f,.78f,.66f):Color.white);
                string buildHint=(beltStage>0?"Conveyor":Model.Rules.Equipment(build.SelectedKind).name)+" / "+buildReason+"\n["+
                    controls.Label(ControlAction.Interact)+"] "+(beltStage==1?"choose output":"confirm")+"   ["+
                    controls.Label(ControlAction.BuildRotate)+"] "+(beltStage>0?"change elbow":"rotate")+"   Escape cancel";
                float buildHeight=MeasuredHeight(hudBody,buildHint,contextWidth-28)+22;
                GUI.Box(new Rect(contextLeft,contextBottom-buildHeight,contextWidth,buildHeight),"");
                GUI.Label(new Rect(contextLeft+14,contextBottom-buildHeight+9,contextWidth-28,buildHeight-17),buildHint,hudBody);
            }
            else
            {
                Fill(new Rect(width*.5f-2,height*.5f-2,4,4),target!=null?new Color(.79f,.72f,.44f):Color.white);
                if(hudHint.Length>0)
                {
                    float top=contextBottom-contextHeight;
                    GUI.Box(new Rect(contextLeft,top,contextWidth,contextHeight),"");
                    Fill(new Rect(contextLeft+1,top+1,3,contextHeight-2),new Color(.62f,.61f,.39f));
                    GUI.Label(new Rect(contextLeft+14,top+9,contextWidth-28,hintHeight+3),hudHint,hudBody);
                    if(hudProgress>=0)
                    {
                        GUI.Label(new Rect(contextLeft+14,top+hintHeight+15,contextWidth-28,progressHeight-17),hudProgressLabel,small);
                        Fill(new Rect(contextLeft+14,contextBottom-11,contextWidth-28,5),new Color(.25f,.29f,.24f));
                        Fill(new Rect(contextLeft+14,contextBottom-11,(contextWidth-28)*Mathf.Clamp01(hudProgress),5),new Color(.70f,.62f,.38f));
                    }
                }
            }
            if(Time.unscaledTime<messageUntil&&message.Length>0)
            {
                float noticeWidth=Mathf.Min(380,width*.4f);
                float noticeHeight=MeasuredHeight(hudBody,message,noticeWidth-26)+22;
                GUI.Box(new Rect(width-noticeWidth-18,69,noticeWidth,noticeHeight),"");
                Fill(new Rect(width-noticeWidth-17,70,noticeWidth-2,3),new Color(.68f,.57f,.34f));
                GUI.Label(new Rect(width-noticeWidth-5,79,noticeWidth-26,noticeHeight-17),message,hudBody);
            }
            float footerWidth=Mathf.Min(700,width-40),footerTop=height-footerHeight-12;
            GUI.Box(new Rect(18,footerTop,footerWidth,footerHeight),"");
            GUI.Label(new Rect(30,footerTop+2,footerWidth-24,footerHeight-3),footer,small);
        }
        static void Fill(Rect rect,Color color)
        {Color before=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=before;}
    }
}
