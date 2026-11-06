#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static class RiftExperienceAudit
    {
        [Serializable] public sealed class Run {public string policy,phase;public int faction,seed,stage,actions,kills;public float seconds,firstHit,firstKill,minBase;public List<float> stageSeconds=new List<float>();}
        [Serializable] public sealed class Report {public string version;public List<Run> runs=new List<Run>();}
        public static void Capture()
        {
            var report=new Report{version=RiftData.Version};
            foreach(string policy in new[]{"human-0.8","human-1.2","human-no-tactics"})for(int faction=0;faction<2;faction++)for(int seed=1;seed<=4;seed++)report.runs.Add(Simulate(policy,faction,seed*1237));
            for(int hero=0;hero<12;hero++)for(int seed=1;seed<=2;seed++)report.runs.Add(Simulate("solo-"+hero,RiftData.Heroes[hero].faction,seed*1237));
            Directory.CreateDirectory("BuildLogs");File.WriteAllText("BuildLogs/experience-model-v"+RiftData.Version+".json",JsonUtility.ToJson(report,true));
        }
        static Run Simulate(string policy,int faction,int seed)
        {
            var b=new RiftBattle(seed,faction);var result=new Run{policy=policy,faction=faction,seed=seed,firstHit=-1,firstKill=-1,minBase=1000};
            float next=0,cadence=policy=="human-1.2"?1.2f:.8f;bool mono=policy.StartsWith("solo-");int hero=mono?int.Parse(policy.Substring(5)):0;
            for(int guard=0;guard<36000&&b.Phase!=BattlePhase.Victory&&b.Phase!=BattlePhase.Defeat;guard++)
            {
                if(b.Phase==BattlePhase.Reward){result.stageSeconds.Add(b.StageTime);b.ChooseReward(RiftAutoPlay.RewardIndex(b,mono?1:3));next=b.TotalTime;}
                else
                {
                    if(b.TotalTime>=next)
                    {
                        next=b.TotalTime+cadence;string action;
                        if(mono)action=b.Fury>=100&&b.Enemies>=3?"Ultimate":b.Allies>=5&&b.Economy<3&&b.Mana>=b.EconomyCost?"Economy":b.Allies>=3&&b.Enemies>0&&b.OrderCooldown<=0?"Order":"Deploy "+RiftData.Heroes[hero].id;
                        else action=RiftExperiencePolicy.Decide(b,policy!="human-no-tactics");
                        if(RiftExperiencePolicy.Apply(b,action))result.actions++;
                    }
                    b.Tick(.05f);result.minBase=Math.Min(result.minBase,b.AllyBase);
                    foreach(var cue in b.Cues){if(cue.kind==Cue.Hit&&result.firstHit<0)result.firstHit=b.TotalTime;if(cue.kind==Cue.Death&&!cue.ally&&result.firstKill<0)result.firstKill=b.TotalTime;}
                    b.Cues.Clear();
                }
            }
            result.stageSeconds.Add(b.StageTime);result.phase=b.Phase.ToString();result.stage=b.Stage;result.seconds=b.TotalTime;result.kills=b.Kills;return result;
        }
    }
}
#endif
