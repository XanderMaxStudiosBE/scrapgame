using System;

namespace Scrapshift
{
    // Manual work applies immediately. Its disk checkpoint uses a fixed window from
    // the first unsaved stroke, so held work cannot keep postponing persistence.
    public sealed class ProgressCheckpoint
    {
        readonly float interval;
        float deadline;
        bool armed;
        public bool Pending { get; private set; }
        public bool Failed { get; private set; }
        public ProgressCheckpoint(float intervalSeconds=1)
        {
            if(!Finite(intervalSeconds) || intervalSeconds<=0)throw new ArgumentOutOfRangeException("intervalSeconds");
            interval=intervalSeconds;
        }
        static bool Finite(float value){return !float.IsNaN(value) && !float.IsInfinity(value);}
        static void CheckTime(float unscaledTime)
        {if(!Finite(unscaledTime) || unscaledTime<0)throw new ArgumentOutOfRangeException("unscaledTime");}
        public void Queue(float unscaledTime)
        {
            CheckTime(unscaledTime);float next=unscaledTime+interval;
            if(!Finite(next))throw new ArgumentOutOfRangeException("unscaledTime");
            if(Pending && armed)return;
            deadline=next;Pending=true;armed=true;
            // A new edit permits one further automatic attempt; the last failure is
            // still visible until a checkpoint actually succeeds.
        }
        public bool Due(float unscaledTime)
        {CheckTime(unscaledTime);return Pending && armed && unscaledTime>=deadline;}
        public void RecordAttempt(bool success)
        {
            armed=false;Failed=!success;
            if(success){Pending=false;deadline=0;}
            // Failed writes retain their pending edits without a per-frame retry.
            // Any explicit successful save, including another action, clears them.
        }
    }
}
