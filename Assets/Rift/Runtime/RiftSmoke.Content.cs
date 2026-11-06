using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiftHeroines
{
    public sealed partial class RiftSmoke
    {
        const int RequiredContentChecks=7;
        readonly List<string> contentChecks=new List<string>();
        readonly HashSet<string> observedArenas=new HashSet<string>(),observedTechniques=new HashSet<string>();
        static string[] Sorted(HashSet<string> source){var values=new string[source.Count];source.CopyTo(values);Array.Sort(values);return values;}
        void VerifyContent(string name,bool passed)
        {if(passed){if(!contentChecks.Contains(name))contentChecks.Add(name);}else Error("Content: "+name);}
        IEnumerator VerifyTrainingRoom()
        {
            // A QA-only saved expedition proves that practice leaves an existing run intact.
            var original=game.Profile.checkpoint;var checkpoint=new RiftBattle(572,1,runId:"practice-preservation").CaptureCheckpoint();checkpoint.stage=4;
            game.Profile.checkpoint=checkpoint;RiftStorage.Save(game.Profile);
            string before=JsonUtility.ToJson(game.Profile);
            Click("Training room");yield return null;
            VerifyContent("practice-opens-with-existing-save",game.Battle.Practice&&game.Battle.PracticeStep==0&&game.CurrentArenaId=="moonbridge"&&game.Profile.runs==0);
            CheckVisibleText();yield return Capture("20-training-start");
            Click("Deploy sakura");Click("Deploy tsunade");yield return null;
            VerifyContent("practice-pair-and-order",game.Battle.PracticeStep==1&&game.Battle.ActiveBonds[0]);
            Click("Order");yield return null;
            for(int i=0;i<100&&game.Battle.WarningTime<=0;i++){game.Battle.Tick(.08f);yield return null;}
            Click("Ward");yield return null;
            bool early=game.Battle.PracticeStep==2&&game.Battle.PerfectGuards==0;
            CheckVisibleText();yield return Capture("21-training-retry");
            for(int i=0;i<400&&!(game.Battle.WarningTime>0&&game.Battle.WarningTime<=.6f&&game.Battle.ShieldCooldown<=0);i++){game.Battle.Tick(.06f);yield return null;}
            game.enabled=false;
            try {game.CancelDeploymentQueue();yield return Capture("22-training-counter");Click("Ward");}
            finally {game.enabled=true;}
            yield return null;
            VerifyContent("practice-failed-timing-and-counter-retry",early&&game.Battle.PracticeStep==3&&game.Battle.PerfectGuards==1&&game.Battle.Fury==100);
            Click("Ultimate");yield return null;
            VerifyContent("practice-finishes-with-real-ultimate",game.Battle.PracticeComplete&&game.Battle.UltimateUses==1&&game.Paused);
            CheckVisibleText();yield return Capture("23-training-complete");
            Click("Training finish");yield return null;
            VerifyContent("practice-preserves-profile-and-disk-checkpoint",JsonUtility.ToJson(game.Profile)==before&&JsonUtility.ToJson(RiftStorage.Load())==before);
            Click("Continue");yield return null;
            VerifyContent("practice-preserves-resumable-expedition",!game.Battle.Practice&&game.Battle.Stage==4&&game.Battle.Faction==1&&game.Battle.RunId==checkpoint.runId);
            game.ReturnHome();yield return null;
            Click("Training room");yield return null;Click("Leave training");yield return null;
            VerifyContent("practice-can-exit-without-completing",game.Battle==null&&JsonUtility.ToJson(game.Profile)==before);
            game.Profile.checkpoint=original;RiftStorage.Save(game.Profile);game.ReturnHome();yield return null;
        }
    }
}
