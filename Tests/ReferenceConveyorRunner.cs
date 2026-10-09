using System;
using Scrapshift.Tests;

class ReferenceConveyorRunner
{
    static int Main()
    {
        int failed=0;
        foreach(var name in CompactReferenceConveyorScenarios.Names)
        {
            try{CompactReferenceConveyorScenarios.Run(name);Console.WriteLine("PASS "+name);}
            catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}
        }
        Console.WriteLine(CompactReferenceConveyorScenarios.Names.Length+" reference conveyor scenarios, "+failed+" failed.");
        return failed==0?0:1;
    }
}
