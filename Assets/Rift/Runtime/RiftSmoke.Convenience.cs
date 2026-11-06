using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftSmoke
    {
        const int RequiredConvenienceChecks=13;
        readonly List<string> convenienceChecks=new List<string>();
        int completedCampaigns;
        bool ownedInventoryChecked;
        void VerifyConvenience(string name,bool passed)
        {if(passed){if(!convenienceChecks.Contains(name))convenienceChecks.Add(name);}else Error("Convenience: "+name);}
        IEnumerator VerifyConvenienceBeforeCampaign()
        {
            Click("Deploy tsunade");Click("Deploy hinata");Click("Deploy sakura");yield return null;
            bool reserved=game.QueuedHero==0;yield return Capture("14-deployment-queue");
            for(int i=0;i<40&&game.QueuedHero>=0;i++){game.Battle.Tick(.1f);yield return null;}
            VerifyConvenience("queued-once",reserved&&game.QueuedHero==-1&&game.Profile.deployments[0]==1);
            Click("Deploy konan");yield return null;bool other=game.QueuedHero==5;Click("Switch roster");yield return null;
            bool stable=game.QueuedHero==5;Click("Cancel deployment");yield return null;
            VerifyConvenience("queue-page-and-cancel",other&&stable&&game.QueuedHero==-1);Click("Switch roster");yield return null;

            game.ReturnHome();yield return null;Click("Start Shinobi");yield return null;
            var card=GameObject.Find("Deploy sakura");var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(card,pointer,ExecuteEvents.pointerDownHandler);
            int before=game.Profile.deployments[0];
            for(int i=0;i<120&&game.Battle.Phase==BattlePhase.Battle;i++){game.Battle.Tick(.08f);yield return null;}
            ExecuteEvents.Execute(card,pointer,ExecuteEvents.pointerUpHandler);yield return null;
            int after=game.Profile.deployments[0];ExecuteEvents.Execute(card,pointer,ExecuteEvents.pointerClickHandler);yield return null;
            VerifyConvenience("hold-and-release",after-before>=3&&game.Profile.deployments[0]==after&&game.QueuedHero==-1);
            game.PauseForFocusLoss();yield return null;float clock=game.Battle.StageTime;
            for(int i=0;i<8;i++)yield return null;
            VerifyConvenience("focus-loss-pauses",game.Paused&&game.Battle.StageTime==clock);
            Click("Pause overview");yield return null;CheckVisibleText();yield return Capture("15-build-overview");
            bool visible=game.OverviewOpen;Click("Close build");yield return null;
            VerifyConvenience("inventory-preserves-pause",visible&&!game.OverviewOpen&&game.Paused);Click("Resume");yield return null;
            game.ReturnHome();yield return null;Click("Start Shinobi");yield return null;
        }
        IEnumerator VerifyPendingReward()
        {
            int[] original=(int[])game.Battle.Choices.Clone();Click("Reward overview");yield return null;
            bool open=game.OverviewOpen&&game.Battle.Phase==BattlePhase.Reward;Click("Close build");yield return null;
            bool same=true;for(int i=0;i<3;i++)same&=original[i]==game.Battle.Choices[i];
            VerifyConvenience("reward-inventory-return",open&&same&&!game.OverviewOpen);
            Click("Reroll");yield return null;var choices=(int[])game.Battle.Choices.Clone();int stage=game.Battle.Stage;string run=game.Battle.RunId;
            var saved=RiftStorage.Load().checkpoint;game.ReturnHome();yield return null;Click("Continue");yield return null;
            same=game.Battle!=null;for(int i=0;i<3&&same;i++)same&=choices[i]==game.Battle.Choices[i];
            VerifyConvenience("pending-reward-cold-continue",saved!=null&&saved.pendingReward&&saved.rewardRerolled&&game.Battle.Phase==BattlePhase.Reward&&game.Battle.Stage==stage&&game.Battle.RunId==run&&game.Battle.Rerolled&&same);
            CheckVisibleText();yield return Capture("16-pending-reward-restored");
        }
        IEnumerator VerifyRecordsAfterVictory()
        {
            yield return null;Click("Result records");yield return null;
            bool recorded=game.Profile.history.Length==1&&game.Profile.history[0].won&&game.Profile.factionWins[0]==1;
            int money=game.Profile.shards;Click("Claim first");yield return null;
            var claim=GameObject.Find("Claim first").GetComponent<Button>();var saved=RiftStorage.Load();
            VerifyConvenience("records-and-claim-once",recorded&&game.Profile.shards==money+10&&!claim.interactable&&RiftProgress.Claimed(saved,0));
            CheckVisibleText();yield return Capture("17-records-and-challenges");
            VerifyConvenience("layout-bounds",ControlsInsideScreen(new[]{"Close records","Claim first","Claim factions"}));
            Click("Close records");yield return null;
        }
        IEnumerator VerifyOwnedInventory(int relic)
        {
            var definition=RiftData.Relics[relic];int count=game.Battle.RelicCount(definition.id);
            Click("Build overview");yield return null;
            var card=GameObject.Find("Owned relic "+definition.id);
            bool populated=game.OverviewOpen&&game.Paused&&card&&card.transform.Find("Owned relic name").GetComponent<Text>().text.Contains("×"+count);
            CheckVisibleText();yield return Capture("19-owned-relic-inventory");Click("Close build");yield return null;
            VerifyConvenience("owned-relic-inventory",populated&&!game.Paused&&game.Battle.RelicCount(definition.id)==count);ownedInventoryChecked=true;
        }
        IEnumerator CompleteSecondFaction()
        {
            int guard=0;
            while(game.Battle.Phase!=BattlePhase.Victory&&game.Battle.Phase!=BattlePhase.Defeat&&guard++<4000)
            {
                if(game.Battle.Phase==BattlePhase.Reward)
                {yield return null;int index=RiftAutoPlay.RewardIndex(game.Battle,3);Click("Reward "+index);yield return null;}
                else {RiftAutoPlay.Drive(game.Battle);game.Battle.Tick(game.Battle.WarningTime>0?.12f:.5f);yield return null;}
            }
            yield return null;
            bool won=game.Battle.Phase==BattlePhase.Victory;if(won)completedCampaigns++;
            VerifyConvenience("both-faction-records",won&&game.Profile.history.Length==2&&game.Profile.factionWins[0]==1&&game.Profile.factionWins[1]==1&&RiftProgress.Ready(game.Profile,5));
            int seed=game.Battle.Seed,faction=game.Battle.Faction;string id=game.Battle.RunId;int money=game.Profile.shards;
            Click("Same field retry");yield return null;
            VerifyConvenience("same-field-new-receipt",game.Battle.Seed==seed&&game.Battle.Faction==faction&&game.Battle.RunId!=id&&game.Battle.Stage==1&&game.Profile.history.Length==2&&game.Profile.shards==money);
            game.ReturnHome();yield return null;Click("Records");yield return null;Click("Claim factions");yield return null;
            VerifyConvenience("two-faction-goal-saved",game.Profile.shards==money+40&&RiftProgress.Claimed(RiftStorage.Load(),5)&&game.Profile.history.Length==2);
            CheckVisibleText();yield return Capture("18-two-faction-records");Click("Close records");yield return null;
        }
        bool ControlsInsideScreen(string[] names)
        {
            var camera=FindFirstObjectByType<Camera>();var corners=new Vector3[4];
            foreach(string name in names)
            {
                var go=GameObject.Find(name);if(!go)return false;go.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach(var p in corners){var s=RectTransformUtility.WorldToScreenPoint(camera,p);if(s.x<0||s.y<0||s.x>Screen.width||s.y>Screen.height)return false;}
            }
            return true;
        }
        void CheckVisibleText()
        {
            foreach(var label in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                string name=label.gameObject.name;
                if(name!="Challenge goal"&&name!="Relic comparison"&&name!="Owned relic effect"&&name!="Build hero state"&&name!="Collected name"&&name!="Training note"&&name!="Training title"&&name!="Training result"&&!name.StartsWith("Workshop item")&&name!="Workshop notice"&&name!="Workshop caption"&&name!="Workshop goal"&&name!="Workshop preview state")continue;
                if(label.gameObject.activeInHierarchy&&label.preferredHeight>label.rectTransform.rect.height+2)Error("Clipped readable text: "+name+" / "+label.text);
            }
        }
    }
}
