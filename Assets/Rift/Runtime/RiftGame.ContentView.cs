using System;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        readonly Sprite[] arenaSprites=new Sprite[3];
        RectTransform dangerRoot;
        Image dangerFill,dangerCharge;
        Text dangerLabel,practiceTitle,practiceNote;
        int practiceShownStep=-1;
        public string CurrentArenaId {get;private set;}
        void LoadArenas()
        {
            for(int i=0;i<3;i++)
            {
                string path=i==0?"Rift/Art/moonbridge":"Rift/Stages/"+RiftData.ArenaIds[i];
                var texture=Resources.Load<Texture2D>(path);
                if(!texture)throw new InvalidOperationException("Missing chapter background: "+path);
                arenaSprites[i]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
            }
        }
        void SetArena(int arena=-1)
        {
            if(arena<0)arena=Battle==null?0:RiftData.ArenaForStage(Battle.Stage);
            CurrentArenaId=RiftData.ArenaIds[arena];
            if(background)background.GetComponent<Image>().sprite=arenaSprites[arena];
        }
        public void BeginPractice()
        {
            ClearChildren(home);ClearChildren(modal);ClearTroops();deployment.Clear();
            Battle=RiftBattle.CreatePractice();Speed=1;practiceShownStep=-1;
            StartBattleScreen();Toast("훈련은 기존 출정·기록·보상을 바꾸지 않습니다",4);
        }
        void BuildPracticeHUD()
        {
            practiceTitle=practiceNote=null;
            if(!Battle.Practice)return;
            var panel=RiftUI.Surface(hud,"Training lesson",465,602,990,132,new Color(.035f,.055f,.115f,.96f));
            practiceTitle=RiftUI.Text(panel.transform,"Training title","",22,77,946,44,27,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            practiceNote=RiftUI.Text(panel.transform,"Training note","",28,12,934,61,22,Color.white,false,TextAnchor.MiddleCenter);
            RiftUI.Button(hud,"Leave training","훈련 나가기 · 진행 보존",1470,795,396,52,new Color(.18f,.25f,.32f,.98f),ReturnHome,20);
            comboText.gameObject.SetActive(false);speedButton.interactable=false;
            toastText.rectTransform.anchoredPosition=new Vector2(410,539);
        }
        void RefreshPracticeHUD()
        {
            if(!Battle.Practice||!practiceTitle)return;
            string[] titles={"1 / 4  ·  두 명으로 조합 켜기","2 / 4  ·  전원 진격","3 / 4  ·  금빛 순간에 결계","4 / 4  ·  궁극기로 마무리","훈련 완료"};
            string[] notes={"아래 카드에서 사쿠라 [1]와 츠나데 [2]를 출격하세요.\n두 명이 함께 살아 있으면 ‘백호의 계승’이 켜집니다.","[Q] 진격을 누르세요. 6초 동안 이동·공격 속도가 45% 빨라집니다.","예고가 0.65초 이하로 줄어 금빛이 되면 [E] 결계!\n일찍 눌러도 곧 다시 시도할 수 있습니다.","각성 게이지가 찼습니다. [R] 궁극기로 적에게 피해를 주세요.","실전에서도 조합 → 진격 → 결계 반격 → 궁극기를 이어 보세요."};
            practiceTitle.text=titles[Battle.PracticeStep];practiceNote.text=notes[Battle.PracticeStep];
            for(int i=0;i<6;i++)stageDots[i].color=i<Math.Min(4,Battle.PracticeStep+1)?RiftUI.Gold:new Color(.3f,.36f,.48f,.6f);
            ultimateButton.interactable=!Paused&&Battle.PracticeStep==3;
            for(int i=0;i<6;i++)
            {
                int hero=CardHero(i);
                summonButtons[i].interactable=!Paused&&hero<=1&&!Battle.Fighters.Exists(f=>f.ally&&f.Alive&&f.hero==hero);
                PracticeHighlight(summonButtons[i],Battle.PracticeStep==0&&summonButtons[i].interactable);
                awakenButtons[i].interactable=false;
            }
            PracticeHighlight(orderButton,Battle.PracticeStep==1);
            PracticeHighlight(shieldButton,Battle.PracticeStep==2&&Battle.WarningTime>0&&Battle.WarningTime<=.65f&&RiftFeedback.CanCounter(Battle));
            PracticeHighlight(ultimateButton,Battle.PracticeStep==3);
            economyButton.interactable=false;
        }
        void PracticeHighlight(Button button,bool active)
        {var edge=button.GetComponent<Outline>();if(edge){edge.effectColor=active?RiftUI.Gold:new Color(.6f,.72f,.95f,.18f);edge.effectDistance=active?new Vector2(3,-3):new Vector2(1,-1);}}
        void UpdatePracticeScreen()
        {
            if(!Battle.Practice||practiceShownStep==Battle.PracticeStep)return;
            practiceShownStep=Battle.PracticeStep;
            if(!Battle.PracticeComplete)return;
            Paused=true;deployment.Clear();
            var panel=Overlay("실전에 나설 준비 완료","조합 · 진격 · 절묘한 결계 · 궁극기, 네 가지 조작 완료");
            RiftUI.Icon(panel,"shield",551,395,118,118,RiftUI.Gold);
            RiftUI.Text(panel,"Training result","짝을 함께 지키고, 비기의 마지막 순간을 노리세요.\n훈련 결과는 출정 기록과 보상에 반영되지 않습니다.",85,263,1050,102,29,Color.white,false,TextAnchor.MiddleCenter);
            RiftUI.Button(panel,"Training restart","다시 연습하기",100,116,470,72,new Color(.24f,.23f,.34f,.98f),BeginPractice,25);
            RiftUI.Button(panel,"Training finish","거점으로 돌아가기",650,116,470,72,new Color(.17f,.29f,.37f,.98f),ReturnHome,25);
        }
        void BuildDangerVisual()
        {
            dangerRoot=RiftUI.Rect(world,"Boss technique target",0,301,500,88);
            dangerFill=RiftUI.Image(dangerRoot,"Danger area",0,0,500,83,new Color(1,.27f,.35f,.25f));
            dangerFill.rectTransform.anchorMax=Vector2.one;dangerFill.rectTransform.sizeDelta=new Vector2(0,-5);
            dangerCharge=RiftUI.Image(dangerRoot,"Danger charge",0,0,500,6,RiftUI.Coral);
            dangerCharge.type=Image.Type.Filled;dangerCharge.fillMethod=Image.FillMethod.Horizontal;
            dangerCharge.rectTransform.anchorMax=new Vector2(1,0);dangerCharge.rectTransform.sizeDelta=new Vector2(0,6);
            dangerLabel=RiftUI.Text(dangerRoot,"Danger label","",0,88,500,34,19,RiftUI.Coral,true,TextAnchor.MiddleCenter);
            dangerLabel.rectTransform.anchorMax=new Vector2(1,0);dangerLabel.rectTransform.sizeDelta=new Vector2(0,34);
            dangerLabel.gameObject.AddComponent<Shadow>().effectColor=Color.black;
            dangerRoot.gameObject.SetActive(false);
        }
        void RefreshDangerVisual()
        {
            bool show=Battle.WarningTime>0&&Battle.Phase==BattlePhase.Battle;
            dangerRoot.gameObject.SetActive(show);if(!show)return;
            float left=Mathf.Max(6,Battle.WarningCenter-Battle.WarningRadius),right=Mathf.Min(94,Battle.WarningCenter+Battle.WarningRadius);
            dangerRoot.anchoredPosition=new Vector2(170+left*15.8f,301);dangerRoot.sizeDelta=new Vector2((right-left)*15.8f,88);
            Color tint=Battle.WarningTime<=.65f?RiftUI.Gold:RiftUI.Coral;
            dangerFill.color=new Color(tint.r,tint.g,tint.b,.16f+Mathf.PingPong(Battle.StageTime*1.4f,.13f));
            dangerCharge.color=tint;dangerCharge.fillAmount=1-Battle.WarningTime/2.5f;dangerLabel.color=tint;
            dangerLabel.text=Battle.WarningTime<=.65f&&RiftFeedback.CanCounter(Battle)?"지금 E!":Battle.ActiveBossTechnique==BossTechnique.Reinforcements?"지원 소집 위치":"비기 위험 범위";
        }
    }
}
