using System;
using System.Globalization;

namespace RiftHeroines
{
    public static class RiftRewardPreview
    {
        static string N(float value){return value.ToString("0.#",CultureInfo.InvariantCulture);}
        public static string Describe(RiftBattle b,int relic)
        {
            string id=RiftData.Relics[relic].id;int n=b.RelicCount(id);string change="";
            switch(id)
            {
                case "edge":change="공격 보너스 +"+16*n+"% → +"+16*(n+1)+"%";break;
                case "heart":change="체력 보너스 +"+22*n+"% → +"+22*(n+1)+"%";break;
                case "spring":change="영력 회복 +"+N(2.5f*n)+" → +"+N(2.5f*(n+1))+"/s";break;
                case "moon":change="시작 각성 "+Math.Min(100,25+15*n)+" → "+Math.Min(100,25+15*(n+1));break;
                case "echo":int hero=RiftData.FactionStart(b.Faction);change=RiftData.Heroes[hero].name+" 출격 "+b.SummonCost(hero)+" → "+(int)Math.Ceiling(RiftData.Heroes[hero].cost*Math.Pow(.85,n+1));break;
                case "hunt":change="기본 처치 영력 "+(5+4*n)+" → "+(5+4*(n+1));break;
                case "bloom":change="치유 보너스 +"+45*n+"% → +"+45*(n+1)+"%";break;
                case "tempo":change="진격 지속 "+N(b.OrderDuration)+" → "+N(b.OrderDuration+2)+"초";break;
                case "bond":change="혼성 공격 +"+20*n+"% → +"+20*(n+1)+"%";break;
                case "ward":change="결계 재사용 "+Math.Max(8,23-6*n)+" → "+Math.Max(8,23-6*(n+1))+"초";break;
                case "cascade":int damage=50+Math.Min(6,b.Stage+1)*7;change="다음 경계 폭발 "+damage*n+" → "+damage*(n+1)+" 피해";break;
                case "riposte":change="반격 각성 "+N(b.GuardFury)+" → "+N(b.GuardFury+10);break;
                case "conquest":change="제단 영력 "+(35+15*n)+" → "+(35+15*(n+1));break;
            }
            return "보유 "+n+"개 → "+(n+1)+"개\n"+change;
        }
    }
}
