using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        GameObject yardClutter;
        readonly List<Bounds> dressingOccupied=new List<Bounds>(96),lastDressingOccupied=new List<Bounds>(96);
        CharacterController dressingController;
        bool dressingSpawnClearance=true,dressingPlayerClearancePending;
        Vector3 lastDressingPlayer;
        float lastDressingPlayerRadius;

        void RequireDressingSpawnClearance(){dressingSpawnClearance=true;}

        void DressingPlayer(out Vector3 center,out float radius)
        {
            if(dressingController==null)dressingController=player.GetComponent<CharacterController>();
            center=player.transform.localPosition;radius=.45f;
            if(dressingController!=null)
            {
                center=transform.InverseTransformPoint(player.transform.TransformPoint(dressingController.center));
                radius=Mathf.Max(.01f,dressingController.radius+dressingController.skinWidth);
            }
        }

        // Called by the coordinator's existing 0.2s view schedule, only while unpaused.
        // Most yards have no deferred module and return without collecting occupancy.
        void RefreshDressingPlayerClearance()
        {
            if(!dressingPlayerClearancePending || yardClutter==null)return;
            DressingPlayer(out Vector3 center,out float radius);
            if(center==lastDressingPlayer && radius==lastDressingPlayerRadius)return;
            SyncDressing();
        }

        void SyncDressing()
        {
            DressingPlayer(out Vector3 center,out float radius);
            bool protectSpawn=dressingSpawnClearance || yardClutter==null;
            CompactDressingOccupancy.Collect(Model.State,Model.Rules,center,dressingOccupied,protectSpawn);
            if(yardClutter==null)
                yardClutter=CompactYardClutter.Build(transform,dressingOccupied,solidStock:true);
            else
            {
                bool same=dressingOccupied.Count==lastDressingOccupied.Count;
                if(same)for(int i=0;i<dressingOccupied.Count;i++)if(dressingOccupied[i]!=lastDressingOccupied[i]){same=false;break;}
                if(same && !protectSpawn && (!dressingPlayerClearancePending || center==lastDressingPlayer && radius==lastDressingPlayerRadius))return;
                dressingPlayerClearancePending=CompactYardClutter.RefreshVisibility(yardClutter,dressingOccupied,center,radius);
            }
            lastDressingOccupied.Clear();lastDressingOccupied.AddRange(dressingOccupied);
            // The first unguarded refresh can release modules hidden by the larger spawn
            // envelope; capsule-overlapping modules stay deferred until the player clears.
            dressingPlayerClearancePending|=protectSpawn;
            dressingSpawnClearance=false;lastDressingPlayer=center;lastDressingPlayerRadius=radius;
        }
    }
}
