#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        static void CheckWorkshop()
        {
            Check("Workshop offers 24 traits 3 amulets 12 styles and 2 frames",RiftWorkshop.Items.Length==41&&RiftWorkshop.Traits.Length==24);
            var ids=new HashSet<int>();foreach(var item in RiftWorkshop.Items)
            {
                ids.Add(item.id);var p=new PlayerProfile{shards=item.cost-1};p.Normalize();
                Check("Workshop rejects unaffordable and unowned equipment "+item.id,!RiftWorkshop.Buy(p,item.id)&&!RiftWorkshop.Equip(p,item.id)&&p.shards==item.cost-1&&p.workshopSpent==0);
                p.shards=item.cost;bool purchased=RiftWorkshop.Buy(p,item.id);bool duplicate=RiftWorkshop.Buy(p,item.id);
                var restored=JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));restored.Normalize();
                Check("Workshop purchase charges once equips and survives serialization "+item.id,purchased&&!duplicate&&p.shards==0&&p.workshopSpent==item.cost&&RiftWorkshop.Owned(restored,item.id)&&RiftWorkshop.Equipped(restored,item.id));
            }
            Check("Workshop catalogue IDs are distinct and invalid IDs cannot spend",ids.Count==41&&!RiftWorkshop.Buy(new PlayerProfile{shards=999},-1));
            var legacy=JsonUtility.FromJson<PlayerProfile>("{\"schema\":1,\"shards\":217,\"healthUpgrade\":3,\"manaUpgrade\":2}");legacy.Normalize();
            Check("Legacy profiles keep currency upgrades and a neutral new loadout",legacy.shards==217&&legacy.healthUpgrade==3&&legacy.manaUpgrade==2&&legacy.equippedAmulet==0&&legacy.equippedTraits.Length==12&&legacy.equippedStyles.Length==12&&legacy.workshopSpent==0);
            var damaged=new PlayerProfile{equippedTraits=new[]{9},equippedStyles=new[]{4},equippedAmulet=9,equippedFrame=8,workshopGoal=444};damaged.Normalize();
            Check("Invalid or unowned workshop selections normalize safely",damaged.equippedTraits[0]==0&&damaged.equippedStyles[0]==0&&damaged.equippedAmulet==0&&damaged.equippedFrame==0&&damaged.workshopGoal==0);
            var choice=new PlayerProfile{shards=200};choice.Normalize();RiftWorkshop.Buy(choice,100);RiftWorkshop.Buy(choice,101);
            Check("Buying a second trait equips one choice without stacking",choice.traitUnlocks[0]==3&&choice.equippedTraits[0]==2&&RiftWorkshop.Equip(choice,100)&&choice.equippedTraits[0]==1);
            choice.workshopGoal=200;Check("A selected collection goal shows the real balance",RiftWorkshop.Goal(choice).Contains("80 / 80")&&RiftProgress.NextGoal(choice)==RiftWorkshop.Goal(choice));RiftWorkshop.Buy(choice,200);
            Check("Buying a wished item completes its goal exactly once",choice.workshopGoal==0&&choice.shards==0&&choice.workshopSpent==200);

            for(int hero=0;hero<12;hero++)for(int pick=1;pick<=2;pick++)
            {
                var b=TraitFixture(hero,pick,false);var normal=TraitFixture(hero,0,false);var trait=RiftWorkshop.Trait(hero,pick);
                var f=b.Fighters[0];var original=normal.Fighters[0];
                Check("Trait spawn stats apply only their declared tradeoff "+hero+"/"+pick,Close(f.maxHealth,original.maxHealth*trait.health)&&Close(f.damage,original.damage*trait.damage));
                float damage=StrikeDamage(b),ordinary=StrikeDamage(normal);
                Check("Trait normal attacks use declared damage and mark effects "+hero+"/"+pick,Close(damage,ordinary*trait.damage*trait.normal, .05f));
                var boss=TraitFixture(hero,pick,true);var baseBoss=TraitFixture(hero,0,true);
                Check("Trait boss damage is applied in real combat "+hero+"/"+pick,Close(StrikeDamage(boss),StrikeDamage(baseBoss)*trait.damage*trait.boss,.05f));
                var attackClock=TraitFixture(hero,pick,false);var originalClock=TraitFixture(hero,0,false);attackClock.Tick(.025f);originalClock.Tick(.025f);
                Check("Trait interval matches the declared advantage or cost "+hero+"/"+pick,attackClock.Fighters[0].windup>0&&Close(attackClock.Fighters[0].cooldown,originalClock.Fighters[0].cooldown*trait.interval));
            }
            var wide=TraitFixture(0,2,false);var plain=TraitFixture(0,0,false);var extra=Dummy(2000,7,60);extra.stun=999;wide.Fighters.Add(extra);var untouched=Dummy(2000,7,60);untouched.stun=999;plain.Fighters.Add(untouched);StrikeDamage(wide);StrikeDamage(plain);
            Check("Sakura wide trait hits an extra enemy at the cost of speed",extra.health<1000&&untouched.health==1000&&wide.Fighters[0].cooldown>plain.Fighters[0].cooldown);
            var range=TraitFixture(2,1,false);var baseRange=TraitFixture(2,0,false);range.Fighters[1].x=baseRange.Fighters[1].x=75;range.Tick(.025f);baseRange.Tick(.025f);
            Check("Hinata range trait attacks from farther away instead of walking",range.Fighters[0].windup>0&&baseRange.Fighters[0].moving);
            foreach(int hero in new[]{1,3,8,10})for(int pick=1;pick<=2;pick++)
            {
                var b=TraitFixture(hero,pick,false);var normal=TraitFixture(hero,0,false);b.Fighters.RemoveAt(1);normal.Fighters.RemoveAt(1);
                var patient=Dummy(2000,0,50);patient.ally=true;patient.health=400;patient.stun=999;var original=Dummy(2000,0,50);original.ally=true;original.health=400;original.stun=999;b.Fighters.Add(patient);normal.Fighters.Add(original);
                b.Fighters[0].healTimer=normal.Fighters[0].healTimer=0;b.Tick(.025f);normal.Tick(.025f);var t=RiftWorkshop.Trait(hero,pick);
                Check("Healing traits change actual recovery and shields "+hero+"/"+pick,Close(patient.health-400,(original.health-400)*t.healing)&&Close(patient.shield,original.shield*t.ward));
            }
            foreach(int hero in new[]{7,11})
            {
                var fast=TraitFixture(hero,1,false);var normal=TraitFixture(hero,0,false);fast.Fighters[1].x=normal.Fighters[1].x=90;fast.Tick(.2f);normal.Tick(.2f);
                Check("Pursuit traits move faster while reducing health "+hero,fast.Fighters[0].x>normal.Fighters[0].x&&fast.Fighters[0].maxHealth<normal.Fighters[0].maxHealth);
            }
            foreach(int hero in new[]{3,6})
            {
                var stun=TraitFixture(hero,1,false);var normal=TraitFixture(hero,0,false);stun.Fighters[1].stun=normal.Fighters[1].stun=0;StrikeDamage(stun);StrikeDamage(normal);
                Check("Control traits extend real target stun with a slower interval "+hero,stun.Fighters[1].stun>normal.Fighters[1].stun+.1f&&stun.Fighters[0].cooldown>normal.Fighters[0].cooldown);
            }
            var pushed=TraitFixture(4,2,false);var unpushed=TraitFixture(4,0,false);StrikeDamage(pushed);StrikeDamage(unpushed);
            Check("Temari compressed wind trades damage for extra knockback",pushed.Fighters[1].x>unpushed.Fighters[1].x+.9f&&pushed.Fighters[0].damage<unpushed.Fighters[0].damage);
            var snow=TraitFixture(9,2,false);var ordinarySnow=TraitFixture(9,0,false);StrikeDamage(snow);StrikeDamage(ordinarySnow);
            Check("Rukia armor trades slow duration for real health",snow.Fighters[0].maxHealth>ordinarySnow.Fighters[0].maxHealth&&snow.Fighters[1].slowTime<ordinarySnow.Fighters[1].slowTime);
            var mark=TraitFixture(11,2,false);var ordinaryMark=TraitFixture(11,0,false);mark.Fighters[1].markSource=mark.Fighters[0].id;ordinaryMark.Fighters[1].markSource=ordinaryMark.Fighters[0].id;mark.Fighters[1].markTime=ordinaryMark.Fighters[1].markTime=5;
            Check("Soifon concentration strengthens the second marked hit",StrikeDamage(mark)>StrikeDamage(ordinaryMark)*1.1f&&mark.Fighters[0].damage<ordinaryMark.Fighters[0].damage);

            var loadout=new RiftLoadout();loadout.traits[0]=2;loadout.styles[0]=1;loadout.amulet=2;loadout.frame=1;
            var frozen=new RiftBattle(4123,0,departure:loadout);loadout.traits[0]=1;var copy=frozen.CaptureLoadout();copy.traits[0]=0;
            var checkpoint=frozen.CaptureCheckpoint();var restoredBattle=RiftBattle.Restore(JsonUtility.FromJson<Checkpoint>(JsonUtility.ToJson(checkpoint)));checkpoint.loadout.traits[0]=0;
            Check("Departure and checkpoint loadouts are independent frozen snapshots",frozen.TraitChoice(0)==2&&restoredBattle.TraitChoice(0)==2&&restoredBattle.StyleChoice(0)==1&&restoredBattle.EquippedAmulet==2&&restoredBattle.EquippedFrame==1);
            var old=new RiftBattle(3123,0).CaptureCheckpoint();old.loadout=null;Check("A checkpoint without workshop fields resumes with neutral equipment",RiftBattle.Restore(old).TraitCount==0&&RiftBattle.Restore(old).EquippedAmulet==0);
            var invalid=new RiftBattle(5123,0).CaptureCheckpoint();invalid.loadout.amulet=9;Check("Malformed saved loadouts are rejected",!RiftBattle.IsValid(invalid));
            var cosmetic=new RiftLoadout{frame=2};for(int i=0;i<12;i++)cosmetic.styles[i]=1;
            var decorated=new RiftBattle(5123,0,departure:cosmetic);var neutral=new RiftBattle(5123,0);decorated.Summon(0);neutral.Summon(0);decorated.Tick(19);neutral.Tick(19);
            bool sameCosmetics=decorated.Fighters.Count==neutral.Fighters.Count;for(int i=0;i<decorated.Fighters.Count&&sameCosmetics;i++)sameCosmetics&=JsonUtility.ToJson(decorated.Fighters[i])==JsonUtility.ToJson(neutral.Fighters[i]);
            Check("Cosmetic collections leave combat health damage and resources unchanged",sameCosmetics&&Close(decorated.Mana,neutral.Mana)&&Close(decorated.AllyBase,neutral.AllyBase));
            var all=new RiftLoadout();for(int i=0;i<12;i++)all.traits[i]=2;var enemies=new RiftBattle(5123,0,departure:all);var originalEnemies=new RiftBattle(5123,0);enemies.Tick(19);originalEnemies.Tick(19);
            bool identical=enemies.Fighters.Count==originalEnemies.Fighters.Count;for(int i=0;i<enemies.Fighters.Count&&identical;i++)identical&=Close(enemies.Fighters[i].maxHealth,originalEnemies.Fighters[i].maxHealth)&&Close(enemies.Fighters[i].damage,originalEnemies.Fighters[i].damage);
            Check("Equipped heroine traits never strengthen opposing units",identical);

            var conquest=AmuletShrine(1);var ordinaryShrine=AmuletShrine(0);TickShrine(conquest,4);TickShrine(ordinaryShrine,4);
            Check("Shrine amulet captures faster with a shorter actual damage buff",conquest.ShrineProgress>ordinaryShrine.ShrineProgress+.1f&&conquest.ShrineBuffDuration==9&&ordinaryShrine.ShrineBuffDuration==12);
            TickShrine(conquest,4);TickShrine(ordinaryShrine,4);Check("The shorter shrine buff is actually paid after amulet capture",conquest.ShrineCaptures==1&&ordinaryShrine.ShrineCaptures==1&&conquest.ShrineBuffTime<9&&ordinaryShrine.ShrineBuffTime>9);
            var ward=AmuletWarning(2);var basicWard=AmuletWarning(0);ward.Tick(ward.WarningTime-.5f);basicWard.Tick(basicWard.WarningTime-.5f);float before=ward.Fury,baseBefore=basicWard.Fury;ward.Shield();basicWard.Shield();
            Check("Counter amulet grants extra fury for a weaker shield",Close(ward.Fury-before,basicWard.Fury-baseBefore+8)&&Close(ward.ShieldStrength,basicWard.ShieldStrength*.75f));
            var march=new RiftBattle(7123,0,departure:new RiftLoadout{amulet=3});var ordinaryMarch=new RiftBattle(7123,0);march.Summon(0);ordinaryMarch.Summon(0);march.Order();ordinaryMarch.Order();
            Check("March amulet adds two real order seconds at a mana income cost",Close(march.OrderTime,ordinaryMarch.OrderTime+2)&&Close(march.ManaRate,ordinaryMarch.ManaRate-.7f));
            var tempo=RiftRewardPreview.Describe(march,7);Check("Reward previews include equipped amulet effects",tempo.Contains("8 → 10")&&RiftRewardPreview.Describe(ward,11).Contains("23 → 33"));
        }
        static bool Close(float a,float b,float tolerance=.01f){return Math.Abs(a-b)<tolerance;}
        static RiftBattle TraitFixture(int hero,int choice,bool boss)
        {
            var loadout=new RiftLoadout();loadout.traits[hero]=choice;var b=new RiftBattle(901,RiftData.Heroes[hero].faction,departure:loadout);b.Summon(hero);var f=b.Fighters[0];f.x=50;f.cooldown=0;
            var target=Dummy(1000,RiftData.Heroes[hero].faction==0?7:0,55);target.boss=boss;target.health=target.maxHealth=10000;target.stun=999;b.Fighters.Add(target);return b;
        }
        static float StrikeDamage(RiftBattle b){float before=b.Fighters[1].health;b.Tick(.225f);return before-b.Fighters[1].health;}
        static RiftBattle AmuletShrine(int amulet)
        {var b=new RiftBattle(707,0,departure:new RiftLoadout{amulet=amulet});b.Tick(12);b.Fighters.Clear();var f=Dummy(1001,1,50);f.ally=true;f.stun=999;b.Fighters.Add(f);return b;}
        static RiftBattle AmuletWarning(int amulet)
        {var save=new RiftBattle(900,0,departure:new RiftLoadout{amulet=amulet}).CaptureCheckpoint();save.stage=3;var b=RiftBattle.Restore(save);b.Tick(24.05f);foreach(var f in b.Fighters)if(f.boss)f.stun=999;else f.health=0;for(int i=0;i<100&&b.WarningTime<=0;i++)b.Tick(.025f);return b;}
    }
}
#endif
