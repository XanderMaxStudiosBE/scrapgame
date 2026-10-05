using System;

namespace Scrapshift.Tests
{
    public static class PreferenceWriteScenarios
    {
        public static readonly string[] Names = {
            "PreferenceWriteDebouncesIdle", "PreferenceWriteUsesLatestValues", "PreferenceWriteImmediateRebind",
            "PreferenceWriteImmediateDefaults", "PreferenceWriteFailureStopsAutomaticRetries", "PreferenceWriteExplicitRetry",
            "PreferenceWriteThrowRetainsPending", "PreferenceWriteKeepsNewerCallbackEdit", "PreferenceWriteRejectsInvalidTiming"
        };
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; }
            throw new Exception("Invalid preference write timing was accepted.");
        }
        public static void Run(string name)
        {
            var pending = new DeferredPreferenceWrite();
            var preferences = new ControlPreferences();
            int writes = 0; float savedSensitivity = -1; bool savedInvert = false;
            Func<bool> write = () => { writes++; savedSensitivity = preferences.sensitivity; savedInvert = preferences.invertY; return true; };
            switch (name)
            {
                case "PreferenceWriteDebouncesIdle":
                    pending.Queue(10); pending.Tick(10.3f, write); Check(writes == 0 && pending.Pending, "no early write");
                    pending.Queue(10.2f); pending.Tick(10.5f, write); Check(writes == 0, "last edit moves deadline");
                    pending.Tick(10.7f, write); pending.Tick(20, write);
                    Check(writes == 1 && !pending.Pending && !pending.Failed, "one write after idle"); break;
                case "PreferenceWriteUsesLatestValues":
                    preferences.sensitivity = 3; pending.Queue(5);
                    preferences.sensitivity = 4.5f; preferences.invertY = true; pending.Queue(5.2f);
                    Check(preferences.sensitivity == 4.5f && preferences.invertY, "previews are live before persistence");
                    pending.Tick(5.7f, write);
                    Check(writes == 1 && savedSensitivity == 4.5f && savedInvert, "writer sees final values"); break;
                case "PreferenceWriteImmediateRebind":
                    preferences.sensitivity = 6; pending.Queue(1);
                    var rebind = new ControlRebind(preferences); rebind.Begin(ControlAction.Interact);
                    Check(rebind.Capture("F"), "binding applies immediately");
                    Check(pending.Immediate(() => { Check(preferences.Binding(ControlAction.Interact) == "F", "binding before write"); return write(); }), "immediate save succeeds");
                    pending.Tick(30, write);
                    Check(writes == 1 && savedSensitivity == 6 && !pending.Pending, "immediate saves include pending look without a later duplicate"); break;
                case "PreferenceWriteImmediateDefaults":
                    preferences.sensitivity = 7; preferences.invertY = true; pending.Queue(1); preferences.RestoreDefaults();
                    Check(pending.Immediate(write), "defaults save succeeds"); pending.Tick(30, write);
                    Check(writes == 1 && savedSensitivity == 2 && !savedInvert, "defaults supersede previews before persistence"); break;
                case "PreferenceWriteFailureStopsAutomaticRetries":
                    pending.Queue(1); pending.Tick(2, () => { writes++; return false; });
                    Check(writes == 1 && pending.Pending && pending.Failed, "failed values remain pending");
                    for (int i = 0; i < 120; i++) pending.Tick(3 + i, write);
                    Check(writes == 1 && pending.Pending, "no per-frame failure loop");
                    preferences.sensitivity = 4; pending.Queue(200); pending.Tick(201, write);
                    Check(writes == 2 && savedSensitivity == 4 && !pending.Pending, "another edit enables one new attempt"); break;
                case "PreferenceWriteExplicitRetry":
                    pending.Queue(1); Check(!pending.Flush(() => { writes++; return false; }), "failed early close reports failure");
                    preferences.sensitivity = 8; Check(pending.Flush(write), "explicit retry is available");
                    Check(writes == 2 && savedSensitivity == 8 && !pending.Pending && !pending.Failed, "retry saves live final values");
                    Check(pending.Flush(write) && writes == 2, "clean close does not write again"); break;
                case "PreferenceWriteThrowRetainsPending":
                    pending.Queue(1);
                    try { pending.Tick(2, () => { writes++; throw new InvalidOperationException("test writer"); }); }
                    catch (InvalidOperationException) { }
                    Check(writes == 1 && pending.Pending && pending.Failed, "throwing writer remains blocked and pending");
                    pending.Tick(50, write); Check(writes == 1, "throwing writer is not retried automatically");
                    Check(pending.Flush(write) && writes == 2 && !pending.Pending, "explicit retry after exception"); break;
                case "PreferenceWriteKeepsNewerCallbackEdit":
                    pending.Queue(1);
                    Check(pending.Flush(() => { writes++; pending.Queue(3); return true; }), "old write succeeds");
                    Check(pending.Pending && !pending.Failed, "newer edit retained during write");
                    pending.Tick(3.5f, write); Check(writes == 2 && !pending.Pending, "newer edit gets its own deadline"); break;
                case "PreferenceWriteRejectsInvalidTiming":
                    Reject(() => new DeferredPreferenceWrite(-1)); Reject(() => new DeferredPreferenceWrite(float.NaN));
                    Reject(() => pending.Queue(float.PositiveInfinity)); Reject(() => pending.Queue(-1));
                    Check(!pending.Pending, "invalid queue changes no state"); pending.Queue(1);
                    Reject(() => pending.Tick(float.NaN, write)); Check(pending.Pending && writes == 0, "invalid tick leaves pending values intact");
                    var immediate = new DeferredPreferenceWrite(0); immediate.Queue(1); immediate.Tick(1, write);
                    Check(writes == 1 && !immediate.Pending, "zero delay supported"); break;
                default: throw new ArgumentException(name);
            }
        }
    }
}
