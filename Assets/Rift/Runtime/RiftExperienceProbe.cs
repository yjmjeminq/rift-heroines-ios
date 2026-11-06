using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    // Opt-in QA: normal game Update, public button events and isolated saves only.
    public sealed class RiftExperienceProbe:MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {public float realTime,simulationTime,frameMs,hitstop,overlayArea;public int effects,largeEffects,numbers,allies,enemies;}
        [Serializable] public sealed class Report
        {public string version;public bool passed;public string[] errors;public int screenWidth,screenHeight,inputs,hitEvents,frames,animationHits,animationMismatches,audioHits;public float firstHit,realSeconds,simulationSeconds;public List<Sample> samples=new List<Sample>();}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();
        RiftGame game;string output;float began,nextInput,nextSample;int captures;
        IEnumerator Start()
        {
            game=GetComponent<RiftGame>();output=RiftGame.Argument("-riftSmokePath")??"BuildLogs/Experience";Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;report.version=RiftData.Version;report.firstHit=-1;began=Time.realtimeSinceStartup;
            game.BeginRun(0,1237);yield return null;
            yield return Capture("opening.png");
            while(Time.realtimeSinceStartup-began<34)
            {
                float elapsed=Time.realtimeSinceStartup-began;
                if(elapsed>=17&&game.Speed<2)Click("Speed");
                if(game.Battle.Phase==BattlePhase.Reward){Click("Reward "+RiftAutoPlay.RewardIndex(game.Battle,3));yield return null;}
                if(game.Battle.Phase!=BattlePhase.Battle){errors.Add("Normal-speed sample ended before its observation window");break;}
                if(elapsed>=nextInput)
                {
                    nextInput=elapsed+.8f;
                    string action=RiftExperiencePolicy.Decide(game.Battle);
                    if(action!=null)Click(action);
                }
                report.frames++;
                if(elapsed>=nextSample)
                {
                    nextSample=elapsed+.1f;report.samples.Add(game.ExperienceSample(elapsed,Time.unscaledDeltaTime*1000));
                }
                if((elapsed>=13&&elapsed<16.3f)||(elapsed>=29&&elapsed<32.3f))
                {
                    // Independent offscreen captures preserve live frame timing in the report.
                    if(elapsed>=captures*.1f+13&&elapsed<16.3f){yield return Capture("one-"+captures.ToString("D3")+".png");captures++;}
                    else if(elapsed>=29&&elapsed>=nextCapture){nextCapture=elapsed+.1f;yield return Capture("two-"+(captures++).ToString("D3")+".png");}
                }
                if(elapsed>12&&!midCaptured){midCaptured=true;yield return Capture("combat-1x.png");}
                if(elapsed>28&&!lateCaptured){lateCaptured=true;yield return Capture("combat-2x.png");}
                yield return null;
            }
            report.realSeconds=Time.realtimeSinceStartup-began;report.simulationSeconds=game.Battle.TotalTime;report.screenWidth=Screen.width;report.screenHeight=Screen.height;
            report.hitEvents=game.ExperienceHitEvents;report.firstHit=game.ExperienceFirstHit;
            report.animationHits=game.ExperienceAnimationHits;report.animationMismatches=game.ExperienceAnimationMismatches;report.audioHits=GetComponent<RiftAudio>().PlayedHits;
            if(report.hitEvents==0)errors.Add("No actual hits observed");
            if(report.animationHits<20||report.animationMismatches>0)errors.Add("Damage and attack poses are not synchronized: "+report.animationMismatches+" / "+report.animationHits);
            if(report.audioHits<20)errors.Add("Actual audio playback path was not exercised");
            report.errors=errors.ToArray();report.passed=errors.Count==0;
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=OnLog;Application.Quit(report.passed?0:1);
        }
        float nextCapture=29;bool midCaptured,lateCaptured;
        void Click(string name)
        {var go=GameObject.Find(name);var b=go?go.GetComponent<Button>():null;if(!b||!b.interactable){errors.Add("Unavailable control: "+name);return;}b.onClick.Invoke();report.inputs++;}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var camera=FindFirstObjectByType<Camera>();var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24);var old=camera.targetTexture;var active=RenderTexture.active;
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;var shot=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);shot.Apply();File.WriteAllBytes(Path.Combine(output,name),shot.EncodeToPNG());Destroy(shot);}
            finally {camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
        }
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    }
    public static class RiftExperiencePolicy
    {
        public static string Decide(RiftBattle b,bool tactics=true)
        {
            if(tactics&&b.WarningTime>0&&b.WarningTime<=.65f&&b.ShieldCooldown<=0&&b.Mana>=30)return "Ward";
            if(tactics&&b.Fury>=100&&(b.Enemies>=3||b.WarningTime>0))return "Ultimate";
            if(tactics&&b.Allies>=3&&b.Enemies>0&&b.OrderCooldown<=0)return "Order";
            if(b.Allies>=5&&b.Economy<3&&b.Mana>=b.EconomyCost)return "Economy";
            int[] roles=new int[3],heroes=new int[12];foreach(var f in b.Fighters)if(f.ally&&f.Alive){roles[(int)RiftData.Heroes[f.hero].role]++;heroes[f.hero]++;}
            int role=roles[0]<2?0:roles[2]<2?2:roles[1]<2?1:(int)(b.StageTime*2)%3;
            int chosen=-1;float best=-10000;
            for(int i=0;i<12;i++)if(b.Ranks[i]>0&&(int)RiftData.Heroes[i].role==role&&b.SummonCooldown[i]<=0&&b.Mana>=b.SummonCost(i)&&b.Allies<18)
            {float score=100f/(heroes[i]+1)-b.SummonCost(i)*.1f;foreach(var bond in RiftData.Bonds)if((bond.a==i&&heroes[bond.b]>0)||(bond.b==i&&heroes[bond.a]>0))if(heroes[i]==0)score+=40;if(score>best){best=score;chosen=i;}}
            if(chosen>=0)return "Deploy "+RiftData.Heroes[chosen].id;
            if(b.Allies>=8&&b.Economy>=2)for(int i=0;i<12;i++)if(heroes[i]>=2&&b.Ranks[i]>0&&b.Ranks[i]<3&&b.Mana>=b.AwakenCost(i))return "Awaken "+RiftData.Heroes[i].id;
            return null;
        }
        public static bool Apply(RiftBattle b,string action)
        {
            if(action=="Ward")return b.Shield();if(action=="Ultimate")return b.Ultimate();if(action=="Order")return b.Order();if(action=="Economy")return b.UpgradeEconomy();
            for(int i=0;i<12;i++){if(action=="Deploy "+RiftData.Heroes[i].id)return b.Summon(i);if(action=="Awaken "+RiftData.Heroes[i].id)return b.Awaken(i);}return false;
        }
    }
    public sealed partial class RiftGame
    {
        public int ExperienceHitEvents {get;private set;}
        public float ExperienceFirstHit {get;private set;}=-1;
        public int ExperienceAnimationHits {get;private set;}
        public int ExperienceAnimationMismatches {get;private set;}
        public void ResetExperienceObservation(){ExperienceHitEvents=ExperienceAnimationHits=ExperienceAnimationMismatches=0;ExperienceFirstHit=-1;}
        public RiftExperienceProbe.Sample ExperienceSample(float elapsed,float frameMs)
        {
            var sample=new RiftExperienceProbe.Sample{realTime=elapsed,simulationTime=Battle.TotalTime,frameMs=frameMs,hitstop=hitstop,effects=effects.Count,numbers=numbers.Count,allies=Battle.Allies,enemies=Battle.Enemies};
            foreach(var e in effects){var size=e.root.sizeDelta;if(size.x>300&&size.y>300)sample.largeEffects++;sample.overlayArea+=size.x*size.y*e.graphic.color.a*Mathf.Max(0,1-e.age/e.duration)/(1920*560f);}
            return sample;
        }
        void ObserveExperienceCue(BattleCue cue)
        {
            if(cue.kind!=Cue.Hit||cue.amount<=0)return;
            ExperienceHitEvents++;if(ExperienceFirstHit<0)ExperienceFirstHit=Battle.TotalTime;
            if(cue.source>0){var fighter=Battle.Fighters.Find(f=>f.id==cue.source);if(fighter!=null){ExperienceAnimationHits++;if(fighter.animation<.10f||fighter.animation>.36f)ExperienceAnimationMismatches++;}}
        }
    }
}
