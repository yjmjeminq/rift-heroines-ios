using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    // Deterministic legal-action driver used by build validation and opt-in player smoke only.
    public static class RiftAutoPlay
    {
        public static void Drive(RiftBattle b,bool tactics=true)
        {
            if(b.Phase!=BattlePhase.Battle)return;
            if(tactics&&b.WarningTime>0 && b.WarningTime<=.6f)b.Shield();
            if(tactics&&b.Fury>=100 && (b.Enemies>=3 || b.WarningTime>0) && (b.WarningTime<=0 || b.ShieldCooldown>0 || b.Mana<30))b.Ultimate();
            if(tactics&&b.Allies>=3 && b.Enemies>0 && b.OrderCooldown==0)b.Order();
            if(b.Allies>=5 && b.Economy<3 && b.Mana>=b.EconomyCost)b.UpgradeEconomy();
            int[] counts=new int[3],heroes=new int[RiftData.HeroCount];
            foreach(var f in b.Fighters)if(f.ally&&f.Alive){counts[(int)RiftData.Heroes[f.hero].role]++;heroes[f.hero]++;}
            CombatRole priority=counts[0]<2?CombatRole.Vanguard:counts[2]<2?CombatRole.Mystic:counts[1]<2?CombatRole.Striker:(CombatRole)(((int)(b.StageTime*2))%3);
            int chosen=-1;float best=float.MinValue;
            for(int i=0;i<RiftData.HeroCount;i++)
            {
                if(b.Ranks[i]<=0 || RiftData.Heroes[i].role!=priority || b.SummonCooldown[i]>0)continue;
                float score=100f/(heroes[i]+1)-b.SummonCost(i)*.1f;
                foreach(var bond in RiftData.Bonds)if((bond.a==i&&heroes[bond.b]>0)||(bond.b==i&&heroes[bond.a]>0))if(heroes[i]==0)score+=40;
                if(score>best){best=score;chosen=i;}
            }
            if(chosen>=0&&b.Mana>=b.SummonCost(chosen))b.Summon(chosen);
            if(b.Allies>=8 && b.Economy>=2)
            {
                chosen=-1;best=0;
                for(int i=0;i<RiftData.HeroCount;i++)if(b.Ranks[i]>0&&b.Ranks[i]<3&&b.Mana>=b.AwakenCost(i)&&heroes[i]>best){best=heroes[i];chosen=i;}
                if(chosen>=0)b.Awaken(chosen);
            }
        }
        public static int RewardIndex(RiftBattle b,int strategy=0)
        {
            int kind=strategy==0?1:strategy==1?0:strategy==2?2:b.Stage%3;
            for(int i=0;i<3;i++){int choice=b.Choices[i];if((choice>=200?2:choice>=100?1:0)==kind)return i;}
            return 0;
        }
    }

    public sealed partial class RiftSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public string[] errors,screenshots;
            public int finalStage,kills,units;
            public string phase;
            public int campaignFinalStage,verifiedFactions;
            public int perfectGuards,shrineCaptures,freeAwakens;
            public int screenWidth,screenHeight,completedCampaigns;
            public bool conveniencePassed;
            public bool contentPassed;
            public bool feedbackPassed;
            public bool workshopPassed;
            public string[] workshopChecks;
            public int workshopEarned,workshopSpent;
            public int workshopAttackSamples,workshopPoseMismatches,workshopEffectPeak;
            public string[] feedbackChecks;
            public string campaignFinalPhase;
            public string version;
            public string[] roster,observedHeroIds,observedBossIds,observedBonds,pickedRewardKinds,convenienceChecks;
            public string[] contentChecks,observedArenaIds,observedTechniqueNames;
        }
        readonly List<string> errors=new List<string>(),screenshots=new List<string>();
        string output;
        int campaignStage;
        string campaignPhase;
        int guards,captures,awakenings;
        readonly HashSet<string> observedHeroes=new HashSet<string>(),observedBosses=new HashSet<string>();
        readonly HashSet<string> observedBonds=new HashSet<string>(),pickedKinds=new HashSet<string>();
        void Update()
        {
            if(!game || game.Battle==null||game.Battle.Practice)return;
            observedArenas.Add(game.CurrentArenaId);
            if(game.Battle.WarningTime>0)observedTechniques.Add(RiftData.BossTechniqueNames[(int)game.Battle.ActiveBossTechnique]);
            guards=Math.Max(guards,game.Battle.PerfectGuards);captures=Math.Max(captures,game.Battle.ShrineCaptures);awakenings=Math.Max(awakenings,game.Battle.FreeAwakens);
            for(int i=0;i<RiftData.Bonds.Length;i++)if(game.Battle.ActiveBonds[i])observedBonds.Add(RiftData.Bonds[i].name);
            foreach(var fighter in game.Battle.Fighters)
            {
                string id=RiftData.Heroes[fighter.hero].id;observedHeroes.Add(id);
                if(fighter.boss)observedBosses.Add(id);
            }
        }
        RiftGame game;
        void Error(string condition){errors.Add(condition);}
        IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            // Hidden batch players do not present a backbuffer. Render the game's camera
            // into an offscreen target so validation never needs to move a desktop window.
            var camera=FindFirstObjectByType<Camera>();
            int width=Screen.width>0?Screen.width:1600,height=Screen.height>0?Screen.height:900;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            Texture2D shot;
            try
            {
                camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                shot=new Texture2D(width,height,TextureFormat.RGB24,false);
                shot.ReadPixels(new Rect(0,0,width,height),0,0);shot.Apply();
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(target);
            }
            if(!shot){Error("Could not capture "+name);yield break;}
            var pixels=shot.GetPixels32();int lit=0;
            for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r+pixels[i].g+pixels[i].b>65)lit++;
            if(lit<100)Error("Screenshot has no rendered game content: "+name);
            File.WriteAllBytes(Path.Combine(output,name+".png"),shot.EncodeToPNG());Destroy(shot);screenshots.Add(name+".png");
            yield return null;
        }
        IEnumerator Start()
        {
            output=RiftGame.Argument("-riftSmokePath")??"BuildLogs/PlayerSmoke";Directory.CreateDirectory(output);
            game=GetComponent<RiftGame>();Application.logMessageReceived+=OnLog;
            yield return null;yield return Capture("01-title");
            Click("Tactics guide");yield return null;yield return Capture("10-tactics-guide");Click("Close guide");yield return null;
            Click("Roster book");yield return null;
            for(int i=0;i<RiftData.HeroCount;i++)if(!GameObject.Find("Roster "+RiftData.Heroes[i].id))Error("Roster book is missing "+RiftData.Heroes[i].id);
            yield return Capture("00-twelve-roster");Click("Close roster");yield return null;
            yield return VerifyTrainingRoom();
            Click("Start Shinobi");yield return null;
            if(game.Battle==null){Error("Shinobi start button did not begin a run");Finish();yield break;}
            yield return VerifyConvenienceBeforeCampaign();
            yield return VerifyFeedbackUI();
            VerifyCards(0);Click("Switch roster");yield return null;VerifyCards(1);
            yield return Capture("09-recruit-deck");Click("Switch roster");yield return null;VerifyCards(0);
            Click("Deploy tsunade");Click("Deploy sakura");
            bool capturedShrine=false,capturedCounter=false,capturedCounterWindow=false;
            for(int i=0;i<170 && game.Battle.Phase==BattlePhase.Battle;i++)
            {
                RiftAutoPlay.Drive(game.Battle);game.Battle.Tick(.16f);yield return null;
                if(!capturedShrine&&game.Battle.ShrineBuffTime>0){yield return Capture("11-shrine-control");capturedShrine=true;}
            }
            yield return Capture("02-shinobi-combat");
            if(game.Battle.Phase==BattlePhase.Battle)
            {
                Click("Pause");yield return null;float mana=game.Battle.Mana,time=game.Battle.StageTime;
                float progress=game.Battle.ShrineProgress,buff=game.Battle.ShrineBuffTime;
                for(int i=0;i<8;i++)yield return null;
                if(!game.Paused || Math.Abs(game.Battle.Mana-mana)>.001f || Math.Abs(game.Battle.StageTime-time)>.001f)Error("Pause advanced combat resources or clock");
                if(game.Battle.ShrineProgress!=progress||game.Battle.ShrineBuffTime!=buff)Error("Pause advanced the shrine objective/buff");
                yield return Capture("03-paused");Click("Resume");yield return null;
            }
            int guard=0;bool capturedReward=false,capturedBoss=false;
            while(game.Battle!=null && game.Battle.Phase!=BattlePhase.Victory && game.Battle.Phase!=BattlePhase.Defeat && guard++<4000)
            {
                if(game.Battle.Phase==BattlePhase.Reward)
                {
                    yield return null;
                    if(!capturedReward){yield return VerifyPendingReward();yield return Capture("04-reward");capturedReward=true;}
                    int index=RiftAutoPlay.RewardIndex(game.Battle,game.Battle.Stage==1?2:game.Battle.Stage==2?0:game.Battle.Stage==3?1:3);
                    int choice=game.Battle.Choices[index],oldRank=choice>=200?game.Battle.Ranks[choice-200]:0;
                    pickedKinds.Add(choice>=200?"awakening":choice>=100?"recruit":"relic");
                    Click("Reward "+index);yield return null;
                    if(game.Battle.Stage==3)yield return Capture("24-stormshrine");
                    if(game.Battle.Stage==5)yield return Capture("25-eclipsecitadel");
                    if(choice>=200&&game.Battle.Ranks[choice-200]!=oldRank+1)Error("Free awakening UI did not apply the new rank");
                    if(choice<RiftData.Relics.Length&&!ownedInventoryChecked)yield return VerifyOwnedInventory(choice);
                }
                else
                {
                    if(game.Battle.WarningTime>0&&game.Battle.WarningTime<=.65f&&game.Battle.ShieldCooldown<=0&&game.Battle.Mana>=30)
                    {
                        int prior=game.Battle.PerfectGuards;
                        if(!capturedCounterWindow)
                        {
                            // Hold the real, already-reached state while its offscreen image renders.
                            game.enabled=false;
                            try {game.CancelDeploymentQueue();yield return Capture("13-counter-window");Click("Ward");}
                            finally {game.enabled=true;}
                            capturedCounterWindow=true;
                        }
                        else Click("Ward");
                        yield return null;
                        if(game.Battle.PerfectGuards!=prior+1)Error("The shield button did not trigger a last-moment counter");
                        if(!capturedCounter){yield return Capture("12-perfect-guard");capturedCounter=true;}
                    }
                    // Preserve mana for the first UI counter, as the normal reservation system does.
                    if(capturedCounterWindow||game.Battle.WarningTime<=0||game.Battle.ShieldCooldown>0)RiftAutoPlay.Drive(game.Battle);
                    game.Battle.Tick(game.Battle.WarningTime>0?.10f:.65f);
                    if(!capturedShrine&&game.Battle.ShrineBuffTime>0){yield return null;yield return Capture("11-shrine-control");capturedShrine=true;}
                    if(game.Battle.BossSpawned&&!capturedBoss){yield return null;yield return Capture("05-boss");capturedBoss=true;}
                }
                yield return null;
            }
            yield return null;yield return Capture("06-result");
            campaignStage=game.Battle!=null?game.Battle.Stage:0;campaignPhase=game.Battle!=null?game.Battle.Phase.ToString():"none";
            if(game.Battle==null || game.Battle.Phase!=BattlePhase.Victory)Error("Legal-action player did not complete the six-stage expedition");
            if(game.Battle!=null&&game.Battle.Phase==BattlePhase.Victory)completedCampaigns++;
            yield return VerifyResultWorkshop();
            yield return VerifyRecordsAfterVictory();
            Click("Start Reapers");yield return null;
            VerifyCards(1);
            Click("Deploy rangiku");Click("Deploy yoruichi");
            for(int i=0;i<130 && game.Battle.Phase==BattlePhase.Battle;i++){RiftAutoPlay.Drive(game.Battle);game.Battle.Tick(.16f);yield return null;}
            yield return Capture("07-reaper-combat");
            var checkpoint=RiftStorage.Load().checkpoint;
            game.ReturnHome();yield return null;Click("Continue");yield return null;
            if(game.Battle==null || game.Battle.Faction!=1 || checkpoint==null || game.Battle.Stage!=checkpoint.stage)Error("Checkpoint continue failed");
            yield return Capture("08-continued");
            yield return CompleteSecondFaction();yield return VerifyWorkshopEconomy();Finish();
        }
        void VerifyCards(int faction)
        {
            for(int slot=0;slot<RiftData.HeroesPerFaction;slot++)
            {
                int hero=RiftData.FactionStart(faction)+slot;var d=RiftData.Heroes[hero];
                var go=GameObject.Find("Deploy "+d.id);var button=go?go.GetComponent<Button>():null;
                if(game.CardHero(slot)!=hero || !button || button.interactable!=(game.Battle.Ranks[hero]>0))Error("Wrong faction card/availability: "+d.id);
            }
        }
        void Click(string name)
        {
            var go=GameObject.Find(name);var button=go?go.GetComponent<Button>():null;
            if(!button || !button.interactable){Error("Missing/noninteractive UI button: "+name);return;}
            button.onClick.Invoke();
        }
        void OnLog(string message,string stack,LogType kind)
        {if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert)errors.Add(message);}
        void Finish()
        {
            Application.logMessageReceived-=OnLog;
            var b=game.Battle;
            if(observedHeroes.Count!=RiftData.HeroCount)Error("The player test did not encounter all twelve canonical characters");
            if(guards==0||captures==0||observedBonds.Count<3)Error("The player test did not exercise counters, shrine capture and live bonds");
            if(awakenings==0||pickedKinds.Count!=3)Error("The player test did not select all three reward types");
            var seen=new string[observedHeroes.Count];observedHeroes.CopyTo(seen);Array.Sort(seen);
            var bosses=new string[observedBosses.Count];observedBosses.CopyTo(bosses);Array.Sort(bosses);
            var bonds=new string[observedBonds.Count];observedBonds.CopyTo(bonds);Array.Sort(bonds);var kinds=new string[pickedKinds.Count];pickedKinds.CopyTo(kinds);Array.Sort(kinds);
            bool convenience=convenienceChecks.Count==RequiredConvenienceChecks;if(!convenience)Error("Not all convenience scenarios completed");
            bool content=contentChecks.Count==RequiredContentChecks&&observedArenas.Count==3&&observedTechniques.Count==3&&observedBosses.Count>=4;if(!content)Error("Not all new content scenarios completed");
            bool feedback=feedbackChecks.Count==RequiredFeedbackChecks;if(!feedback)Error("Feedback UI scenarios incomplete");
            var report=new Report {passed=errors.Count==0,errors=errors.ToArray(),screenshots=screenshots.ToArray(),finalStage=b!=null?b.Stage:0,kills=b!=null?b.Kills:0,units=b!=null?b.Allies:0,phase=b!=null?b.Phase.ToString():"home",campaignFinalStage=campaignStage,campaignFinalPhase=campaignPhase,verifiedFactions=2,version=RiftData.Version,roster=Array.ConvertAll(RiftData.Heroes,h=>h.name),observedHeroIds=seen,observedBossIds=bosses,perfectGuards=guards,shrineCaptures=captures,freeAwakens=awakenings,observedBonds=bonds,pickedRewardKinds=kinds,conveniencePassed=convenience,convenienceChecks=convenienceChecks.ToArray(),screenWidth=Screen.width,screenHeight=Screen.height,completedCampaigns=completedCampaigns};
            report.contentPassed=content;report.contentChecks=contentChecks.ToArray();report.observedArenaIds=Sorted(observedArenas);report.observedTechniqueNames=Sorted(observedTechniques);
            report.feedbackPassed=feedback;report.feedbackChecks=feedbackChecks.ToArray();report.passed=errors.Count==0;
            bool workshop=workshopChecks.Count==RequiredWorkshopChecks;if(!workshop)Error("Workshop scenario coverage incomplete: "+workshopChecks.Count);
            report.workshopPassed=workshop;report.workshopChecks=workshopChecks.ToArray();report.workshopEarned=workshopEarned;report.workshopSpent=workshopSpent;report.passed=errors.Count==0;report.errors=errors.ToArray();
            report.workshopAttackSamples=workshopAttackSamples;report.workshopPoseMismatches=workshopPoseMismatches;report.workshopEffectPeak=workshopEffectPeak;
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
        }
    }
}
