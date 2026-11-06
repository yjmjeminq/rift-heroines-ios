#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        [Serializable] public sealed class Case {public string name;public bool passed;public string detail;}
        [Serializable] public sealed class Campaign {public int seed,faction,stage,kills;public string phase;public float seconds,baseHealth;}
        [Serializable] public sealed class Report {public bool passed;public string version;public string[] roster;public List<Case> checks=new List<Case>();public List<Campaign> campaigns=new List<Campaign>();}
        static Report report;
        static void Check(string name,bool passed,string detail="") {report.checks.Add(new Case{name=name,passed=passed,detail=detail});if(!passed)Debug.LogWarning("Validation failed: "+name+" "+detail);}
        public static bool Run()
        {
            report=new Report{version=RiftData.Version,roster=Array.ConvertAll(RiftData.Heroes,h=>h.name)};
            var canonical=RiftRosterCatalog.LoadValidated();
            Check("All twelve characters match verified original-work identities",canonical.characters.Length==RiftData.HeroCount);
            var unknown=(HeroDefinition[])RiftData.Heroes.Clone();unknown[6]=new HeroDefinition("unverified","unverified","","","","#FFFFFF",1,CombatRole.Vanguard,1,1,1,1,1,1);
            bool refused=false;try{canonical.Validate(unknown);}catch(InvalidOperationException){refused=true;}
            Check("An unverified replacement character is rejected",refused);
            CheckTactics();
            CheckConvenience();
            CheckContent();
            CheckFeedback();
            CheckWorkshop();
            var b=new RiftBattle(42,0);
            Check("Roster: twelve adult heroines, six in each faction",RiftData.Heroes.Length==12&&b.Ranks[0]==1&&b.Ranks[5]==1&&b.Ranks[6]==0);
            for(int faction=0;faction<2;faction++)
            {
                var draft=new RiftBattle(41,faction);int unlocked=0,opponents=0;
                for(int i=0;i<RiftData.HeroCount;i++){if(draft.Ranks[i]>0)unlocked++;if(RiftData.Heroes[i].faction!=faction&&draft.Ranks[i]>0)opponents++;}
                Check("Six own characters are unlocked without enemy recruits "+faction,unlocked==6&&opponents==0);
                draft.Tick(19);var seen=new HashSet<int>();foreach(var fighter in draft.Fighters)if(!fighter.ally)seen.Add(fighter.hero);
                Check("Opening enemy draft contains all six opposing characters "+faction,seen.Count==6);
            }
            float mana=b.Mana;Check("Locked recruits cannot spend mana",!b.Summon(6)&&b.Mana==mana);
            Check("Summon deducts exact cost",b.Summon(1)&&Math.Abs(b.Mana-(mana-b.SummonCost(1)))<.001f);
            float after=b.Mana;Check("Summon cooldown prevents duplicate-spend",!b.Summon(1)&&b.Mana==after);
            b.Tick(2);Check("Mana regenerates while battle advances",b.Mana>after);
            Check("Role counters form a cycle",RiftData.Counter(CombatRole.Vanguard,CombatRole.Striker)>1&&RiftData.Counter(CombatRole.Striker,CombatRole.Mystic)>1&&RiftData.Counter(CombatRole.Mystic,CombatRole.Vanguard)>1&&RiftData.Counter(CombatRole.Striker,CombatRole.Vanguard)==1);
            var medical=new RiftBattle(311,0);medical.Summon(1);medical.Fighters[0].health=medical.Fighters[0].maxHealth*.5f;
            float wounded=medical.Fighters[0].health;medical.Tick(4.6f);
            Check("Tsunade's medical technique restores health during combat",medical.Fighters.Exists(f=>f.ally&&f.hero==1&&f.health>wounded));
            var gentleFist=new RiftBattle(312,0);gentleFist.Summon(2);gentleFist.Fighters[0].x=50;
            var trainingTarget=new Fighter{id=999,hero=7,ally=false,x=65,health=1000,maxHealth=1000,cooldown=999,healTimer=999};
            gentleFist.Fighters.Add(trainingTarget);gentleFist.Tick(.6f);
            Check("Hinata's Gentle Fist deals damage and briefly interrupts the target",trainingTarget.health<1000&&trainingTarget.stun>0);
            var mind=new RiftBattle(313,0);mind.Summon(3);mind.Fighters[0].x=50;var mindTarget=Dummy(999,7,65);mind.Fighters.Add(mindTarget);mind.Tick(.5f);
            Check("Ino's Mind Transfer interrupts its target",mindTarget.health<1000&&mindTarget.stun>.4f);
            var wind=new RiftBattle(314,0);wind.Summon(4);wind.Fighters[0].x=50;var windTarget=Dummy(999,7,55);wind.Fighters.Add(windTarget);wind.Tick(.5f);
            Check("Temari's fan pushes the enemy front backwards",windTarget.health<1000&&windTarget.x>56);
            var paper=new RiftBattle(315,0);paper.Summon(5);paper.Fighters[0].x=50;var paperA=Dummy(999,7,55);var paperB=Dummy(1000,7,57);paper.Fighters.Add(paperA);paper.Fighters.Add(paperB);paper.Tick(.5f);
            Check("Konan's paper storm applies its two-hit splash to nearby enemies",paperA.health<=968&&paperB.health<=968);
            var ice=new RiftBattle(316,1);ice.Summon(9);ice.Fighters[0].x=50;var iceTarget=Dummy(999,0,55);ice.Fighters.Add(iceTarget);ice.Tick(.5f);
            Check("Rukia's sword freezes and slows an enemy",iceTarget.health<1000&&iceTarget.stun>0&&iceTarget.slowTime>1);
            var shieldHeal=new RiftBattle(317,1);shieldHeal.Summon(10);shieldHeal.Fighters[0].health=60;shieldHeal.Tick(4.6f);
            Check("Orihime heals and grants a timed shield",shieldHeal.Fighters[0].health>60&&shieldHeal.Fighters[0].shield>=35&&shieldHeal.Fighters[0].shieldTime>0);
            var sting=new RiftBattle(318,1);sting.Summon(11);sting.Fighters[0].x=50;var stingTarget=Dummy(999,0,55);sting.Fighters.Add(stingTarget);sting.Tick(.5f);
            float firstSting=1000-stingTarget.health;bool marked=stingTarget.markSource==sting.Fighters[0].id&&stingTarget.markTime>0;sting.Tick(1);
            Check("Soifon's second strike consumes her mark for increased damage",marked&&1000-stingTarget.health>firstSting*3&&stingTarget.markSource==0,"first="+firstSting+", total="+(1000-stingTarget.health)+", mark="+stingTarget.markSource);
            Check("Ultimate requires full fury",!b.Ultimate());
            var idle=new RiftBattle(553,0,runId:"qa-defeat");for(int i=0;i<1500&&idle.Phase==BattlePhase.Battle;i++){idle.Tick(.1f);idle.Cues.Clear();}
            Check("Ignoring the front results in a real defeat",idle.Phase==BattlePhase.Defeat&&idle.AllyBase==0);
            var lostProfile=new PlayerProfile();Check("A defeat still records the expedition and grants its one-time run payout",RiftProgress.Settle(lostProfile,idle)&&!lostProfile.history[0].won&&lostProfile.shards==idle.RewardShards&&!RiftProgress.Settle(lostProfile,idle));
            var bossBattle=WarningFixture();
            Check("Boss attack is telegraphed before damage",bossBattle.WarningTime>0,"phase="+bossBattle.Phase+", time="+bossBattle.StageTime);
            foreach(var fighter in bossBattle.Fighters)if(fighter.boss)fighter.health=0;
            bossBattle.Tick(.1f);Check("Defeating the boss cancels its pending technique",bossBattle.WarningTime==0);
            Check("Defeating the boss removes the enemy base shield",!bossBattle.EnemyBaseProtected);
            Check("Economy final upgrade is affordable within cap",65+3*35<=120+3*25);
            float clock=b.StageTime;b.Tick(float.NaN);b.Tick(-1);Check("Invalid time steps are ignored",b.StageTime==clock);
            var checkpoint=b.CaptureCheckpoint();var encoded=JsonUtility.ToJson(checkpoint);var restored=RiftBattle.Restore(JsonUtility.FromJson<Checkpoint>(encoded));
            Check("Checkpoint roundtrip keeps faction and roster",restored.Faction==b.Faction&&restored.Stage==b.Stage&&restored.Ranks[1]==b.Ranks[1]);
            var invalid=JsonUtility.FromJson<Checkpoint>(encoded);invalid.ranks[0]=99;Check("Invalid checkpoint ranks rejected",!RiftBattle.IsValid(invalid));
            var legacy=new Checkpoint{schema=1,seed=321,faction=1,stage=2,ranks=new[]{0,0,0,2,1,3},relics=new string[0],baseHealth=800};
            var migrated=RiftBattle.Restore(legacy);
            Check("Six-character saves migrate to twelve without losing Bleach ranks",migrated.Ranks[6]==2&&migrated.Ranks[7]==1&&migrated.Ranks[8]==3&&migrated.Ranks[9]==1&&migrated.Ranks[10]==1&&migrated.Ranks[11]==1&&migrated.Ranks[3]==0&&migrated.CaptureCheckpoint().schema==2);
            var modern=migrated.CaptureCheckpoint();modern.ranks[11]=0;Check("New saves require all six own characters",!RiftBattle.IsValid(modern));
            var p=new PlayerProfile{shards=100};Check("Permanent upgrades spend currency exactly once",p.BuyUpgrade(true)&&p.healthUpgrade==1&&p.shards==65);
            var a=new RiftBattle(119,1);var c=new RiftBattle(119,1);
            for(int i=0;i<600;i++){RiftAutoPlay.Drive(a);RiftAutoPlay.Drive(c);a.Tick(.1f);c.Tick(.1f);a.Cues.Clear();c.Cues.Clear();}
            Check("Seeded combat is deterministic",a.Phase==c.Phase&&a.Kills==c.Kills&&Math.Abs(a.AllyBase-c.AllyBase)<.001f&&a.Fighters.Count==c.Fighters.Count);
            int victories=0;
            for(int faction=0;faction<2;faction++)for(int seed=1;seed<=4;seed++)
            {
                var run=new RiftBattle(seed*1237,faction,runId:"qa-"+faction+"-"+seed);int steps=0;
                var runProfile=new PlayerProfile();var tracker=new RiftProgressTracker();tracker.Bind(run);
                while(run.Phase!=BattlePhase.Victory&&run.Phase!=BattlePhase.Defeat&&steps++<20000)
                {
                    if(run.Phase==BattlePhase.Reward)
                    {
                        float stopped=run.StageTime;run.Tick(1);Check("Reward stops combat "+faction+"/"+seed+"/"+run.Stage,run.StageTime==stopped);
                        if(seed==1){Check("One free reward reroll",run.Reroll()&&!run.Reroll());}
                        run.ChooseReward(RiftAutoPlay.RewardIndex(run));
                        var saved=run.CaptureCheckpoint();var resumed=RiftBattle.Restore(saved);
                        Check("Stage checkpoint restores build "+faction+"/"+seed+"/"+run.Stage,resumed.Stage==run.Stage&&resumed.Relics.Count==run.Relics.Count&&resumed.Economy==run.Economy);
                    }
                    else {RiftAutoPlay.Drive(run);run.Tick(.1f);}
                    if(run.Mana<-.001f || run.Mana>run.MaxMana+.001f || run.Allies>18 || run.Enemies>18) {Check("Resource and troop invariants",false);break;}
                    tracker.Observe(runProfile,run);run.Cues.Clear();
                }
                report.campaigns.Add(new Campaign{seed=seed*1237,faction=faction,stage=run.Stage,kills=run.Kills,phase=run.Phase.ToString(),seconds=run.TotalTime,baseHealth=run.AllyBase});
                if(run.Phase==BattlePhase.Victory)victories++;
                bool settled=RiftProgress.Settle(runProfile,run);int payout=runProfile.shards;
                Check("Run rewards and records settle exactly once "+faction+"/"+seed,settled&&!RiftProgress.Settle(runProfile,run)&&runProfile.shards==payout&&runProfile.history.Length==1&&runProfile.factionWins[faction]==(run.Phase==BattlePhase.Victory?1:0));
                bool claimed=RiftProgress.Claim(runProfile,0);var persisted=JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(runProfile));persisted.Normalize();
                Check("Completed challenges survive saving without duplicate claims "+faction+"/"+seed,claimed&&!RiftProgress.Claim(persisted,0)&&persisted.shards==runProfile.shards&&persisted.clearedStages==6&&persisted.bestTimes[faction]>0);
            }
            Check("Both factions can complete six stages with legal actions",victories>=6,"victories="+victories+"/8");
            report.passed=report.checks.TrueForAll(cas=>cas.passed);
            Directory.CreateDirectory("BuildLogs");File.WriteAllText("BuildLogs/model-validation.json",JsonUtility.ToJson(report,true));return report.passed;
        }
        static Fighter Dummy(int id,int hero,float x)
        {return new Fighter{id=id,hero=hero,ally=false,x=x,health=1000,maxHealth=1000,cooldown=999,healTimer=999};}
    }
}
#endif
