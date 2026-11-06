using System;
using System.Collections.Generic;

namespace RiftHeroines
{
    public enum WorkshopKind { Trait,Amulet,Style,Frame }
    public sealed class HeroTrait
    {
        public string name,description;
        public float health=1,damage=1,interval=1,speed=1,healing=1,ward=1,boss=1,normal=1,slow=1;
        public float splash,range,stun,push,mark;
        public HeroTrait(string name,string description){this.name=name;this.description=description;}
    }
    public sealed class WorkshopItem
    {
        public int id,hero,choice,cost;public string name,description,glyph;public WorkshopKind kind;
        public WorkshopItem(int id,WorkshopKind kind,int hero,int choice,int cost,string name,string description,string glyph)
        {this.id=id;this.kind=kind;this.hero=hero;this.choice=choice;this.cost=cost;this.name=name;this.description=description;this.glyph=glyph;}
    }
    [Serializable]
    public sealed class RiftLoadout
    {
        public int[] traits=new int[12],styles=new int[12];
        public int amulet,frame;
        public static bool IsValid(RiftLoadout value)
        {
            if(value==null)return true;
            if(value.traits==null||value.styles==null||value.traits.Length!=12||value.styles.Length!=12||value.amulet<0||value.amulet>3||value.frame<0||value.frame>2)return false;
            for(int i=0;i<12;i++)if(value.traits[i]<0||value.traits[i]>2||value.styles[i]<0||value.styles[i]>1)return false;
            return true;
        }
        public static RiftLoadout Copy(RiftLoadout value)
        {if(value==null)return new RiftLoadout();if(!IsValid(value))throw new ArgumentException("Invalid departure loadout");return new RiftLoadout{traits=(int[])value.traits.Clone(),styles=(int[])value.styles.Clone(),amulet=value.amulet,frame=value.frame};}
    }
    public static class RiftWorkshop
    {
        public static readonly HeroTrait Basic=new HeroTrait("기본 전술","원래의 기술과 능력치로 전투합니다.");
        public static readonly HeroTrait[] Traits={
            new HeroTrait("집중 앵화","보스 피해 +25%\n일반 적 피해 -10%") {boss=1.25f,normal=.9f},
            new HeroTrait("광역 앵화","광역 범위 +4\n공격 간격 +12%") {splash=4,interval=1.12f},
            new HeroTrait("선봉 백호","최대 체력 +18%\n치유량 -25%") {health=1.18f,healing=.75f},
            new HeroTrait("의료 백호","치유량 +25%\n공격력 -12%") {healing=1.25f,damage=.88f},
            new HeroTrait("원거리 공장","공격 사거리 +4\n공격력 -10%") {range=4,damage=.9f},
            new HeroTrait("파쇄 쌍사권","보스 피해 +28%\n광역 범위 -2") {boss=1.28f,splash=-2},
            new HeroTrait("정밀 심전신","심전신 경직 +0.2초\n공격 간격 +15%") {stun=.2f,interval=1.15f},
            new HeroTrait("감지 연결","치유량 +35%\n공격력 -15%") {healing=1.35f,damage=.85f},
            new HeroTrait("폭풍 부채","광역 범위 +4\n공격 간격 +14%") {splash=4,interval=1.14f},
            new HeroTrait("압축 바람","밀쳐내기 거리 +1\n공격력 -10%") {push=1,damage=.9f},
            new HeroTrait("집속 종이","보스 피해 +25%\n광역 범위 -3") {boss=1.25f,splash=-3},
            new HeroTrait("종이 장막","최대 체력 +18%\n공격력 -10%") {health=1.18f,damage=.9f},
            new HeroTrait("회묘의 우리","회묘 경직 +0.16초\n공격 간격 +15%") {stun=.16f,interval=1.15f},
            new HeroTrait("재의 파도","광역 범위 +3\n공격력 -10%") {splash=3,damage=.9f},
            new HeroTrait("순신의 추격","이동 속도 +18%\n최대 체력 -12%") {speed=1.18f,health=.88f},
            new HeroTrait("집중 순홍","보스 피해 +25%\n일반 적 피해 -10%") {boss=1.25f,normal=.9f},
            new HeroTrait("회도의 온기","치유량 +25%\n공격력 -12%") {healing=1.25f,damage=.88f},
            new HeroTrait("검무의 호흡","공격 간격 -13%\n치유량 -25%") {interval=.87f,healing=.75f},
            new HeroTrait("빙결 영역","광역 범위 +3\n공격 간격 +15%") {splash=3,interval=1.15f},
            new HeroTrait("백설 갑주","최대 체력 +18%\n감속 지속 -35%") {health=1.18f,slow=.65f},
            new HeroTrait("치유 육화","치유량 +25%\n기술 보호막 -30%") {healing=1.25f,ward=.7f},
            new HeroTrait("수호 육화","기술 보호막 +50%\n치유량 -20%") {ward=1.5f,healing=.8f},
            new HeroTrait("추격 순보","이동 속도 +18%\n최대 체력 -12%") {speed=1.18f,health=.88f},
            new HeroTrait("집속 작봉","표식 재타격 2.2 → 2.8배\n공격력 -10%") {mark=.6f,damage=.9f}
        };
        public static readonly string[] AmuletNames={"부적 없음","개척의 월석","되받는 월석","진격의 월석"};
        public static readonly WorkshopItem[] Items=BuildItems();
        static WorkshopItem[] BuildItems()
        {
            var items=new List<WorkshopItem>();
            for(int hero=0;hero<12;hero++)for(int choice=1;choice<=2;choice++)
            {var t=Traits[hero*2+choice-1];items.Add(new WorkshopItem(100+hero*2+choice-1,WorkshopKind.Trait,hero,choice,60,t.name,t.description,"blade"));}
            items.Add(new WorkshopItem(200,WorkshopKind.Amulet,-1,1,80,AmuletNames[1],"아군 제단 점령 속도 +20%\n제단 공격 버프 12 → 9초","target"));
            items.Add(new WorkshopItem(201,WorkshopKind.Amulet,-1,2,80,AmuletNames[2],"절묘한 결계의 각성 +8\nE 결계 보호막 -25%","shield"));
            items.Add(new WorkshopItem(202,WorkshopKind.Amulet,-1,3,80,AmuletNames[3],"진격 지속 시간 +2초\n기본 영력 회복 -0.7/s","wind"));
            for(int hero=0;hero<12;hero++)items.Add(new WorkshopItem(300+hero,WorkshopKind.Style,hero,1,40,RiftData.Heroes[hero].name+" · 별빛","별빛 색의 타격·소환과 작은 빛 파편\n전투 능력치는 그대로 유지","spark"));
            items.Add(new WorkshopItem(400,WorkshopKind.Frame,-1,1,50,"월광 액자","모든 동료 카드에 금빛 달의 테두리\n전투 능력치는 그대로 유지","moon"));
            items.Add(new WorkshopItem(401,WorkshopKind.Frame,-1,2,70,"성운 액자","모든 동료 카드에 청록·보라 테두리\n전투 능력치는 그대로 유지","bond"));
            return items.ToArray();
        }
        public static WorkshopItem Find(int id){return Array.Find(Items,item=>item.id==id);}
        public static HeroTrait Trait(int hero,int choice){return hero>=0&&hero<12&&choice>=1&&choice<=2?Traits[hero*2+choice-1]:Basic;}
        public static bool Owned(PlayerProfile p,int id)
        {
            var item=Find(id);if(item==null)return false;
            switch(item.kind){case WorkshopKind.Trait:return (p.traitUnlocks[item.hero]&(1<<(item.choice-1)))!=0;case WorkshopKind.Amulet:return (p.amuletUnlocks&(1<<(item.choice-1)))!=0;case WorkshopKind.Style:return p.styleUnlocks[item.hero]>0;default:return (p.frameUnlocks&(1<<(item.choice-1)))!=0;}
        }
        public static bool Equipped(PlayerProfile p,int id)
        {var i=Find(id);if(i==null)return false;switch(i.kind){case WorkshopKind.Trait:return p.equippedTraits[i.hero]==i.choice;case WorkshopKind.Amulet:return p.equippedAmulet==i.choice;case WorkshopKind.Style:return p.equippedStyles[i.hero]==i.choice;default:return p.equippedFrame==i.choice;}}
        public static bool Equip(PlayerProfile p,int id)
        {
            if(!Owned(p,id))return false;var i=Find(id);
            switch(i.kind){case WorkshopKind.Trait:p.equippedTraits[i.hero]=i.choice;break;case WorkshopKind.Amulet:p.equippedAmulet=i.choice;break;case WorkshopKind.Style:p.equippedStyles[i.hero]=i.choice;break;default:p.equippedFrame=i.choice;break;}return true;
        }
        public static bool Buy(PlayerProfile p,int id)
        {
            var i=Find(id);if(i==null||Owned(p,id)||p.shards<i.cost)return false;
            p.shards-=i.cost;p.workshopSpent+=i.cost;
            switch(i.kind){case WorkshopKind.Trait:p.traitUnlocks[i.hero]|=1<<(i.choice-1);break;case WorkshopKind.Amulet:p.amuletUnlocks|=1<<(i.choice-1);break;case WorkshopKind.Style:p.styleUnlocks[i.hero]=1;break;default:p.frameUnlocks|=1<<(i.choice-1);break;}
            Equip(p,id);if(p.workshopGoal==id)p.workshopGoal=0;return true;
        }
        public static string Goal(PlayerProfile p)
        {var item=Find(p.workshopGoal);return item==null?"원하는 항목의 ‘목표 지정’을 눌러 다음 출정의 목표를 정하세요":"목표: "+(item.hero>=0&&item.kind==WorkshopKind.Trait?RiftData.Heroes[item.hero].name+" · ":"")+item.name+"  "+Math.Min(p.shards,item.cost)+" / "+item.cost+" 조각"+(p.shards>=item.cost?" · 해금 가능":"");}
    }
    public sealed partial class PlayerProfile
    {
        public int[] traitUnlocks=new int[12],equippedTraits=new int[12],styleUnlocks=new int[12],equippedStyles=new int[12];
        public int amuletUnlocks,frameUnlocks,equippedAmulet,equippedFrame,workshopGoal,workshopSpent;
        public void NormalizeWorkshop()
        {
            traitUnlocks=Resize(traitUnlocks,12);equippedTraits=Resize(equippedTraits,12);styleUnlocks=Resize(styleUnlocks,12);equippedStyles=Resize(equippedStyles,12);
            for(int i=0;i<12;i++)
            {traitUnlocks[i]&=3;styleUnlocks[i]=Math.Min(1,styleUnlocks[i]);if(equippedTraits[i]>2||(equippedTraits[i]>0&&(traitUnlocks[i]&(1<<(equippedTraits[i]-1)))==0))equippedTraits[i]=0;if(equippedStyles[i]>1||styleUnlocks[i]==0)equippedStyles[i]=0;}
            amuletUnlocks&=7;frameUnlocks&=3;
            if(equippedAmulet<0||equippedAmulet>3||(equippedAmulet>0&&(amuletUnlocks&(1<<(equippedAmulet-1)))==0))equippedAmulet=0;
            if(equippedFrame<0||equippedFrame>2||(equippedFrame>0&&(frameUnlocks&(1<<(equippedFrame-1)))==0))equippedFrame=0;
            workshopSpent=Math.Max(0,workshopSpent);if(RiftWorkshop.Find(workshopGoal)==null||RiftWorkshop.Owned(this,workshopGoal))workshopGoal=0;
        }
        public RiftLoadout DepartureLoadout(){NormalizeWorkshop();return new RiftLoadout{traits=(int[])equippedTraits.Clone(),styles=(int[])equippedStyles.Clone(),amulet=equippedAmulet,frame=equippedFrame};}
    }
}
