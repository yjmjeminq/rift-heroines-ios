using System;

namespace RiftHeroines
{
    public sealed partial class RiftBattle
    {
        public readonly bool[] ActiveBonds=new bool[RiftData.Bonds.Length];
        readonly bool[] presentHeroes=new bool[RiftData.HeroCount];
        readonly int[] stageSummons=new int[RiftData.HeroCount];
        bool resolvingCascade;
        public int PerfectGuards {get;private set;}
        public int ShrineCaptures {get;private set;}
        public int ChainBursts {get;private set;}
        public int FreeAwakens {get;private set;}
        public int BondActivations {get;private set;}
        public int PeakBonds {get;private set;}
        public float BossBreakTime {get;private set;}
        public float ShrineBuffTime {get;private set;}
        public float EnemyShrineBuffTime {get;private set;}
        public float ShrineProgress {get;private set;}
        public int ShrineRound {get;private set;}
        public int ShrineOwner {get;private set;} // 0 contested, 1 ally, -1 enemy
        public int Encounter {get{return RiftData.RuleForStage(Seed,Stage);}}
        public bool EnemyBaseProtected {get{return enemyBaseShield>0;}}
        public bool ShrineActive {get{return StageTime>=12&&ShrineOwner==0;}}
        public float NextShrine {get{return StageTime<12?12-StageTime:12+ShrineRound*40-StageTime;}}
        public int BondCount {get{int count=0;foreach(bool active in ActiveBonds)if(active)count++;return count;}}
        public float BondManaBonus {get{float bonus=0;for(int i=0;i<ActiveBonds.Length;i++)if(ActiveBonds[i])bonus+=RiftData.Bonds[i].mana;return bonus;}}
        public int BondProgress(int bond){var b=RiftData.Bonds[bond];return (presentHeroes[b.a]?1:0)+(presentHeroes[b.b]?1:0);}
        void ResetTactics()
        {
            Array.Clear(ActiveBonds,0,ActiveBonds.Length);Array.Clear(presentHeroes,0,presentHeroes.Length);Array.Clear(stageSummons,0,stageSummons.Length);
            BossBreakTime=ShrineBuffTime=EnemyShrineBuffTime=ShrineProgress=0;ShrineRound=ShrineOwner=0;resolvingCascade=false;
        }
        void RefreshBonds(bool cue)
        {
            Array.Clear(presentHeroes,0,presentHeroes.Length);
            foreach(var f in Fighters)if(f.ally&&f.Alive)presentHeroes[f.hero]=true;
            for(int i=0;i<ActiveBonds.Length;i++)
            {
                var b=RiftData.Bonds[i];bool active=presentHeroes[b.a]&&presentHeroes[b.b];
                if(active&&!ActiveBonds[i]&&cue){BondActivations++;Cues.Add(new BattleCue(Cue.Bond,0,0,b.a,CenterControl,i));}
                ActiveBonds[i]=active;
            }
            PeakBonds=Math.Max(PeakBonds,BondCount);
        }
        float HeroDamage(int hero)
        {float value=1;for(int i=0;i<ActiveBonds.Length;i++)if(ActiveBonds[i]&&(RiftData.Bonds[i].a==hero||RiftData.Bonds[i].b==hero))value+=RiftData.Bonds[i].damage;return value;}
        float HeroHaste(int hero)
        {float value=1;for(int i=0;i<ActiveBonds.Length;i++)if(ActiveBonds[i]&&(RiftData.Bonds[i].a==hero||RiftData.Bonds[i].b==hero))value+=RiftData.Bonds[i].haste;return value;}
        void CounterBoss()
        {
            PerfectGuards++;WarningTime=0;BossBreakTime=4+Count("riposte");Fury=Math.Min(100,Fury+GuardFury);
            foreach(var f in Fighters)if(f.boss&&f.Alive){f.stun=Math.Max(f.stun,2);f.windup=0;f.animation=0;}
            Cues.Add(new BattleCue(Cue.PerfectGuard,0,0,RiftData.FactionStart(Faction),CenterControl,35,true,true));
        }
        void UpdateShrine(float dt)
        {
            if(StageTime<12)return;
            int round=1+(int)((StageTime-12)/40);
            if(round!=ShrineRound){ShrineRound=round;ShrineOwner=0;ShrineProgress=0;}
            if(ShrineOwner!=0)return;
            int ally=0,enemy=0;
            foreach(var f in Fighters)if(f.Alive&&f.x>=32&&f.x<=68){if(f.ally)ally++;else enemy++;}
            ally=Math.Min(3,ally);enemy=Math.Min(3,enemy);
            if(ally==enemy){ShrineProgress-=Math.Sign(ShrineProgress)*Math.Min(Math.Abs(ShrineProgress),dt*.06f);return;}
            int direction=ally>enemy?1:-1;
            ShrineProgress=Math.Max(-1,Math.Min(1,ShrineProgress+direction*dt*(.13f+.025f*Math.Abs(ally-enemy))*(direction>0&&EquippedAmulet==1?1.2f:1)));
            if(Math.Abs(ShrineProgress)<1)return;
            ShrineOwner=direction;
            if(direction>0){ShrineCaptures++;Mana=Math.Min(MaxMana,Mana+35+Count("conquest")*15);Fury=Math.Min(100,Fury+20);ShrineBuffTime=ShrineBuffDuration;}
            else EnemyShrineBuffTime=12;
            Cues.Add(new BattleCue(Cue.Capture,0,0,RiftData.FactionStart(direction>0?Faction:1-Faction),50,direction>0?35+Count("conquest")*15:0,direction>0));
        }
        void RewardChain(Fighter fallen,Fighter source)
        {
            if(Combo<5||Combo%5!=0)return;
            Mana=Math.Min(MaxMana,Mana+10);Fury=Math.Min(100,Fury+8);Cues.Add(new BattleCue(Cue.Chain,0,0,fallen.hero,fallen.x,Combo,true,true));
            int count=Count("cascade");if(count==0||resolvingCascade)return;
            ChainBursts++;resolvingCascade=true;
            try {foreach(var f in Fighters.ToArray())if(!f.ally&&f.Alive&&Math.Abs(f.x-fallen.x)<=10)Damage(source,f,(50+Stage*7)*count,true);}
            finally {resolvingCascade=false;}
        }
    }
}
