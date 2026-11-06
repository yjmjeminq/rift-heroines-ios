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
        const int RequiredFeedbackChecks=5;
        readonly List<string> feedbackChecks=new List<string>();
        void FeedbackCheck(string name,bool passed){if(passed)feedbackChecks.Add(name);else Error("Feedback: "+name);}
        IEnumerator VerifyFeedbackUI()
        {
            bool descriptions=true;
            for(int faction=0;faction<2;faction++)
            {
                if(faction!=game.Battle.Faction){Click("Switch roster");yield return null;}
                for(int slot=0;slot<6;slot++)
                {
                    var card=GameObject.Find("Deploy "+RiftData.Heroes[faction*6+slot].id);
                    ExecuteEvents.Execute(card,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return null;
                    var label=GameObject.Find("Selected description").GetComponent<Text>();
                    descriptions&=label.preferredHeight<=label.rectTransform.rect.height+2;
                }
            }
            Click("Switch roster");yield return null;
            FeedbackCheck("twelve-hero-details-fit-backed-strip",descriptions&&GameObject.Find("Character detail backing"));
            Click("Deploy sakura");yield return null;
            var fill=GameObject.Find("Deploy sakura").transform.Find("Deploy readiness/Fill").GetComponent<Image>();
            FeedbackCheck("summon-cooldown-has-progress-indicator",fill.fillAmount<.5f&&game.Battle.SummonCooldown[0]>0);
            yield return Capture("26-readable-deck");
            game.Toast("important cue",3,3);game.Toast("ordinary cue",3,0);
            FeedbackCheck("critical-toast-survives-ordinary-notices",GameObject.Find("Toast").GetComponent<Text>().text=="important cue");
            game.ReturnHome();yield return null;Click("Start Shinobi");yield return null;
            FeedbackCheck("active-controls-stay-on-screen",ControlsInsideScreen(new[]{"Deploy sakura","Deploy konan","Awaken sakura","Awaken konan","Order","Ward","Ultimate","Economy","Build overview","Pause","Switch roster"}));
            // Practice provides a naturally reached, retryable warning with a real early E cooldown.
            game.ReturnHome();yield return null;Click("Training room");yield return null;Click("Deploy sakura");Click("Deploy tsunade");Click("Order");
            for(int i=0;i<100&&game.Battle.WarningTime<=0;i++){game.Battle.Tick(.04f);yield return null;}
            Click("Ward");
            for(int i=0;i<100&&game.Battle.WarningTime>.55f;i++){game.Battle.Tick(.025f);yield return null;}
            yield return null;
            var text=GameObject.Find("Threat").GetComponent<Text>().text;
            bool noFalsePrompt=game.Battle.WarningTime>0&&game.Battle.ShieldCooldown>0&&!text.Contains("지금 E")&&text.Contains("재사용");
            FeedbackCheck("unavailable-counter-never-shows-press-E",noFalsePrompt);
            yield return Capture("27-counter-unavailable");
            game.ReturnHome();yield return null;Click("Start Shinobi");yield return null;
        }
    }
}
