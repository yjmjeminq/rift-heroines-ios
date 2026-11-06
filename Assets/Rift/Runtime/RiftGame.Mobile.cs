using System;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        RectTransform touchOverlay;
        bool mobileSheetOpen,mobileBattleSheet,mobileWasPaused;
        public bool TouchLayout {get{return Application.isMobilePlatform||Array.IndexOf(Environment.GetCommandLineArgs(),"-riftMobileQA")>=0;}}
        public bool MobileSheetOpen {get{return mobileSheetOpen;}}
        void TouchTarget(Transform parent,string name,float x,float y,float w,float h,Action action)
        {RiftUI.Button(parent,name,"",x,y,w,h,new Color(0,0,0,0),action);}
        void BuildMobileHUD()
        {
            if(!TouchLayout)return;
            // Broad targets lead to sheets with full-size actions; summoning remains a single tap/hold.
            TouchTarget(hud,"Touch pause",610,976,740,104,TogglePause);
            RiftUI.Button(hud,"Touch hero details","동료 정보 · 각성",296,320,510,126,new Color(.11f,.17f,.26f,.97f),()=>ShowMobileHero(selectedHero),32);
            RiftUI.Button(hud,"Touch roster",showingRecruits?"기본 진영":"영입 동료",24,268,260,160,new Color(.13f,.21f,.3f,.98f),SwitchRoster,30);
            for(int i=0;i<6;i++)awakenButtons[i].gameObject.SetActive(false);
        }
        RectTransform MobilePanel(string title,string caption)
        {
            ClearChildren(touchOverlay);touchOverlay.gameObject.SetActive(true);
            var shade=RiftUI.Image(touchOverlay,"Touch shade",0,0,1920,1080,new Color(.01f,.02f,.05f,.94f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(touchOverlay,"Touch panel",90,50,1740,980,new Color(.045f,.068f,.13f,1)).rectTransform;
            RiftUI.Text(panel,"Touch title",title,60,854,1320,70,48,RiftUI.Gold,true);
            RiftUI.Text(panel,"Touch caption",caption,60,792,1300,60,30,RiftUI.Muted);
            RiftUI.Button(panel,"Touch close","돌아가기",1430,812,250,140,new Color(.17f,.29f,.37f,1),CloseMobileSheet,34);
            mobileSheetOpen=true;return panel;
        }
        public void CloseMobileSheet()
        {
            if(!mobileSheetOpen)return;
            mobileSheetOpen=false;touchOverlay.gameObject.SetActive(false);ClearChildren(touchOverlay);
            if(mobileBattleSheet){Paused=mobileWasPaused;mobileBattleSheet=false;}
        }
        public void ShowMobileHero(int hero)
        {
            if(Battle==null||Battle.Phase!=BattlePhase.Battle||hero<0||hero>=12)return;
            if(!mobileSheetOpen){mobileWasPaused=Paused;mobileBattleSheet=true;deployment.EndHold();Paused=true;}
            selectedHero=hero;var definition=RiftData.Heroes[hero];
            var panel=MobilePanel(definition.name+" · 동료 정보 / 각성","전투 일시정지 · 아래에서 동료를 골라 강화하세요");
            for(int i=0;i<6;i++)
            {
                int index=CardHero(i);bool owned=Battle.Ranks[index]>0;
                var card=RiftUI.Button(panel,"Touch hero "+index,RiftData.Heroes[index].name,60+i*273,565,248,210,index==hero?new Color(.31f,.29f,.22f,1):new Color(.12f,.19f,.29f,1),()=>ShowMobileHero(index),31);
                var label=card.GetComponentInChildren<Text>();label.rectTransform.sizeDelta=new Vector2(232,65);
                var image=RiftUI.Image(card.transform,"Face",54,65,140,135,owned?Color.white:RiftUI.Muted,portraits[index]);image.preserveAspect=true;
            }
            bool unlocked=Battle.Ranks[hero]>0;
            string detail=definition.description+"\n"+SelectedBondHint()+"\n"+(unlocked?"각성 "+Battle.Ranks[hero]+" / 3  ·  출격 "+Battle.SummonCost(hero)+" 영력":"구간 돌파 보상에서 영입할 수 있습니다.");
            RiftUI.Text(panel,"Touch hero description",detail,65,300,1610,230,35,Color.white,false,TextAnchor.MiddleCenter);
            var awaken=RiftUI.Button(panel,"Touch awaken",Battle.Ranks[hero]>=3?"최종 각성":!unlocked?"미영입":"각성 · "+Battle.AwakenCost(hero)+" 영력",60,100,510,155,new Color(.3f,.24f,.34f,1),()=>{Battle.Awaken(hero);ShowMobileHero(hero);},35);
            awaken.interactable=unlocked&&Battle.Ranks[hero]<3&&Battle.Mana>=Battle.AwakenCost(hero);
            var summon=RiftUI.Button(panel,"Touch summon","출격 / 예약",615,100,510,155,new Color(.18f,.32f,.38f,1),()=>{CloseMobileSheet();Deploy(hero);},35);summon.interactable=unlocked;
            RiftUI.Button(panel,"Touch resume","전투 계속",1170,100,510,155,new Color(.18f,.27f,.36f,1),CloseMobileSheet,35);
        }
        void BuildMobileWorkshop(Transform panel)
        {
            TouchTarget(panel,"Touch choose heroine",42,280,510,455,ShowMobileHeroPicker);
            foreach(Transform child in panel)
            {
                if(!child.name.StartsWith("Workshop item ",StringComparison.Ordinal))continue;
                int id;if(!int.TryParse(child.name.Substring("Workshop item ".Length),out id))continue;
                var image=child.GetComponent<Image>();image.raycastTarget=true;
                var button=child.gameObject.AddComponent<Button>();button.targetGraphic=image;int selected=id;
                button.onClick.AddListener(()=>ShowMobileWorkshopItem(selected));
            }
            RiftUI.Text(panel,"Touch shop hint","카드를 터치하면 크게 보기 · 캐릭터 그림을 터치하면 12명 선택",580,708,1084,45,25,RiftUI.Ice,false,TextAnchor.MiddleCenter);
        }
        public void ShowMobileHeroPicker()
        {
            if(!workshopOpen)return;
            var panel=MobilePanel("공방 동료 선택","작품별 여섯 명 · 준비할 동료를 터치하세요");
            for(int i=0;i<12;i++)
            {
                int hero=i;var card=RiftUI.Button(panel,"Touch workshop hero "+i,RiftData.Heroes[i].name,60+(i%6)*273,110+(1-i/6)*305,248,280,new Color(.12f,.19f,.29f,1),()=>{CloseMobileSheet();WorkshopSelectHero(hero);},32);
                card.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(232,68);
                var face=RiftUI.Image(card.transform,"Face",34,74,180,190,Color.white,portraits[i]);face.preserveAspect=true;
            }
        }
        public void ShowMobileWorkshopItem(int id)
        {
            if(!workshopOpen)return;var item=RiftWorkshop.Find(id);if(item==null)return;
            bool owned=RiftWorkshop.Owned(Profile,id),equipped=RiftWorkshop.Equipped(Profile,id);
            var panel=MobilePanel(item.name,"인연 조각 "+Profile.shards+" · 영구 해금 / 다음 출정부터 적용");
            RiftUI.Icon(panel,item.glyph,90,440,190,190,RiftUI.Gold);
            RiftUI.Text(panel,"Touch item description",item.description,350,415,1265,300,46,Color.white,false,TextAnchor.MiddleCenter);
            string action=equipped?"장착 중":owned?"장착하기":item.cost+" 조각 · 해금 / 장착";
            var buy=RiftUI.Button(panel,"Touch workshop buy",action,60,105,510,155,new Color(.2f,.31f,.35f,1),()=>{CloseMobileSheet();WorkshopAction(owned?1:0,id);},32);buy.interactable=!equipped&&(owned||Profile.shards>=item.cost);
            var wish=RiftUI.Button(panel,"Touch workshop wish",Profile.workshopGoal==id?"수집 목표 해제":"수집 목표 지정",615,105,510,155,new Color(.18f,.25f,.34f,1),()=>{CloseMobileSheet();WorkshopAction(2,id);},32);wish.interactable=!owned;
            bool visual=item.kind==WorkshopKind.Style||item.kind==WorkshopKind.Frame;
            RiftUI.Button(panel,"Touch workshop preview",visual?"연출 미리보기":"공방으로",1170,105,510,155,new Color(.23f,.25f,.33f,1),()=>{CloseMobileSheet();if(visual){workshopPreview=id;DrawWorkshop();}},32);
        }
        void ShowMobilePause()
        {
            ClearChildren(modal);var shade=RiftUI.Image(modal,"Pause shade",0,0,1920,1080,new Color(.01f,.02f,.05f,.94f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,"Touch pause panel",170,90,1580,900,new Color(.045f,.068f,.13f,1));
            RiftUI.Text(panel.transform,"Pause title","잠시 숨을 고르세요",65,740,1450,100,55,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Pause tip","카드: 출격 · 길게: 연속 출격 · 동료 정보에서 각성",65,640,1450,90,34,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Button(panel.transform,"Resume","전투 계속",65,430,700,155,new Color(.2f,.34f,.4f,1),TogglePause,38);
            RiftUI.Button(panel.transform,"Pause overview","편성 · 서약 보기",815,430,700,155,new Color(.17f,.24f,.34f,1),ShowBuildOverview,36);
            RiftUI.Button(panel.transform,"Touch speed","배속 "+Speed+"×",65,240,450,155,new Color(.17f,.24f,.34f,1),()=>{ToggleSpeed();ShowPause();},36);
            RiftUI.Button(panel.transform,"Touch sound",Profile.muted?"소리 켜기":"소리 끄기",565,240,450,155,new Color(.17f,.24f,.34f,1),()=>{Profile.muted=!Profile.muted;sound.Muted=Profile.muted;SaveProfile();ShowPause();},36);
            RiftUI.Button(panel.transform,"Touch motion",Profile.reducedMotion?"흔들림 켜기":"흔들림 줄이기",1065,240,450,155,new Color(.17f,.24f,.34f,1),()=>{Profile.reducedMotion=!Profile.reducedMotion;SaveProfile();ShowPause();},32);
            RiftUI.Button(panel.transform,"Home","거점으로 · 현재 구간 자동 저장",65,50,1450,155,new Color(.23f,.25f,.34f,1),ReturnHome,36);
        }
    }
}
