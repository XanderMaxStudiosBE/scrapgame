using System;

namespace Scrapshift
{
    // Slider previews apply to live preferences immediately. Only their disk write is delayed.
    // A failed attempt remains pending, but automatic retries wait for another edit.
    public sealed class DeferredPreferenceWrite
    {
        readonly float delay;
        float due;
        int revision;
        public bool Pending { get; private set; }
        public bool Failed { get; private set; }
        public DeferredPreferenceWrite(float delaySeconds = .4f)
        {
            if (!Finite(delaySeconds) || delaySeconds < 0) throw new ArgumentOutOfRangeException("delaySeconds");
            delay = delaySeconds;
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public void Queue(float unscaledTime)
        {
            if (!Finite(unscaledTime) || unscaledTime < 0 || !Finite(unscaledTime + delay))
                throw new ArgumentOutOfRangeException("unscaledTime");
            due = unscaledTime + delay;
            unchecked { revision++; }
            Pending = true; Failed = false;
        }
        public void Tick(float unscaledTime, Func<bool> write)
        {
            if (!Finite(unscaledTime) || unscaledTime < 0) throw new ArgumentOutOfRangeException("unscaledTime");
            if (Pending && !Failed && unscaledTime >= due) Flush(write);
        }
        public bool Flush(Func<bool> write)
        {
            if (!Pending) return true;
            if (write == null) throw new ArgumentNullException("write");
            int attemptedRevision = revision;
            bool success = false;
            try { success = write(); return success; }
            finally
            {
                // Keep a newer edit pending if a writer queues it while committing its snapshot.
                if (revision == attemptedRevision) { Pending = !success; Failed = !success; }
            }
        }
        public bool Immediate(Func<bool> write)
        {
            unchecked { revision++; }
            Pending = true; Failed = false;
            return Flush(write);
        }
    }
}
