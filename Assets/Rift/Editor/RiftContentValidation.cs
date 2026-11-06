#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        static void CheckContent()
        {
            var p=RiftBattle.CreatePractice();
            Check("Practice starts without a campaign receipt or checkpoint",p.Practice&&p.PracticeStep==0&&p.RunId==""&&p.CaptureCheckpoint()==null);
            Check("Practice cannot skip its pair with a different character",!p.Summon(2)&&p.PracticeStep==0);
            p.Summon(0);p.Tick(2);
            Check("Practice requires two distinct partners",!p.Summon(0)&&p.PracticeStep==0);
            p.Summon(1);Check("The actual Sakura Tsunade bond advances the lesson",p.PracticeStep==1&&p.ActiveBonds[0]);
            Check("Practice cannot skip the final lesson with an early ultimate",!p.Ultimate()&&p.UltimateUses==0);
            p.Order();Check("A real order advances practice and spawns its opponent",p.PracticeStep==2&&p.OrderTime>0&&p.Fighters.Exists(f=>f.boss&&f.hero==9));
            p.Tick(2.1f);p.Shield();
            Check("An early practice shield does not pass the counter lesson",p.PracticeStep==2&&p.PerfectGuards==0&&p.WarningTime>.65f);
            p.Tick(8);
            Check("Failed practice timing remains retryable without a long cooldown",p.PracticeStep==2&&p.Phase==BattlePhase.Battle&&p.ShieldCooldown==0&&p.Mana>=30);
            for(int i=0;i<1000&&!(p.WarningTime>0&&p.WarningTime<=.5f);i++)p.Tick(.025f);
            p.Shield();Check("A real perfect guard advances the practice lesson",p.PracticeStep==3&&p.PerfectGuards==1&&p.WarningTime==0&&p.Fury==100);
            p.Ultimate();Check("A real ultimate finishes all four practice steps",p.PracticeComplete&&p.UltimateUses==1);
            var profile=new PlayerProfile{shards=71,runs=3,checkpoint=new RiftBattle(83,1).CaptureCheckpoint()};profile.Normalize();
            string before=JsonUtility.ToJson(profile);var tracker=new RiftProgressTracker();tracker.Bind(p);
            Check("Practice never increments lifetime goals or deployments",!tracker.Observe(profile,p)&&JsonUtility.ToJson(profile)==before);
            Check("Practice never settles currency or campaign records",!RiftProgress.Settle(profile,p)&&JsonUtility.ToJson(profile)==before);
            p.Tick(300);bool alive=true;foreach(var f in p.Fighters)alive&=f.Alive;
            Check("Practice cannot end in defeat or a campaign reward",alive&&p.Phase==BattlePhase.Battle&&p.AllyBase==1000&&p.EnemyBase==860&&p.ShrineCaptures==0&&p.CaptureCheckpoint()==null);

            for(int faction=0;faction<2;faction++)
            {
                var seen=new HashSet<int>();bool varied=true,stable=true;
                for(int seed=0;seed<6;seed++)
                {
                    var save=new RiftBattle(seed,faction).CaptureCheckpoint();save.stage=3;var a=RiftBattle.Restore(save);seen.Add(a.BossHero);
                    var resumed=RiftBattle.Restore(a.CaptureCheckpoint());stable&=a.BossHero==resumed.BossHero;
                    save.stage=6;var z=RiftBattle.Restore(save);varied&=a.BossHero!=z.BossHero&&RiftData.Heroes[z.BossHero].faction==1-faction;
                }
                Check("Boss drafts cover all six opposing women "+faction,seen.Count==6&&varied);
                Check("A saved seed preserves its boss draft "+faction,stable);
            }
            for(int kind=0;kind<3;kind++)
            {
                var b=TechniqueFixture(kind);float center=b.WarningCenter;
                Check("Boss telegraph locks a matching target region "+kind,(int)b.ActiveBossTechnique==kind&&b.WarningTime>0&&b.WarningRadius>0&&Math.Abs(center-(kind==0?70:kind==1?20:87))<.1f);
                var rear=b.Fighters.Find(f=>f.id==1000);var front=b.Fighters.Find(f=>f.id==1001);
                b.Cues.Clear();TickTechnique(b,b.WarningTime+.01f);
                bool effect=kind==0?front.health<1000&&rear.health==1000&&front.x==66:kind==1?rear.health<1000&&front.health==1000:b.Cues.FindAll(c=>c.kind==Cue.Spawn&&!c.ally).Count>=2&&rear.health==1000&&front.health==1000;
                Check("Boss technique has its distinct real effect "+kind,b.BossTechniquesUsed[kind]==1&&effect,"rear="+rear.health+", front="+front.health+", x="+front.x);
                var interrupted=TechniqueFixture(kind);interrupted.Tick(interrupted.WarningTime-.5f);interrupted.Shield();interrupted.Tick(.55f);
                Check("Perfect guard interrupts each boss technique "+kind,interrupted.PerfectGuards==1&&interrupted.BossTechniquesUsed[kind]==0);
                var ultimate=TechniqueFixture(kind,true);
                bool used=ultimate.Ultimate();ultimate.Tick(.2f);
                Check("Ultimate interrupts each boss technique "+kind,used&&ultimate.WarningTime==0&&ultimate.BossTechniquesUsed[kind]==0);
            }
            bool arenas=true;for(int stage=1;stage<=6;stage++)arenas&=RiftData.ArenaForStage(stage)==(stage-1)/2;
            Check("Six campaign stages map to three successive environments",arenas&&new HashSet<string>(RiftData.ArenaIds).Count==3);
            for(int i=0;i<3;i++)
            {
                string path=i==0?"Rift/Art/moonbridge":"Rift/Stages/"+RiftData.ArenaIds[i];var texture=Resources.Load<Texture2D>(path);
                Check("Chapter environment is a real wide texture "+i,texture&&texture.width>=1500&&texture.height>=840&&texture.width>texture.height*1.7f);
            }
        }
        static void TickTechnique(RiftBattle b,float seconds)
        {while(seconds>0){float step=Math.Min(seconds,.025f);foreach(var f in b.Fighters)if(!f.ally&&!f.boss)f.health=0;b.Tick(step);seconds-=step;}}
        static RiftBattle TechniqueFixture(int kind,bool charged=false)
        {
            var save=new RiftBattle(900+kind,0).CaptureCheckpoint();save.stage=3;if(charged)save.relics=new[]{"moon","moon","moon","moon","moon"};var b=RiftBattle.Restore(save);b.Tick(24.05f);
            foreach(var f in b.Fighters)if(f.boss)f.stun=999;else f.health=0;
            var rear=Dummy(1000,0,20);rear.ally=true;rear.stun=999;
            var front=Dummy(1001,0,70);front.ally=true;front.stun=999;b.Fighters.Add(rear);b.Fighters.Add(front);
            for(int i=0;i<100&&b.WarningTime<=0;i++)b.Tick(.025f);
            return b;
        }
    }
}
#endif
