using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        Text encounterText,shrineText,bondText;
        RectTransform shrineRoot;
        Image shrineGlow,shrineMeter;
        RiftGlyph shrineCrystal;
        void BuildShrineVisual()
        {
            shrineRoot=RiftUI.Rect(world,"Moonlight shrine",900,333,120,160);
            shrineGlow=RiftUI.Image(shrineRoot,"Shrine glow",2,14,116,110,new Color(.8f,.65f,1,.14f),RiftUI.Disc);
            shrineCrystal=RiftUI.Icon(shrineRoot,"mana",37,30,46,67,RiftUI.Gold);
            shrineMeter=RiftUI.Bar(shrineRoot,"Shrine capture",0,6,120,9,RiftUI.Gold);
            var label=RiftUI.Text(shrineRoot,"Shrine name","월광 제단",-50,113,220,30,19,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<Shadow>().effectColor=Color.black;
            shrineRoot.gameObject.SetActive(false);
        }
        void BuildTacticalHUD()
        {
            RiftUI.Surface(hud,"Front status",500,852,920,111,new Color(.035f,.055f,.115f,.9f));
            frontText=RiftUI.Text(hud,"Front","",510,922,900,32,19,RiftUI.Gold,false,TextAnchor.MiddleCenter);
            encounterText=RiftUI.Text(hud,"Encounter rule","",510,887,900,34,18,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            shrineText=RiftUI.Text(hud,"Shrine objective","",510,854,900,34,19,RiftUI.Gold,true,TextAnchor.MiddleCenter);
            RiftUI.Surface(hud,"Bond status",354,795,1052,52,new Color(.035f,.055f,.115f,.88f));
            bondText=RiftUI.Text(hud,"Bonds","",365,797,1030,48,18,Color.white,false,TextAnchor.MiddleCenter);
        }
        void RefreshTacticsHUD()
        {
            var rule=RiftData.Encounters[Battle.Encounter];encounterText.text="적 전술: "+rule.name+"  ·  "+rule.description;
            shrineText.text=Battle.StageTime<12?"월광 제단  "+Battle.NextShrine.ToString("0.0")+"초 후 개방  ·  중앙을 선점하세요":Battle.ShrineActive?"월광 제단 쟁탈  ·  "+(Battle.ShrineProgress>=0?"아군 ":"적 ")+Mathf.RoundToInt(Mathf.Abs(Battle.ShrineProgress)*100)+"%":Battle.ShrineBuffTime>0?"제단 확보!  공격력 +20%  "+Battle.ShrineBuffTime.ToString("0.0")+"초":Battle.EnemyShrineBuffTime>0?"적이 제단 확보  ·  적 공격력 +20%  "+Battle.EnemyShrineBuffTime.ToString("0.0")+"초":"다음 제단 개방  "+Battle.NextShrine.ToString("0.0")+"초";
            shrineText.color=Battle.EnemyShrineBuffTime>0?RiftUI.Coral:RiftUI.Gold;
            var links=new StringBuilder("생존 조합  ");
            for(int i=0;i<RiftData.Bonds.Length;i++)
            {
                var b=RiftData.Bonds[i];if(RiftData.Heroes[b.a].faction!=Battle.Faction&&!(Battle.Ranks[b.a]>0&&Battle.Ranks[b.b]>0))continue;
                if(links.Length>7)links.Append("    ");
                links.Append(Battle.ActiveBonds[i]?"<color=#98E8CE>":"<color=#A6B6CE>");links.Append(b.name);links.Append(Battle.ActiveBonds[i]?" ON":" "+Battle.BondProgress(i)+"/2");links.Append("</color>");
            }
            bondText.text=links.ToString();
            shrineRoot.gameObject.SetActive(!Battle.Practice&&Battle.StageTime>=10);
            if(Battle.Practice){shrineText.text="훈련 전용  ·  전투 패배 없음  ·  저장된 출정과 보상 보존";encounterText.text="사쿠라·츠나데 조합 → Q 진격 → E 결계 반격 → R 궁극기";}
            Color tint=Battle.ShrineOwner<0||Battle.ShrineProgress<0?RiftUI.Coral:Battle.ShrineOwner>0?RiftUI.Ice:RiftUI.Gold;
            shrineCrystal.color=tint;shrineMeter.color=tint;shrineMeter.fillAmount=Mathf.Abs(Battle.ShrineProgress);
            shrineGlow.color=new Color(tint.r,tint.g,tint.b,Battle.ShrineActive?.14f+Mathf.Sin(Battle.StageTime*3)*.045f:.06f);
        }
        string SelectedBondHint()
        {
            for(int i=0;i<RiftData.Bonds.Length;i++)
            {
                var b=RiftData.Bonds[i];if(b.a!=selectedHero&&b.b!=selectedHero)continue;
                int partner=b.a==selectedHero?b.b:b.a;
                return b.name+" · "+RiftData.Heroes[partner].name+"와 함께 생존: "+b.description+(Battle.ActiveBonds[i]?"  [활성]":"  ["+Battle.BondProgress(i)+"/2]");
            }
            return "전위 > 격투 > 술사 > 전위";
        }
        string RecruitBondHint(int hero)
        {
            foreach(var b in RiftData.Bonds)if(b.a==hero||b.b==hero)
            {int partner=b.a==hero?b.b:b.a;return RiftData.Heroes[partner].name+"와 "+b.name+(Battle.Ranks[partner]>0?" 완성 가능":" 조합");}
            return "다음 전투부터 출격 가능";
        }
        public void ShowTacticsGuide()
        {
            homeOverlayOpen=true;
            home.gameObject.SetActive(false);
            var panel=Overlay("전선을 뒤집는 네 가지 선택","조합 → 제단 → 반격 → 성장");
            string[] titles={"01  두 명을 함께 지키세요","02  중앙 제단을 차지하세요","03  마지막 순간에 결계!","04  다음 전투를 보고 고르세요"};
            string[] notes={"사쿠라 + 츠나데처럼 짝이 함께 살아 있으면 조합 효과 ON. 카드에 마우스를 올리면 짝과 효과를 확인합니다.","12초 후 중앙 제단 개방, 이후 40초마다 재개방. 중앙 병력으로 점령하면 영력 +35, 각성 +20, 12초간 공격력 +20%.","대장 비기의 마지막 0.65초에 E. 비기를 끊고 각성 +15, 4초간 대장 피해 +35%. 금빛 예고가 반격 신호입니다.","구간 보상에서 동료 영입 · 서약 · 무료 각성을 선택. 다음 적의 전술을 보고 공격·생존·성장 중 하나에 투자하세요."};
            for(int i=0;i<4;i++)
            {
                float x=60+(i%2)*570,y=316-(i/2)*201;
                var card=RiftUI.Surface(panel,"Guide card "+i,x,y,530,184,new Color(.10f,.15f,.245f,.98f));
                RiftUI.Text(card.transform,"Guide heading",titles[i],20,125,490,43,25,RiftUI.Gold,true);
                RiftUI.Text(card.transform,"Guide note",notes[i],22,15,486,109,21,RiftUI.Muted);
            }
            RiftUI.Button(panel,"Close guide","진영 선택으로 [Esc]",421,36,378,55,new Color(.17f,.29f,.37f,.98f),CloseHomeOverlay,23);
        }
    }
}
