using System;
using System.Collections.Generic;

namespace RiftHeroines
{
    // Deterministic combat rules. No scene, frame-rate, audio or UI dependencies.
    public sealed partial class RiftBattle
    {
        public readonly List<Fighter> Fighters = new List<Fighter>();
        public readonly List<BattleCue> Cues = new List<BattleCue>();
        public readonly List<string> Relics = new List<string>();
        public readonly int[] Ranks = new int[RiftData.HeroCount];
        public readonly float[] SummonCooldown = new float[RiftData.HeroCount];
        public readonly int[] Choices = new int[3]; // relic index, 100+ recruit, 200+ free awakening.
        public BattlePhase Phase { get; private set; }
        public int Faction { get; private set; }
        public int Stage { get; private set; }
        public int Economy { get; private set; }
        public int Kills { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int EnemySpawned { get; private set; }
        public int UltimateUses { get; private set; }
        public bool BossSpawned { get; private set; }
        public bool Rerolled { get; private set; }
        public float Mana { get; private set; }
        public float Fury { get; private set; }
        public float AllyBase { get; private set; }
        public float EnemyBase { get; private set; }
        public float StageTime { get; private set; }
        public float TotalTime { get; private set; }
        public float OrderCooldown { get; private set; }
        public float ShieldCooldown { get; private set; }
        public float OrderTime { get; private set; }
        public float WarningTime { get; private set; }
        public float CenterControl { get; private set; }
        public int Seed { get; private set; }
        public string RunId {get;private set;}
        public string LastError { get; private set; }
        public int MetaHealth { get; private set; }
        public int MetaMana { get; private set; }
        public float MaxMana { get { return 120+Economy*25; } }
        public float ManaRate { get { return 9+Economy*2.4f+Count("spring")*2.5f+BondManaBonus+(CenterControl>65 ? 2 : 0)-(EquippedAmulet==3?.7f:0); } }
        public float BaseMax { get { return 1000; } }
        public int EconomyCost { get { return 65+Economy*35; } }
        public int SpawnBudget { get { return 13+Stage*3; } }
        public int Allies { get { return Living(true); } }
        public int Enemies { get { return Living(false); } }
        public int RewardShards { get { return 10+Stage*8+(Phase==BattlePhase.Victory?35:0); } }
        public bool Mixed { get { bool a=false,b=false; foreach(var f in Fighters) if(f.ally && f.Alive) { if(RiftData.Heroes[f.hero].faction==0)a=true; else b=true; } return a&&b; } }

        Random random;
        int nextId=1, warningCount;
        float spawnClock, comboClock, enemyBaseShield,bossEntryTime;
        readonly List<Fighter> targets = new List<Fighter>();

        public RiftBattle(int seed, int faction, int metaHealth=0, int metaMana=0,string runId="",RiftLoadout departure=null)
        {
            loadout=RiftLoadout.Copy(departure);
            Seed=seed;RunId=runId??""; Faction=Math.Max(0,Math.Min(1,faction)); random=new Random(seed);
            MetaHealth=Clamp(metaHealth,0,5); MetaMana=Clamp(metaMana,0,5);
            for(int i=0;i<RiftData.HeroCount;i++) Ranks[i]=RiftData.Heroes[i].faction==Faction?1:0;
            AllyBase=BaseMax; BeginStage(1);
        }

        public static bool IsValid(Checkpoint save)
        {
            if(save==null || save.ranks==null)return false;
            if(!RiftLoadout.IsValid(save.loadout))return false;
            bool legacy=save.schema==1&&save.ranks.Length==6;
            if((!legacy && (save.schema!=2 || save.ranks.Length!=RiftData.HeroCount)) || save.stage<1 || save.stage>6 || save.faction<0 || save.faction>1 ||
                save.relics==null || save.relics.Length>20 ||
                save.economy<0 || save.economy>4 || float.IsNaN(save.baseHealth) || float.IsInfinity(save.baseHealth) || save.baseHealth<=0 || save.baseHealth>1000) return false;
            for(int i=0;i<save.ranks.Length;i++)
            {
                int hero=legacy&&i>=3?i+3:i;
                if(save.ranks[i]<0 || save.ranks[i]>3 || (RiftData.Heroes[hero].faction==save.faction && save.ranks[i]==0))return false;
            }
            foreach(string relic in save.relics) if(Array.FindIndex(RiftData.Relics,r=>r.id==relic)<0) return false;
            if(save.pendingReward)
            {
                if(legacy||save.stage>=RiftData.StageCount||save.rewardChoices==null||save.rewardChoices.Length!=3)return false;
                if(save.rewardSummons!=null){if(save.rewardSummons.Length!=RiftData.HeroCount)return false;foreach(int count in save.rewardSummons)if(count<0)return false;}
                var used=new HashSet<int>();
                foreach(int choice in save.rewardChoices)
                {
                    if(!used.Add(choice))return false;
                    if(choice>=200){int hero=choice-200;if(hero>=RiftData.HeroCount||save.ranks[hero]<=0||save.ranks[hero]>=3)return false;}
                    else if(choice>=100){int hero=choice-100;if(hero>=RiftData.HeroCount||save.ranks[hero]!=0)return false;}
                    else if(choice<0||choice>=RiftData.Relics.Length)return false;
                }
            }
            return true;
        }

        public static RiftBattle Restore(Checkpoint save)
        {
            if(!IsValid(save)) throw new ArgumentException("Invalid Rift checkpoint");
            var model=new RiftBattle(save.seed,save.faction,save.metaHealth,save.metaMana,save.runId,save.loadout);
            if(save.schema==1)for(int i=0;i<6;i++)model.Ranks[i>=3?i+3:i]=save.ranks[i];
            else Array.Copy(save.ranks,model.Ranks,RiftData.HeroCount);
            model.Relics.AddRange(save.relics);
            model.Economy=save.economy; model.Kills=save.kills; model.BestCombo=save.bestCombo;
            model.AllyBase=save.baseHealth; model.TotalTime=save.elapsed;
            model.PerfectGuards=save.perfectGuards;model.ShrineCaptures=save.shrineCaptures;model.ChainBursts=save.chainBursts;model.FreeAwakens=save.freeAwakens;
            model.PeakBonds=save.peakBonds;
            model.BeginStage(save.stage);
            if(save.pendingReward)
            {model.Phase=BattlePhase.Reward;model.EnemyBase=0;model.Rerolled=save.rewardRerolled;Array.Copy(save.rewardChoices,model.Choices,3);if(save.rewardSummons!=null)Array.Copy(save.rewardSummons,model.stageSummons,RiftData.HeroCount);}
            return model;
        }

        // Checkpoints intentionally restart the current stage, not a half-resolved attack.
        public Checkpoint CaptureCheckpoint()
        {
            if(Practice||(Phase!=BattlePhase.Battle&&Phase!=BattlePhase.Reward)) return null;
            return new Checkpoint { seed=Seed,faction=Faction,stage=Stage,economy=Economy,kills=Kills,bestCombo=BestCombo,
                ranks=(int[])Ranks.Clone(),relics=Relics.ToArray(),baseHealth=AllyBase,elapsed=TotalTime,metaHealth=MetaHealth,metaMana=MetaMana,
                perfectGuards=PerfectGuards,shrineCaptures=ShrineCaptures,chainBursts=ChainBursts,freeAwakens=FreeAwakens,peakBonds=PeakBonds,runId=RunId,
                pendingReward=Phase==BattlePhase.Reward,rewardRerolled=Rerolled,rewardChoices=Phase==BattlePhase.Reward?(int[])Choices.Clone():null,rewardSummons=Phase==BattlePhase.Reward?(int[])stageSummons.Clone():null,loadout=CaptureLoadout() };
        }

        void BeginStage(int stage)
        {
            random=new Random(Seed+stage*7919);
            Stage=stage; Phase=BattlePhase.Battle; Fighters.Clear(); Cues.Clear();
            StageTime=0; EnemySpawned=0; BossSpawned=false; Rerolled=false; warningCount=0; WarningTime=0;
            bossEntryTime=0;
            BossHero=RiftData.FactionStart(1-Faction)+(int)(((uint)Seed+(stage==6?3u:0u))%RiftData.HeroesPerFaction);
            WarningCenter=50;WarningRadius=0;
            spawnClock=1.2f; Mana=Math.Min(MaxMana,95+MetaMana*7); Fury=Math.Min(100,25+Count("moon")*15);
            EnemyBase=720+Stage*140; enemyBaseShield=Stage%3==0 ? .35f : 0;
            OrderCooldown=0; ShieldCooldown=0; OrderTime=0; CenterControl=50; Combo=0; comboClock=0;
            Array.Clear(SummonCooldown,0,SummonCooldown.Length);
            ResetTactics();
        }

        public int SummonCost(int hero) { return (int)Math.Ceiling(RiftData.Heroes[hero].cost*Math.Pow(.85,Count("echo"))); }
        public int AwakenCost(int hero) { return 75+Ranks[hero]*40; }
        int Count(string id) { int n=0; foreach(string r in Relics) if(r==id)n++; return n; }
        public int RelicCount(string id){return Count(id);}
        int Living(bool ally) { int n=0; foreach(var f in Fighters) if(f.ally==ally && f.Alive)n++; return n; }
        static float Decay(float value,float dt) { return Math.Max(0,value-dt); }
        static int Clamp(int value,int min,int max) { return Math.Max(min,Math.Min(max,value)); }

        public bool Summon(int hero)
        {
            LastError="";
            if(hero<0 || hero>=RiftData.HeroCount || Phase!=BattlePhase.Battle) return false;
            if(Practice&&(hero>1||Fighters.Exists(f=>f.ally&&f.Alive&&f.hero==hero)))
            {LastError="훈련에서는 사쿠라와 츠나데를 한 명씩 출격하세요.";return false;}
            if(Ranks[hero]==0) { LastError="구간 보상에서 동료로 영입하세요."; return false; }
            if(Allies>=RiftData.UnitCap) { LastError="출격 인원이 가득 찼습니다 (18명)."; return false; }
            if(SummonCooldown[hero]>0) { LastError="출격 준비 중입니다."; return false; }
            int cost=SummonCost(hero);
            if(Mana<cost) { LastError="영력이 부족합니다. 영맥을 강화하면 더 빠르게 회복됩니다."; return false; }
            Mana-=cost; SummonCooldown[hero]=1.6f; Spawn(hero,true,false);stageSummons[hero]++;RefreshBonds(true);AdvancePractice();return true;
        }

        public bool Awaken(int hero)
        {
            if(hero<0 || hero>=RiftData.HeroCount || Phase!=BattlePhase.Battle || Ranks[hero]==0 || Ranks[hero]>=3 || Mana<AwakenCost(hero)) return false;
            Mana-=AwakenCost(hero); Ranks[hero]++;
            foreach(var f in Fighters) if(f.ally && f.hero==hero && f.Alive)
            { f.maxHealth*=1.32f; f.health=Math.Min(f.maxHealth,f.health+f.maxHealth*.32f); f.damage*=1.32f; }
            Cues.Add(new BattleCue(Cue.Heal,0,0,hero,10,0,true,true)); return true;
        }

        public bool UpgradeEconomy()
        {
            if(Phase!=BattlePhase.Battle || Economy>=4 || Mana<EconomyCost) return false;
            Mana-=EconomyCost; Economy++; return true;
        }

        public bool Order()
        {
            if(Phase!=BattlePhase.Battle || OrderCooldown>0 || Allies==0) return false;
            OrderCooldown=20; OrderTime=OrderDuration;
            Cues.Add(new BattleCue(Cue.Order,0,0,RiftData.FactionStart(Faction),CenterControl));AdvancePractice();return true;
        }

        public bool Shield()
        {
            if(Phase!=BattlePhase.Battle || ShieldCooldown>0 || Mana<30) return false;
            Mana-=30; ShieldCooldown=Math.Max(8,23-Count("ward")*6);
            foreach(var f in Fighters) if(f.ally && f.Alive) { f.shield=ShieldStrength; f.shieldTime=5; }
            Cues.Add(new BattleCue(Cue.Shield,0,0,RiftData.FactionStart(Faction),CenterControl));
            if(WarningTime>0 && WarningTime<=.65f)CounterBoss();AdvancePractice();return true;
        }

        public bool Ultimate()
        {
            if(Phase!=BattlePhase.Battle || Fury<100 || (Practice&&PracticeStep!=3)) return false;
            Fury=0; UltimateUses++;
            Cues.Add(new BattleCue(Cue.Ultimate,0,0,RiftData.FactionStart(Faction),CenterControl));
            float power=(180+Stage*18)*(1+Count("moon")*.35f);
            var snapshot=Fighters.ToArray();
            foreach(var f in snapshot) if(!f.ally && f.Alive) { Damage(null,f,power,true); f.stun=Math.Max(f.stun,1.1f); f.x=Math.Min(94,f.x+5); }
            if(!Practice)EnemyBase=Math.Max(0,EnemyBase-power*.22f);
            if(WarningTime>0) WarningTime=0; // Interrupt the telegraphed boss technique.
            AdvancePractice();return true;
        }

        Fighter Spawn(int hero, bool ally, bool boss)
        {
            var d=RiftData.Heroes[hero];
            float rank=ally?(float)Math.Pow(1.32,Math.Max(0,Ranks[hero]-1)):1+(Stage-1)*.105f;
            float health=d.health*rank*(ally?(1+Count("heart")*.22f+Count("bloom")*.08f+MetaHealth*.035f):.85f)*(boss?5.2f:1);
            if(boss)health=1500+Stage*120;
            if(!ally && Encounter==2 && d.role==CombatRole.Vanguard)health*=1.18f;
            var f=new Fighter { id=nextId++,hero=hero,ally=ally,boss=boss,x=ally?6:94,
                health=health,maxHealth=health,damage=d.damage*rank*(ally?(1+Count("edge")*.16f):.92f)*(boss?1.7f:1),cooldown=.3f,healTimer=4.5f };
            if(!ally && Encounter==1 && d.role==CombatRole.Striker)f.damage*=1.12f;
            if(ally){var trait=TraitOf(f);f.health*=trait.health;f.maxHealth=f.health;f.damage*=trait.damage;}
            if(boss)f.damage=24f/d.hits*rank*.92f*1.7f;
            if(boss)bossEntryTime=StageTime;
            Fighters.Add(f); Cues.Add(new BattleCue(boss?Cue.Boss:Cue.Spawn,f.id,0,hero,f.x,0,ally));
            return f;
        }

        public void Tick(float dt)
        {
            if(Phase!=BattlePhase.Battle || dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            // Fixed-size steps keep attacks/telegraphs stable even after a slow rendering frame.
            while(dt>.0251f && Phase==BattlePhase.Battle) { Step(.025f); dt-=.025f; }
            if(dt>0 && Phase==BattlePhase.Battle) Step(dt);
        }

        void Step(float dt)
        {
            StageTime+=dt; TotalTime+=dt;
            OrderCooldown=Decay(OrderCooldown,dt); ShieldCooldown=Decay(ShieldCooldown,dt); OrderTime=Decay(OrderTime,dt);
            BossBreakTime=Decay(BossBreakTime,dt);ShrineBuffTime=Decay(ShrineBuffTime,dt);EnemyShrineBuffTime=Decay(EnemyShrineBuffTime,dt);RefreshBonds(true);
            for(int i=0;i<RiftData.HeroCount;i++) SummonCooldown[i]=Decay(SummonCooldown[i],dt);
            Mana=Math.Min(MaxMana,Mana+ManaRate*dt); Fury=Math.Min(100,Fury+1.5f*dt);
            comboClock=Decay(comboClock,dt); if(comboClock==0)Combo=0;
            spawnClock-=dt;
            if(!Practice && spawnClock<=0 && EnemySpawned<SpawnBudget && Enemies<RiftData.UnitCap)
            {
                int enemyFaction=1-Faction;
                int offset=enemyFaction==0?1:0;
                int slot=EnemySpawned<RiftData.HeroesPerFaction?(EnemySpawned+offset)%RiftData.HeroesPerFaction:random.Next(RiftData.HeroesPerFaction);
                // Every opening draft includes all six members of the opposing franchise.
                Spawn(RiftData.FactionStart(enemyFaction)+slot,false,false); EnemySpawned++;
                spawnClock=Math.Max(1.4f,3.25f-Stage*.13f)+(EnemySpawned%5==0?2:0);
            }
            if(!Practice && Stage%3==0 && !BossSpawned && (StageTime>=24 || EnemyBase<(720+Stage*140)*.6f))
            { BossSpawned=true; Spawn(BossHero,false,true); }

            bool livingBoss=false;
            foreach(var f in Fighters) if(f.boss && f.Alive) { livingBoss=true; break; }
            if(BossSpawned&&!livingBoss)enemyBaseShield=0;
            if(!livingBoss)WarningTime=0;
            UpdateBossTechniques(dt,livingBoss);

            bool mixed=Mixed;
            foreach(var f in Fighters)
            {
                if(!f.Alive)continue;
                var d=RiftData.Heroes[f.hero];
                f.moving=false; f.cooldown=Decay(f.cooldown,dt);
                f.shieldTime=Decay(f.shieldTime,dt); if(f.shieldTime==0)f.shield=0;
                f.slowTime=Decay(f.slowTime,dt);f.markTime=Decay(f.markTime,dt);f.healLock=Decay(f.healLock,dt);
                if(f.stun>0) { f.stun=Decay(f.stun,dt); continue; }
                // A suspended windup keeps its pose until the attack clock resumes.
                f.animation=Decay(f.animation,dt);
                if(d.power==HeroPower.MedicalGuard || d.power==HeroPower.MedicSword || d.power==HeroPower.ShieldHealer || d.power==HeroPower.MindTransfer)
                {
                    f.healTimer-=dt;
                    if(f.healTimer<=0)
                    {
                        f.healTimer=d.power==HeroPower.MedicSword?5:6;
                        float ratio=(d.power==HeroPower.MedicSword?.15f:d.power==HeroPower.ShieldHealer?.12f:d.power==HeroPower.MindTransfer?.06f:.10f)*TraitOf(f).healing;
                        foreach(var friend in Fighters) if(friend.Alive && friend.ally==f.ally && Math.Abs(friend.x-f.x)<=30)
                        {
                            float effectiveRatio=friend.boss?Math.Min(.035f,ratio):ratio;
                            float heal=friend.healLock>0?0:Math.Min(friend.maxHealth-friend.health,friend.maxHealth*effectiveRatio*(f.ally?1+Count("bloom")*.45f:1));
                            if(heal>1) { friend.health+=heal;friend.healLock=2;Cues.Add(new BattleCue(Cue.Heal,f.id,friend.id,f.hero,friend.x,heal,f.ally)); }
                            if(d.power==HeroPower.ShieldHealer){friend.shield=Math.Max(friend.shield,35*TraitOf(f).ward);friend.shieldTime=Math.Max(friend.shieldTime,4);}
                        }
                    }
                }
                if(f.windup>0)
                {
                    f.windup-=dt;
                    if(f.windup<=0) ResolveAttack(f,mixed);
                    continue;
                }
                var target=NearestEnemy(f);
                float baseX=f.ally?95:5;
                float distance=target!=null?Math.Abs(target.x-f.x):Math.Abs(baseX-f.x);
                if(distance<=RangeOf(f))
                {
                    if(f.cooldown<=0)
                    {
                        f.pendingTarget=target!=null?target.id:0;
                        f.windup=.18f; f.animation=.52f;
                        float speed=f.ally?(1+Count("tempo")*.14f)*(OrderTime>0?1.45f:1)*HeroHaste(f.hero):Encounter==3&&d.role==CombatRole.Mystic?1.15f:1;
                        f.cooldown=d.interval*TraitOf(f).interval/speed;
                        Cues.Add(new BattleCue(Cue.Windup,f.id,f.pendingTarget,f.hero,f.x,0,f.ally));
                    }
                }
                else
                {
                    float direction=target!=null?Math.Sign(target.x-f.x):(f.ally?1:-1);
                    float movement=d.speed*TraitOf(f).speed*dt*(f.ally&&OrderTime>0?1.45f:1)*(f.slowTime>0?.55f:1)*(!f.ally&&Encounter==1&&d.role==CombatRole.Striker?1.18f:1);
                    // Nearby allies use a small formation gap instead of stacking into one sprite.
                    foreach(var friend in Fighters)
                    {
                        if(friend==f || !friend.Alive || friend.ally!=f.ally || friend.id%3!=f.id%3)continue;
                        // Front-line fighters may pass stationary long-range supports.
                        if(RangeOf(f)+2<RangeOf(friend))continue;
                        float ahead=(friend.x-f.x)*direction;
                        if(ahead>0 && ahead<2.2f)movement=Math.Min(movement,Math.Max(0,ahead-1.9f));
                    }
                    f.x=Math.Max(6,Math.Min(94,f.x+movement*direction));f.moving=movement>.001f;
                }
            }
            float front=50; bool haveAlly=false,haveEnemy=false; float left=5,right=95;
            foreach(var f in Fighters) if(f.Alive) { if(f.ally) { left=Math.Max(left,f.x);haveAlly=true; } else { right=Math.Min(right,f.x);haveEnemy=true; } }
            if(haveAlly&&haveEnemy)front=(left+right)*.5f; else if(haveAlly)front=left; else if(haveEnemy)front=right;
            CenterControl=CenterControl+(front-CenterControl)*Math.Min(1,dt*2);
            Fighters.RemoveAll(f=>!f.Alive);
            if(!Practice)UpdateShrine(dt);RefreshBonds(false);
            if(Practice){UpdatePractice(dt);return;}
            // A boss gate cannot be rushed while its captain is still alive or waiting to enter.
            if(EnemyBase<=0 && (Stage%3!=0 || (BossSpawned && !livingBoss))) FinishStage();
            if(AllyBase<=0) { AllyBase=0; Phase=BattlePhase.Defeat; }
            if(StageTime>=165 && Phase==BattlePhase.Battle)
            { AllyBase=Math.Max(0,AllyBase-22*dt); if(AllyBase<=0)Phase=BattlePhase.Defeat; }
        }

        Fighter NearestEnemy(Fighter source)
        {
            Fighter result=null; float best=float.MaxValue;
            foreach(var f in Fighters) if(f.Alive && f.ally!=source.ally)
            {
                float distance=Math.Abs(f.x-source.x);
                if(distance<best) {best=distance;result=f;}
            }
            return result;
        }

        void ResolveAttack(Fighter f, bool mixed)
        {
            if(!f.Alive)return;
            var d=RiftData.Heroes[f.hero];
            Fighter target=null;
            foreach(var candidate in Fighters)if(candidate.id==f.pendingTarget && candidate.Alive) { target=candidate;break; }
            if(target==null)target=NearestEnemy(f);
            bool critical=d.power==HeroPower.FlashStrike && (f.attacks+1)%4==0;
            float damage=f.damage*d.hits*(critical?1.8f:1)*(f.ally && mixed ?1+Count("bond")*.20f:1)*(f.ally?HeroDamage(f.hero)*(ShrineBuffTime>0?1.2f:1):EnemyShrineBuffTime>0?1.2f:1);
            f.attacks++;
            if(target!=null)
            {
                if(Math.Abs(target.x-f.x)>RangeOf(f)+3)return;
                targets.Clear();
                float splash=Math.Max(0,d.splash+TraitOf(f).splash);
                foreach(var enemy in Fighters)if(enemy.Alive && enemy.ally!=f.ally && (enemy==target || (splash>0 && Math.Abs(enemy.x-target.x)<=splash)))targets.Add(enemy);
                foreach(var enemy in targets)
                {
                    float dealt=damage*RiftData.Counter(d.role,RiftData.Heroes[enemy.hero].role)*(enemy.boss?TraitOf(f).boss:TraitOf(f).normal);
                    bool marked=d.power==HeroPower.DoubleMark&&enemy.markSource==f.id&&enemy.markTime>0;
                    if(d.power==HeroPower.DoubleMark){dealt*=marked?2.2f+TraitOf(f).mark:1;enemy.markSource=marked?0:f.id;enemy.markTime=marked?0:5;}
                    Damage(f,enemy,dealt,critical||marked);
                    if(!enemy.Alive)continue;
                    if(d.power==HeroPower.MedicalGuard || d.power==HeroPower.WindFan)enemy.x=Math.Max(6,Math.Min(94,enemy.x+(f.ally?1:-1)*((d.power==HeroPower.WindFan?1.7f:1.3f)+TraitOf(f).push)));
                    if(d.power==HeroPower.GentleFist)enemy.stun=Math.Max(enemy.stun,.18f);
                    if(d.power==HeroPower.AshPrison)enemy.stun=Math.Max(enemy.stun,.32f+TraitOf(f).stun);
                    if(d.power==HeroPower.MindTransfer)enemy.stun=Math.Max(enemy.stun,.55f+TraitOf(f).stun);
                    if(d.power==HeroPower.IceSword){enemy.stun=Math.Max(enemy.stun,.45f);enemy.slowTime=1.6f*TraitOf(f).slow;}
                }
            }
            else if(Math.Abs(f.x-(f.ally?95:5))<=RangeOf(f)+2)
            {
                float baseDamage=damage*(f.ally && enemyBaseShield>0?.65f:1);
                if(!Practice){if(f.ally)EnemyBase=Math.Max(0,EnemyBase-baseDamage); else AllyBase=Math.Max(0,AllyBase-baseDamage);}
                Cues.Add(new BattleCue(Cue.BaseHit,f.id,0,f.hero,f.ally?95:5,baseDamage,f.ally));
            }
        }

        void Damage(Fighter source, Fighter target, float amount, bool critical)
        {
            if(!target.Alive)return;
            if(RiftData.Heroes[target.hero].role==CombatRole.Vanguard)amount*=.75f;
            if(target.boss&&!target.ally&&BossBreakTime>0)amount*=1.35f;
            float absorbed=Math.Min(target.shield,amount); target.shield-=absorbed; amount-=absorbed;
            if(Practice)amount=Math.Min(amount,Math.Max(0,target.health-1));
            target.health=Math.Max(0,target.health-amount);
            Cues.Add(new BattleCue(Cue.Hit,source!=null?source.id:0,target.id,source!=null?source.hero:target.hero,target.x,amount,source!=null?source.ally:!target.ally,critical));
            if(source!=null && source.ally)Fury=Math.Min(100,Fury+.35f);
            if(!target.Alive)
            {
                Cues.Add(new BattleCue(Cue.Death,source!=null?source.id:0,target.id,target.hero,target.x,0,target.ally,target.boss));
                if(!target.ally)
                {
                    Kills++; Combo++; BestCombo=Math.Max(BestCombo,Combo); comboClock=4;
                    Mana=Math.Min(MaxMana,Mana+5+Count("hunt")*4+(Combo>=5?2:0)); Fury=Math.Min(100,Fury+(target.boss?22:5));
                    RewardChain(target,source);
                }
            }
        }

        void FinishStage()
        {
            Cues.Add(new BattleCue(Cue.StageClear,0,0,RiftData.FactionStart(Faction),95));
            if(Stage>=RiftData.StageCount) { Phase=BattlePhase.Victory; return; }
            Phase=BattlePhase.Reward; RollChoices();
        }

        void RollChoices()
        {
            var rng=new Random(Seed+Stage*3571+(Rerolled?104729:0));
            var available=new List<int>();
            for(int i=0;i<RiftData.Relics.Length;i++)available.Add(i);
            for(int i=0;i<3;i++) { int at=rng.Next(available.Count);Choices[i]=available[at];available.RemoveAt(at); }
            var recruits=new List<int>(); for(int i=0;i<RiftData.HeroCount;i++)if(Ranks[i]==0)recruits.Add(i);
            if(recruits.Count>0)Choices[0]=100+recruits[rng.Next(recruits.Count)];
            int awaken=-1,best=-1;
            for(int i=0;i<RiftData.HeroCount;i++)if(Ranks[i]>0&&Ranks[i]<3)
            {int score=stageSummons[i]*10+rng.Next(5);if(score>best){best=score;awaken=i;}}
            if(awaken>=0)Choices[2]=200+awaken;
            for(int i=2;i>0;i--){int j=rng.Next(i+1),swap=Choices[i];Choices[i]=Choices[j];Choices[j]=swap;}
        }

        public bool Reroll()
        {
            if(Phase!=BattlePhase.Reward || Rerolled)return false;
            Rerolled=true; RollChoices(); return true;
        }

        public bool ChooseReward(int index)
        {
            if(Phase!=BattlePhase.Reward || index<0 || index>=3)return false;
            int chosen=Choices[index];
            if(chosen>=200){int hero=chosen-200;if(Ranks[hero]<=0||Ranks[hero]>=3)return false;Ranks[hero]++;FreeAwakens++;}
            else if(chosen>=100)Ranks[chosen-100]=1;else Relics.Add(RiftData.Relics[chosen].id);
            AllyBase=Math.Min(BaseMax,AllyBase+BaseMax*.3f);
            // Re-seed at a stage boundary so a checkpoint resumes the exact same enemy draft.
            random=new Random(Seed+(Stage+1)*7919); BeginStage(Stage+1); return true;
        }
    }
}
