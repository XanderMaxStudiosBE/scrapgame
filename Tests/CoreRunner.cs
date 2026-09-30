using System;
using Scrapshift.Tests;
class CoreRunner
{
    static int Main()
    {
        int failed = 0;
        foreach (var name in CoreScenarios.Names)
        {
            try { CoreScenarios.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        Console.WriteLine((CoreScenarios.Names.Length - failed) + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
