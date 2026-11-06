#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static partial class RiftValidation
    {
        static int Spawns(RiftBattle b,int hero){int count=0;foreach(var cue in b.Cues)if(cue.kind==Cue.Spawn&&cue.ally&&cue.hero==hero)count++;return count;}
        static void CheckConvenience()
        {
            var b=new RiftBattle(810,0);b.Summon(1);b.Summon(2);var plan=new RiftDeployment();float mana=b.Mana;
            Check("An unaffordable summon becomes a single reservation without spending mana",plan.Request(b,0)==DeploymentResult.Queued&&plan.QueuedHero==0&&b.Mana==mana);
            plan.Advance(b,2,true);Check("A paused reservation cannot deploy or spend",plan.QueuedHero==0&&b.Mana==mana);
            for(int i=0;i<40&&plan.QueuedHero>=0;i++){b.Tick(.1f);plan.Advance(b,.1f,false);}
            Check("A reservation deploys exactly once when normal resources are ready",plan.QueuedHero==-1&&Spawns(b,0)==1);
            for(int i=0;i<10;i++){b.Tick(.1f);plan.Advance(b,.1f,false);}Check("A fulfilled reservation does not repeat",Spawns(b,0)==1);
            var cancelled=plan.Request(b,4);Check("Clicking the same reservation cancels it",cancelled==DeploymentResult.Queued&&plan.Request(b,4)==DeploymentResult.Cancelled&&plan.QueuedHero==-1);
            mana=b.Mana;Check("Locked heroes cannot be reserved or spend",plan.Request(b,6)==DeploymentResult.Unavailable&&plan.QueuedHero==-1&&b.Mana==mana);
            var nearReady=new RiftBattle(810,0);nearReady.Summon(0);var cancelGesture=new RiftDeployment();cancelGesture.Request(nearReady,0);nearReady.Tick(1.59f);int earlier=Spawns(nearReady,0);
            cancelGesture.BeginHold(0,true);nearReady.Tick(.15f);cancelGesture.Advance(nearReady,.15f,false);cancelGesture.EndHold();
            Check("Pressing a queued card cancels before readiness and cannot requeue on release",cancelGesture.QueuedHero==-1&&Spawns(nearReady,0)==earlier&&cancelGesture.ConsumeClick());
            var held=new RiftBattle(811,0);var hold=new RiftDeployment();hold.BeginHold(0);
            for(int i=0;i<100;i++){held.Tick(.1f);hold.Advance(held,.1f,false);}
            int produced=Spawns(held,0);hold.EndHold();
            for(int i=0;i<10;i++){held.Tick(.1f);hold.Advance(held,.1f,false);}
            Check("Holding a card repeats legal summons and stops on release",produced>=4&&Spawns(held,0)==produced&&held.Mana>=0&&held.Allies<=RiftData.UnitCap);
            Check("A held gesture suppresses the release click exactly once",hold.ConsumeClick()&&!hold.ConsumeClick());
            var telegraph=WarningFixture();telegraph.Summon(0);var waiting=new RiftDeployment();waiting.Request(telegraph,0);telegraph.Tick(1.7f);mana=telegraph.Mana;int prior=Spawns(telegraph,0);
            waiting.Advance(telegraph,.1f,false);Check("Reserved spending waits during a live boss telegraph",telegraph.WarningTime>0&&telegraph.SummonCooldown[0]==0&&Spawns(telegraph,0)==prior&&telegraph.Mana==mana&&waiting.QueuedHero==0);
            waiting.Clear();Check("Changing battle context clears held and queued actions",waiting.QueuedHero==-1&&waiting.HeldHero==-1);

            var reward=FirstReward(1237);var beforeReroll=RiftBattle.Restore(JsonUtility.FromJson<Checkpoint>(JsonUtility.ToJson(reward.CaptureCheckpoint())));
            bool originalReroll=reward.Reroll(),restoredReroll=beforeReroll.Reroll(),sameReroll=true;for(int i=0;i<3;i++)sameReroll&=reward.Choices[i]==beforeReroll.Choices[i];
            Check("An unused reroll keeps played-hero preferences across a reward-screen restart",originalReroll&&restoredReroll&&sameReroll);
            var snapshot=reward.CaptureCheckpoint();
            var reloaded=RiftBattle.Restore(JsonUtility.FromJson<Checkpoint>(JsonUtility.ToJson(snapshot)));bool same=true;for(int i=0;i<3;i++)same&=reward.Choices[i]==reloaded.Choices[i];float clock=reloaded.StageTime;reloaded.Tick(20);
            Check("Quitting before choosing resumes the exact rerolled reward screen",snapshot.pendingReward&&reloaded.Phase==BattlePhase.Reward&&reloaded.Rerolled&&same&&!reloaded.Reroll()&&reloaded.StageTime==clock);
            int choice=RiftAutoPlay.RewardIndex(reloaded,2),hero=reloaded.Choices[choice]-200,rank=hero>=0?reloaded.Ranks[hero]:0;
            Check("A resumed pending reward advances once and keeps its free awakening",hero>=0&&reloaded.ChooseReward(choice)&&reloaded.Stage==2&&reloaded.Ranks[hero]==rank+1&&!reloaded.ChooseReward(choice));
            var invalid=JsonUtility.FromJson<Checkpoint>(JsonUtility.ToJson(snapshot));invalid.rewardChoices[0]=999;
            Check("Invalid pending reward identities are rejected",!RiftBattle.IsValid(invalid));
            invalid=JsonUtility.FromJson<Checkpoint>(JsonUtility.ToJson(snapshot));invalid.rewardChoices[1]=invalid.rewardChoices[0];Check("Duplicate pending reward entries are rejected",!RiftBattle.IsValid(invalid));
            var identity=new RiftBattle(812,1,runId:"persistent-run");Check("A stage checkpoint preserves its result receipt identity",RiftBattle.Restore(identity.CaptureCheckpoint()).RunId=="persistent-run");

            var profile=new PlayerProfile();var tracking=new RiftProgressTracker();var cast=new RiftBattle(813,0);tracking.Bind(cast);cast.Summon(0);cast.Summon(1);tracking.Observe(profile,cast);tracking.Observe(profile,cast);
            Check("Re-observing the same combat cues cannot duplicate collection progress",profile.deployments[0]==1&&profile.deployments[1]==1&&profile.bestBondCount==1);
            Check("Incomplete challenges cannot be claimed",!RiftProgress.Claim(profile,0)&&profile.shards==0);
            profile.clearedStages=1;Check("A completed challenge grants its reward only once",RiftProgress.Claim(profile,0)&&!RiftProgress.Claim(profile,0)&&profile.shards==10);
            var legacy=JsonUtility.FromJson<PlayerProfile>("{\"schema\":1,\"shards\":45,\"wins\":2,\"healthUpgrade\":3}");legacy.Normalize();
            Check("Old profiles keep currency and upgrades while gaining new record fields",legacy.shards==45&&legacy.wins==2&&legacy.healthUpgrade==3&&legacy.deployments.Length==12&&legacy.factionWins.Length==2&&legacy.preferredSpeed==1);
            profile.preferredSpeed=2;var preference=JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));preference.Normalize();Check("Battle speed preference survives profile saving",preference.preferredSpeed==2);
            var preview=new RiftBattle(814,0);int echo=Array.FindIndex(RiftData.Relics,r=>r.id=="echo"),ward=Array.FindIndex(RiftData.Relics,r=>r.id=="ward");
            Check("Cost previews use the actual rounded summon cost",RiftRewardPreview.Describe(preview,echo).Contains("28 → 24"));
            preview.Relics.Add("ward");preview.Relics.Add("ward");preview.Relics.Add("ward");Check("Cooldown previews respect the actual minimum",RiftRewardPreview.Describe(preview,ward).Contains("8 → 8초"));

            string previous=RiftStorage.OverridePath;
            string folder=Path.GetFullPath(Path.Combine("BuildLogs","StorageValidation",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(folder);
            try
            {
                RiftStorage.OverridePath=Path.Combine(folder,"profile.json");profile.shards=31;profile.checkpoint=snapshot;RiftStorage.Save(profile);profile.shards=57;RiftStorage.Save(profile);
                File.WriteAllText(RiftStorage.Path,"{broken");var recovered=RiftStorage.Load();
                Check("A damaged primary save recovers the valid backup including pending rewards",recovered.shards==31&&recovered.checkpoint!=null&&recovered.checkpoint.pendingReward&&Directory.GetFiles(folder,"*.damaged-*").Length==1);
                recovered.shards+=3;RiftStorage.Save(recovered);var fresh=RiftStorage.Load();
                var backup=JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(RiftStorage.Path+".bak"));
                Check("Saving after recovery preserves the healthy backup and new progress",fresh.shards==34&&backup.shards==31&&fresh.checkpoint.rewardRerolled&&RiftProgress.Claimed(fresh,0));
            }
            finally {RiftStorage.OverridePath=previous;}
        }
    }
}
#endif
