using System;

namespace RiftHeroines
{
    public sealed partial class RiftBattle
    {
        public int BossHero {get;private set;}
        public BossTechnique ActiveBossTechnique {get;private set;}
        public float WarningCenter {get;private set;}
        public float WarningRadius {get;private set;}
        public readonly int[] BossTechniquesUsed=new int[3];
        public bool Practice {get;private set;}
        public int PracticeStep {get;private set;}
        public bool PracticeComplete {get{return Practice&&PracticeStep==4;}}
        float practiceClock;

        public static RiftBattle CreatePractice()
        {
            var b=new RiftBattle(7007,0);
            b.Practice=true;b.Mana=b.MaxMana;b.Fury=0;b.Cues.Clear();
            return b;
        }
        void AdvancePractice()
        {
            if(!Practice||PracticeComplete)return;
            bool done=PracticeStep==0?ActiveBonds[0]:PracticeStep==1?OrderTime>0:PracticeStep==2?PerfectGuards>0:UltimateUses>0;
            if(!done)return;
            PracticeStep++;practiceClock=0;
            if(PracticeStep==2)
            {
                // Move the lesson into view without a long march through an empty lane.
                foreach(var f in Fighters)if(f.ally)f.x=45+(f.id%3)*2;
                BossHero=9;BossSpawned=true;var boss=Spawn(BossHero,false,true);boss.x=66;
                ShieldCooldown=0;Mana=MaxMana;
            }
            if(PracticeStep==3){WarningTime=0;Fury=100;}
        }
        void UpdatePractice(float dt)
        {
            practiceClock+=dt;Mana=Math.Max(95,Mana);
            // Failed timing can be retried quickly; training never kills a unit or a base.
            if(PracticeStep==2)ShieldCooldown=Math.Min(3,ShieldCooldown);
            if(PracticeStep==3)Fury=100;
            AdvancePractice();
        }
        void BeginBossWarning(BossTechnique technique)
        {
            ActiveBossTechnique=technique;WarningTime=2.5f;
            float target=technique==BossTechnique.BacklineRain?94:6;bool found=false;
            foreach(var f in Fighters)if(f.ally&&f.Alive)
            {found=true;target=technique==BossTechnique.BacklineRain?Math.Min(target,f.x):Math.Max(target,f.x);}
            WarningCenter=technique==BossTechnique.Reinforcements?87:found?target:50;
            WarningRadius=technique==BossTechnique.FrontWave?22:technique==BossTechnique.BacklineRain?14:7;
            Cues.Add(new BattleCue(Cue.Warning,0,0,BossHero,WarningCenter,(int)technique,false));
        }
        void UpdateBossTechniques(float dt,bool livingBoss)
        {
            bool ready=Practice?PracticeStep==2&&practiceClock>=2:
                warningCount<(Stage==6?3:2)&&StageTime>=bossEntryTime+1.2f+warningCount*18;
            if(livingBoss&&WarningTime<=0&&ready)
            {
                var kind=Practice?BossTechnique.FrontWave:(BossTechnique)(((uint)Seed+(uint)(Stage/3-1+warningCount))%3);
                BeginBossWarning(kind);warningCount++;if(Practice)practiceClock=0;
            }
            else if(WarningTime>0)
            {
                WarningTime=Math.Max(0,WarningTime-dt);
                if(WarningTime==0)ResolveBossTechnique();
            }
        }
        void ResolveBossTechnique()
        {
            BossTechniquesUsed[(int)ActiveBossTechnique]++;
            if(ActiveBossTechnique==BossTechnique.Reinforcements)
            {
                int start=RiftData.FactionStart(1-Faction);
                for(int i=0;i<2&&Enemies<RiftData.UnitCap;i++)Spawn(start+(BossHero-start+1+i)%6,false,false);
            }
            else
            {
                foreach(var f in Fighters.ToArray())if(f.ally&&f.Alive&&Math.Abs(f.x-WarningCenter)<=WarningRadius)
                {
                    Damage(null,f,ActiveBossTechnique==BossTechnique.FrontWave?105+Stage*10:130+Stage*10,true);
                    if(ActiveBossTechnique==BossTechnique.FrontWave)f.x=Math.Max(6,f.x-4);
                }
            }
            Cues.Add(new BattleCue(Cue.Ultimate,0,0,BossHero,WarningCenter,(int)ActiveBossTechnique,false));
        }
    }
}
