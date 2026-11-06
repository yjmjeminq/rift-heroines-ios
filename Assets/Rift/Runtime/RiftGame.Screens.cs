using System;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        RectTransform[] homeFigures;
        float[] homeBaseY;
        void ShowHome()
        {
            Speed=Profile.preferredSpeed;SetArena(0);
            homeOverlayOpen=false;
            ClearChildren(home);home.gameObject.SetActive(true);world.gameObject.SetActive(false);hud.gameObject.SetActive(false);
            RiftUI.Image(home,"Title shade",0,0,1920,1080,new Color(.025f,.035f,.09f,.43f));
            RiftUI.Text(home,"Brand","R I F T   /   H E R O I N E S",42,998,700,40,22,RiftUI.Gold,true);
            RiftUI.Text(home,"Version","PROTOTYPE  "+RiftData.Version,1520,998,355,40,18,RiftUI.Muted,false,TextAnchor.MiddleRight);
            homeFigures=new RectTransform[4];homeBaseY=new float[4];int[] heroes={1,0,6,7};float[] xs={-82,160,1235,1460};
            for(int i=0;i<4;i++)
            {
                var glow=RiftUI.Image(home,"Hero aura",xs[i]+50,255,350,400,new Color(i<2?1:.5f,.5f,1,.07f),RiftUI.Disc);
                homeBaseY[i]=268+(i%2)*20;
                var im=RiftUI.Image(home,"New heroine "+heroes[i],xs[i],homeBaseY[i],630,630*(1-frameFooters[heroes[i]][0]),Color.white,frames[heroes[i]][0]);im.preserveAspect=true;
                if(i>=2)im.rectTransform.localScale=new Vector3(-1,1,1);
                // Reflect around the visual center, rather than the default lower-left pivot.
                if(i>=2){im.rectTransform.pivot=new Vector2(.5f,0);im.rectTransform.anchoredPosition+=new Vector2(315,0);}
                homeFigures[i]=im.rectTransform;
            }
            var panel=RiftUI.Surface(home,"Departure",583,253,754,660,new Color(.035f,.055f,.115f,.93f));
            RiftUI.Icon(panel.transform,"moon",333,552,88,88,RiftUI.Gold);
            RiftUI.Text(panel.transform,"Subtitle","나루토 6명 × 블리치 6명, 열두 인연의 격돌",37,497,680,44,21,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Title","월야의 서약",20,401,714,104,69,Color.white,true,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Pitch","6경계의 여정 · 인연을 모아 새로운 전술을 깨우세요.",30,345,694,50,22,RiftUI.Gold,false,TextAnchor.MiddleCenter);
            RiftUI.Button(panel.transform,"Workshop","인연 공방  ·  특성 · 출정 부적 · 외형 수집",36,294,682,46,new Color(.25f,.28f,.22f,.98f),ShowWorkshop,21);
            var b0=RiftUI.Button(panel.transform,"Start Shinobi","나루토 진영\n사쿠라 · 츠나데 · 히나타\n이노 · 테마리 · 코난",36,169,330,111,new Color(.5f,.18f,.18f,.96f),()=>BeginRun(0),20);
            var b1=RiftUI.Button(panel.transform,"Start Reapers","블리치 진영\n란기쿠 · 요루이치 · 우노하나\n루키아 · 오리히메 · 소이폰",388,169,330,111,new Color(.16f,.30f,.48f,.96f),()=>BeginRun(1),20);
            if(Profile.checkpoint!=null)
                RiftUI.Button(panel.transform,"Continue",Profile.checkpoint.pendingReward?"제 "+Profile.checkpoint.stage+"경계 보상 선택 이어하기":"제 "+Profile.checkpoint.stage+"경계부터 이어하기",36,98,682,51,new Color(.2f,.31f,.34f,.96f),ContinueRun,21);
            else RiftUI.Text(panel.transform,"Save hint",string.IsNullOrEmpty(RiftStorage.RecoveryNotice)?"구간·보상 선택 자동 저장 · 실패해도 인연 조각 획득":RiftStorage.RecoveryNotice,36,103,682,44,18,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Record","최고 "+Profile.bestStage+"경계   ·   완주 "+Profile.wins+"회   ·   인연 조각 "+Profile.shards,36,33,682,44,20,RiftUI.Gold,true,TextAnchor.MiddleCenter);

            RiftUI.Surface(home,"Archive",42,48,1836,150,new Color(.035f,.055f,.115f,.93f));
            RiftUI.Text(home,"Archive label","인연의 기록",72,133,230,38,24,Color.white,true);
            RiftUI.Text(home,"Archive note",RiftProgress.NextGoal(Profile),72,102,570,31,17,RiftUI.Muted);
            RiftUI.Button(home,"Records","기록·도전 보기"+(RiftProgress.ReadyCount(Profile)>0?"  ·  받을 보상 "+RiftProgress.ReadyCount(Profile)+"개":""),72,59,570,40,new Color(.18f,.25f,.34f,.98f),ShowRecords,19);
            int hc=Profile.UpgradeCost(true),mc=Profile.UpgradeCost(false);
            var hp=RiftUI.Button(home,"Meta vitality","수호의 인연  "+Profile.healthUpgrade+" / 5\n아군 체력 +3.5%  ·  "+hc+" 조각",695,74,430,94,new Color(.14f,.24f,.30f,.95f),()=>BuyMeta(true),21);
            var mp=RiftUI.Button(home,"Meta mana","출정의 인연  "+Profile.manaUpgrade+" / 5\n시작 영력 +7  ·  "+mc+" 조각",1148,74,430,94,new Color(.22f,.20f,.33f,.95f),()=>BuyMeta(false),21);
            hp.interactable=Profile.healthUpgrade<5&&Profile.shards>=hc;mp.interactable=Profile.manaUpgrade<5&&Profile.shards>=mc;
            RiftUI.Button(home,"Sound",Profile.muted?"소리 꺼짐":"소리 켜짐",1602,121,237,46,new Color(.14f,.19f,.28f,.95f),()=>{Profile.muted=!Profile.muted;sound.Muted=Profile.muted;RiftStorage.Save(Profile);ShowHome();},19);
            RiftUI.Button(home,"Exit","종료",1602,70,237,41,new Color(.14f,.19f,.28f,.95f),()=>Application.Quit(),19);
            RiftUI.Button(home,"Roster book","12명 캐릭터 보기",583,922,237,49,new Color(.15f,.23f,.34f,.98f),ShowRosterBook,20);
            RiftUI.Button(home,"Tactics guide","전투 가이드",838,922,237,49,new Color(.23f,.23f,.32f,.98f),ShowTacticsGuide,20);
            RiftUI.Button(home,"Training room","직접 배우기 · 훈련",1093,922,244,49,new Color(.24f,.31f,.24f,.98f),BeginPractice,20);
            RiftUI.Text(home,"Controls","1–6 출격     C 진영 카드 전환     Q 진격     E 결계     R 궁극기     U 영맥 강화     Space 일시정지",270,209,1380,34,18,new Color(.85f,.9f,1,.85f),false,TextAnchor.MiddleCenter);
        }
        public void ShowRosterBook()
        {
            homeOverlayOpen=true;
            home.gameObject.SetActive(false);ClearChildren(modal);
            var shade=RiftUI.Image(modal,"Roster shade",0,0,1920,1080,new Color(.015f,.025f,.06f,.9f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,"Twelve heroines",180,90,1560,900,new Color(.045f,.068f,.13f,.98f));
            RiftUI.Text(panel.transform,"Roster title","열두 명의 인연",40,812,1120,65,44,Color.white,true);
            RiftUI.Text(panel.transform,"Roster caption","작품별 6명 · 성인 모습 · 8등신 · 각자의 전용 모션과 기술",40,769,1420,36,21,RiftUI.Muted);
            RiftUI.Button(panel.transform,"Close roster","돌아가기 [Esc]",1280,824,230,48,new Color(.17f,.29f,.37f,.98f),CloseHomeOverlay,21);
            for(int faction=0;faction<2;faction++)
            {
                float row=430-faction*345;
                RiftUI.Text(panel.transform,"Roster faction "+faction,RiftData.Factions[faction]+"  /  6명",40,row+295,1450,40,25,faction==0?RiftUI.Coral:RiftUI.Ice,true);
                for(int slot=0;slot<RiftData.HeroesPerFaction;slot++)
                {
                    int hero=RiftData.FactionStart(faction)+slot;var d=RiftData.Heroes[hero];
                    var card=RiftUI.Surface(panel.transform,"Roster "+d.id,40+slot*248,row,234,290,new Color(.10f,.15f,.245f,.98f));
                    var image=RiftUI.Image(card.transform,"Full body",0,54,234,234*(1-frameFooters[hero][0]),Color.white,frames[hero][0]);image.preserveAspect=true;
                    RiftUI.Text(card.transform,"Name",d.name,8,22,218,34,25,Color.white,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(card.transform,"Role",RiftData.RoleName(d.role)+" · "+d.skill,8,1,218,25,16,RiftUI.Hex(d.color),false,TextAnchor.MiddleCenter);
                    DecorateFrame(card.transform,234,290,Profile.equippedFrame);
                }
            }
        }
        void BuyMeta(bool health){if(Profile.BuyUpgrade(health)){RiftStorage.Save(Profile);ShowHome();}}
        void AnimateHome(float dt)
        {
            if(homeFigures==null)return;
            for(int i=0;i<homeFigures.Length;i++)if(homeFigures[i])
            {var p=homeFigures[i].anchoredPosition;p.y=homeBaseY[i]+Mathf.Sin(Time.unscaledTime*.9f+i)*5;homeFigures[i].anchoredPosition=p;}
        }

        void BuildHUD()
        {
            toastPriority=0;toastTime=0;
            ClearChildren(hud);
            RiftUI.Image(hud,"Header background",0,976,1920,104,new Color(.025f,.035f,.07f,.95f));
            RiftUI.Text(hud,"Brand","R I F T",30,1014,220,42,30,RiftUI.Gold,true);
            RiftUI.Text(hud,"Faction",RiftData.Factions[Battle.Faction],30,978,270,36,18,RiftUI.Muted);
            stageText=RiftUI.Text(hud,"Stage title","",620,1009,680,46,28,Color.white,true,TextAnchor.MiddleCenter);
            for(int i=0;i<6;i++)
            {
                stageDots[i]=RiftUI.Image(hud,"Stage "+i,788+i*59,985,30,7,RiftUI.Muted,null,true);
            }
            speedButton=RiftUI.Button(hud,"Speed","1×",1523,1001,90,49,new Color(.11f,.16f,.24f,.95f),ToggleSpeed,21);
            RiftUI.Button(hud,"Pause","일시정지",1630,1001,148,49,new Color(.11f,.16f,.24f,.95f),TogglePause,20);
            RiftUI.Button(hud,"Mute",Profile.muted?"음소거":"소리",1790,1001,99,49,new Color(.11f,.16f,.24f,.95f),()=>{Profile.muted=!Profile.muted;sound.Muted=Profile.muted;RiftStorage.Save(Profile);BuildHUD();},19);

            RiftUI.Surface(hud,"Ally status",26,871,458,92,new Color(.035f,.055f,.115f,.9f));
            baseAllyText=RiftUI.Text(hud,"Ally base","",44,919,424,31,20,Color.white,true);
            allyBar=RiftUI.Bar(hud,"Ally vitality",44,893,422,17,RiftUI.Hex("#7FDBD2"));
            RiftUI.Surface(hud,"Enemy status",1436,871,458,92,new Color(.035f,.055f,.115f,.9f));
            baseEnemyText=RiftUI.Text(hud,"Enemy base","",1453,919,423,31,20,Color.white,true,TextAnchor.MiddleRight);
            enemyBar=RiftUI.Bar(hud,"Enemy vitality",1453,893,423,17,RiftUI.Coral);
            RiftUI.Surface(hud,"Energy status",26,773,305,85,new Color(.035f,.055f,.115f,.88f));
            RiftUI.Icon(hud,"mana",42,803,28,32,RiftUI.Ice);
            manaText=RiftUI.Text(hud,"Mana","",81,799,228,42,25,RiftUI.Ice,true);
            manaBar=RiftUI.Bar(hud,"Mana meter",44,786,267,9,RiftUI.Ice);
            BuildTacticalHUD();
            comboText=RiftUI.Text(hud,"Combo","",1433,778,449,70,37,RiftUI.Gold,true,TextAnchor.MiddleRight);
            threatBacking=RiftUI.Surface(hud,"Critical warning backing",490,741,940,53,new Color(.035f,.055f,.115f,.94f));
            threatText=RiftUI.Text(hud,"Threat","",500,741,920,53,25,RiftUI.Coral,true,TextAnchor.MiddleCenter);
            stageHint=RiftUI.Text(hud,"Stage hint",RiftData.StageHints[Battle.Stage-1],430,683,1060,50,22,Color.white,false,TextAnchor.MiddleCenter);
            var shadow=stageHint.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(1,-2);shadow.effectColor=Color.black;
            toastBacking=RiftUI.Surface(hud,"Notice backing",410,686,1100,50,new Color(.035f,.055f,.115f,.9f));
            toastText=RiftUI.Text(hud,"Toast","",410,686,1100,50,24,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            toastText.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(1,-2);
            RiftUI.Button(hud,"Switch roster",showingRecruits?"기본 진영 보기 [C]":"영입 동료 보기 [C]",24,268,260,38,new Color(.10f,.15f,.25f,.96f),SwitchRoster,19);
            RiftUI.Surface(hud,"Character detail backing",296,262,1600,58,new Color(.035f,.055f,.115f,.96f));
            statusText=RiftUI.Text(hud,"Selected description","",312,265,1570,52,20,new Color(.94f,.96f,1,.97f),false,TextAnchor.MiddleCenter);
            statusText.gameObject.AddComponent<Shadow>().effectColor=Color.black;

            RiftUI.Image(hud,"Deck background",0,0,1920,257,new Color(.025f,.035f,.075f,.98f));
            RiftUI.Image(hud,"Deck gold edge",0,255,1920,2,new Color(.7f,.63f,.46f,.6f));
            for(int i=0;i<6;i++)
            {
                int hero=CardHero(i);var d=RiftData.Heroes[hero];float x=24+i*191;
                var b=RiftUI.Button(hud,"Deploy "+d.id,"",x,57,178,182,new Color(.10f,.15f,.25f,.98f),()=>DeployFromCard(hero));summonButtons[i]=b;
                var hold=b.gameObject.AddComponent<RiftDeployHold>();hold.Game=this;hold.Hero=hero;
                var face=RiftUI.Image(b.transform,"New portrait",4,36,170,138,Color.white,portraits[hero]);face.preserveAspect=true;cardPortraits[i]=face;
                RiftUI.Image(b.transform,"Portrait footer",2,0,174,53,new Color(.035f,.05f,.09f,.95f),null,true);
                RiftUI.Text(b.transform,"Hero name",d.name,13,24,100,30,23,Color.white,true);
                RiftUI.Text(b.transform,"Role",RiftData.RoleName(d.role),13,2,95,28,16,RiftUI.Hex(d.color));
                costTexts[i]=RiftUI.Text(b.transform,"Energy cost","",103,0,64,45,24,RiftUI.Ice,true,TextAnchor.MiddleRight);
                RiftUI.Text(b.transform,"Shortcut",(i+1).ToString(),9,147,28,27,18,RiftUI.Gold,true,TextAnchor.MiddleCenter);
                rankTexts[i]=RiftUI.Text(b.transform,"Rank","",40,149,129,27,16,RiftUI.Gold,true,TextAnchor.MiddleRight);
                summonReadyBars[i]=RiftUI.Bar(b.transform,"Deploy readiness",6,53,166,6,RiftUI.Ice);
                b.GetComponent<RiftButtonFeel>().Hover=()=>{selectedHero=hero;};
                DecorateFrame(b.transform,178,182,Battle.EquippedFrame);
                awakenButtons[i]=RiftUI.Button(hud,"Awaken "+d.id,"",x,14,178,34,new Color(.18f,.18f,.28f,.96f),()=>{selectedHero=hero;if(Battle!=null&&!Paused&&!Battle.Awaken(hero))Toast("각성은 영력이 필요하며 최대 3단계입니다.",2);},16);
            }

            RiftUI.Text(hud,"Tactics label","전선을 뒤집는 한 수",1192,201,482,35,21,RiftUI.Gold,true);
            orderButton=RiftUI.Button(hud,"Order","",1190,60,139,132,new Color(.22f,.22f,.34f,.98f),UseOrder);
            RiftUI.Icon(orderButton.transform,"wind",47,68,45,45,RiftUI.Gold);
            orderText=RiftUI.Text(orderButton.transform,"Cooldown","",4,5,131,59,20,Color.white,true,TextAnchor.MiddleCenter);
            shieldButton=RiftUI.Button(hud,"Ward","",1343,60,139,132,new Color(.13f,.26f,.32f,.98f),UseShield);
            RiftUI.Icon(shieldButton.transform,"shield",47,68,45,45,RiftUI.Ice);
            shieldText=RiftUI.Text(shieldButton.transform,"Cooldown","",4,5,131,59,20,Color.white,true,TextAnchor.MiddleCenter);
            ultimateButton=RiftUI.Button(hud,"Ultimate","",1496,60,139,132,new Color(.36f,.22f,.30f,.98f),UseUltimate);
            RiftUI.Icon(ultimateButton.transform,"moon",47,68,45,45,RiftUI.Gold);
            ultimateText=RiftUI.Text(ultimateButton.transform,"Charge","",4,5,131,59,20,Color.white,true,TextAnchor.MiddleCenter);
            ultimateBar=RiftUI.Bar(hud,"Fury",1192,23,441,15,RiftUI.Gold);
            economyButton=RiftUI.Button(hud,"Economy","",1662,23,231,215,new Color(.15f,.22f,.28f,.98f),()=>{if(Battle!=null&&!Paused&&!Battle.UpgradeEconomy())Toast("영맥 강화 비용과 최대 단계를 확인하세요.",2);});
            RiftUI.Icon(economyButton.transform,"mana",87,134,57,57,RiftUI.Ice);
            economyText=RiftUI.Text(economyButton.transform,"Economy description","",12,10,207,117,20,Color.white,true,TextAnchor.MiddleCenter);
            BuildConvenienceHUD();
            BuildPracticeHUD();
            BuildMobileHUD();
        }

        void RefreshHUD()
        {
            if(Battle==null || !hud.gameObject.activeSelf)return;
            stageText.text=Battle.Practice?"훈련장  ·  실전 전술 익히기":"제 "+Battle.Stage+"경계  ·  "+RiftData.ArenaNames[RiftData.ArenaForStage(Battle.Stage)];
            for(int i=0;i<6;i++)stageDots[i].color=i<Battle.Stage?RiftUI.Gold:new Color(.3f,.36f,.48f,.6f);
            float enemyMax=720+Battle.Stage*140;
            baseAllyText.text="우리 거점   "+Mathf.CeilToInt(Battle.AllyBase)+" / 1000";
            baseEnemyText.text=(Battle.EnemyBaseProtected?"수호 결계  ·  ":"적 거점   ")+Mathf.CeilToInt(Battle.EnemyBase)+" / "+(int)enemyMax;
            allyBar.fillAmount=Battle.AllyBase/1000;enemyBar.fillAmount=Battle.EnemyBase/enemyMax;
            manaText.text=Mathf.FloorToInt(Battle.Mana)+" / "+(int)Battle.MaxMana+"   +"+Battle.ManaRate.ToString("0.0")+"/s";
            manaBar.fillAmount=Battle.Mana/Battle.MaxMana;
            frontText.text="전선 "+Mathf.RoundToInt(Battle.CenterControl)+"%   ·   아군 "+Battle.Allies+" / 18"+(Battle.CenterControl>65?"   영맥 점유 +2/s":"");
            RefreshTacticsHUD();
            comboText.text=Battle.Combo>=2?Battle.Combo+"  CHAIN"+(Battle.Combo>=5?"\n<size=18>연속 격파 · 영력 보너스</size>":""):"";
            bool perfectWindow=Battle.WarningTime>0&&Battle.WarningTime<=.65f;
            bool readyWindow=perfectWindow&&RiftFeedback.CanCounter(Battle);
            if(readyWindow&&!wasCounterWindow)sound.CounterWindow();wasCounterWindow=readyWindow;
            threatText.color=(perfectWindow&&RiftFeedback.CanCounter(Battle))||Battle.BossBreakTime>0?RiftUI.Gold:RiftUI.Coral;
            threatText.text=Battle.WarningTime>0?RiftFeedback.WarningAction(Battle)+"  ·  "+RiftData.BossTechniqueNames[(int)Battle.ActiveBossTechnique]+"  "+Battle.WarningTime.ToString("0.0")+"초":Battle.BossBreakTime>0?"대장 약점 노출  "+Battle.BossBreakTime.ToString("0.0")+"초  ·  받는 피해 +35%":"";
            if(Battle.StageTime>165)threatText.text="경계 붕괴! 거점 체력이 서서히 감소합니다.";
            threatBacking.gameObject.SetActive(!string.IsNullOrEmpty(threatText.text));
            var selected=RiftData.Heroes[selectedHero];
            statusText.text=roster.characters[selectedHero].canonicalName+"    "+selected.description+"\n<color=#F3D7A0>"+(Battle.TraitChoice(selectedHero)>0?RiftWorkshop.Trait(selectedHero,Battle.TraitChoice(selectedHero)).name+" · ":"")+SelectedBondHint()+"</color>";
            bool active=Battle.Phase==BattlePhase.Battle&&!Paused;
            for(int i=0;i<6;i++)
            {
                int hero=CardHero(i);bool unlocked=Battle.Ranks[hero]>0;
                summonButtons[i].interactable=active&&unlocked;
                cardPortraits[i].color=unlocked?Color.white:new Color(.35f,.39f,.5f,.45f);
                costTexts[i].text=unlocked?Battle.SummonCost(hero).ToString():"영입";
                costTexts[i].color=Battle.Mana>=Battle.SummonCost(hero)?RiftUI.Ice:RiftUI.Muted;
                int count=0;foreach(var f in Battle.Fighters)if(f.ally&&f.Alive&&f.hero==hero)count++;
                rankTexts[i].text=unlocked?"각성 "+Battle.Ranks[hero]+" · "+(Battle.SummonCooldown[hero]>.05f?Battle.SummonCooldown[hero].ToString("0.0")+"s":count+"명"):"미계약";
                summonReadyBars[i].fillAmount=Battle.SummonCooldown[hero]>.05f?1-Battle.SummonCooldown[hero]/1.6f:Mathf.Min(1,Battle.Mana/Battle.SummonCost(hero));
                if(!unlocked)summonReadyBars[i].fillAmount=0;
                summonReadyBars[i].color=Battle.SummonCooldown[hero]>.05f?RiftUI.Gold:Battle.Mana>=Battle.SummonCost(hero)?RiftUI.Ice:RiftUI.Muted;
                var t=awakenButtons[i].GetComponentInChildren<Text>();
                t.text=!unlocked?"구간 보상으로 영입":Battle.Ranks[hero]>=3?"최종 각성":"각성  "+Battle.AwakenCost(hero);
                awakenButtons[i].interactable=active&&unlocked&&Battle.Ranks[hero]<3&&Battle.Mana>=Battle.AwakenCost(hero);
            }
            orderText.text=Battle.OrderCooldown>0?"진격 "+Mathf.CeilToInt(Battle.OrderCooldown)+"s":"[Q] 진격\n공속 +45%";
            shieldText.text=Battle.ShieldCooldown>0?"결계 "+Mathf.CeilToInt(Battle.ShieldCooldown)+"s":"[E] 결계\n영력 30";
            if(perfectWindow&&Battle.ShieldCooldown<=0&&Battle.Mana>=30)shieldText.text="[E] 지금!\n절묘한 결계";
            shieldButton.targetGraphic.color=perfectWindow&&RiftFeedback.CanCounter(Battle)?new Color(.42f,.34f,.17f,.98f):new Color(.13f,.26f,.32f,.98f);
            ultimateText.text=Battle.Fury>=100?"[R] 궁극기\n발동 가능":"[R] 궁극기\n"+Mathf.FloorToInt(Battle.Fury)+"%";
            orderButton.interactable=active&&Battle.OrderCooldown<=0&&Battle.Allies>0;shieldButton.interactable=active&&Battle.ShieldCooldown<=0&&Battle.Mana>=30;ultimateButton.interactable=active&&Battle.Fury>=100;
            ultimateBar.fillAmount=Battle.Fury/100;
            economyText.text="[U] 영맥 "+Battle.Economy+" / 4\n"+(Battle.Economy>=4?"최대 강화":Battle.EconomyCost+" 영력")+"\n<size=16>회복 +2.4/s · 최대 +25</size>";
            economyButton.interactable=active&&Battle.Economy<4&&Battle.Mana>=Battle.EconomyCost;
            speedButton.GetComponentInChildren<Text>().text=Speed.ToString("0")+"×";
            RefreshConvenienceHUD();
            RefreshDangerVisual();RefreshPracticeHUD();
        }

        RectTransform Overlay(string title,string caption)
        {
            ClearChildren(modal);
            var shade=RiftUI.Image(modal,"Modal shade",0,0,1920,1080,new Color(.015f,.025f,.06f,.83f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,title,350,183,1220,714,new Color(.045f,.068f,.13f,.98f));
            RiftUI.Text(panel.transform,"Eyebrow",caption,60,624,1100,42,20,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Title",title,40,542,1140,84,49,Color.white,true,TextAnchor.MiddleCenter);
            return panel.rectTransform;
        }
        void ShowReward()
        {
            deployment.Clear();SaveCheckpoint();
            var next=RiftData.Encounters[RiftData.RuleForStage(Battle.Seed,Battle.Stage+1)];
            var panel=Overlay("어떤 인연을 이어갈까요?","다음: "+next.name+"  ·  "+next.description);
            for(int i=0;i<3;i++)
            {
                int index=i,choice=Battle.Choices[i];float x=48+i*379;
                var b=RiftUI.Button(panel,"Reward "+i,"",x,130,365,385,new Color(.10f,.15f,.245f,.98f),()=>PickReward(index));
                RiftUI.Text(b.transform,"Reward shortcut","["+(i+1)+"]",13,337,55,32,18,RiftUI.Gold,true);
                if(choice>=200)
                {
                    int hero=choice-200;var d=RiftData.Heroes[hero];
                    var im=RiftUI.Image(b.transform,"Awakening art",77,151,210,180,Color.white,frames[hero][0]);im.preserveAspect=true;
                    RiftUI.Text(b.transform,"Type","무료 각성",20,337,325,32,17,RiftUI.Gold,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Name",d.name+" 각성 +1",20,98,325,49,29,Color.white,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Description",Battle.Ranks[hero]+" → "+(Battle.Ranks[hero]+1)+"단계\n체력·공격력 +32% · 영력 소모 없음",24,29,317,70,19,RiftUI.Muted,false,TextAnchor.MiddleCenter);
                }
                else if(choice>=100)
                {
                    int hero=choice-100;var d=RiftData.Heroes[hero];
                    var im=RiftUI.Image(b.transform,"Recruit art",77,151,210,180,Color.white,frames[hero][0]);im.preserveAspect=true;
                    RiftUI.Text(b.transform,"Type","새 동료",20,337,325,32,17,RiftUI.Hex(d.color),true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Name",d.name+" 영입",20,98,325,49,31,Color.white,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Description",RiftData.RoleName(d.role)+" · "+d.title+"\n"+RecruitBondHint(hero),24,20,317,82,19,RiftUI.Muted,false,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Recruit cost","출격 "+Battle.SummonCost(hero)+" 영력",24,5,317,24,16,RiftUI.Gold,false,TextAnchor.MiddleCenter);
                }
                else
                {
                    var r=RiftData.Relics[choice];RiftUI.Icon(b.transform,r.glyph,126,195,112,112,RiftUI.Gold);
                    RiftUI.Text(b.transform,"Type","이번 출정 동안 유지",20,337,325,32,17,RiftUI.Gold,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Name",r.name,20,122,325,50,29,Color.white,true,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Description",r.description,24,68,317,51,20,RiftUI.Muted,false,TextAnchor.MiddleCenter);
                    RiftUI.Text(b.transform,"Relic comparison",RiftRewardPreview.Describe(Battle,choice),24,10,317,54,17,RiftUI.Gold,false,TextAnchor.MiddleCenter);
                }
            }
            RiftUI.Button(panel,"Reward overview","편성·서약 보기 [B]",48,61,280,53,new Color(.15f,.23f,.31f,.98f),ShowBuildOverview,19);
            var reroll=RiftUI.Button(panel,"Reroll",Battle.Rerolled?"이번 경계 새로고침 사용 완료":"다른 인연 보기 [R] · 1회 무료",349,61,522,53,new Color(.18f,.22f,.31f,.98f),RerollRewards,19);reroll.interactable=!Battle.Rerolled;
            RiftUI.Text(panel,"Reward save","선택 전 종료해도 이 보상부터 이어집니다",48,12,1124,35,18,RiftUI.Muted,false,TextAnchor.MiddleCenter);
        }
        void ShowPause()
        {
            if(TouchLayout){ShowMobilePause();return;}
            var panel=Overlay("잠시 숨을 고르세요","전투와 재사용 대기시간이 멈췄습니다");
            RiftUI.Text(panel,"Controls","1–6  동료 출격       C  진영 카드 전환\nQ  진격       E  보호 결계       R  전장 궁극기\nU  영맥 강화       Tab  배속       Space / Esc  일시정지",110,296,1000,202,27,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Button(panel,"Resume","전투 계속",353,237,514,64,new Color(.22f,.36f,.42f,.98f),TogglePause,25);
            RiftUI.Button(panel,"Pause overview","편성·서약 보기 [B]",103,160,485,55,new Color(.16f,.22f,.32f,.98f),ShowBuildOverview,21);
            RiftUI.Button(panel,"Motion",Profile.reducedMotion?"화면 흔들림 줄이기: 켜짐":"화면 흔들림 줄이기: 꺼짐",632,160,485,55,new Color(.16f,.22f,.32f,.98f),()=>{Profile.reducedMotion=!Profile.reducedMotion;SaveProfile();ShowPause();},19);
            RiftUI.Button(panel,"Home",Battle.Practice?"훈련 나가기  ·  기존 출정 진행 보존":"거점으로  ·  현재 구간 시작 지점 저장됨",283,82,654,54,new Color(.16f,.22f,.32f,.98f),ReturnHome,20);
            RiftUI.Text(panel,"Save status",RiftStorage.LastSaveSucceeded?"진행 기록 자동 저장 완료":"저장하지 못했습니다. 잠시 후 다시 저장합니다",110,22,1000,37,19,RiftUI.Muted,false,TextAnchor.MiddleCenter);
        }
        void ShowResult()
        {
            bool won=Battle.Phase==BattlePhase.Victory;
            if(!resultRecorded)
            {
                RecordResult();
            }
            var panel=Overlay(won?"새벽을 되찾았습니다":"다음 출정은 더 강하게",won?"여섯 경계 완주  ·  "+RiftData.Factions[Battle.Faction]:"제 "+Battle.Stage+"경계에서 전선 붕괴");
            RiftUI.Icon(panel,won?"moon":"heart",546,400,128,128,RiftUI.Gold);
            RiftUI.Text(panel,"Stats","격파 "+Battle.Kills+"명   ·   최고 "+Battle.BestCombo+"연쇄   ·   "+(int)(Battle.TotalTime/60)+"분 "+(int)(Battle.TotalTime%60)+"초\n<size=21>제단 점령 "+Battle.ShrineCaptures+"회   ·   절묘한 결계 "+Battle.PerfectGuards+"회   ·   무료 각성 "+Battle.FreeAwakens+"회</size>",110,306,1000,90,28,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Text(panel,"Shards","인연 조각 +"+Battle.RewardShards,110,239,1000,72,42,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            RiftUI.Button(panel,"Retry","새로운 전장",60,132,350,64,new Color(.28f,.23f,.34f,.98f),()=>BeginRun(Battle.Faction),23);
            RiftUI.Button(panel,"Same field retry","같은 전장 재도전",435,132,350,64,new Color(.28f,.23f,.34f,.98f),()=>BeginRun(Battle.Faction,Battle.Seed),23);
            RiftUI.Button(panel,"Home","인연 강화 · 진영 선택",810,132,350,64,new Color(.17f,.29f,.37f,.98f),ReturnHome,23);
            RiftUI.Button(panel,"Result records","기록·도전 보기"+(RiftProgress.ReadyCount(Profile)>0?" · 보상 "+RiftProgress.ReadyCount(Profile)+"개":""),353,58,514,55,new Color(.18f,.28f,.36f,.98f),()=>{ReturnHome();ShowRecords();},22);
            RiftUI.Button(panel,"Result workshop","인연 공방",60,58,270,55,new Color(.25f,.28f,.22f,.98f),ShowWorkshop,22);
            RiftUI.Text(panel,"Next",RiftProgress.NextGoal(Profile),70,8,1080,39,18,RiftUI.Muted,false,TextAnchor.MiddleCenter);
        }
    }
}
