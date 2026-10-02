using System;
using Scrapshift.Tests;
class CoreRunner
{
    static int Main()
    {
        int failed = 0;
        int total = 0;
        foreach(var name in CompactStageCIntegrationScenarios.Names)
        {
            total++;
            try{CompactStageCIntegrationScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in CompactStorageScenarios.Names)
        {
            total++;
            try{CompactStorageScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in CompactAutomationScenarios.Names)
        {
            total++;
            try{CompactAutomationScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in CompactScrappingScenarios.Names)
        {
            total++;
            try{CompactScrappingScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in CompactConstructionScenarios.Names)
        {
            total++;
            try{CompactConstructionScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in CompactIntegrationScenarios.Names)
        {
            total++;
            try{CompactIntegrationScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
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
        foreach(var name in ApplianceScenarios.Names)
        {
            total++;
            try{ApplianceScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in RestorationOrderScenarios.Names)
        {
            total++;
            try{RestorationOrderScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in DayReportScenarios.Names)
        {
            total++;
            try{DayReportScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
        }
        foreach(var name in JourneyScenarios.Names)
        {
            total++;
            try{JourneyScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
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
        foreach (var name in IntegrationScenarios.Names)
        {
            total++;
            try { IntegrationScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in WorkFeedbackScenarios.Names)
        {
            total++;
            try { WorkFeedbackScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        foreach (var name in NavigationScenarios.Names)
        {
            total++;
            try { NavigationScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        Console.WriteLine((total - failed) + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
