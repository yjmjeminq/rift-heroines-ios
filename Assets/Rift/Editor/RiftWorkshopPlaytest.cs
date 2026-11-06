#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static class RiftWorkshopPlaytest
    {
        [Serializable] public sealed class Run {public int faction,seed,traitSet,amulet,stage,kills,guards,captures;public string phase;public float seconds,baseHealth;}
        [Serializable] public sealed class Report {public string version;public List<Run> runs=new List<Run>();}
        public static void Capture()
        {
            var report=new Report{version=RiftData.Version};
            for(int faction=0;faction<2;faction++)for(int seed=1;seed<=4;seed++)for(int traits=1;traits<=2;traits++)for(int amulet=1;amulet<=3;amulet++)
            {
                var loadout=new RiftLoadout{amulet=amulet,frame=1};for(int i=0;i<12;i++){loadout.traits[i]=traits;loadout.styles[i]=1;}
                var b=new RiftBattle(seed*1237,faction,departure:loadout);
                for(int guard=0;guard<18000&&b.Phase!=BattlePhase.Victory&&b.Phase!=BattlePhase.Defeat;guard++)
                {if(b.Phase==BattlePhase.Reward)b.ChooseReward(RiftAutoPlay.RewardIndex(b,3));else{RiftAutoPlay.Drive(b);b.Tick(.1f);b.Cues.Clear();}}
                report.runs.Add(new Run{faction=faction,seed=seed*1237,traitSet=traits,amulet=amulet,stage=b.Stage,kills=b.Kills,guards=b.PerfectGuards,captures=b.ShrineCaptures,phase=b.Phase.ToString(),seconds=b.TotalTime,baseHealth=b.AllyBase});
            }
            File.WriteAllText("BuildLogs/workshop-playtest-v"+RiftData.Version+".json",JsonUtility.ToJson(report,true));
        }
    }
}
#endif
