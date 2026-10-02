using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        GUIStyle wrap,small;
        Vector2 menuScroll;
        void OnGUI()
        {
            if(Model==null)return;
            float scale=Mathf.Clamp(Mathf.Min(Screen.height/800f,Screen.width/960f),.25f,2);
            using(theme.Begin(scale))
            {
                float width=Screen.width/scale,height=Screen.height/scale;
                if(wrap==null){wrap=new GUIStyle(GUI.skin.label){wordWrap=true};small=new GUIStyle(wrap){fontSize=14};}
                if(paused){GUI.color=new Color(0,0,0,.46f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;}
                if(settings.IsOpen){settings.Draw(width,height);return;}
                if(page==Page.None){DrawHud(width,height);return;}
                if(page==Page.Title){DrawTitle(width,height);return;}
                if(page==Page.Pause){DrawPause(width,height);return;}
                var panel=Panel(width,height,PageTitle(),780,720);
                GUILayout.BeginArea(new Rect(panel.x+22,panel.y+52,panel.width-44,panel.height-145));
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
                }
                GUILayout.EndScrollView();GUILayout.EndArea();
                if(Time.unscaledTime<messageUntil)GUI.Label(new Rect(panel.x+22,panel.yMax-92,panel.width-44,34),message,small);
                if(GUI.Button(new Rect(panel.x+22,panel.yMax-54,panel.width-44,38),page==Page.Welcome?"Open the yard / Escape":"Back / Escape"))
                {if(page==Page.Welcome)FinishWelcome();else Back();}
            }
        }
        Rect Panel(float width,float height,string title,float desiredWidth=700,float desiredHeight=680)
        {
            float w=Mathf.Min(desiredWidth,width-40),h=Mathf.Min(desiredHeight,height-40);
            var r=new Rect((width-w)*.5f,(height-h)*.5f,w,h);GUI.Box(r,"");
            GUI.Label(new Rect(r.x+22,r.y+13,r.width-44,34),title);return r;
        }
        string PageTitle()
        {
            switch(page)
            {
                case Page.Welcome:return "YOUR FIRST SCRAPSHIFT";
                case Page.Help:return "HOW TO PLAY / CURRENT CONTROLS";
                case Page.Catalogue:return "EQUIPMENT CATALOGUE / €"+Model.State.money+" / LEVEL "+Model.Level;
                case Page.Equipment:return "EQUIPMENT / OUTPUT / POWER";
                case Page.LargeScrap:return "SCRAP INSPECTION";
                case Page.Sales:return "SALES COUNTER / LEVEL "+Model.Level+" / +"+Model.SaleBonusPercent+"%";
                case Page.Delivery:return "SCRAP DELIVERY";
                case Page.Import:return "OPTIONAL LEGACY PROGRESS IMPORT";
                default:return "SCRAPSHIFT";
            }
        }
        void Text(string text){GUILayout.Label(text,wrap);GUILayout.Space(8);}
        bool Button(string text,bool enabled=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool clicked=GUILayout.Button(text,GUILayout.MinHeight(36));GUI.enabled=previous;return clicked;
        }
        void DrawTitle(float width,float height)
        {
            var r=Panel(width,height,"SCRAPSHIFT / YOUR YARD, YOUR WAY",690,690);
            if(logo!=null)GUI.DrawTexture(new Rect(r.x+70,r.y+51,r.width-140,105),logo,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(r.x+24,r.y+163,r.width-48,57),"Start small. Take forgotten machines apart, sell what you recover and build a yard that works for you.",wrap);
            float x=r.x+24,w=r.width-48,y=r.y+238;
            bool previous=GUI.enabled;GUI.enabled=!saveBlocked;
            if(GUI.Button(new Rect(x,y,w,42),hasSave?"Continue compact yard":"Start compact yard"))BeginYard();GUI.enabled=previous;
            if(GUI.Button(new Rect(x,y+48,w,38),"Settings"))settings.Open();
            if(GUI.Button(new Rect(x,y+92,w,38),"How to play"))Show(Page.Help);
            if(GUI.Button(new Rect(x,y+136,w,38),confirmNew?"Confirm new yard / current compact files archived":"New compact yard"))
            {if(confirmNew)NewYard();else confirmNew=true;}
            if(File.Exists(LegacyPath)||File.Exists(LegacyPath+".bak"))
                if(GUI.Button(new Rect(x,y+180,w,38),"Import compatible legacy progress…"))Show(Page.Import);
            if(GUI.Button(new Rect(x,y+224,w,38),"Open legacy yard"))OpenLegacy();
            if(GUI.Button(new Rect(x,y+268,w,38),"Quit"))Quit();
            GUI.Label(new Rect(x,r.yMax-82,w,67),saveBlocked?saveNotice:Model.State.importedLegacy?"Legacy source and full snapshot retained. Old repairs and requests continue in the legacy scene.":saveNotice,small);
        }
        void DrawPause(float width,float height)
        {
            var r=Panel(width,height,"YARD PAUSED / €"+Model.State.money+" / LEVEL "+Model.Level,650,635);
            float x=r.x+24,w=r.width-48,y=r.y+60;
            if(GUI.Button(new Rect(x,y,w,42),"Resume / Escape")){page=Page.None;Pause(false);}
            if(GUI.Button(new Rect(x,y+50,w,40),"Settings"))settings.Open();
            if(GUI.Button(new Rect(x,y+98,w,40),"Buy and place equipment"))Show(Page.Catalogue);
            if(GUI.Button(new Rect(x,y+146,w,40),"How to play / controls"))Show(Page.Help);
            if(GUI.Button(new Rect(x,y+194,w,40),"Save yard")){if(Save())Tell("Compact yard saved.");}
            if(GUI.Button(new Rect(x,y+242,w,40),"Return to title")){if(Save()){page=Page.Title;Pause(true);}}
            if(GUI.Button(new Rect(x,y+290,w,40),"Save and quit"))Quit();
            GUI.Label(new Rect(x,y+352,w,85),Time.unscaledTime<messageUntil?message:"Only the office, sales counter, receiving area and fence are fixed. Place your equipment anywhere the preview shows clear ground.",wrap);
            GUI.Label(new Rect(x,r.yMax-62,w,45),"Your yard and Settings are saved separately. Escape always stays available.",small);
        }
        void DrawWelcome()
        {
            Text(Model.State.importedLegacy?"Your compatible progress is in the compact yard. Existing bundles and wire jobs are retained. Order replacement scrap at delivery, or use free wiring. Your old yard remains available from the title.":"A compact 48 × 36 metre yard, one manual bench, a scrap car and a refrigerator. There are no daily fees or deadlines. Your equipment layout is yours to design.");
            Text("1. Look at a car or refrigerator and press ["+controls.Label(ControlAction.Interact)+"] to inspect it. Close the inspection, then use separate ["+controls.Label(ControlAction.ManualWork)+"] strokes with empty hands. Inspect again to remove each component.");
            Text("2. Carry a motor, compressor, wiring or metal casing to your manual bench. ["+controls.Label(ControlAction.Interact)+"] loads it; ["+controls.Label(ControlAction.ManualWork)+"] processes it. Collect every output, then sell materials at the office counter.");
            Text("3. Earn money and sale XP. Open the equipment catalogue ["+controls.Label(ControlAction.BuildToggle)+"], buy a generator and a Tier 1 scrapper, and connect their power ports. Its powered work replaces hand strokes; input and collection still need you.");
            Text("Free wiring at delivery keeps you earning if cash is low. Put items down with ["+controls.Label(ControlAction.Drop)+"]. Escape pauses work and opens Settings.");
            Text("Level 10 is the planned conveyor/storage/Tier 2 milestone. Those later production stages are displayed in the catalogue, but are not purchasable in this stage.");
            if(Model.State.importedLegacy)Text(Model.State.legacyNotice);
            if(Button("Settings"))settings.Open();
        }
        void DrawHelp()
        {
            Text("CURRENT BINDINGS\nWalk: ["+controls.Label(ControlAction.MoveForward)+"] forward, ["+controls.Label(ControlAction.MoveBackward)+"] back, ["+controls.Label(ControlAction.MoveLeft)+"] left, ["+controls.Label(ControlAction.MoveRight)+"] right. Mouse look.\n["+controls.Label(ControlAction.Interact)+"] inspect/use/pick up; ["+controls.Label(ControlAction.ManualWork)+"] one work stroke; ["+controls.Label(ControlAction.Drop)+"] put down.\n["+controls.Label(ControlAction.BuildToggle)+"] catalogue/cancel construction; ["+controls.Label(ControlAction.BuildRotate)+"] rotate preview. Escape cancels construction or returns from menus.");
            Text("CAR → MOTOR / WIRING / BODY METAL\nREFRIGERATOR → COMPRESSOR / WIRING / CASING / PLASTIC\nInspect, finish its individual dismantling steps, then remove the components. Whole cars and refrigerators stay in the receiving area. Hands must be empty for manual work.");
            foreach(var recipe in Model.Rules.recipes)
                Text(Model.Rules.Part(recipe.input).name+" → "+YieldText(recipe.yields)+" / "+recipe.strokes+" strokes per "+recipe.inputQuantity+" input");
            Text("Each input is reserved once, then its exact outputs remain until collected. A machine with uncollected output cannot accept another load. Power cuts and overloaded networks preserve inputs and progress. Pause/Settings stop every processing clock.");
            Text("POWER & BUILDING\nSelect a catalogue item, aim at nearby gravel and rotate its ghost. Green is valid; red explains the blockage. Toggle optional grid snap in the catalogue. Confirm with ["+controls.Label(ControlAction.Interact)+"]. Cancel is free. Keep the entrance and receiving area clear. Empty equipment and disconnect its cables before moving or dismantling it. Keep at least one manual bench.");
            Text("Inspect powered equipment to connect or disconnect a cable to another nearby port. A generator supplies its connected network; if combined machine demand exceeds supply every consumer pauses. Generators have no fuel cost in this stage.");
            Text("PROGRESSION\nOnly completed sales of eligible recovered materials earn XP. Collection, moving, purchases and repeated button presses do not. Levels increase the editable material-sale bonus. Replacement cars and refrigerators are bought at delivery; renewable free wiring protects the basic earning loop.");
            Text("CURRENT STAGES\nManual dismantling, component processing, sales/XP, free equipment placement, generators and manual-feed Tier 1 are implemented in source. Level-10 conveyor networks, ported storage, Tier 2 and automated intake/export remain later stages.");
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
            build.GridSnap=GUILayout.Toggle(build.GridSnap,"Snap previews to a 0.5 metre grid");GUILayout.Space(12);
            foreach(var d in Model.Rules.equipment)
            {
                string requirement=!d.available?"Planned stage / level "+d.unlockLevel:Model.Level<d.unlockLevel?"Requires level "+d.unlockLevel:Model.State.money<d.price?"Save €"+(d.price-Model.State.money)+" more":"Ready to place";
                Text(d.name+" / €"+d.price+" / "+d.width.ToString("0.#")+" × "+d.depth.ToString("0.#")+"m\n"+requirement+(d.powerOutput>0?" / supplies "+d.powerOutput+" kW":"")+(d.powerDemand>0?" / draws "+d.powerDemand+" kW":""));
                if(Button("Choose "+d.name,d.available&&Model.Level>=d.unlockLevel&&Model.State.money>=d.price))BeginBuild(d.kind);
                GUILayout.Space(12);
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
            Text(recipe.name+" / "+scrap.strokes+" of "+Model.RequiredScrapStrokes(scrap.id)+" dismantling steps");
            Text(Model.ScrapWorkStage(scrap.id));Text("Recover: "+YieldText(scrap.remaining??recipe.yields));
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
            var equipment=Model.FindEquipment(selectedId);if(equipment==null){Text("Equipment was removed.");return;}
            var definition=Model.Rules.Equipment(equipment.kind);
            Text(definition.name+" / "+definition.width.ToString("0.#")+" × "+definition.depth.ToString("0.#")+"m");
            if(equipment.job!=null)
            {
                var job=equipment.job;
                Text(Model.Rules.Part(job.input).name+" ×"+job.inputQuantity+" reserved / output: "+YieldText(job.yields));
                Text(job.ready?"Ready to collect":equipment.kind==EquipmentKind.Workbench?"Hand work "+job.strokes+" / "+job.requiredStrokes+". Close this page and use ["+controls.Label(ControlAction.ManualWork)+"].":"Processing time left: "+job.remaining.ToString("0.0")+"s. "+Model.ProcessingBlockReason(equipment.id));
                for(int i=0;i<job.yields.Length;i++)
                {
                    int slot=i;var output=job.yields[i];if(output.quantity<=0)continue;
                    if(Button("Collect "+Model.Rules.Part(output.kind).name+" ×"+output.quantity,job.ready&&Model.State.carriedId==0))
                        if(Act(()=>Model.CollectOutput(selectedId,slot))){page=Page.None;Pause(false);return;}
                }
            }
            else Text("Empty. Carry a component here and use ["+controls.Label(ControlAction.Interact)+"] to load it.");
            if(definition.powerOutput>0||definition.powerDemand>0)
            {
                var power=Construction.PowerFor(selectedId);Text("POWER / "+power.reason+"\nSupply "+power.supply+" kW / demand "+power.demand+" kW. Cable range "+Model.Rules.CableRange+"m.");
                foreach(var other in Model.State.equipment)
                {
                    if(other.id==selectedId)continue;var d=Model.Rules.Equipment(other.kind);if(d.powerOutput<=0&&d.powerDemand<=0)continue;
                    int partner=other.id;bool linked=Construction.CanDisconnect(selectedId,partner,out string reason);
                    bool allowed=linked||Construction.CanConnect(selectedId,partner,out reason);
                    if(Button((linked?"Disconnect from ":"Connect to ")+d.name+" #"+partner,allowed))
                        ConstructionAct(()=>linked?Construction.Disconnect(selectedId,partner):Construction.Connect(selectedId,partner));
                    if(!allowed)GUILayout.Label(reason,small);
                }
            }
            bool canMove=Construction.CanMove(selectedId,out string moveReason);
            Text("PLACEMENT / "+moveReason);
            if(Button("Move this equipment",canMove))BeginBuild(equipment.kind,selectedId);
            bool canRemove=Construction.CanRemove(selectedId,out string removeReason);
            if(Button(confirmRemove?"Confirm dismantle / refund €"+Construction.RefundFor(selectedId):"Dismantle / refund €"+Construction.RefundFor(selectedId),canRemove))
            {
                if(confirmRemove){if(ConstructionAct(()=>Construction.Remove(selectedId))){page=Page.None;Pause(false);}}
                else confirmRemove=true;
            }
            if(!canRemove)Text(removeReason);
        }
        bool ConstructionAct(Func<bool> action)
        {
            bool changed=action();Tell(Construction.LastMessage);
            if(changed){sounds.Play(YardSound.Tool);SyncViews();Save();}return changed;
        }
        void DrawSales()
        {
            var quote=Model.SaleQuote();
            if(Model.Carried==null)Text("Carry a recovered material bundle to this counter. Prices below are per unit before the level bonus.");
            else
            {
                Text(quote.name+" ×"+quote.quantity+" / unit €"+quote.unitPrice+"\nBase €"+quote.baseTotal+" + "+quote.bonusPercent+"% material bonus (€"+quote.bonusTotal+") = €"+quote.total+"\nSale XP: "+quote.experience+" / "+quote.reason);
                if(Button("Sell carried bundle for €"+quote.total,quote.allowed))
                    if(Act(()=>Model.Sell(),YardSound.Sale)){page=Page.None;Pause(false);return;}
            }
            Text("LEVEL "+Model.Level+" / total "+Model.State.experience+" XP / "+(Model.XPToNextLevel>0?Model.XPToNextLevel+" XP to next level":"maximum configured level"));
            foreach(var part in Model.Rules.parts)if(part.unitPrice>0)
                Text(part.name+" / €"+part.unitPrice+" per unit / "+(part.isMaterial?part.saleXp+" XP per eligible unit":"no recovery XP"));
        }
        void DrawDelivery()
        {
            Text("Buy another full-size object when its receiving space is clear. These objects stay in the yard while you dismantle them. The free wiring crate remains available if cash is low.");
            foreach(var recipe in Model.Rules.largeRecipes)
            {
                var kind=recipe.kind;Text(recipe.name+" / €"+recipe.purchasePrice+" / "+recipe.strokes+" manual stages\nComponents: "+YieldText(recipe.yields));
                if(Button("Buy "+recipe.name,Model.State.money>=recipe.purchasePrice))Act(()=>Model.BuyScrap(kind));
                GUILayout.Space(12);
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
            if((sessionStarted||hasSave)&&!saveBlocked&&!Save())return;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
        }
        void DrawHud(float width,float height)
        {
            GUI.Box(new Rect(18,18,370,65),"");GUI.Label(new Rect(30,25,347,26),hudTitle);
            Fill(new Rect(30,57,347,5),new Color(.25f,.29f,.24f));Fill(new Rect(30,57,347*Model.LevelFraction,5),new Color(.69f,.62f,.34f));
            if(presentation.Preferences.showFrameRate)GUI.Label(new Rect(width-232,18,215,30),fpsLabel,small);
            GUI.Label(new Rect(23,91,540,48),hudObjective,small);
            if(hudHeld.Length>0){GUI.Box(new Rect(width*.5f-285,height-155,570,35),"");GUI.Label(new Rect(width*.5f-273,height-152,546,29),hudHeld,small);}
            if(IsBuilding)
            {
                GUI.Box(new Rect(width*.5f-330,height-113,660,88),"");
                GUI.Label(new Rect(width*.5f-316,height-107,632,70),Model.Rules.Equipment(build.SelectedKind).name+" / "+buildReason+"\n["+controls.Label(ControlAction.Interact)+"] confirm   ["+controls.Label(ControlAction.BuildRotate)+"] rotate   Escape cancel",wrap);
            }
            else
            {
                Fill(new Rect(width*.5f-2,height*.5f-2,4,4),target!=null?new Color(.79f,.72f,.44f):Color.white);
                if(hudHint.Length>0)
                {GUI.Box(new Rect(width*.5f-330,height-103,660,75),"");GUI.Label(new Rect(width*.5f-316,height-97,632,63),hudHint,wrap);}
            }
            if(Time.unscaledTime<messageUntil&&message.Length>0)GUI.Label(new Rect(22,height-218,540,55),message,small);
        }
        static void Fill(Rect rect,Color color)
        {Color before=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=before;}
    }
}
