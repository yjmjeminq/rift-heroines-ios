using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        sealed class UnitVisual
        {
            public RectTransform root;
            public Image image,health,ward,shadow;
            public Text name;
            public Material material;
            public float flash,nextNumber,nextHeal,nextBolt,death=-1;
            public float lastX,facing,recoil;
            public int hero,id;
            public bool ally;
        }
        sealed class EffectVisual
        {
            public RectTransform root;
            public RiftEffectGraphic graphic;
            public float age,duration;
        }
        sealed class NumberVisual {public Text text;public float age;public Vector2 origin;}
        readonly Dictionary<int,UnitVisual> troopViews=new Dictionary<int,UnitVisual>();
        readonly List<int> deadViews=new List<int>();
        readonly List<UnitVisual> depthViews=new List<UnitVisual>();
        readonly List<EffectVisual> effects=new List<EffectVisual>();
        readonly Stack<EffectVisual> effectPool=new Stack<EffectVisual>();
        readonly List<NumberVisual> numbers=new List<NumberVisual>();
        readonly Stack<NumberVisual> numberPool=new Stack<NumberVisual>();
        float visualClock,lastStop,lastChain=-10;

        Vector2 Point(float x,int id=0){return new Vector2(170+x*15.8f,299+(id%3)*21);}
        UnitVisual CreateTroop(Fighter f)
        {
            var v=new UnitVisual {hero=f.hero,id=f.id,ally=f.ally,lastX=f.x,facing=f.ally?1:-1};
            v.root=RiftUI.Rect(unitsRoot,RiftData.Heroes[f.hero].name+" "+f.id,0,0,230,250);
            v.shadow=RiftUI.Image(v.root,"Shadow",55,-2,120,17,new Color(.015f,.02f,.055f,.43f),RiftUI.Disc);
            RiftUI.Image(v.root,"Team marker",84,0,62,10,f.ally?new Color(.4f,1,.82f,.6f):new Color(1,.35f,.4f,.7f),RiftUI.Disc);
            v.ward=RiftUI.Image(v.root,"Ward aura",40,12,150,183,new Color(.4f,.8f,1,.22f),RiftUI.Disc);v.ward.gameObject.SetActive(false);
            float size=f.boss?350:300;
            v.ward.rectTransform.sizeDelta=new Vector2(150,size*.82f);
            v.image=RiftUI.Image(v.root,"New animated character",(230-size)*.5f,0,size,size*(1-frameFooters[f.hero][0]),Color.white,frames[f.hero][0]);
            v.image.rectTransform.pivot=new Vector2(.5f,0);v.image.rectTransform.anchoredPosition=new Vector2(115,0);
            var shader=Resources.Load<Shader>("Rift/Flash");if(shader){v.material=new Material(shader);v.image.material=v.material;}
            v.health=RiftUI.Bar(v.root,"Health",77,size*.96f,76,6,f.ally?RiftUI.Hex("#7DE0CA"):RiftUI.Coral);
            v.name=RiftUI.Text(v.root,"Name",f.boss?"각성 · "+RiftData.Heroes[f.hero].name:"",20,size*.96f+10,190,25,16,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            troopViews.Add(f.id,v);return v;
        }
        void UpdateTroops(float dt,bool animate)
        {
            if(Battle==null)return;
            if(animate)visualClock+=dt*Speed;
            foreach(var f in Battle.Fighters)
            {
                UnitVisual v;if(!troopViews.TryGetValue(f.id,out v))v=CreateTroop(f);
                if(v.death>=0)continue;
                Vector2 p=Point(f.x,f.id);v.root.anchoredPosition=p-new Vector2(115,0);
                int frame=0;
                if(f.animation>.34f)frame=3;else if(f.animation>.12f)frame=4;else if(f.animation>0)frame=5;
                else if(f.moving)frame=1+((int)(visualClock*10+f.id)%2);
                v.image.sprite=frames[f.hero][frame];
                float imageSize=f.boss?350:300;
                v.image.rectTransform.sizeDelta=new Vector2(imageSize,imageSize*(1-frameFooters[f.hero][frame]));
                float scale=1+(!f.moving&&f.animation==0?Mathf.Sin(visualClock*3+f.id)*.008f:0);
                if(f.moving&&Mathf.Abs(f.x-v.lastX)>.001f)v.facing=Mathf.Sign(f.x-v.lastX);
                else if(f.windup>0||f.animation>.12f){var target=Battle.Fighters.Find(t=>t.id==f.pendingTarget&&t.Alive);if(target!=null)v.facing=Mathf.Sign(target.x-f.x);}
                v.lastX=f.x;v.recoil=Mathf.MoveTowards(v.recoil,0,dt*45);
                v.image.rectTransform.anchoredPosition=new Vector2(115-v.facing*v.recoil,0);
                v.image.rectTransform.localScale=new Vector3(v.facing*scale,scale,1);
                v.image.color=f.slowTime>0?new Color(.65f,.86f,1):f.stun>0?new Color(.8f,.84f,.9f):Color.white;
                v.flash=Mathf.Max(0,v.flash-dt*12);if(v.material)v.material.SetFloat("_Flash",v.flash);
                v.health.fillAmount=f.health/f.maxHealth;
                v.health.transform.parent.gameObject.SetActive(f.health<f.maxHealth || f.boss);
                v.ward.gameObject.SetActive(f.shield>0);
            }
            deadViews.Clear();
            foreach(var pair in troopViews)
            {
                var v=pair.Value;if(v.death<0)continue;
                v.death+=dt;var color=v.image.color;color.a=Mathf.Clamp01(1-v.death/.45f);v.image.color=color;
                v.root.anchoredPosition+=new Vector2((v.ally?-1:1)*dt*35,-dt*24);v.image.rectTransform.localRotation=Quaternion.Euler(0,0,(v.ally?1:-1)*v.death*25);
                v.health.transform.parent.gameObject.SetActive(false);v.name.gameObject.SetActive(false);v.ward.gameObject.SetActive(false);
                if(v.death>.45f)deadViews.Add(pair.Key);
            }
            foreach(int id in deadViews){var v=troopViews[id];if(v.material)Destroy(v.material);Destroy(v.root.gameObject);troopViews.Remove(id);}
            depthViews.Clear();foreach(var v in troopViews.Values)depthViews.Add(v);
            depthViews.Sort((a,b)=>b.root.anchoredPosition.y.CompareTo(a.root.anchoredPosition.y));
            foreach(var v in depthViews)v.root.SetAsLastSibling();
        }

        void ProcessCues()
        {
            if(Battle==null)return;
            foreach(var cue in Battle.Cues)
            {
                ObserveExperienceCue(cue);
                var color=RiftUI.Hex(RiftData.Heroes[Mathf.Clamp(cue.hero,0,RiftData.HeroCount-1)].color);
                bool starStyle=cue.ally&&Battle.StyleChoice(Mathf.Clamp(cue.hero,0,11))>0;
                if(starStyle)color=Color.Lerp(color,RiftUI.Hex("#A9E8FF"),.6f);
                Vector2 at=Point(cue.x,cue.target!=0?cue.target:cue.source)+new Vector2(0,155);
                sound.Play(cue.kind,cue.x,cue.critical,cue.hero);
                UnitVisual v;
                switch(cue.kind)
                {
                    case Cue.Spawn:color.a=.65f;Effect(Point(cue.x,cue.source)+new Vector2(0,40),95,color,starStyle?7:0,.34f);break;
                    case Cue.Windup:
                        if(RiftData.Heroes[cue.hero].role==CombatRole.Mystic && cue.target!=0)
                        {
                            Effect(Point(cue.x,cue.source)+new Vector2(0,185),42,new Color(color.r,color.g,color.b,.6f),5,.16f);
                        }
                        break;
                    case Cue.Hit:case Cue.BaseHit:
                        bool showHit=true;
                        if(troopViews.TryGetValue(cue.target,out v))
                        {
                            v.flash=.65f;if(cue.amount>0)v.recoil=cue.critical?7:4;
                            showHit=Time.unscaledTime>=v.nextNumber;
                            if(showHit)v.nextNumber=Time.unscaledTime+(cue.critical?.10f:.18f);
                        }
                        if(showHit)
                        {
                            var power=RiftData.Heroes[cue.hero].power;
                            UnitVisual caster;
                            if(cue.source>0&&RiftData.Heroes[cue.hero].role==CombatRole.Mystic&&troopViews.TryGetValue(cue.source,out caster)&&Time.unscaledTime>=caster.nextBolt)
                            {caster.nextBolt=Time.unscaledTime+.12f;var source=Battle.Fighters.Find(f=>f.id==cue.source);if(source!=null)Beam(Point(source.x,source.id)+new Vector2(0,185),at,color);}
                            Effect(at,cue.critical?112:78,color,RiftFeedback.HitStyle(power),cue.critical?.22f:.17f);
                            if(starStyle)Effect(at+new Vector2(0,14),48,new Color(.8f,.94f,1,.65f),7,.22f);
                            if(cue.amount>=1)Number(at+new Vector2(UnityEngine.Random.Range(-13,14),56),Mathf.CeilToInt(cue.amount).ToString(),cue.critical?RiftUI.Gold:cue.ally?Color.white:RiftUI.Coral,cue.critical?31:22);
                        }
                        if(cue.critical || cue.kind==Cue.BaseHit)shake=Mathf.Max(shake,.28f);
                        if(cue.amount>42 && Time.unscaledTime-lastStop>.16f){hitstop=.018f;lastStop=Time.unscaledTime;}
                        break;
                    case Cue.Death:
                        if(troopViews.TryGetValue(cue.target,out v))v.death=0;
                        Effect(at,125,color,1,.42f);
                        if(cue.critical) {shake=.85f;flash=.25f;Toast("각성 대장 격파!",2);}
                        break;
                    case Cue.Heal:
                        if(troopViews.TryGetValue(cue.target,out v))
                        {
                            if(Time.unscaledTime<v.nextHeal)break;
                            v.nextHeal=Time.unscaledTime+.8f;
                        }
                        Effect(at-new Vector2(0,40),65,new Color(.6f,1,.76f,.5f),5,.32f);
                        if(cue.amount>0)Number(at+new Vector2(0,48),"+"+Mathf.CeilToInt(cue.amount),new Color(.6f,1,.76f),20);
                        break;
                    case Cue.Ultimate:
                        hitstop=.075f;shake=1;flash=.75f;
                        if(cue.ally){color.a=.8f;for(int i=0;i<3;i++)Effect(new Vector2(620+i*330,480),290,color,4,.48f);}
                        else
                        {
                            var technique=(BossTechnique)(int)cue.amount;
                            for(int i=0;i<3;i++)Effect(Point(Mathf.Clamp(cue.x+(i-1)*Battle.WarningRadius*.6f,6,94))+new Vector2(0,technique==BossTechnique.BacklineRain?185:55),technique==BossTechnique.Reinforcements?180:310,RiftUI.Coral,technique==BossTechnique.BacklineRain?1:0,.7f);
                        }
                        Toast(cue.ally?(Battle.Faction==0?"나루토 비기  ·  백호의 술":"블리치 비기  ·  영압 해방"):RiftData.Heroes[cue.hero].name+" · "+RiftData.BossTechniqueNames[(int)cue.amount],2.3f,2);break;
                    case Cue.Order:Toast("전원 진격! "+Mathf.RoundToInt(Battle.OrderTime)+"초 동안 이동·공격 속도 +45%",2,1);Effect(Point(Battle.CenterControl)+new Vector2(0,65),170,RiftUI.Gold,4,.34f);break;
                    case Cue.Shield:Toast("월광 결계  ·  5초 동안 아군 보호",2,1);Effect(Point(Battle.CenterControl)+new Vector2(0,65),185,new Color(.6f,.85f,1,.5f),0,.4f);break;
                    case Cue.Boss:Toast("각성 대장 출현  ·  "+RiftData.Heroes[cue.hero].name,3,2);shake=.7f;Effect(Point(cue.x,cue.source)+new Vector2(0,100),245,color,0,.65f);break;
                    case Cue.Warning:Toast(RiftData.BossTechniqueNames[(int)cue.amount]+" · 바닥 예고와 결계 준비를 확인하세요",2.5f,3);break;
                    case Cue.Bond:
                        Toast("조합 활성  ·  "+RiftData.Bonds[(int)cue.amount].description,2.2f);Effect(Point(cue.x)+new Vector2(0,85),180,RiftUI.Gold,0,.55f);break;
                    case Cue.Capture:
                        Toast(cue.ally?"제단 확보! 영력 +"+Mathf.RoundToInt(cue.amount)+" · 각성 +20 · "+Mathf.CeilToInt(Battle.ShrineBuffTime)+"초 돌파":"적이 제단을 차지했습니다. 결계로 버티고 반격하세요.",2.6f);
                        Effect(Point(50)+new Vector2(0,100),225,cue.ally?RiftUI.Gold:RiftUI.Coral,0,.55f);shake=Mathf.Max(shake,.35f);break;
                    case Cue.PerfectGuard:
                        hitstop=.07f;shake=.7f;flash=.35f;Toast("절묘한 결계!  비기 차단 · 대장 약점 노출",3,4);
                        Effect(Point(cue.x)+new Vector2(0,130),310,RiftUI.Gold,3,.5f);Number(new Vector2(960,570),"PERFECT GUARD",RiftUI.Gold,40);break;
                    case Cue.Chain:
                        if(Time.unscaledTime-lastChain>.65f){lastChain=Time.unscaledTime;Effect(at,170,RiftUI.Gold,3,.35f);Number(at+new Vector2(0,100),(int)cue.amount+" CHAIN!",RiftUI.Gold,34);shake=Mathf.Max(shake,.45f);}break;
                    case Cue.StageClear:shake=.2f;flash=.25f;break;
                }
            }
            Battle.Cues.Clear();
        }

        EffectVisual Effect(Vector2 at,float size,Color color,int variant,float duration)
        {
            if(effects.Count>=RiftFeedback.MaxEffects)
            {var oldest=effects[0];oldest.root.gameObject.SetActive(false);effects.RemoveAt(0);effectPool.Push(oldest);}
            EffectVisual v;
            if(effectPool.Count>0)v=effectPool.Pop();
            else
            {
                var r=RiftUI.Rect(effectsRoot,"Pooled impact",0,0,100,100);var g=r.gameObject.AddComponent<RiftEffectGraphic>();g.raycastTarget=false;v=new EffectVisual{root=r,graphic=g};
            }
            v.root.gameObject.SetActive(true);v.root.sizeDelta=new Vector2(size,size);v.root.anchoredPosition=at-new Vector2(size*.5f,size*.5f);
            v.root.pivot=Vector2.zero;v.root.localRotation=Quaternion.identity;
            v.age=0;v.duration=duration;v.graphic.color=color;v.graphic.Variant=variant;v.graphic.Duration=duration;v.graphic.Age=0;v.graphic.Refresh();effects.Add(v);return v;
        }
        void Beam(Vector2 from,Vector2 to,Color color)
        {
            color.a=.55f;var v=Effect(from,8,color,6,.095f);var delta=to-from;
            v.root.pivot=new Vector2(0,.5f);v.root.sizeDelta=new Vector2(delta.magnitude,8);v.root.anchoredPosition=from;v.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        void Number(Vector2 at,string label,Color color,int size)
        {
            // Bound text allocations when a large area attack hits a crowded front.
            if(numbers.Count>=RiftFeedback.MaxNumbers)return;
            NumberVisual n;
            if(numberPool.Count>0)n=numberPool.Pop();else
            {n=new NumberVisual { text=RiftUI.Text(effectsRoot,"Pooled number","",0,0,160,55,size,color,true,TextAnchor.MiddleCenter) };n.text.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.8f);}
            float width=Mathf.Clamp(label.Length*size*.72f,160,600);n.text.rectTransform.sizeDelta=new Vector2(width,55);
            n.text.gameObject.SetActive(true);n.text.text=label;n.text.color=color;n.text.fontSize=size;n.origin=at-new Vector2(width*.5f,0);n.age=0;n.text.rectTransform.anchoredPosition=n.origin;n.text.transform.SetAsLastSibling();numbers.Add(n);
        }
        void UpdateEffects(float dt)
        {
            for(int i=effects.Count-1;i>=0;i--)
            {
                var e=effects[i];e.age+=dt;e.graphic.Age=e.age;
                e.graphic.Refresh();if(e.age>=e.duration){e.root.gameObject.SetActive(false);effects.RemoveAt(i);effectPool.Push(e);}
            }
            for(int i=numbers.Count-1;i>=0;i--)
            {
                var n=numbers[i];n.age+=dt;n.text.rectTransform.anchoredPosition=n.origin+new Vector2(0,n.age*66);var c=n.text.color;c.a=Mathf.Clamp01(1-n.age/.65f);n.text.color=c;
                if(n.age>=.65f){n.text.gameObject.SetActive(false);numbers.RemoveAt(i);numberPool.Push(n);}
            }
        }
        void ClearTroops()
        {
            foreach(var v in troopViews.Values){if(v.material)Destroy(v.material);Destroy(v.root.gameObject);}troopViews.Clear();
            foreach(var e in effects){e.root.gameObject.SetActive(false);effectPool.Push(e);}effects.Clear();
            foreach(var n in numbers){n.text.gameObject.SetActive(false);numberPool.Push(n);}numbers.Clear();
        }
    }
}
