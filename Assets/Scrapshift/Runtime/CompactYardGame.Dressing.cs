using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        GameObject yardClutter;
        readonly List<Bounds> dressingOccupied=new List<Bounds>(96),lastDressingOccupied=new List<Bounds>(96);
        void SyncDressing()
        {
            CompactDressingOccupancy.Collect(Model.State,Model.Rules,player.transform.localPosition,dressingOccupied);
            if(yardClutter==null)
                yardClutter=CompactYardClutter.Build(transform,dressingOccupied);
            else
            {
                bool same=dressingOccupied.Count==lastDressingOccupied.Count;
                if(same)for(int i=0;i<dressingOccupied.Count;i++)if(dressingOccupied[i]!=lastDressingOccupied[i]){same=false;break;}
                if(same)return;
                CompactYardClutter.RefreshVisibility(yardClutter,dressingOccupied);
            }
            lastDressingOccupied.Clear();lastDressingOccupied.AddRange(dressingOccupied);
        }
    }
}
