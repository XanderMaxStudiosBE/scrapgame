using System;
using System.Collections.Generic;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class ProgressCheckpointScenarios
    {
        public static readonly string[] Names={
            "ProgressHeldWorkUsesFixedWindow", "ProgressTerminalSaveIncludesFinalStroke",
            "ProgressPauseSaveIncludesPartialDismantling", "ProgressFailedSaveStopsAutomaticRetry",
            "ProgressOtherSuccessfulSaveClearsPending", "ProgressExplicitRetryPreservesFinalOutput",
            "ProgressForcedFailureWithoutQueuedWork", "ProgressInvalidTimingRetainsEdits"
        };
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Reject(Action operation)
        {try{operation();}catch(ArgumentException){return;}throw new Exception("Invalid checkpoint timing accepted.");}
        sealed class Session
        {
            public readonly ScrappingModel model;
            public readonly ProgressCheckpoint checkpoint=new ProgressCheckpoint();
            public readonly List<CompactYardState> snapshots=new List<CompactYardState>();
            public int attempts;
            public int Bench{get{return model.State.equipment[0].id;}}
            public Session(int strokes=4)
            {
                var rules=new CompactRules();rules.Recipe(PartKind.Wire).strokes=strokes;model=new ScrappingModel(rules);
                Check(model.AcquireWire() && model.BeginProcessing(Bench),"Real manual input reserved.");
            }
            public void Stroke(float time)
            {Check(model.Work(Bench),model.LastNotice);checkpoint.Queue(time);}
            public void Attempt(bool success)
            {
                // The writer sees the current authoritative model, including every
                // stroke since the previous checkpoint. Only success stores a copy.
                Func<bool> writer=()=>{attempts++;if(success)snapshots.Add(CompactCareerScenarios.Copy(model.State));return success;};
                checkpoint.RecordAttempt(writer());
            }
            public void Automatic(float time,bool success=true)
            {if(checkpoint.Due(time))Attempt(success);}
        }
        public static void Run(string name)
        {
            switch(name)
            {
                case "ProgressHeldWorkUsesFixedWindow":
                {
                    var session=new Session(40);int next=session.model.State.nextId,cash=session.model.State.money;
                    for(int i=0;i<39;i++)
                    {
                        float time=i*.05f;session.Stroke(time);session.Automatic(time);
                        Check(session.attempts<=1,"Further held strokes cannot extend the first deadline or write per stroke.");
                    }
                    Check(session.attempts==1 && session.snapshots[0].equipment[0].job.strokes==21,"First window checkpoints all twenty-one live strokes.");
                    session.Automatic(2.04f);Check(session.attempts==1,"Second fixed window does not save early.");
                    session.Automatic(2.05f);session.Automatic(20);
                    Check(session.attempts==2 && session.snapshots[1].equipment[0].job.strokes==39 && !session.checkpoint.Pending,"Next window stores the latest remaining strokes once.");
                    Check(session.model.State.nextId==next && session.model.State.money==cash && session.model.State.experience==0,"Persistence timing changes no inventory, cash or sale XP.");
                    break;
                }
                case "ProgressTerminalSaveIncludesFinalStroke":
                {
                    var session=new Session();for(int i=0;i<3;i++)session.Stroke(i*.2f);
                    Check(!session.checkpoint.Due(.6f),"Terminal boundary can precede the normal deadline.");
                    Check(session.model.Work(session.Bench) && session.model.FindEquipment(session.Bench).job.ready,"Fourth live stroke completes actual work.");
                    session.Attempt(true);session.Automatic(100);
                    var saved=session.snapshots[0].equipment[0].job;
                    Check(session.attempts==1 && saved.ready && saved.strokes==4 && saved.yields[0].quantity==3 && saved.yields[1].quantity==2,"Immediate terminal save includes the last stroke and exact output snapshot.");
                    Check(!session.checkpoint.Pending && !session.checkpoint.Failed && !session.model.Work(session.Bench),"Ready work cannot queue duplicate processing output.");
                    break;
                }
                case "ProgressPauseSaveIncludesPartialDismantling":
                {
                    var model=new ScrappingModel(new CompactRules());int car=model.State.scrap[0].id;var checkpoint=new ProgressCheckpoint();
                    Check(model.InspectScrap(car),"Inspect actual delivered car.");
                    for(int i=0;i<3;i++){Check(model.WorkScrap(car),model.LastNotice);checkpoint.Queue(10+i*.2f);}
                    Check(!checkpoint.Due(10.9f),"Pause occurs before delayed checkpoint.");
                    CompactYardState snapshot=null;Func<bool> writer=()=>{snapshot=CompactCareerScenarios.Copy(model.State);return true;};
                    checkpoint.RecordAttempt(writer());var resumed=new ScrappingModel(model.Rules,snapshot);
                    Check(resumed.FindScrap(car).strokes==3 && resumed.FindScrap(car).requiredStrokes==6 && resumed.FindScrap(car).remaining.Length==3,"Pause boundary retains actual partial dismantling and reserved yields.");
                    Check(!checkpoint.Pending && !checkpoint.Due(100) && snapshot.money==model.State.money && snapshot.experience==0,"Successful pause save clears the older due request without granting rewards.");
                    break;
                }
                case "ProgressFailedSaveStopsAutomaticRetry":
                {
                    var session=new Session();session.Stroke(0);session.Automatic(1,false);
                    Check(session.attempts==1 && session.checkpoint.Pending && session.checkpoint.Failed && session.snapshots.Count==0,"Failed write keeps unsaved live progress.");
                    for(int i=0;i<180;i++)session.Automatic(2+i);
                    Check(session.attempts==1 && session.model.FindEquipment(session.Bench).job.strokes==1,"A disk failure does not create an automatic frame-by-frame retry loop.");
                    session.Stroke(200);Check(session.checkpoint.Failed,"Editing re-arms persistence without pretending the previous write succeeded.");
                    session.Automatic(200.99f);Check(session.attempts==1,"A new edit starts a fresh fixed window after failure.");
                    session.Automatic(201);session.Automatic(300);
                    Check(session.attempts==2 && session.snapshots[0].equipment[0].job.strokes==2 && !session.checkpoint.Pending && !session.checkpoint.Failed,"One new automatic attempt saves all retained strokes.");
                    break;
                }
                case "ProgressOtherSuccessfulSaveClearsPending":
                {
                    var session=new Session();session.Stroke(5);
                    Check(session.model.AcquireWire(),"Another real inventory transaction can save the same authoritative state.");
                    int carried=session.model.State.carriedId;session.Attempt(true);session.Automatic(6);session.Automatic(100);
                    Check(session.attempts==1 && session.snapshots[0].equipment[0].job.strokes==1 && session.snapshots[0].carriedId==carried,"A different successful transaction includes pending work and current inventory.");
                    Check(!session.checkpoint.Pending && !session.checkpoint.Failed,"Any successful forced save consumes the redundant checkpoint request.");
                    break;
                }
                case "ProgressExplicitRetryPreservesFinalOutput":
                {
                    var session=new Session();session.Stroke(0);session.Attempt(false);
                    session.Stroke(.2f);session.Stroke(.4f);Check(session.model.Work(session.Bench),"Actual terminal stroke.");session.Attempt(false);
                    Check(session.checkpoint.Pending && session.checkpoint.Failed && session.model.FindEquipment(session.Bench).job.ready,"Failed terminal boundary keeps final work in memory.");
                    session.Automatic(100);Check(session.attempts==2,"Terminal failure remains suppressed until explicit retry or a new edit.");
                    session.Attempt(true);session.Automatic(200);
                    var resumed=new ScrappingModel(session.model.Rules,session.snapshots[0]);
                    Check(session.attempts==3 && !session.checkpoint.Pending && !session.checkpoint.Failed && resumed.FindEquipment(session.Bench).job.ready,"Explicit retry stores the final rather than the earlier partial snapshot.");
                    Check(resumed.CollectOutput(session.Bench,0) && resumed.Carried.quantity==3 && resumed.Sell() && !resumed.CollectOutput(session.Bench,0),"Recovered saved output is collectible and saleable once.");
                    Check(resumed.State.experience==6,"Checkpoint failure/retry cannot duplicate recovery XP.");
                    break;
                }
                case "ProgressForcedFailureWithoutQueuedWork":
                {
                    var session=new Session();session.Attempt(false);
                    Check(!session.checkpoint.Pending && session.checkpoint.Failed && !session.checkpoint.Due(100),"A failed unrelated forced save records failure without inventing a manual edit.");
                    session.Stroke(200);Check(session.checkpoint.Pending && session.checkpoint.Failed && !session.checkpoint.Due(200.9f),"A later real stroke receives its own window.");
                    session.Attempt(true);session.Automatic(300);
                    Check(session.attempts==2 && !session.checkpoint.Pending && !session.checkpoint.Failed && session.snapshots[0].equipment[0].job.strokes==1,"A successful explicit save clears the queued work and earlier failure.");
                    break;
                }
                case "ProgressInvalidTimingRetainsEdits":
                {
                    Reject(()=>new ProgressCheckpoint(0));Reject(()=>new ProgressCheckpoint(-1));Reject(()=>new ProgressCheckpoint(float.NaN));Reject(()=>new ProgressCheckpoint(float.PositiveInfinity));
                    var session=new Session();Reject(()=>session.checkpoint.Queue(-1));Reject(()=>session.checkpoint.Queue(float.NaN));Reject(()=>session.checkpoint.Queue(float.PositiveInfinity));
                    Check(!session.checkpoint.Pending && !session.checkpoint.Failed,"Invalid initial timing changes no state.");session.Stroke(1);
                    Reject(()=>session.checkpoint.Due(-1));Reject(()=>session.checkpoint.Due(float.NaN));Reject(()=>session.checkpoint.Due(float.NegativeInfinity));
                    Check(session.checkpoint.Pending && session.model.FindEquipment(session.Bench).job.strokes==1,"Invalid queries do not lose actual unsaved work.");
                    var large=new ProgressCheckpoint(float.MaxValue);Reject(()=>large.Queue(float.MaxValue));Check(!large.Pending,"Overflowing deadline is rejected before arming.");
                    session.Automatic(2);Check(session.attempts==1 && session.snapshots[0].equipment[0].job.strokes==1,"Valid deadline remains intact after rejected timing.");
                    break;
                }
                default:throw new ArgumentException(name);
            }
        }
    }
}
