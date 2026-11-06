using System;
using System.Collections.Generic;

namespace RiftHeroines
{
    [Serializable] public sealed class RunRecord
    {
        public string id;public int faction,stage,kills,combo,captures,guards;public float seconds;public bool won;
    }
    public sealed class ChallengeDefinition
    {
        public string id,name,description;public int target,reward;
        public ChallengeDefinition(string id,string name,string description,int target,int reward)
        {this.id=id;this.name=name;this.description=description;this.target=target;this.reward=reward;}
    }
    public sealed partial class PlayerProfile
    {
        public int clearedStages,lifetimeCaptures,lifetimeGuards,bestBondCount;
        public int[] deployments=new int[RiftData.HeroCount],factionWins=new int[2];
        public float[] bestTimes=new float[2];
        public string[] claimedChallenges=new string[0];
        public RunRecord[] history=new RunRecord[0];
        public string lastSettledRunId;
        public float preferredSpeed=1;
        public void Normalize()
        {
            NormalizeWorkshop();
            deployments=Resize(deployments,RiftData.HeroCount);factionWins=Resize(factionWins,2);
            if(bestTimes==null||bestTimes.Length!=2){var old=bestTimes;bestTimes=new float[2];if(old!=null)Array.Copy(old,bestTimes,Math.Min(2,old.Length));}
            for(int i=0;i<2;i++)if(float.IsNaN(bestTimes[i])||float.IsInfinity(bestTimes[i])||bestTimes[i]<0)bestTimes[i]=0;
            if(claimedChallenges==null)claimedChallenges=new string[0];if(history==null)history=new RunRecord[0];
            var valid=new List<RunRecord>();foreach(var record in history)if(record!=null&&record.faction>=0&&record.faction<2&&record.stage>=1&&record.stage<=6&&!float.IsNaN(record.seconds)&&!float.IsInfinity(record.seconds)&&record.seconds>=0&&valid.Count<6)valid.Add(record);history=valid.ToArray();
            preferredSpeed=preferredSpeed>=1.5f?2:1;
            clearedStages=Math.Max(0,clearedStages);lifetimeCaptures=Math.Max(0,lifetimeCaptures);lifetimeGuards=Math.Max(0,lifetimeGuards);bestBondCount=Math.Max(0,Math.Min(RiftData.Bonds.Length,bestBondCount));
        }
        static int[] Resize(int[] source,int size)
        {var result=new int[size];if(source!=null)Array.Copy(source,result,Math.Min(source.Length,size));for(int i=0;i<size;i++)result[i]=Math.Max(0,result[i]);return result;}
    }
    public static class RiftProgress
    {
        public static readonly ChallengeDefinition[] Challenges={
            new ChallengeDefinition("first","첫 경계 돌파","경계를 한 번 돌파하세요",1,10),
            new ChallengeDefinition("bonds","세 갈래 인연","한 전투에서 조합 3종을 함께 켜세요",3,20),
            new ChallengeDefinition("shrines","월광의 개척자","제단을 누적 5회 차지하세요",5,20),
            new ChallengeDefinition("guards","찰나의 반격","절묘한 결계를 누적 3회 성공하세요",3,25),
            new ChallengeDefinition("cast","열두 명의 동료","12명 모두 한 번 이상 출격하세요",12,30),
            new ChallengeDefinition("factions","경계의 중재자","두 진영으로 각각 6경계를 완주하세요",2,40)
        };
        public static int Value(PlayerProfile p,int index)
        {
            switch(index)
            {
                case 0:return p.clearedStages;case 1:return p.bestBondCount;case 2:return p.lifetimeCaptures;case 3:return p.lifetimeGuards;
                case 4:int cast=0;foreach(int n in p.deployments)if(n>0)cast++;return cast;
                default:return (p.factionWins[0]>0?1:0)+(p.factionWins[1]>0?1:0);
            }
        }
        public static bool Claimed(PlayerProfile p,int index){return Array.IndexOf(p.claimedChallenges,Challenges[index].id)>=0;}
        public static bool Ready(PlayerProfile p,int index){return !Claimed(p,index)&&Value(p,index)>=Challenges[index].target;}
        public static int ReadyCount(PlayerProfile p){int count=0;for(int i=0;i<Challenges.Length;i++)if(Ready(p,i))count++;return count;}
        public static bool Claim(PlayerProfile p,int index)
        {
            if(index<0||index>=Challenges.Length||!Ready(p,index))return false;
            var claims=new List<string>(p.claimedChallenges);claims.Add(Challenges[index].id);p.claimedChallenges=claims.ToArray();p.shards+=Challenges[index].reward;return true;
        }
        public static string NextGoal(PlayerProfile p)
        {
            if(p.workshopGoal!=0)return RiftWorkshop.Goal(p);
            for(int i=0;i<Challenges.Length;i++)if(Ready(p,i))return "완료한 도전 보상 "+ReadyCount(p)+"개를 받아 다음 출정을 준비하세요";
            for(int i=0;i<Challenges.Length;i++)if(!Claimed(p,i))return Challenges[i].name+"  "+Math.Min(Value(p,i),Challenges[i].target)+" / "+Challenges[i].target;
            return "두 진영의 최고 시간을 줄여 보세요";
        }
        public static bool Settle(PlayerProfile p,RiftBattle b)
        {
            if(b==null||b.Practice||(b.Phase!=BattlePhase.Victory&&b.Phase!=BattlePhase.Defeat)||string.IsNullOrEmpty(b.RunId)||p.lastSettledRunId==b.RunId)return false;
            foreach(var record in p.history)if(record!=null&&record.id==b.RunId)return false;
            bool won=b.Phase==BattlePhase.Victory;p.lastSettledRunId=b.RunId;p.shards+=b.RewardShards;
            p.bestStage=Math.Max(p.bestStage,won?6:b.Stage-1);p.bestBondCount=Math.Max(p.bestBondCount,b.PeakBonds);
            if(won){p.wins++;p.factionWins[b.Faction]++;if(p.bestTimes[b.Faction]<=0||b.TotalTime<p.bestTimes[b.Faction])p.bestTimes[b.Faction]=b.TotalTime;}
            var history=new List<RunRecord>{new RunRecord{id=b.RunId,faction=b.Faction,stage=b.Stage,kills=b.Kills,combo=b.BestCombo,captures=b.ShrineCaptures,guards=b.PerfectGuards,seconds=b.TotalTime,won=won}};
            foreach(var record in p.history)if(record!=null&&history.Count<6)history.Add(record);p.history=history.ToArray();p.checkpoint=null;return true;
        }
    }
    public sealed class RiftProgressTracker
    {
        int guards,captures,clears,lastSpawn;
        public void Bind(RiftBattle b)
        {guards=b.PerfectGuards;captures=b.ShrineCaptures;clears=Clears(b);lastSpawn=0;}
        static int Clears(RiftBattle b){return b.Phase==BattlePhase.Reward||b.Phase==BattlePhase.Victory?b.Stage:b.Stage-1;}
        public bool Observe(PlayerProfile p,RiftBattle b)
        {
            if(b.Practice)return false;
            bool changed=false;
            foreach(var cue in b.Cues)if(cue.kind==Cue.Spawn&&cue.source>lastSpawn)
            {lastSpawn=cue.source;if(cue.ally){p.deployments[cue.hero]++;changed=true;}}
            if(b.PerfectGuards>guards){p.lifetimeGuards+=b.PerfectGuards-guards;guards=b.PerfectGuards;changed=true;}
            if(b.ShrineCaptures>captures){p.lifetimeCaptures+=b.ShrineCaptures-captures;captures=b.ShrineCaptures;changed=true;}
            int completed=Clears(b);if(completed>clears){p.clearedStages+=completed-clears;clears=completed;changed=true;}
            if(b.PeakBonds>p.bestBondCount){p.bestBondCount=b.PeakBonds;changed=true;}return changed;
        }
    }
}
