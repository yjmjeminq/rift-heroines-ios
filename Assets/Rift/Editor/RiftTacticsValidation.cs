#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        static void CheckTactics()
        {
            var passing=new RiftBattle(700,0);passing.Summon(0);passing.Summon(3);
            var melee=passing.Fighters[0];var caster=passing.Fighters[1];melee.x=71;caster.x=73;caster.id=4;caster.cooldown=999;
            passing.Tick(1);Check("Melee fighters pass supports stopped outside melee range",melee.x>77&&caster.x<74);
            var turned=new RiftBattle(701,0);turned.Summon(0);turned.Fighters[0].x=70;turned.Fighters.Add(Dummy(999,7,45));turned.Tick(.2f);
            Check("A fighter turns toward an enemy behind it",turned.Fighters[0].x<69);
            bool bounded=true;for(int i=0;i<100;i++){RiftAutoPlay.Drive(turned);turned.Tick(.1f);foreach(var f in turned.Fighters)bounded&=f.x>=6&&f.x<=94;turned.Cues.Clear();}
            Check("Combatants remain inside the two base boundaries",bounded);

            var duplicate=new RiftBattle(702,0);duplicate.Summon(0);duplicate.Tick(1.7f);duplicate.Summon(0);
            Check("Copies of one hero cannot activate a two-hero bond",duplicate.BondCount==0);
            var pair=new RiftBattle(703,0);pair.Summon(0);pair.Summon(1);pair.Fighters[0].x=50;
            var pairedTarget=Dummy(999,7,55);pair.Fighters.Add(pairedTarget);pair.Tick(.5f);
            var solo=new RiftBattle(703,0);solo.Summon(0);solo.Fighters[0].x=50;var soloTarget=Dummy(999,7,55);solo.Fighters.Add(soloTarget);solo.Tick(.5f);
            Check("A live Sakura/Tsunade pair strengthens actual attack damage",pair.ActiveBonds[0]&&pairedTarget.health<soloTarget.health-5);
            var sensing=new RiftBattle(704,0);sensing.Summon(2);sensing.Summon(3);
            Check("Hinata/Ino add a single nonstacking mana bond",sensing.ActiveBonds[1]&&Math.Abs(sensing.ManaRate-10.2f)<.01f);
            sensing.Fighters[1].health=0;sensing.Tick(.025f);
            Check("Losing a partner removes its live bond",!sensing.ActiveBonds[1]&&Math.Abs(sensing.ManaRate-9)<.01f);
            var fast=new RiftBattle(704,1);fast.Summon(7);fast.Summon(11);fast.Fighters[0].x=50;var fastTarget=Dummy(999,0,55);fastTarget.health=fastTarget.maxHealth=10000;fast.Fighters.Add(fastTarget);fast.Tick(6);
            var slow=new RiftBattle(704,1);slow.Summon(7);slow.Fighters[0].x=50;var slowTarget=Dummy(999,0,55);slowTarget.health=slowTarget.maxHealth=10000;slow.Fighters.Add(slowTarget);slow.Tick(6);
            Check("Yoruichi/Soifon's haste bond produces more real attacks",fast.ActiveBonds[4]&&fast.Fighters[0].attacks>slow.Fighters[0].attacks,"bond="+fast.Fighters[0].attacks+", solo="+slow.Fighters[0].attacks);
            var members=new HashSet<int>();foreach(var bond in RiftData.Bonds){members.Add(bond.a);members.Add(bond.b);}
            Check("The six configured bonds cover all twelve distinct heroes",RiftData.Bonds.Length==6&&members.Count==12);

            var shrine=ShrineFixture(true,false);shrine.UpgradeEconomy();int before=shrine.ShrineCaptures;float earned=0,fury=0;
            for(int i=0;i<140;i++)
            {
                float mana=shrine.Mana,charge=shrine.Fury;TickShrine(shrine,.05f);
                if(shrine.ShrineCaptures>before){earned=shrine.Mana-mana;fury=shrine.Fury-charge;break;}
            }
            Check("Holding the central shrine grants resources and a timed offensive buff",shrine.ShrineCaptures==1&&shrine.ShrineOwner==1&&shrine.ShrineBuffTime>11&&earned>1&&fury>=19.9f);
            int captures=shrine.ShrineCaptures;TickShrine(shrine,4);
            Check("A claimed shrine pays once per opening",shrine.ShrineCaptures==captures);
            TickShrine(shrine,52.1f-shrine.StageTime);
            Check("The shrine reopens for another contest after forty seconds",shrine.ShrineRound==2&&shrine.ShrineOwner==0&&Math.Abs(shrine.ShrineProgress)<.1f);
            var contest=ShrineFixture(true,true);TickShrine(contest,8);
            Check("Equal opposing pressure keeps the shrine contested",contest.ShrineOwner==0&&Math.Abs(contest.ShrineProgress)<.01f);
            var enemyShrine=ShrineFixture(false,true);TickShrine(enemyShrine,7);
            Check("The enemy can win shrine control and receive a visible buff",enemyShrine.ShrineOwner==-1&&enemyShrine.EnemyShrineBuffTime>0&&enemyShrine.ShrineCaptures==0);
            var conquest=ShrineFixture(true,false);conquest.Relics.Add("conquest");TickShrine(conquest,6.7f);
            Check("The conquest relic extends the shrine buff and reports its larger payout",conquest.ShrineBuffTime>15&&conquest.Cues.Exists(c=>c.kind==Cue.Capture&&c.ally&&c.amount==50));

            var early=WarningFixture();early.Shield();
            Check("An early shield protects without granting a perfect counter",early.WarningTime>.65f&&early.PerfectGuards==0&&early.BossBreakTime==0);
            var perfect=WarningFixture();perfect.Tick(perfect.WarningTime-.5f);float available=perfect.Mana;bool guarded=perfect.Shield();
            Check("A last-moment shield interrupts the boss and exposes its weakness",guarded&&perfect.PerfectGuards==1&&perfect.WarningTime==0&&perfect.BossBreakTime>=4&&Math.Abs(perfect.Mana-(available-30))<.01f&&perfect.Cues.Exists(c=>c.kind==Cue.PerfectGuard));
            Check("A perfect shield cannot be repeatedly triggered during cooldown",!perfect.Shield()&&perfect.PerfectGuards==1);
            var riposte=WarningFixture();riposte.Relics.Add("riposte");riposte.Tick(riposte.WarningTime-.5f);float oldFury=riposte.Fury;riposte.Shield();
            Check("The riposte relic increases the counter's actual fury and weakness duration",riposte.BossBreakTime==5&&Math.Abs(riposte.Fury-oldFury-25)<.01f);
            var counterSave=perfect.CaptureCheckpoint();var resumed=RiftBattle.Restore(counterSave);
            Check("Stage checkpoints retain earned tactical records and reset transient buffs",resumed.PerfectGuards==1&&resumed.BossBreakTime==0&&resumed.ShrineRound==0);
            var ordinary=WarningFixture();ordinary.Tick(ordinary.WarningTime-.5f);
            perfect.Fighters.RemoveAll(f=>!f.boss);ordinary.Fighters.RemoveAll(f=>!f.boss);
            var bossA=perfect.Fighters.Find(f=>f.boss);var bossB=ordinary.Fighters.Find(f=>f.boss);
            perfect.Summon(2);ordinary.Summon(2);perfect.Fighters.Find(f=>f.ally).x=bossA.x-10;ordinary.Fighters.Find(f=>f.ally).x=bossB.x-10;
            float hpA=bossA.health,hpB=bossB.health;perfect.Tick(.56f);ordinary.Tick(.56f);
            Check("Countering opens a real damage window against the boss",hpA-bossA.health>(hpB-bossB.health)*1.3f&&hpB>bossB.health,"counter="+(hpA-bossA.health)+", normal="+(hpB-bossB.health));

            var heal=new RiftBattle(705,0);heal.Ranks[8]=1;heal.Summon(1);heal.Summon(8);var patient=heal.Fighters[0];patient.health=patient.maxHealth*.5f;
            foreach(var f in heal.Fighters){f.x=50;f.healTimer=0;}float hurt=patient.health;heal.Tick(.025f);
            Check("Simultaneous healers do not stack healing onto the same recipient",Math.Abs(patient.health-hurt-patient.maxHealth*.1f)<.1f&&patient.healLock>0);
            var bossHeal=new RiftBattle(706,0);var patientBoss=Dummy(1000,6,60);patientBoss.boss=true;patientBoss.maxHealth=10000;var medic=Dummy(1001,8,60);medic.healTimer=0;bossHeal.Fighters.Add(patientBoss);bossHeal.Fighters.Add(medic);bossHeal.Tick(.025f);
            Check("Boss healing is capped to prevent an unbounded recovery stalemate",Math.Abs(patientBoss.health-1350)<.1f);

            var explosion=ChainFixture(true);var plain=ChainFixture(false);explosion.Ultimate();plain.Ultimate();
            Check("The fifth chain kill detonates a relic explosion without duplicate kills",explosion.ChainBursts==1&&explosion.Kills==5&&plain.Fighters[5].health-explosion.Fighters[5].health>56);
            explosion.Tick(4.1f);Check("Chain progress expires while the best chain is retained",explosion.Combo==0&&explosion.BestCombo==5);
            var rules=new HashSet<int>();for(int seed=1;seed<=3;seed++)rules.Add(RiftData.RuleForStage(seed,2));
            var encounterSave=new RiftBattle(3,0).CaptureCheckpoint();encounterSave.stage=2;var encounter=RiftBattle.Restore(encounterSave);
            var encounterReload=RiftBattle.Restore(UnityEngine.JsonUtility.FromJson<Checkpoint>(UnityEngine.JsonUtility.ToJson(encounter.CaptureCheckpoint())));
            Check("Repeat runs vary non-boss encounters without changing a saved seed",rules.Count==3&&encounter.Encounter==encounterReload.Encounter&&encounterReload.Seed==3&&RiftData.RuleForStage(1,3)==4);

            var reward=FirstReward(1237);int recruit=0,relic=0,awaken=0,awakeIndex=-1;
            for(int i=0;i<3;i++){int choice=reward.Choices[i];if(choice>=200){awaken++;awakeIndex=i;}else if(choice>=100)recruit++;else relic++;}
            Check("A clear offers distinct recruit, relic and free-awakening decisions",recruit==1&&relic==1&&awaken==1);
            if(awakeIndex>=0){int hero=reward.Choices[awakeIndex]-200,rank=reward.Ranks[hero];reward.ChooseReward(awakeIndex);Check("A free awakening carries its hero's upgrade into the next stage",reward.Ranks[hero]==rank+1&&reward.FreeAwakens==1&&reward.Stage==2);}
            var locations=new HashSet<int>();
            for(int seed=1;seed<=3;seed++){var draft=FirstReward(seed*1237);locations.Add(RiftAutoPlay.RewardIndex(draft));}
            Check("Recruit rewards do not occupy a fixed default button",locations.Count>1);
        }
        static RiftBattle WarningFixture()
        {
            var save=new RiftBattle(821,0).CaptureCheckpoint();save.stage=3;var b=RiftBattle.Restore(save);
            while(!b.BossSpawned&&b.Phase==BattlePhase.Battle)b.Tick(.1f);
            foreach(var f in b.Fighters)if(!f.boss)f.health=0;
            for(int i=0;i<400&&b.WarningTime<=0&&b.Phase==BattlePhase.Battle;i++)b.Tick(.025f);
            return b;
        }
        static RiftBattle ShrineFixture(bool ally,bool enemy)
        {
            var b=new RiftBattle(707,0);b.Tick(12);b.Fighters.Clear();
            if(ally){var f=Dummy(1001,1,50);f.ally=true;f.stun=999;b.Fighters.Add(f);}
            if(enemy){var f=Dummy(1002,6,50);f.stun=999;b.Fighters.Add(f);}
            return b;
        }
        static void TickShrine(RiftBattle b,float seconds)
        {while(seconds>0){float step=Math.Min(seconds,.025f);b.Fighters.RemoveAll(f=>f.id<1000);b.Tick(step);seconds-=step;}}
        static RiftBattle ChainFixture(bool cascade)
        {
            var save=new RiftBattle(708,0).CaptureCheckpoint();save.relics=cascade?new[]{"moon","moon","moon","moon","moon","cascade"}:new[]{"moon","moon","moon","moon","moon"};
            var b=RiftBattle.Restore(save);for(int i=0;i<6;i++){var f=Dummy(1000+i,7,50);f.health=f.maxHealth=i==5?2000:10;f.stun=999;b.Fighters.Add(f);}return b;
        }
        static RiftBattle FirstReward(int seed)
        {var b=new RiftBattle(seed,0);for(int i=0;i<3000&&b.Phase==BattlePhase.Battle;i++){RiftAutoPlay.Drive(b);b.Tick(.1f);b.Cues.Clear();}return b;}
    }
}
#endif
