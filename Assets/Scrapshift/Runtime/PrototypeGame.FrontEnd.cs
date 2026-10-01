using System.IO;
using UnityEngine;
namespace Scrapshift
{
    public sealed partial class PrototypeGame
    {
        bool titleOpen, helpOpen, introOpen, creditsOpen, chapterOpen, chapterReview, chapterCheckPending, sessionStarted, hasYardSave;
        int helpPage;
        Vector2 helpScroll;
        void InitializeFrontEnd()
        {
            hasYardSave=File.Exists(SavePath)||File.Exists(SavePath+".bak");
            titleOpen=true;SetPaused(true);
        }
        void BeginYard()
        {
            titleOpen=false;sessionStarted=true;
            if(!Model.State.introSeen && !YardJourney.HasProgress(Model.State))
            { introOpen=true;SetPaused(true);return; }
            if(TryShowOpeningCompletion())return;
            SetPaused(false);Save();
        }
        bool TryShowOpeningCompletion()
        {
            if(!sessionStarted || saveBlocked)return false;
            chapterCheckPending=false;
            if(!YardJourney.PresentOpeningCompletion(Model.State))return false;
            OpenChapterRecap();Save();return true;
        }
        void OpenChapterRecap(){chapterReview=false;chapterOpen=true;SetPaused(true);}
        void CloseOpeningChapter()
        {
            if(chapterReview){chapterOpen=chapterReview=false;helpOpen=true;SetPaused(true);}
            else SetPaused(false);
        }
        void DrawOpeningCompletion(float width,float height)
        {
            var panel=FrontPanel(width,height,"CHAPTER ONE / A YARD OF YOUR OWN",650,510);
            float x=panel.x+28,w=panel.width-56;
            GUI.Label(new Rect(x,panel.y+66,w,55),"THE YARD IS ESTABLISHED",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+129,w,100),"You recovered copper, earned your first income, helped a neighbour, powered the workshop, restored an appliance and invested in your yard.",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+253,w,85),"Keep the gates open. Finish the customer requests, try different repairs and build the workshop at your own pace. The diary brings fresh salvage each day.",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+353,w,38),"DAY "+((long)Model.State.dayIndex+1)+"   /   €"+Model.State.money+" in the till",controlLegend);
            if(Time.unscaledTime<messageUntil && message!=null && message.StartsWith("SAVE FAILED",System.StringComparison.Ordinal))
                GUI.Label(new Rect(x,panel.yMax-112,w,32),message,controlLegend);
            if(GUI.Button(new Rect(x,panel.yMax-74,w,44),chapterReview?"Back to journal / Escape":"Keep working / Free play / Escape"))CloseOpeningChapter();
        }
        void FinishIntroduction()
        {
            Model.State.introSeen=true;sessionStarted=true;
            navigator.Select(YardLandmark.Automatic);SetPaused(false);Save();
            Tell("Welcome to the yard. Follow the delivery marker for your first wire bundle.");
        }
        void ReturnToTitle()
        {
            Save();settings.Close();SetPaused(true);
            mapOpen=ordersOpen=repairOpen=dayOpen=dayReportOpen=helpOpen=introOpen=creditsOpen=chapterOpen=chapterReview=false;
            titleOpen=true;confirmNew=false;
        }
        void QuitFromMenu()
        {
            // A failed write keeps the menu open; unreadable files stay protected.
            if(!saveBlocked && (sessionStarted || hasYardSave) && !Save())return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        Rect FrontPanel(float width,float height,string title,float maxWidth=680,float maxHeight=700)
        {
            float w=Mathf.Min(width-30,maxWidth),h=Mathf.Min(height-30,maxHeight);
            var panel=new Rect((width-w)/2,(height-h)/2,w,h);GUI.Box(panel,title);return panel;
        }
        void DrawTitle(float width,float height)
        {
            var panel=FrontPanel(width,height,"SCRAPSHIFT / A COZY SCRAPYARD SIM");
            if(logo!=null)GUI.DrawTexture(new Rect(panel.center.x-170,panel.y+45,340,105),logo,ScaleMode.ScaleToFit);
            else GUI.Label(new Rect(panel.x+25,panel.y+65,panel.width-50,70),"S C R A P S H I F T",mapLabel);
            GUI.Label(new Rect(panel.x+25,panel.y+160,panel.width-50,65),"A little work. A second life.\nTurn forgotten wire, fans and radios into a yard you can call your own.",wrappedLabel);
            float x=panel.x+50,w=panel.width-100;
            GUI.enabled=!saveBlocked;
            if(GUI.Button(new Rect(x,panel.y+245,w,42),hasYardSave||sessionStarted?"Continue yard / Day "+((long)Model.State.dayIndex+1):"Start your yard"))BeginYard();
            GUI.enabled=true;
            if(GUI.Button(new Rect(x,panel.y+296,w,42),confirmNew?"Confirm new yard / previous save archived":"New yard"))
            {if(confirmNew)NewGame();else confirmNew=true;}
            if(GUI.Button(new Rect(x,panel.y+347,w,42),"Settings")){confirmNew=false;settings.Open();}
            if(GUI.Button(new Rect(x,panel.y+398,w,42),"How to play / Yard journal")){helpPage=0;helpScroll=Vector2.zero;helpOpen=true;confirmNew=false;}
            if(GUI.Button(new Rect(x,panel.y+449,w,42),"Credits")){creditsOpen=true;confirmNew=false;}
            if(GUI.Button(new Rect(x,panel.y+500,w,42),sessionStarted||hasYardSave?saveBlocked?"Quit / unreadable save preserved":"Save & quit":"Quit"))QuitFromMenu();
            if(saveBlocked)GUI.Label(new Rect(panel.x+25,panel.y+565,panel.width-50,100),"Your save and backup could not be read. They are kept safe. New yard archives them before starting over.\n"+message,wrappedLabel);
            else if(Time.unscaledTime<messageUntil)GUI.Label(new Rect(panel.x+25,panel.y+565,panel.width-50,90),message,wrappedLabel);
        }
        void DrawPause(float width,float height)
        {
            var panel=FrontPanel(width,height,"PAUSED / DAY "+((long)Model.State.dayIndex+1),470,700);
            float x=panel.x+35,w=panel.width-70;
            if(logo!=null)GUI.DrawTexture(new Rect(panel.center.x-140,panel.y+38,280,85),logo,ScaleMode.ScaleToFit);
            if(GUI.Button(new Rect(x,panel.y+137,w,40),"Resume"))SetPaused(false);
            if(GUI.Button(new Rect(x,panel.y+183,w,40),"Save yard")){if(Save())Tell("Yard saved.");}
            if(GUI.Button(new Rect(x,panel.y+229,w,40),"Settings")){confirmNew=false;settings.Open();}
            if(GUI.Button(new Rect(x,panel.y+275,w,40),"Yard map")){mapOpen=true;confirmNew=false;}
            if(GUI.Button(new Rect(x,panel.y+321,w,40),"Customer requests & storage")){ordersOpen=true;confirmNew=false;}
            if(GUI.Button(new Rect(x,panel.y+367,w,40),"How to play / Yard journal")){helpOpen=true;helpPage=0;helpScroll=Vector2.zero;confirmNew=false;}
            if(GUI.Button(new Rect(x,panel.y+413,w,40),"Return to title"))ReturnToTitle();
            if(GUI.Button(new Rect(x,panel.y+459,w,40),saveBlocked?"Quit / unreadable save preserved":"Save & quit"))QuitFromMenu();
            if(GUI.Button(new Rect(x,panel.y+505,w,40),confirmNew?"Confirm new yard / old save archived":"New yard"))
            {if(confirmNew)NewGame();else confirmNew=true;}
            if(saveBlocked)GUI.Label(new Rect(x,panel.y+563,w,100),"Saving is blocked to protect unreadable data. New yard archives it before restarting.",wrappedLabel);
            else if(Time.unscaledTime<messageUntil)GUI.Label(new Rect(x,panel.y+563,w,100),message,wrappedLabel);
        }
        void DrawIntroduction(float width,float height)
        {
            var panel=FrontPanel(width,height,"WELCOME TO YOUR YARD",720,670);
            float x=panel.x+25,w=panel.width-50;
            GUI.Label(new Rect(x,panel.y+55,w,85),"Start with scrap wire and a sturdy hand bench. Earn your first money, restore forgotten appliances and build a workshop that does more of the work for you. Take your time: there are no daily fees or order deadlines.",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+158,w,100),"YOUR FIRST JOB\n1. Follow the delivery marker and take wire ["+controls.Label(ControlAction.Interact)+"].\n2. Place it on the stripping bench ["+controls.Label(ControlAction.Interact)+"].\n3. Empty hands, then "+Model.WireWorkSteps+" separate ["+controls.Label(ControlAction.ManualWork)+"] work strokes.",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+277,w,90),"Collect "+Model.Rules.copperPerWire+" copper and carry it to the buyer. That bundle earns €"+(Model.Rules.copperPerWire*Model.Rules.copperUnitPrice)+". Your first powered stripper costs €"+Model.Rules.machinePrice+".",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+385,w,95),"LOOK AROUND\nFans are in western appliance salvage; radios are in eastern electronics salvage. Inspect each fault at the restoration bench before choosing repair or copper recovery. The customer board and yard diary open up your next goals.",wrappedLabel);
            GUI.Label(new Rect(x,panel.y+495,w,65),"Mouse to look • ["+controls.Label(ControlAction.Drop)+"] to put an item down\nEscape opens the map, help, settings and journal. All hints use your current controls.",controlLegend);
            if(GUI.Button(new Rect(x,panel.yMax-75,(w-12)/2,45),"Settings"))settings.Open();
            if(GUI.Button(new Rect(panel.center.x+6,panel.yMax-75,(w-12)/2,45),"Open the yard / Escape"))FinishIntroduction();
        }
        void DrawHelp(float width,float height)
        {
            var panel=FrontPanel(width,height,"HOW TO PLAY / YARD JOURNAL",780,730);
            float x=panel.x+20,w=panel.width-40,tab=(w-18)/4;
            string[] tabs={"Getting started","Controls","Restoration","Yard journal"};
            for(int i=0;i<tabs.Length;i++)if(GUI.Button(new Rect(x+i*(tab+6),panel.y+45,tab,38),(helpPage==i?"• ":"")+tabs[i])){helpPage=i;helpScroll=Vector2.zero;}
            if(helpPage==0)
                HelpBody(panel,"WIRE TO WORKSHOP\nTake wire from delivery or a district crate. Load the hand bench, use "+Model.WireWorkSteps+" separate work strokes with empty hands and collect "+Model.Rules.copperPerWire+" copper. Sell it, store it or deliver it to a customer.\n\nYOUR FIRST MACHINE\nThe powered stripper costs €"+Model.Rules.machinePrice+". Feed it wire and collect the tray when ready. Work on a hand bench or explore while it runs.\n\nA RELAXED WORKING DAY\nCustomer requests have no deadline. Material bins keep your wire and copper. The diary at the entry shows income and yard investments; the day review records income, parts, equipment and completed work. Ending a day brings fresh fans/radios and finishes the current powered load. Hand jobs and inventory stay with you.\n\nFIND YOUR WAY\nOpen the pause map and select a destination. The HUD shows its direction and distance; use the gravel lanes around obstacles. Follow objective restores automatic guidance.");
            else if(helpPage==1)
                HelpBody(panel,
                    "YOUR CURRENT CONTROLS\nForward ["+controls.Label(ControlAction.MoveForward)+"] / backward ["+controls.Label(ControlAction.MoveBackward)+"]\nLeft ["+controls.Label(ControlAction.MoveLeft)+"] / right ["+controls.Label(ControlAction.MoveRight)+"]\nMouse: look around\n["+controls.Label(ControlAction.Interact)+"] use a station or pick up\n["+controls.Label(ControlAction.ManualWork)+"] one manual-work stroke\n["+controls.Label(ControlAction.Drop)+"] put down your carried item\nEscape: pause, go back or cancel a new binding\n\nUSING A STATION\nLook at its body or displayed item within reach. The prompt explains the next action, or why it is blocked. Carry one item at a time; empty your hands before doing hand work or collecting an output.\n\nCHANGE THE FEEL\nSettings offers keyboard/mouse rebinding, sensitivity, invert-Y, graphics and audio. Choose Laptop and enable the FPS display if play feels slow. Escape always stays available. Menus pause work and movement.");
            else if(helpPage==2)
                HelpBody(panel,"FANS & RADIOS\nTake up to "+Model.Rules.fanDailyLimit+" fans from western appliance salvage and "+Model.Rules.radioDailyLimit+" radios from eastern electronics salvage each day. Carry one to the restoration bench and inspect it.\n\nCHOOSE WHAT IS WORTH KEEPING\nA seized motor or failed capacitor needs a replacement. A loose power lead needs a cheaper repair. Dusty bearings or dirty tuner contacts can be cleaned without buying parts. The inspection page compares cost, work and tested resale against copper salvage.\n\nREPAIR, THEN TEST\nChoose the repair, use ["+controls.Label(ControlAction.ManualWork)+"] with empty hands, then power on ["+controls.Label(ControlAction.Interact)+"]. A tested fan spins; a tested radio plays a short listening phrase. Collect it for ordinary resale at the buyer, or bring it to the customer board for a matching accepted request.\n\nRESTORE FOR A NEIGHBOUR\nThe board has a separate appliance request alongside its copper order. Accepted requests keep their agreed payout, have no deadline and pay after every required item is delivered. Two-item requests accept separate deliveries, including across days. Tested appliances only; broken ones stay with you.\n\nOR RECOVER COPPER\nDismantling needs no parts. It produces copper for sales, storage or customer requests. Once repair or salvage starts, finish that choice; your parts payment and partial work are kept when you save.");
            else DrawJournal(panel);
            if(helpPage!=3 && GUI.Button(new Rect(x,panel.yMax-108,w,38),"Open Settings"))settings.Open();
            if(GUI.Button(new Rect(x,panel.yMax-55,w,40),"Back / Escape"))helpOpen=false;
        }
        void HelpBody(Rect panel,string text)
        {
            var viewport=new Rect(panel.x+20,panel.y+110,panel.width-40,panel.height-238);
            float textWidth=viewport.width-24;
            float textHeight=wrappedLabel.CalcHeight(new GUIContent(text),textWidth);
            helpScroll=GUI.BeginScrollView(viewport,helpScroll,new Rect(0,0,textWidth,Mathf.Max(viewport.height,textHeight+12)));
            GUI.Label(new Rect(0,0,textWidth,textHeight),text,wrappedLabel);
            GUI.EndScrollView();
        }
        void DrawJournal(Rect panel)
        {
            var completed=YardJourney.Completed(Model.State);
            GUI.Label(new Rect(panel.x+20,panel.y+100,panel.width-40,32),"Small goals for a growing yard. Complete them in your own order.",controlLegend);
            for(int i=0;i<YardJourney.Goals.Length;i++)
            {
                var goal=YardJourney.Goals[i];bool done=(completed&goal.milestone)!=0;
                float y=panel.y+146+i*73;
                GUI.color=done?new Color(.7f,.83f,.6f):Color.white;
                GUI.Label(new Rect(panel.x+20,y,panel.width-40,26),(done?"DONE / ":"TRY / ")+goal.title);
                GUI.color=Color.white;
                GUI.Label(new Rect(panel.x+20,y+28,panel.width-40,43),goal.detail,controlLegend);
            }
            if(YardJourney.OpeningComplete(Model.State))
            {
                if(GUI.Button(new Rect(panel.x+20,panel.y+600,panel.width-40,38),"Yard established / Review chapter one")){helpOpen=false;OpenChapterRecap();chapterReview=true;}
                return;
            }
            GUI.Label(new Rect(panel.x+20,panel.y+600,panel.width-40,45),"The journal records your progress. Complete all six goals to establish your yard, then keep working in free play.",controlLegend);
        }
        void DrawCredits(float width,float height)
        {
            var panel=FrontPanel(width,height,"SCRAPSHIFT / CREDITS",640,500);
            GUI.Label(new Rect(panel.x+25,panel.y+75,panel.width-50,310),"SCRAPSHIFT\nXanderMaxStudiosBE\n\nAn original scrapyard world of forgotten things and second chances.\n\nModels, worn palette textures and synthesized yard sounds are authored for this project. Built with Unity and Universal Render Pipeline.\n\nThank you for playing and helping shape the yard.",wrappedLabel);
            if(GUI.Button(new Rect(panel.x+25,panel.yMax-60,panel.width-50,40),"Back / Escape"))creditsOpen=false;
        }
    }
}
