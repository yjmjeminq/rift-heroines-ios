using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftSmoke
    {
        const int RequiredWorkshopChecks=16;
        readonly List<string> workshopChecks=new List<string>();
        int workshopEarned,workshopSpent;
        int workshopAttackSamples,workshopPoseMismatches,workshopEffectPeak;
        void WorkshopCheck(string name,bool passed){if(passed){if(!workshopChecks.Contains(name))workshopChecks.Add(name);}else Error("Workshop: "+name);}
        IEnumerator VerifyResultWorkshop()
        {
            int money=game.Profile.shards;string run=game.Battle.RunId;
            Click("Result workshop");yield return null;
            bool opened=game.WorkshopOpen;Click("Workshop wish 100");yield return null;CheckVisibleText();yield return Capture("28-workshop-goal");
            WorkshopCheck("result-collection-goal-persists",opened&&game.Profile.workshopGoal==100&&RiftStorage.Load().workshopGoal==100&&game.Profile.shards==money);
            Click("Close workshop");yield return null;
            WorkshopCheck("result-shop-return-never-duplicates-payout",game.Battle.RunId==run&&game.Battle.Phase==BattlePhase.Victory&&game.Profile.shards==money&&!game.WorkshopOpen);
            yield return Capture("29-result-collection-goal");
        }
        IEnumerator VerifyWorkshopEconomy()
        {
            workshopEarned=game.Profile.shards;Click("Workshop");yield return null;
            bool catalogue=true;
            for(int hero=0;hero<12;hero++)
            {
                Click("Workshop hero "+hero);yield return null;CheckVisibleText();
                catalogue&=GameObject.Find("Workshop item "+(100+hero*2))&&GameObject.Find("Workshop item "+(101+hero*2));
            }
            WorkshopCheck("all-twelve-hero-specializations-visible",catalogue);Click("Workshop hero 0");yield return null;
            string originalPath=RiftStorage.OverridePath;string originalDisk=File.ReadAllText(RiftStorage.Path);string block=Path.Combine(output,"workshop-blocked-parent");File.WriteAllText(block,"QA write failure fixture");
            try
            {
                RiftStorage.OverridePath=Path.Combine(block,"profile.json");Click("Workshop buy 101");yield return null;
                WorkshopCheck("failed-purchase-write-rolls-back-currency",game.Profile.shards==workshopEarned&&!RiftWorkshop.Owned(game.Profile,101)&&game.Profile.workshopSpent==0);
            }
            finally{RiftStorage.OverridePath=originalPath;}
            WorkshopCheck("failed-purchase-write-preserves-disk-save",File.ReadAllText(RiftStorage.Path)==originalDisk);
            var rapidBuy=GameObject.Find("Workshop buy 101").GetComponent<Button>();rapidBuy.onClick.Invoke();int paid=game.Profile.shards;rapidBuy.onClick.Invoke();yield return null;
            bool bought=game.Profile.shards==workshopEarned-60&&RiftWorkshop.Owned(game.Profile,101)&&game.Profile.equippedTraits[0]==2;
            WorkshopCheck("earned-shards-buy-and-equip-a-real-trait",bought);
            WorkshopCheck("repeat-purchase-cannot-spend-again",game.Profile.shards==paid&&game.Profile.workshopSpent==60);
            Click("Workshop reset");yield return null;bool reset=game.Profile.equippedTraits[0]==0&&RiftWorkshop.Owned(game.Profile,101);Click("Workshop buy 101");yield return null;
            WorkshopCheck("owned-traits-switch-and-reset-without-cost",reset&&game.Profile.equippedTraits[0]==2&&game.Profile.shards==paid);
            yield return Capture("30-trait-unlocked");
            Click("Workshop tab 1");yield return null;CheckVisibleText();yield return Capture("31-amulet-choices");Click("Workshop buy 200");yield return null;
            WorkshopCheck("amulet-purchase-uses-earned-currency",game.Profile.equippedAmulet==1&&game.Profile.shards==workshopEarned-140);
            Click("Workshop tab 2");yield return null;int before=game.Profile.shards;Click("Preview item 300");yield return null;
            WorkshopCheck("cosmetic-preview-is-free-without-unlocking",game.Profile.shards==before&&!RiftWorkshop.Owned(game.Profile,300));
            CheckVisibleText();yield return Capture("32-cosmetic-preview");Click("Workshop buy 300");yield return null;Click("Workshop buy 400");yield return null;
            var saved=RiftStorage.Load();workshopSpent=game.Profile.workshopSpent;
            WorkshopCheck("style-frame-and-loadout-survive-disk-saving",saved.equippedStyles[0]==1&&saved.equippedFrame==1&&saved.equippedTraits[0]==2&&saved.equippedAmulet==1&&saved.workshopSpent==230&&saved.shards==workshopEarned-230);
            WorkshopCheck("shop-control-bounds-and-readable-text",ControlsInsideScreen(new[]{"Close workshop","Workshop tab 0","Workshop tab 2","Workshop buy 300","Workshop buy 400","Workshop wish 401","Workshop reset"}));
            yield return Capture("33-cosmetics-equipped");Click("Close workshop");yield return null;Click("Continue");yield return null;
            WorkshopCheck("saved-expedition-keeps-its-original-equipment",game.Battle.EquippedAmulet==0&&game.Battle.TraitChoice(0)==0&&game.Battle.StyleChoice(0)==0&&game.Battle.EquippedFrame==0);
            game.ReturnHome();yield return null;Click("Start Shinobi");yield return null;Click("Deploy sakura");yield return null;
            bool star=false;foreach(var effect in FindObjectsByType<RiftEffectGraphic>(FindObjectsSortMode.None))if(effect.gameObject.activeInHierarchy&&effect.Variant==7)star=true;
            var frame=GameObject.Find("Deploy sakura").transform.Find("Equipped portrait frame");
            WorkshopCheck("new-expedition-applies-trait-amulet-and-cosmetics",game.Battle.TraitChoice(0)==2&&game.Battle.EquippedAmulet==1&&game.Battle.StyleChoice(0)==1&&game.Battle.EquippedFrame==1&&frame&&star);
            yield return Capture("34-equipped-expedition");
            game.ResetExperienceObservation();float began=Time.realtimeSinceStartup,next=0;bool combatShot=false;
            while(Time.realtimeSinceStartup-began<12)
            {
                float elapsed=Time.realtimeSinceStartup-began;
                if(elapsed>=next){next=elapsed+.8f;string action=RiftExperiencePolicy.Decide(game.Battle);if(action!=null)Click(action);}
                var sample=game.ExperienceSample(elapsed,Time.unscaledDeltaTime*1000);workshopEffectPeak=Math.Max(workshopEffectPeak,sample.effects);
                if(elapsed>10&&!combatShot){combatShot=true;yield return Capture("35-equipped-live-combat");}
                yield return null;
            }
            workshopAttackSamples=game.ExperienceAnimationHits;workshopPoseMismatches=game.ExperienceAnimationMismatches;
            WorkshopCheck("purchased-loadout-plays-with-synchronized-live-impacts",workshopAttackSamples>=5&&workshopPoseMismatches==0&&workshopEffectPeak<=RiftFeedback.MaxEffects);
            game.ReturnHome();yield return null;
            WorkshopCheck("post-purchase-goal-shows-real-remaining-currency",game.Profile.shards==workshopEarned-230&&game.Profile.workshopGoal==100&&RiftProgress.NextGoal(game.Profile).Contains("6 / 60")&&game.Profile.history.Length==2);
        }
    }
}
