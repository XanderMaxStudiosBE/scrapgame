using System;
using Scrapshift.Tests;
class CoreRunner
{
    static int Main()
    {
        int failed = 0;
        int total = 0;
        foreach (var name in CoreScenarios.Names)
        {
            total++;
            try { CoreScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in ControlScenarios.Names)
        {
            total++;
            try { ControlScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in GuidanceScenarios.Names)
        {
            total++;
            try { GuidanceScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in WorldScenarios.Names)
        {
            total++;
            try { WorldScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in ProgressionScenarios.Names)
        {
            total++;
            try { ProgressionScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in FanScenarios.Names)
        {
            total++;
            try { FanScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in UpgradeScenarios.Names)
        {
            total++;
            try { UpgradeScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in PresentationScenarios.Names)
        {
            total++;
            try { PresentationScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        Console.WriteLine((total - failed) + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
