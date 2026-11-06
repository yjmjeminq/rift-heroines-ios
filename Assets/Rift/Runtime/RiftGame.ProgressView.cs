using UnityEngine;
using UnityEngine.UI;

namespace RiftHeroines
{
    public sealed partial class RiftGame
    {
        static string Clock(float seconds){return seconds<=0?"기록 없음":((int)seconds/60)+"분 "+((int)seconds%60)+"초";}
        public void ShowRecords()
        {
            homeOverlayOpen=true;home.gameObject.SetActive(false);ClearChildren(modal);
            var shade=RiftUI.Image(modal,"Records shade",0,0,1920,1080,new Color(.015f,.025f,.06f,.88f));shade.raycastTarget=true;
            var panel=RiftUI.Surface(modal,"Records and challenges",180,90,1560,900,new Color(.045f,.068f,.13f,.98f));
            RiftUI.Text(panel.transform,"Records title","인연의 기록 · 도전",40,812,1120,65,43,Color.white,true);
            RiftUI.Text(panel.transform,"Records caption","한 번의 출정이 다음 목표로 이어집니다  ·  인연 조각 "+Profile.shards,40,768,1450,38,22,RiftUI.Gold);
            RiftUI.Button(panel.transform,"Close records","돌아가기 [Esc]",1275,824,240,48,new Color(.17f,.29f,.37f,.98f),CloseHomeOverlay,21);
            for(int i=0;i<RiftProgress.Challenges.Length;i++)
            {
                int index=i;var d=RiftProgress.Challenges[i];int value=Mathf.Min(d.target,RiftProgress.Value(Profile,i));bool claimed=RiftProgress.Claimed(Profile,i),ready=RiftProgress.Ready(Profile,i);
                float x=40+(i%3)*323,y=530-(i/3)*230;
                var card=RiftUI.Surface(panel.transform,"Challenge "+d.id,x,y,307,211,new Color(.10f,.15f,.245f,.98f));
                RiftUI.Text(card.transform,"Challenge title",d.name,16,163,275,34,24,claimed?RiftUI.Muted:RiftUI.Gold,true);
                RiftUI.Text(card.transform,"Challenge goal",d.description,16,103,275,59,19,RiftUI.Muted);
                var bar=RiftUI.Bar(card.transform,"Challenge progress",16,88,275,10,RiftUI.Ice);bar.fillAmount=value/(float)d.target;
                RiftUI.Text(card.transform,"Challenge count",value+" / "+d.target,16,57,275,30,18,Color.white,false,TextAnchor.MiddleRight);
                var claim=RiftUI.Button(card.transform,"Claim "+d.id,claimed?"수령 완료":ready?"보상 받기  +"+d.reward+" 조각":"달성 보상  +"+d.reward+" 조각",16,14,275,40,new Color(.18f,.32f,.37f,.98f),()=>ClaimChallenge(index),18);claim.interactable=ready;
            }
            RiftUI.Text(panel.transform,"Collection title","함께 출격한 동료  "+RiftProgress.Value(Profile,4)+" / 12",40,247,950,37,25,Color.white,true);
            for(int hero=0;hero<RiftData.HeroCount;hero++)
            {
                bool seen=Profile.deployments[hero]>0;float x=40+hero*80;
                var card=RiftUI.Surface(panel.transform,"Collected "+RiftData.Heroes[hero].id,x,117,73,118,new Color(.10f,.15f,.245f,.98f));
                var face=RiftUI.Image(card.transform,"Collected portrait",4,35,65,77,seen?Color.white:new Color(.35f,.4f,.5f,.4f),portraits[hero]);face.preserveAspect=true;
                RiftUI.Text(card.transform,"Collected name",RiftData.Heroes[hero].name,1,11,71,25,15,seen?Color.white:RiftUI.Muted,true,TextAnchor.MiddleCenter);
                DecorateFrame(card.transform,73,118,Profile.equippedFrame);
            }
            var history=RiftUI.Surface(panel.transform,"History",1030,117,490,624,new Color(.10f,.15f,.245f,.98f));
            RiftUI.Text(history.transform,"History title","최근 출정 · 최고 기록",22,567,446,43,27,Color.white,true);
            RiftUI.Text(history.transform,"Best times","나루토  "+Clock(Profile.bestTimes[0])+"\n블리치  "+Clock(Profile.bestTimes[1]),22,487,446,77,21,RiftUI.Gold);
            for(int i=0;i<Profile.history.Length&&i<6;i++)
            {
                var r=Profile.history[i];if(r==null)continue;
                string name=r.faction==0?"나루토":"블리치";
                RiftUI.Text(history.transform,"Run "+i,(r.won?"완주":"제 "+r.stage+"경계")+" · "+name+" · "+Clock(r.seconds)+"\n<size=16>격파 "+r.kills+" · "+r.combo+"연쇄 · 제단 "+r.captures+" · 반격 "+r.guards+"</size>",22,405-i*65,446,61,21,r.won?Color.white:RiftUI.Muted);
            }
            if(Profile.history.Length==0)RiftUI.Text(history.transform,"No history","첫 출정을 마치면\n이곳에 전투 기록이 남습니다",22,220,446,180,24,RiftUI.Muted,false,TextAnchor.MiddleCenter);
            RiftUI.Text(panel.transform,"Records note","도전 보상은 한 번씩 받으며, 누적 기록과 출격 도감은 자동 저장됩니다.",40,39,1480,50,21,RiftUI.Muted,false,TextAnchor.MiddleCenter);
        }
        public void ClaimChallenge(int index)
        {if(!RiftProgress.Claim(Profile,index))return;SaveProfile();ShowRecords();}
    }
}
