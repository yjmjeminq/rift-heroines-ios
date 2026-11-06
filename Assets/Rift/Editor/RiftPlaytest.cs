#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    // Repeatable pacing evidence. The driver uses the same public actions as the player.
    public static class RiftPlaytest
    {
        [Serializable] public sealed class StageSample
        {
            public int stage;public float seconds,baseHealth,enemyBase,enemyHealing,allyDamage,bossHealth;
            public string phase;public bool bossSpawned;
        }
        [Serializable] public sealed class RunSample
        {
            public int seed,faction,kills,ultimates,perfectGuards,shrineCaptures,bondActivations,freeAwakens,chainBursts;public string policy,phase;public float seconds;
            public List<StageSample> stages=new List<StageSample>();public int[] summons=new int[RiftData.HeroCount];
            public Fighter[] finalTroops;
        }
        [Serializable] public sealed class Report
        {public string version;public List<RunSample> runs=new List<RunSample>();}

        [MenuItem("Tools/Rift Heroines/Capture pacing playtest")]
        public static void Capture()
        {
            var report=new Report{version=RiftData.Version};
            foreach(string policy in new[]{"recruit","relic","mixed","no-tactics","legacy"})
            for(int faction=0;faction<2;faction++)for(int seed=1;seed<=4;seed++)
                report.runs.Add(Simulate(seed*1237,faction,policy));
            Directory.CreateDirectory("BuildLogs");
            File.WriteAllText("BuildLogs/playtest-v"+RiftData.Version+".json",JsonUtility.ToJson(report,true));
        }
        static RunSample Simulate(int seed,int faction,string policy)
        {
            var b=new RiftBattle(seed,faction);var run=new RunSample{seed=seed,faction=faction,policy=policy};
            var stage=new StageSample{stage=1};int guard=0;
            while(b.Phase!=BattlePhase.Victory&&b.Phase!=BattlePhase.Defeat&&guard++<18000)
            {
                if(b.Phase==BattlePhase.Reward)
                {
                    int pick=RiftAutoPlay.RewardIndex(b,policy=="recruit"||policy=="legacy"?0:policy=="relic"?1:3);
                    b.ChooseReward(pick);stage=new StageSample{stage=b.Stage};
                }
                else
                {
                    if(policy=="legacy")DriveLegacy(b);else RiftAutoPlay.Drive(b,policy!="no-tactics");b.Tick(.1f);
                    foreach(var cue in b.Cues)
                    {
                        if(cue.kind==Cue.Spawn&&cue.ally)run.summons[cue.hero]++;
                        if(cue.kind==Cue.Heal&&!cue.ally)stage.enemyHealing+=cue.amount;
                        if(cue.kind==Cue.Hit&&cue.ally)stage.allyDamage+=cue.amount;
                    }
                    if(b.Phase!=BattlePhase.Battle)
                    {
                        stage.seconds=b.StageTime;stage.baseHealth=b.AllyBase;stage.enemyBase=b.EnemyBase;stage.phase=b.Phase.ToString();stage.bossSpawned=b.BossSpawned;
                        foreach(var f in b.Fighters)if(f.boss&&f.Alive)stage.bossHealth=f.health;
                        run.stages.Add(stage);
                    }
                    b.Cues.Clear();
                }
            }
            run.phase=b.Phase.ToString();run.seconds=b.TotalTime;run.kills=b.Kills;run.ultimates=b.UltimateUses;run.finalTroops=b.Fighters.ToArray();
            run.perfectGuards=b.PerfectGuards;run.shrineCaptures=b.ShrineCaptures;run.bondActivations=b.BondActivations;run.freeAwakens=b.FreeAwakens;run.chainBursts=b.ChainBursts;return run;
        }
        // Retain the old action policy for a comparison unaffected by the improved driver.
        static void DriveLegacy(RiftBattle b)
        {
            if(b.Phase!=BattlePhase.Battle)return;
            if(b.Fury>=100&&(b.Enemies>=3||b.WarningTime>0))b.Ultimate();
            if(b.Allies>=3&&b.Enemies>0&&b.OrderCooldown==0)b.Order();
            if(b.WarningTime>0&&b.WarningTime<1.4f)b.Shield();
            if(b.Allies>=5&&b.Economy<3&&b.Mana>=b.EconomyCost)b.UpgradeEconomy();
            int[] counts=new int[3];foreach(var f in b.Fighters)if(f.ally&&f.Alive)counts[(int)RiftData.Heroes[f.hero].role]++;
            CombatRole priority=counts[0]<2?CombatRole.Vanguard:counts[2]<2?CombatRole.Mystic:counts[1]<2?CombatRole.Striker:(CombatRole)(((int)(b.StageTime*2))%3);
            int start=((int)(b.StageTime*2))%RiftData.HeroCount;
            for(int offset=0;offset<RiftData.HeroCount;offset++){int i=(start+offset)%RiftData.HeroCount;if(b.Ranks[i]>0&&RiftData.Heroes[i].role==priority&&b.Summon(i))break;}
            if(b.Allies>=8&&b.Economy>=2)for(int i=0;i<RiftData.HeroCount;i++)if(b.Ranks[i]>0&&b.Ranks[i]<3&&b.Awaken(i))break;
        }
    }
}
#endif
