using System;

namespace RiftHeroines
{
    public static class RiftFeedback
    {
        public const int MaxEffects=42,MaxNumbers=24;
        public static bool CanCounter(RiftBattle b){return b.ShieldCooldown<=0&&b.Mana>=30;}
        public static string WarningAction(RiftBattle b)
        {
            if(CanCounter(b))return b.WarningTime<=.65f?"지금 E!":"금빛 예고에 E";
            if(b.Fury>=100)return "R로 비기 차단 가능";
            return b.ShieldCooldown>0?"결계 재사용 중 · 피해 대비":"결계 영력 "+(int)Math.Ceiling(30-b.Mana)+" 부족";
        }
        public static int HitStyle(HeroPower power)
        {
            if(power==HeroPower.ShockPunch||power==HeroPower.MedicalGuard||power==HeroPower.GentleFist||power==HeroPower.FlashStrike)return 3;
            if(power==HeroPower.AshPrison||power==HeroPower.MedicSword||power==HeroPower.IceSword||power==HeroPower.DoubleMark)return 4;
            return 5;
        }
    }
}
