using System;

namespace RiftHeroines
{
    public enum DeploymentResult { Unavailable,Deployed,Queued,Cancelled }
    // One explicit reservation and a hold gesture; both use normal summon costs/cooldowns.
    public sealed class RiftDeployment
    {
        public int QueuedHero {get;private set;}=-1;
        public int HeldHero {get;private set;}=-1;
        public bool ConsumedHold {get;private set;}
        float heldTime,repeatClock;
        public DeploymentResult Request(RiftBattle b,int hero)
        {
            if(b==null||b.Phase!=BattlePhase.Battle||hero<0||hero>=RiftData.HeroCount||b.Ranks[hero]==0)return DeploymentResult.Unavailable;
            if(QueuedHero==hero){CancelQueue();return DeploymentResult.Cancelled;}
            CancelQueue();if(b.Summon(hero))return DeploymentResult.Deployed;
            QueuedHero=hero;return DeploymentResult.Queued;
        }
        public void CancelQueue(){QueuedHero=-1;}
        public void BeginHold(int hero,bool pointer=false)
        {
            bool cancelled=pointer&&QueuedHero==hero;
            if(QueuedHero!=hero||cancelled)CancelQueue();HeldHero=hero;heldTime=repeatClock=0;ConsumedHold=cancelled;
        }
        public void EndHold(){HeldHero=-1;heldTime=repeatClock=0;}
        public bool ConsumeClick(){bool result=ConsumedHold;ConsumedHold=false;return result;}
        public void Clear(){CancelQueue();EndHold();ConsumedHold=false;}
        public int Advance(RiftBattle b,float dt,bool paused)
        {
            if(b==null||b.Phase!=BattlePhase.Battle){Clear();return -1;}
            if(paused||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return -1;
            // Automatic spending waits during boss telegraphs, leaving mana for a counter.
            if(b.WarningTime>0)return -1;
            if(QueuedHero>=0&&b.Summon(QueuedHero)){int deployed=QueuedHero;CancelQueue();return deployed;}
            if(HeldHero<0||HeldHero>=RiftData.HeroCount)return -1;
            heldTime+=dt;repeatClock-=dt;if(heldTime<.35f||repeatClock>0)return -1;
            ConsumedHold=true;repeatClock=.15f;return b.Summon(HeldHero)?HeldHero:-1;
        }
    }
}
