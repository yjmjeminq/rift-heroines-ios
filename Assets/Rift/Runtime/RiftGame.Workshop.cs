using System;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        bool workshopOpen,workshopFromResult;
        int workshopTab,workshopHero,workshopPreview;
        string workshopNotice="";
        public bool WorkshopOpen {get{return workshopOpen;}}
        public void ShowWorkshop()
        {
            if(Battle!=null&&Battle.Phase!=BattlePhase.Victory&&Battle.Phase!=BattlePhase.Defeat)return;
            workshopNotice="";workshopPreview=0;
            workshopFromResult=Battle!=null;workshopOpen=homeOverlayOpen=true;home.gameObject.SetActive(false);DrawWorkshop();
        }
        public void CloseWorkshop()
        {
            workshopOpen=homeOverlayOpen=false;ClearChildren(modal);
            if(workshopFromResult&&Battle!=null)ShowResult();else ShowHome();
        }
        public void WorkshopSelectHero(int hero){workshopHero=Mathf.Clamp(hero,0,11);workshopPreview=0;DrawWorkshop();}
        public void WorkshopTab(int tab){workshopTab=Mathf.Clamp(tab,0,2);workshopPreview=0;DrawWorkshop();}
        void WorkshopAction(int action,int id)
        {
            string previous=JsonUtility.ToJson(Profile);var item=RiftWorkshop.Find(id);bool changed=false;
            if(action==0)changed=RiftWorkshop.Buy(Profile,id);
            else if(action==1)changed=RiftWorkshop.Equip(Profile,id);
            else if(action==2&&item!=null&&!RiftWorkshop.Owned(Profile,id)){Profile.workshopGoal=Profile.workshopGoal==id?0:id;changed=true;}
            else if(action==3)
            {
                if(workshopTab==0)Profile.equippedTraits[workshopHero]=0;
                else if(workshopTab==1)Profile.equippedAmulet=0;
                else {Profile.equippedStyles[workshopHero]=0;Profile.equippedFrame=0;}
                changed=true;
            }
            if(!changed){workshopNotice=item!=null&&RiftWorkshop.Owned(Profile,id)?"이미 해금한 항목입니다. 보유 항목은 추가 비용 없이 장착할 수 있습니다.":"보유 여부와 필요한 인연 조각을 확인하세요.";DrawWorkshop();return;}
            if(!RiftStorage.Save(Profile))
            {Profile=JsonUtility.FromJson<PlayerProfile>(previous);Profile.Normalize();workshopNotice="저장하지 못해 변경을 되돌렸습니다. 다시 시도해 주세요.";}
            else
            {
                if(action==0||action==1)workshopPreview=0;
                workshopNotice=action==0?(item.hero>=0&&item.kind==WorkshopKind.Trait?RiftData.Heroes[item.hero].name+" · ":"")+item.name+" 해금 완료! 새 출정에 장착됩니다.":action==1?item.name+" 장착 · 다음 출정을 준비했습니다.":action==2?"다음 출정의 수집 목표를 저장했습니다.":"기본 준비로 돌아왔습니다. 해금한 항목은 계속 보유합니다.";
                if(action==0)sound.Play(Cue.StageClear,50,true);
            }
            DrawWorkshop();
        }
        void DrawWorkshop()
        {
            ClearChildren(modal);var shade=RiftUI.Image(modal,"Workshop shade",0,0,1920,1080,new Color(.015f,.025f,.06f,.9f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,"Bond workshop",100,55,1720,970,new Color(.045f,.068f,.13f,.98f));
            RiftUI.Text(panel.transform,"Workshop title","인연 공방",42,867,1050,65,46,Color.white,true);
            RiftUI.Text(panel.transform,"Workshop currency","인연 조각  "+Profile.shards,1080,884,360,50,28,RiftUI.Gold,true,TextAnchor.MiddleRight);
            RiftUI.Button(panel.transform,"Close workshop",workshopFromResult?"전투 결과로 [Esc]":"거점으로 [Esc]",1460,885,215,50,new Color(.17f,.29f,.37f,.98f),CloseWorkshop,20);
            RiftUI.Text(panel.transform,"Workshop caption","해금은 영구 보유 · 장착 변경은 새 출정부터 적용 · 이어하기는 저장된 준비 유지",42,820,1625,42,22,RiftUI.Muted);
            string[] tabs={"01  캐릭터 특성","02  출정 부적","03  외형 수집"};
            for(int i=0;i<3;i++){int tab=i;RiftUI.Button(panel.transform,"Workshop tab "+i,tabs[i],42+i*550,755,532,55,i==workshopTab?new Color(.3f,.29f,.22f,.98f):new Color(.13f,.19f,.29f,.98f),()=>WorkshopTab(tab),25);}
            BuildWorkshopFigure(panel.transform);
            int[] ids=workshopTab==0?new[]{100+workshopHero*2,101+workshopHero*2}:workshopTab==1?new[]{200,201,202}:new[]{300+workshopHero,400,401};
            for(int i=0;i<ids.Length;i++)BuildWorkshopItem(panel.transform,ids[i],580+i*(ids.Length==2?558:367),300,ids.Length==2?530:350,410);
            string reset=workshopTab==0?RiftData.Heroes[workshopHero].name+" · 기본 특성 장착":workshopTab==1?"출정 부적 해제":"선택한 동료의 기본 연출 · 기본 액자로";
            RiftUI.Button(panel.transform,"Workshop reset",reset,580,228,1084,49,new Color(.12f,.2f,.29f,.98f),()=>WorkshopAction(3,0),21);
            RiftUI.Surface(panel.transform,"Workshop goal backing",580,116,1084,91,new Color(.10f,.15f,.245f,.98f));
            RiftUI.Text(panel.transform,"Workshop goal",RiftWorkshop.Goal(Profile),599,147,1046,49,22,RiftUI.Gold,true);
            var goal=RiftWorkshop.Find(Profile.workshopGoal);var progress=RiftUI.Bar(panel.transform,"Workshop goal progress",600,133,1044,9,RiftUI.Gold);progress.fillAmount=goal==null?0:Mathf.Min(1,Profile.shards/(float)goal.cost);
            RiftUI.Text(panel.transform,"Workshop notice",string.IsNullOrEmpty(workshopNotice)?"특성은 캐릭터마다 1개, 부적은 출정마다 1개 · 외형 수집은 능력치에 영향을 주지 않습니다.":workshopNotice,42,37,1632,57,23,RiftUI.Gold,false,TextAnchor.MiddleCenter);
            if(TouchLayout)BuildMobileWorkshop(panel.transform);
        }
        void BuildWorkshopFigure(Transform panel)
        {
            int hero=workshopHero;var definition=RiftData.Heroes[hero];var root=RiftUI.Surface(panel,"Workshop heroine",42,116,510,620,new Color(.07f,.11f,.19f,.98f));
            RiftUI.Text(root.transform,"Workshop heroine name",definition.name,70,553,370,51,37,Color.white,true,TextAnchor.MiddleCenter);
            RiftUI.Button(root.transform,"Workshop previous hero","‹",14,556,48,44,new Color(.13f,.2f,.3f,.98f),()=>WorkshopSelectHero((workshopHero+11)%12),31);
            RiftUI.Button(root.transform,"Workshop next hero","›",448,556,48,44,new Color(.13f,.2f,.3f,.98f),()=>WorkshopSelectHero((workshopHero+1)%12),31);
            var preview=RiftWorkshop.Find(workshopPreview);int style=Profile.equippedStyles[hero],frame=Profile.equippedFrame;
            if(preview!=null){if(preview.kind==WorkshopKind.Style)style=1;else if(preview.kind==WorkshopKind.Frame)frame=preview.choice;}
            string state=preview!=null?"미리보기 · "+preview.name:RiftWorkshop.Trait(hero,Profile.equippedTraits[hero]).name+" · "+RiftWorkshop.AmuletNames[Profile.equippedAmulet];
            RiftUI.Text(root.transform,"Workshop preview state",state,16,510,478,40,21,RiftUI.Gold,false,TextAnchor.MiddleCenter);
            Color aura=style>0?RiftUI.Hex("#A9E8FF"):RiftUI.Hex(definition.color);
            RiftUI.Image(root.transform,"Workshop character aura",70,184,370,310,new Color(aura.r,aura.g,aura.b,.13f),RiftUI.Disc);
            var figure=RiftUI.Image(root.transform,"Workshop full body",90,173,330,330*(1-frameFooters[hero][0]),Color.white,frames[hero][0]);figure.preserveAspect=true;
            var pulseRoot=RiftUI.Rect(root.transform,"Workshop effect preview",95,205,320,250);var pulse=pulseRoot.gameObject.AddComponent<RiftWorkshopPulse>();pulse.Style=style;pulse.Tint=aura;
            DecorateFrame(root.transform,510,620,frame);
            for(int i=0;i<12;i++)
            {
                int selected=i;float x=17+(i%6)*80,y=12+(1-i/6)*73;
                var button=RiftUI.Button(root.transform,"Workshop hero "+i,"",x,y,74,66,i==hero?new Color(.3f,.29f,.22f,.98f):new Color(.12f,.18f,.28f,.98f),()=>WorkshopSelectHero(selected));
                var portrait=RiftUI.Image(button.transform,"Workshop hero portrait",8,19,58,45,Color.white,portraits[i]);portrait.preserveAspect=true;
                RiftUI.Text(button.transform,"Workshop hero label",RiftData.Heroes[i].name,0,0,74,23,16,Color.white,true,TextAnchor.MiddleCenter);
            }
        }
        void BuildWorkshopItem(Transform panel,int id,float x,float y,float width,float height)
        {
            var item=RiftWorkshop.Find(id);bool owned=RiftWorkshop.Owned(Profile,id),equipped=RiftWorkshop.Equipped(Profile,id);
            var card=RiftUI.Surface(panel,"Workshop item "+id,x,y,width,height,new Color(.10f,.15f,.245f,.98f));
            RiftUI.Text(card.transform,"Workshop item name",item.name,20,343,width-40,49,width>400?30:27,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            RiftUI.Icon(card.transform,item.glyph,width*.5f-35,260,70,65,equipped?RiftUI.Ice:RiftUI.Gold);
            RiftUI.Text(card.transform,"Workshop item effect",item.description,24,160,width-48,97,24,Color.white,false,TextAnchor.MiddleCenter);
            if(item.kind==WorkshopKind.Style||item.kind==WorkshopKind.Frame)
                RiftUI.Button(card.transform,"Preview item "+id,"연출 미리보기",24,112,width-48,39,new Color(.16f,.23f,.32f,.98f),()=>{workshopPreview=id;DrawWorkshop();},21);
            else RiftUI.Text(card.transform,"Workshop item state",equipped?"현재 장착됨":owned?"영구 해금 완료":"장점과 대가를 비교해 선택하세요",24,111,width-48,41,20,equipped?RiftUI.Ice:RiftUI.Muted,false,TextAnchor.MiddleCenter);
            string action=equipped?"장착 중":owned?"장착하기":item.cost+" 조각 · 해금하고 장착";
            var buy=RiftUI.Button(card.transform,"Workshop buy "+id,action,24,51,width-48,53,new Color(.2f,.29f,.32f,.98f),()=>WorkshopAction(owned?1:0,id),22);buy.interactable=!equipped&&(owned||Profile.shards>=item.cost);
            var wish=RiftUI.Button(card.transform,"Workshop wish "+id,Profile.workshopGoal==id?"수집 목표 해제":"수집 목표 지정",24,10,width-48,34,new Color(.14f,.2f,.3f,.98f),()=>WorkshopAction(2,id),19);wish.interactable=!owned;
        }
        void DecorateFrame(Transform parent,float width,float height,int frame)
        {
            if(frame==0)return;var rect=RiftUI.Rect(parent,"Equipped portrait frame",0,0,width,height);var graphic=rect.gameObject.AddComponent<RiftPortraitFrame>();graphic.Style=frame;graphic.raycastTarget=false;
        }
    }
    public sealed class RiftPortraitFrame:RiftGlyph
    {
        public int Style;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh;vh.Clear();center=rectTransform.rect.center;unit=1;float x=rectTransform.rect.width*.5f-3,y=rectTransform.rect.height*.5f-3,l=Mathf.Min(25,x*.25f);
            Color a=Style==1?RiftUI.Gold:RiftUI.Ice,b=Style==1?RiftUI.Gold:RiftUI.Hex("#D7A0FF");
            for(int side=-1;side<=1;side+=2)for(int vertical=-1;vertical<=1;vertical+=2)
            {var p=new Vector2(x*side,y*vertical);Color tint=side==vertical?a:b;Line(p,p-new Vector2(l*side,0),2.2f,tint);Line(p,p-new Vector2(0,l*vertical),2.2f,tint);}
            Line(new Vector2(-9,y),new Vector2(0,y-7),2,a);Line(new Vector2(0,y-7),new Vector2(9,y),2,a);
        }
    }
    public sealed class RiftWorkshopPulse:MonoBehaviour
    {
        public int Style;public Color Tint;RiftEffectGraphic effect;float clock;
        void Start(){effect=gameObject.AddComponent<RiftEffectGraphic>();effect.raycastTarget=false;effect.color=new Color(Tint.r,Tint.g,Tint.b,.6f);effect.Variant=Style>0?7:3;effect.Duration=.6f;}
        void Update(){if(!effect)return;clock+=Time.unscaledDeltaTime;effect.Age=clock%1.8f;effect.Refresh();}
    }
}
