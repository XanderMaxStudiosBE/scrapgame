using System;
using System.Collections.Generic;
namespace Scrapshift.Compact
{
    // Values 0-5 match the reusable legacy portable-item identities; no whole vehicle is portable.
    public enum PartKind { Wire, Copper, BrokenFan, RestoredFan, BrokenRadio, RestoredRadio, Motor, BodyMetal, Steel, Plastic, Insulation, Compressor }
    public enum ScrapObjectKind { Car, Refrigerator }
    public enum EquipmentKind { Workbench, Generator, Tier1Scrapper, Storage, Tier2Scrapper, Conveyor, Splitter, Merger, ExportStation }
    [Serializable] public sealed class PartAmount { public PartKind kind;public int quantity;public PartAmount(){}public PartAmount(PartKind k,int q){kind=k;quantity=q;} }
    [Serializable] public sealed class CompactStack
    {public int id;public PartKind kind;public int quantity;public float x,y=.25f,z;public bool xpEligible;}
    [Serializable] public sealed class LargeScrapJob
    {public int id;public ScrapObjectKind kind;public float x,z;public bool inspected;public int strokes,requiredStrokes;public PartAmount[] remaining;}
    [Serializable] public sealed class ProcessingJob
    {
        public int recipeId;public PartKind input;public int inputQuantity;public int requiredStrokes,strokes;
        public float duration,remaining;public bool ready,xpEligible;public PartAmount[] yields;
    }
    [Serializable] public sealed class EquipmentState
    {
        public int id;public EquipmentKind kind;public float x,z,yaw;public int paidPrice;public bool starter;
        public ProcessingJob job;public List<CompactStack> contents=new List<CompactStack>();
        public int filterKind=-1,routeCursor;
    }
    [Serializable] public sealed class ConveyorItem
    {public int id;public PartKind kind;public int quantity=1;public bool xpEligible;public float progress;}
    [Serializable] public sealed class ConveyorLink
    {
        public int id,fromId,fromPort,toId,toPort,paidPrice;public bool bendXFirst=true;
        public float launchRemaining;public List<ConveyorItem> items=new List<ConveyorItem>();
    }
    [Serializable] public sealed class PowerLink {public int a,b;public PowerLink(){}public PowerLink(int first,int second){a=first;b=second;} }
    [Serializable] public sealed class EquipmentDefinition
    {
        public EquipmentKind kind;public string name;public int price,unlockLevel=1;public bool available;
        public float width,depth,powerOutput,powerDemand,processingSeconds=4;public int outputCapacity=24;
    }
    [Serializable] public sealed class CompactYardState
    {
        public int version=3;public int money,experience,nextId=1,carriedId;
        public float playerX,playerY=1.1f,playerZ=-13,yaw,pitch;public bool welcomeSeen;
        public List<CompactStack> items=new List<CompactStack>();public List<LargeScrapJob> scrap=new List<LargeScrapJob>();
        public List<EquipmentState> equipment=new List<EquipmentState>();public List<PowerLink> powerLinks=new List<PowerLink>();
        public List<ConveyorLink> belts=new List<ConveyorLink>();
        public bool importedLegacy;public string legacySnapshot,legacyNotice;
    }
}
