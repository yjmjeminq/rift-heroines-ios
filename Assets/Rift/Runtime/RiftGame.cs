using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame : MonoBehaviour
    {
        public RiftBattle Battle { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public bool Paused { get; private set; }
        public float Speed { get; private set; } = 1;
        public bool SmokeMode { get; private set; }
        public RectTransform Content { get; private set; }
        readonly Sprite[][] frames = new Sprite[RiftData.HeroCount][];
        readonly Sprite[] portraits = new Sprite[RiftData.HeroCount];
        readonly float[][] frameFooters = new float[RiftData.HeroCount][];
        RiftRosterCatalog roster;
        RectTransform background,world,unitsRoot,effectsRoot,hud,modal,home;
        Image screenFlash;
        Image threatBacking,toastBacking;
        RiftAudio sound;
        Text stageText,stageHint,manaText,baseAllyText,baseEnemyText,comboText,statusText,threatText,frontText,toastText;
        Text economyText,orderText,shieldText,ultimateText;
        Image manaBar,allyBar,enemyBar,ultimateBar;
        Button economyButton,orderButton,shieldButton,ultimateButton,speedButton;
        readonly Button[] summonButtons=new Button[6],awakenButtons=new Button[6];
        readonly Text[] costTexts=new Text[6],rankTexts=new Text[6];
        readonly Image[] cardPortraits=new Image[6],stageDots=new Image[6];
        readonly Image[] summonReadyBars=new Image[6];
        float hitstop,shake,flash,toastTime,tutorialTime;
        int toastPriority;
        bool wasCounterWindow;
        bool resultRecorded,showingRecruits;
        BattlePhase observedPhase;
        int selectedHero;
        public static string Argument(string key)
        {string[] args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return null;}

        void Awake()
        {
            Application.targetFrameRate=60;
            bool experience=Array.IndexOf(Environment.GetCommandLineArgs(),"-riftExperience")>=0;
            bool mobileSmoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-riftMobileSmoke")>=0;
            SmokeMode=experience||mobileSmoke||Array.IndexOf(Environment.GetCommandLineArgs(),"-riftSmoke")>=0;
            if(SmokeMode)RiftStorage.OverridePath=System.IO.Path.Combine(Argument("-riftSmokePath")??"BuildLogs/PlayerSmoke","profile.json");
            Profile=SmokeMode?new PlayerProfile{muted=true}:RiftStorage.Load();
            Profile.Normalize();Speed=Profile.preferredSpeed;
            roster=RiftRosterCatalog.LoadValidated();
            RiftUI.Regular=Resources.Load<Font>("Rift/UI/Pretendard-Regular");RiftUI.Bold=Resources.Load<Font>("Rift/UI/Pretendard-SemiBold");
            if(!RiftUI.Regular)RiftUI.Regular=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
            if(!RiftUI.Bold)RiftUI.Bold=RiftUI.Regular;
            var layoutAsset=Resources.Load<TextAsset>("Rift/art-layout");
            if(!layoutAsset)throw new InvalidOperationException("Missing character sprite layout");
            var artLayout=JsonUtility.FromJson<RiftArtLayout>(layoutAsset.text);
            for(int hero=0;hero<RiftData.HeroCount;hero++)
            {
                var texture=Resources.Load<Texture2D>("Rift/Art/"+RiftData.Heroes[hero].id);
                if(!texture)throw new InvalidOperationException("Missing new character art: "+RiftData.Heroes[hero].id);
                var layout=Array.Find(artLayout.heroes,item=>item.id==RiftData.Heroes[hero].id);
                if(layout==null || layout.frameFooters==null || layout.frameFooters.Length!=6)throw new InvalidOperationException("Invalid sprite layout for "+RiftData.Heroes[hero].id);
                frameFooters[hero]=layout.frameFooters;
                float w=texture.width/3f,h=texture.height/2f;frames[hero]=new Sprite[6];
                for(int f=0;f<6;f++)
                {
                    float footer=Mathf.Clamp(layout.frameFooters[f],0,.15f)*h;
                    frames[hero][f]=Sprite.Create(texture,new Rect((f%3)*w,(1-f/3)*h+footer,w,h-footer),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
                }
                portraits[hero]=Sprite.Create(texture,new Rect(w*.34f,h*1.71f,w*.30f,h*.28f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            }
            sound=gameObject.AddComponent<RiftAudio>();sound.Muted=Profile.muted;
            BuildCanvas();ShowHome();
            if(experience){AudioListener.volume=0;sound.Muted=false;}
            if(mobileSmoke)gameObject.AddComponent<RiftMobileSmoke>();else if(experience)gameObject.AddComponent<RiftExperienceProbe>();else if(SmokeMode)gameObject.AddComponent<RiftSmoke>();
        }

        void BuildCanvas()
        {
            var cameraGO=new GameObject("Moonbridge Camera");var camera=cameraGO.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
            cameraGO.AddComponent<AudioListener>();
            var canvasGO=new GameObject("Rift Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=20;
            var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            Content=RiftUI.Rect(canvas.transform,"Game content",0,0,1920,1080);Content.anchorMin=Content.anchorMax=new Vector2(.5f,.5f);Content.pivot=new Vector2(.5f,.5f);Content.anchoredPosition=Vector2.zero;
            Content.gameObject.AddComponent<RiftSafeArea>();
            LoadArenas();
            background=RiftUI.Image(Content,"Chapter battlefield illustration",0,0,1920,1080,Color.white,arenaSprites[0]).rectTransform;
            world=RiftUI.Rect(Content,"Battlefield",0,0,1920,1080);
            RiftUI.Image(world,"Left shrine glow",10,300,300,300,new Color(1,.35f,.25f,.13f),RiftUI.Disc);
            RiftUI.Image(world,"Right shrine glow",1610,300,300,300,new Color(.3f,.7f,1,.16f),RiftUI.Disc);
            BuildShrineVisual();
            BuildDangerVisual();
            unitsRoot=RiftUI.Rect(world,"Heroines",0,0,1920,1080);
            effectsRoot=RiftUI.Rect(world,"Combat effects",0,0,1920,1080);
            hud=RiftUI.Rect(Content,"Battle HUD",0,0,1920,1080);
            screenFlash=RiftUI.Image(Content,"Impact flash",0,0,1920,1080,new Color(1,1,1,0));
            modal=RiftUI.Rect(Content,"Overlays",0,0,1920,1080);
            home=RiftUI.Rect(Content,"Title",0,0,1920,1080);
            touchOverlay=RiftUI.Rect(Content,"Touch sheets",0,0,1920,1080);touchOverlay.gameObject.SetActive(false);
            if(!FindFirstObjectByType<EventSystem>())new GameObject("UI input",typeof(EventSystem),typeof(StandaloneInputModule));
        }

        public void BeginRun(int faction,int? seed=null)
        {
            ClearChildren(home);ClearChildren(modal);ClearTroops();
            deployment.Clear();overviewOpen=homeOverlayOpen=workshopOpen=false;
            Battle=new RiftBattle(seed??(SmokeMode?1237:(int)(DateTime.UtcNow.Ticks&0x7fffffff)),faction,Profile.healthUpgrade,Profile.manaUpgrade,Guid.NewGuid().ToString("N"),Profile.DepartureLoadout());
            Profile.runs++;Profile.checkpoint=Battle.CaptureCheckpoint();RiftStorage.Save(Profile);
            StartBattleScreen();
        }

        public void ContinueRun()
        {
            Profile=RiftStorage.Load();
            Speed=Profile.preferredSpeed;sound.Muted=Profile.muted;
            if(!RiftBattle.IsValid(Profile.checkpoint))return;
            if(string.IsNullOrEmpty(Profile.checkpoint.runId))Profile.checkpoint.runId=Guid.NewGuid().ToString("N");
            ClearChildren(home);ClearChildren(modal);ClearTroops();Battle=RiftBattle.Restore(Profile.checkpoint);StartBattleScreen();
        }

        void StartBattleScreen()
        {
            home.gameObject.SetActive(false);world.gameObject.SetActive(true);hud.gameObject.SetActive(true);
            Paused=false;resultRecorded=false;showingRecruits=false;observedPhase=Battle.Phase;selectedHero=RiftData.FactionStart(Battle.Faction);tutorialTime=13;
            deployment.Clear();overviewOpen=homeOverlayOpen=workshopOpen=false;progressTracker.Bind(Battle);
            hitstop=shake=flash=0;SetArena();BuildHUD();RefreshHUD();
            if(Battle.Phase==BattlePhase.Reward)ShowReward();else Toast("제 "+Battle.Stage+"경계  ·  "+RiftData.StageNames[Battle.Stage-1],3.2f);
        }

        public void Deploy(int hero)
        {
            if(Battle==null || Paused || hero<0 || hero>=RiftData.HeroCount)return;
            selectedHero=hero;
            var result=deployment.Request(Battle,hero);
            if(result==DeploymentResult.Queued)Toast(RiftData.Heroes[hero].name+" 출격 예약 · 준비되면 한 번 출격합니다",2);
            else if(result==DeploymentResult.Cancelled)Toast("출격 예약을 취소했습니다",1.5f);
            else if(result==DeploymentResult.Unavailable)Toast("구간 보상에서 동료로 영입하세요",2);
            RefreshHUD();
        }
        public int CardHero(int slot){return RiftData.FactionStart(showingRecruits?1-Battle.Faction:Battle.Faction)+slot;}
        public void SwitchRoster()
        {
            if(Battle==null || Paused || Battle.Phase!=BattlePhase.Battle)return;
            deployment.EndHold();
            showingRecruits=!showingRecruits;selectedHero=CardHero(0);BuildHUD();RefreshHUD();
        }
        public void UseOrder(){if(Battle!=null&&!Paused&&!Battle.Order())Toast("아군을 먼저 출격시키거나 재사용을 기다리세요.",2);}
        public void UseShield(){if(Battle!=null&&!Paused&&!Battle.Shield())Toast("결계는 영력 30이 필요합니다.",2);}
        public void UseUltimate(){if(Battle!=null&&!Paused&&!Battle.Ultimate())Toast("타격과 처치로 각성 게이지를 100까지 채우세요.",2);}

        public void PickReward(int index)
        {
            if(Battle==null || !Battle.ChooseReward(index))return;
            deployment.Clear();
            ClearChildren(modal);ClearTroops();observedPhase=Battle.Phase;Profile.checkpoint=Battle.CaptureCheckpoint();RiftStorage.Save(Profile);
            SetArena();BuildHUD();Toast("제 "+Battle.Stage+"경계  ·  "+RiftData.ArenaNames[RiftData.ArenaForStage(Battle.Stage)]+"  ·  거점 30% 회복",3);tutorialTime=8;
        }

        void Update()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.08f);
            HandleInput();
            if(Battle!=null)
            {
                int dispatched=deployment.Advance(Battle,dt,Paused);if(dispatched>=0)selectedHero=dispatched;
                if(!Paused && Battle.Phase==BattlePhase.Battle)
                {if(hitstop>0)hitstop-=dt;else Battle.Tick(dt*Speed);}
                SyncProgress();ProcessCues();UpdateTroops(dt,!Paused);RefreshHUD();UpdatePracticeScreen();
                if(Battle.Phase!=observedPhase)
                {
                    observedPhase=Battle.Phase;
                    if(Battle.Phase==BattlePhase.Reward)ShowReward();
                    else if(Battle.Phase==BattlePhase.Victory || Battle.Phase==BattlePhase.Defeat)ShowResult();
                }
            }
            saveClock+=dt;if(progressDirty&&saveClock>=5)SaveProfile();
            UpdateEffects(dt);
            toastTime=Mathf.Max(0,toastTime-dt);bool showToast=toastTime>0&&(Battle==null||!Battle.Practice);
            if(toastText)toastText.gameObject.SetActive(showToast);
            if(toastBacking){toastBacking.gameObject.SetActive(showToast);toastBacking.rectTransform.anchoredPosition=toastText.rectTransform.anchoredPosition;}
            tutorialTime=Mathf.Max(0,tutorialTime-dt);if(stageHint)stageHint.gameObject.SetActive(tutorialTime>0&&!showToast&&(Battle==null||(!Battle.Practice&&Battle.WarningTime<=0)));
            shake=Mathf.Max(0,shake-dt*5);flash=Mathf.Max(0,flash-dt*2.8f);
            world.anchoredPosition=Profile.reducedMotion?Vector2.zero:UnityEngine.Random.insideUnitCircle*(shake*8);
            screenFlash.color=new Color(.78f,.9f,1,Profile.reducedMotion?flash*.12f:flash*.3f);
            if(home.gameObject.activeSelf)AnimateHome(dt);
        }

        void HandleInput()
        {
            if(mobileSheetOpen){if(Input.GetKeyDown(KeyCode.Escape))CloseMobileSheet();return;}
            if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
            if(workshopOpen){if(Input.GetKeyDown(KeyCode.Escape))CloseWorkshop();return;}
            if(Battle==null){if(homeOverlayOpen&&Input.GetKeyDown(KeyCode.Escape))CloseHomeOverlay();return;}
            if(Battle.PracticeComplete){if(Input.GetKeyDown(KeyCode.Escape))ReturnHome();return;}
            if((Battle.Phase==BattlePhase.Victory||Battle.Phase==BattlePhase.Defeat)&&Input.GetKeyDown(KeyCode.Escape)){ReturnHome();return;}
            if(overviewOpen){if(Input.GetKeyDown(KeyCode.B)||Input.GetKeyDown(KeyCode.Escape))CloseBuildOverview();return;}
            if(Input.GetKeyDown(KeyCode.B)){ShowBuildOverview();return;}
            if(Battle.Phase==BattlePhase.Reward)
            {
                for(int i=0;i<3;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){PickReward(i);return;}
                if(Input.GetKeyDown(KeyCode.R))RerollRewards();return;
            }
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {if(Battle.Phase==BattlePhase.Battle)TogglePause();return;}
            if(Paused || Battle.Phase!=BattlePhase.Battle)return;
            for(int i=0;i<RiftData.HeroesPerFaction;i++)
            {
                var key=(KeyCode)((int)KeyCode.Alpha1+i);int hero=CardHero(i);
                if(Input.GetKeyDown(key)){BeginDeploymentHold(hero);Deploy(hero);}
                if(Input.GetKeyUp(key))EndDeploymentHold(hero);
            }
            if(Input.GetKeyDown(KeyCode.Backspace))CancelDeploymentQueue();
            if(Input.GetKeyDown(KeyCode.C))SwitchRoster();
            if(Input.GetKeyDown(KeyCode.Q))UseOrder();if(Input.GetKeyDown(KeyCode.E))UseShield();if(Input.GetKeyDown(KeyCode.R))UseUltimate();
            if(Input.GetKeyDown(KeyCode.U)){if(!Battle.UpgradeEconomy())Toast("영맥 강화에 필요한 영력을 확인하세요.",2);}
            if(Input.GetKeyDown(KeyCode.Tab))ToggleSpeed();
        }
        void ToggleSpeed(){if(Battle!=null&&Battle.Practice)return;Speed=Speed==1?2:1;Profile.preferredSpeed=Speed;SaveProfile();}
        public void TogglePause()
        {
            if(Battle==null || Battle.Phase!=BattlePhase.Battle||Battle.PracticeComplete)return;
            deployment.EndHold();
            Paused=!Paused;if(Paused)ShowPause();else ClearChildren(modal);
        }
        public void ReturnHome()
        {
            SyncProgress();if(Battle!=null&&Battle.Phase==BattlePhase.Reward)Profile.checkpoint=Battle.CaptureCheckpoint();SaveProfile();
            deployment.Clear();overviewOpen=homeOverlayOpen=workshopOpen=false;Paused=false;Battle=null;ClearChildren(modal);ClearTroops();ShowHome();
        }
        public void Toast(string text,float duration=2.5f,int priority=0)
        {
            if(toastTime>0&&priority<toastPriority)return;
            toastPriority=priority;
            if(!toastText)return;toastText.text=text;toastTime=duration;toastText.gameObject.SetActive(true);
        }
        static void ClearChildren(Transform root)
        {for(int i=root.childCount-1;i>=0;i--){var child=root.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void OnApplicationQuit(){SyncProgress();if(Battle!=null&&(Battle.Phase==BattlePhase.Victory||Battle.Phase==BattlePhase.Defeat))RecordResult();if(Battle!=null&&Battle.Phase==BattlePhase.Reward)Profile.checkpoint=Battle.CaptureCheckpoint();SaveProfile();}
        void OnApplicationPause(bool pause){if(pause && Battle!=null && Battle.Phase==BattlePhase.Battle && !Paused)TogglePause();}
        void OnApplicationFocus(bool focus){if(!focus&&!SmokeMode)PauseForFocusLoss();}
    }
}
