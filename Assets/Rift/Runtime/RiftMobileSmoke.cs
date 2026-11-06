using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiftHeroines
{
    // Opt-in hidden-player checks. Pointer events are sent inside Unity; no desktop input is used.
    public sealed class RiftMobileSmoke:MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed;public int width,height;public string version;public string[] checks,errors;public bool physicalIPhoneTested=false;}
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        RiftGame game;string output;
        void Fail(string message){errors.Add(message);Debug.LogWarning("MOBILE_QA: "+message);}
        void Check(string name,bool value){if(value)checks.Add(name);else Fail(name);}
        void Log(string message,string trace,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors.Add(message);}
        void Tap(string name)
        {
            Canvas.ForceUpdateCanvases();
            var target=GameObject.Find(name);if(!target){Fail("Missing target: "+name);return;}
            var button=target.GetComponent<Button>();if(!button||!button.interactable){Fail("Disabled target: "+name);return;}
            var camera=FindFirstObjectByType<Camera>();var rect=(RectTransform)target.transform;
            // A hidden batch player has no presented backbuffer. Populate UGUI graphic depth
            // for freshly built sheets with the same offscreen render used for screenshots.
            var render=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var previous=camera.targetTexture;
            try{camera.targetTexture=render;Canvas.ForceUpdateCanvases();camera.Render();}
            finally{camera.targetTexture=previous;RenderTexture.ReleaseTemporary(render);}
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=target){Fail("Occluded touch target: "+name+" by "+(hits.Count>0?hits[0].gameObject.name:"nothing"));return;}
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
        }
        void Bounds()
        {
            var camera=FindFirstObjectByType<Camera>();var corners=new Vector3[4];game.Content.GetWorldCorners(corners);
            Rect safe=game.Content.GetComponent<RiftSafeArea>().AppliedArea;bool inside=true;
            foreach(var p in corners){Vector2 v=RectTransformUtility.WorldToScreenPoint(camera,p);inside&=v.x>=safe.xMin-2&&v.x<=safe.xMax+2&&v.y>=safe.yMin-2&&v.y<=safe.yMax+2;}
            Check("complete-game-inside-safe-area",inside);
            foreach(var label in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if(label.name.StartsWith("Touch ",StringComparison.Ordinal)&&label.preferredHeight>label.rectTransform.rect.height+2)errors.Add("Clipped mobile text: "+label.name);
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();Bounds();
            var camera=FindFirstObjectByType<Camera>();var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var old=camera.targetTexture;var active=RenderTexture.active;Texture2D shot=null;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;shot=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);shot.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),shot.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(shot)Destroy(shot);}
        }
        IEnumerator Start()
        {
            game=GetComponent<RiftGame>();output=RiftGame.Argument("-riftSmokePath");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            yield return null;yield return Shot("01-phone-home");Tap("Start Shinobi");yield return null;
            Tap("Deploy sakura");yield return null;Check("tap-deploys-hero",game.Battle.Allies>0);
            yield return Shot("02-phone-battle");Tap("Touch hero details");yield return null;
            float time=game.Battle.StageTime;yield return new WaitForSecondsRealtime(.3f);
            Check("hero-sheet-pauses-combat",game.MobileSheetOpen&&game.Paused&&game.Battle.StageTime==time);
            Tap("Touch hero 1");yield return null;yield return Shot("03-phone-hero");
            // Wait for real mana regeneration if an upgrade is not yet affordable.
            Tap("Touch resume");yield return null;
            while(game.Battle.Mana<game.Battle.AwakenCost(1))yield return null;
            Tap("Touch hero details");yield return null;int rank=game.Battle.Ranks[1];Tap("Touch awaken");yield return null;
            Check("large-awaken-action-upgrades-hero",game.Battle.Ranks[1]==rank+1);
            Tap("Touch resume");yield return null;Check("closing-hero-sheet-resumes",!game.Paused&&!game.MobileSheetOpen);
            Tap("Touch pause");yield return null;Check("header-opens-pause-menu",game.Paused);yield return Shot("04-phone-pause");
            Tap("Touch speed");yield return null;Check("touch-speed-control",game.Speed==2);Tap("Home");yield return null;
            Tap("Workshop");yield return null;Tap("Touch choose heroine");yield return null;yield return Shot("05-phone-picker");
            Tap("Touch workshop hero 11");yield return null;Check("large-picker-selects-last-hero",GameObject.Find("Workshop item 122"));
            Tap("Workshop item 122");yield return null;Check("whole-item-card-opens-sheet",game.MobileSheetOpen);yield return Shot("06-phone-item");
            Tap("Touch workshop wish");yield return null;Check("touch-goal-is-saved",game.Profile.workshopGoal==122&&RiftStorage.Load().workshopGoal==122);
            // Explicit isolated fixture only: test purchase, loadout persistence and insufficient funds.
            game.Profile.shards=60;RiftStorage.Save(game.Profile);Tap("Workshop item 122");yield return null;Tap("Touch workshop buy");yield return null;
            Check("touch-purchase-spends-and-saves",game.Profile.shards==0&&game.Profile.equippedTraits[11]==1&&RiftStorage.Load().equippedTraits[11]==1);
            Tap("Workshop item 123");yield return null;var buy=GameObject.Find("Touch workshop buy");Check("unaffordable-purchase-is-disabled",buy&&!buy.GetComponent<Button>().interactable);
            Tap("Touch close");yield return null;Tap("Close workshop");yield return null;Tap("Start Reapers");yield return null;
            Check("new-run-uses-purchased-trait",game.Battle.TraitChoice(11)==1);Tap("Deploy rangiku");yield return null;Check("second-faction-touch-deploys",game.Battle.Allies>0);
            yield return Shot("07-phone-reapers");
            Application.logMessageReceived-=Log;
            File.WriteAllText(Path.Combine(output,"mobile-report.json"),JsonUtility.ToJson(new Report{passed=errors.Count==0,width=Screen.width,height=Screen.height,version=RiftData.Version,checks=checks.ToArray(),errors=errors.ToArray()},true));
            Application.Quit(errors.Count==0?0:1);
        }
    }
}
