using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        readonly RiftDeployment deployment=new RiftDeployment();
        readonly RiftProgressTracker progressTracker=new RiftProgressTracker();
        bool progressDirty,homeOverlayOpen,overviewOpen,overviewWasPaused;
        float saveClock;
        Button queueButton;
        Text queueText;
        public int QueuedHero {get{return deployment.QueuedHero;}}
        public bool OverviewOpen {get{return overviewOpen;}}
        void SyncProgress(){if(Profile!=null&&Battle!=null&&!Battle.Practice&&progressTracker.Observe(Profile,Battle))progressDirty=true;}
        void SaveProfile(){if(Profile==null)return;if(RiftStorage.Save(Profile)){progressDirty=false;saveClock=0;}else Toast("진행 기록을 저장하지 못했습니다. 잠시 후 다시 저장합니다",3);}
        void SaveCheckpoint(){if(Battle.Practice)return;SyncProgress();Profile.checkpoint=Battle.CaptureCheckpoint();SaveProfile();}
        void RecordResult()
        {if(resultRecorded||Battle==null||Battle.Practice)return;SyncProgress();RiftProgress.Settle(Profile,Battle);resultRecorded=true;Profile.checkpoint=null;SaveProfile();}
        public void BeginDeploymentHold(int hero,bool pointer=false)
        {if(Battle==null||Paused||Battle.Phase!=BattlePhase.Battle||hero<0||hero>=RiftData.HeroCount||Battle.Ranks[hero]==0)return;selectedHero=hero;deployment.BeginHold(hero,pointer);}
        public void EndDeploymentHold(int hero){if(deployment.HeldHero==hero)deployment.EndHold();}
        void DeployFromCard(int hero){if(!deployment.ConsumeClick())Deploy(hero);}
        public void CancelDeploymentQueue(){deployment.CancelQueue();RefreshHUD();}
        public void PauseForFocusLoss()
        {deployment.EndHold();if(Battle!=null&&Battle.Phase==BattlePhase.Battle&&!Paused)TogglePause();}
        public void RerollRewards()
        {if(Battle==null||Battle.Phase!=BattlePhase.Reward||!Battle.Reroll())return;ShowReward();}
        void BuildConvenienceHUD()
        {
            RiftUI.Button(hud,"Build overview","편성·서약 [B]",326,1001,262,49,new Color(.11f,.16f,.24f,.95f),ShowBuildOverview,21);
            queueButton=RiftUI.Button(hud,"Cancel deployment","카드 길게 누르기: 연속 출격",26,716,305,49,new Color(.11f,.16f,.24f,.95f),CancelDeploymentQueue,18);
            queueText=queueButton.GetComponentInChildren<Text>();
        }
        void RefreshConvenienceHUD()
        {
            int hero=deployment.QueuedHero;queueButton.interactable=hero>=0;
            queueText.text=hero>=0?"예약: "+RiftData.Heroes[hero].name+(Battle.WarningTime>0?" · 비기 대응 대기":" · 취소 [Backspace]"):"카드·숫자키 길게: 연속 출격";
            for(int slot=0;slot<6;slot++)
            {var outline=summonButtons[slot].GetComponent<Outline>();if(outline)outline.effectColor=hero==CardHero(slot)?new Color(1,.82f,.4f,.9f):new Color(.6f,.72f,.95f,.18f);}
        }
        void CloseHomeOverlay(){homeOverlayOpen=false;ClearChildren(modal);ShowHome();}
        public void ShowBuildOverview()
        {
            if(Battle==null||overviewOpen||(Battle.Phase!=BattlePhase.Battle&&Battle.Phase!=BattlePhase.Reward))return;
            overviewWasPaused=Paused;overviewOpen=true;deployment.EndHold();if(Battle.Phase==BattlePhase.Battle)Paused=true;
            ClearChildren(modal);var shade=RiftUI.Image(modal,"Build shade",0,0,1920,1080,new Color(.015f,.025f,.06f,.88f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,"Build inventory",180,90,1560,900,new Color(.045f,.068f,.13f,.98f));
            RiftUI.Text(panel.transform,"Build title","현재 편성 · 서약",40,812,1120,65,43,Color.white,true);
            RiftUI.Text(panel.transform,"Build caption","출정 부적: "+RiftWorkshop.AmuletNames[Battle.EquippedAmulet]+" · 특성 "+Battle.TraitCount+"명 · "+(Battle.Phase==BattlePhase.Reward?"다음 적 전술과 보상을 비교하세요":"전투 일시정지 · 조합과 보유 서약을 확인하세요"),40,770,1450,36,21,RiftUI.Muted);
            RiftUI.Button(panel.transform,"Close build","돌아가기 [B / Esc]",1230,824,290,48,new Color(.17f,.29f,.37f,.98f),CloseBuildOverview,20);
            for(int faction=0;faction<2;faction++)
            {
                float row=580-faction*195;
                RiftUI.Text(panel.transform,"Build faction "+faction,RiftData.Factions[faction],40,row+149,1450,39,24,faction==0?RiftUI.Coral:RiftUI.Ice,true);
                for(int slot=0;slot<6;slot++)
                {
                    int hero=RiftData.FactionStart(faction)+slot;var d=RiftData.Heroes[hero];bool owned=Battle.Ranks[hero]>0;int alive=0;foreach(var f in Battle.Fighters)if(f.ally&&f.Alive&&f.hero==hero)alive++;
                    var card=RiftUI.Surface(panel.transform,"Build hero "+d.id,40+slot*248,row,234,143,new Color(.10f,.15f,.245f,.98f));
                    var face=RiftUI.Image(card.transform,"Build portrait",8,30,82,101,owned?Color.white:new Color(.4f,.45f,.55f,.5f),portraits[hero]);face.preserveAspect=true;
                    RiftUI.Text(card.transform,"Build hero name",d.name,97,97,128,33,24,owned?Color.white:RiftUI.Muted,true);
                    RiftUI.Text(card.transform,"Build hero state",owned?"각성 "+Battle.Ranks[hero]+" · "+alive+"명\n출격 "+Battle.SummonCost(hero):"미영입\n보상으로 영입",97,39,130,58,18,RiftUI.Muted);
                    RiftUI.Text(card.transform,"Build hero role",RiftData.RoleName(d.role)+" · "+d.skill,10,5,214,29,15,RiftUI.Hex(d.color),false,TextAnchor.MiddleCenter);
                    DecorateFrame(card.transform,234,143,Battle.EquippedFrame);
                }
            }
            RiftUI.Text(panel.transform,"Relic inventory title","보유 서약  ·  "+Battle.Relics.Count+"개",40,330,1440,38,26,RiftUI.Gold,true);
            int shown=0;
            for(int i=0;i<RiftData.Relics.Length;i++)
            {
                var r=RiftData.Relics[i];int count=Battle.RelicCount(r.id);if(count==0)continue;
                var card=RiftUI.Surface(panel.transform,"Owned relic "+r.id,40+(shown%6)*248,174-(shown/6)*140,234,135,new Color(.10f,.15f,.245f,.98f));shown++;
                RiftUI.Text(card.transform,"Owned relic name",r.name+" ×"+count,13,88,208,35,22,RiftUI.Gold,true);
                RiftUI.Text(card.transform,"Owned relic effect",r.description,13,14,208,73,18,RiftUI.Muted);
            }
            if(shown==0)RiftUI.Text(panel.transform,"No relics","구간 보상에서 서약을 고르면 여기에 표시됩니다.",40,200,1440,100,24,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Build tip","카드 클릭: 한 번 출격 / 부족하면 예약 · 길게 누르기: 연속 출격 · 예약 취소: Backspace",40,73,1450,50,21,RiftUI.Muted,false,TextAnchor.MiddleCenter);
        }
        public void CloseBuildOverview()
        {
            if(!overviewOpen)return;overviewOpen=false;Paused=overviewWasPaused;
            if(Battle.Phase==BattlePhase.Reward)ShowReward();else if(Paused)ShowPause();else ClearChildren(modal);
        }
    }
}
