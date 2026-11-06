using System;

namespace RiftHeroines
{
    public enum BattlePhase { Battle, Reward, Victory, Defeat }
    public enum CombatRole { Vanguard, Striker, Mystic }
    public enum BossTechnique { FrontWave, BacklineRain, Reinforcements }
    public enum HeroPower { None, ShockPunch, MedicalGuard, GentleFist, MindTransfer, WindFan, PaperStorm, AshPrison, FlashStrike, MedicSword, IceSword, ShieldHealer, DoubleMark }
    public enum Cue { Spawn, Windup, Hit, Heal, Death, BaseHit, Ultimate, Order, Shield, Boss, Warning, StageClear, Bond, Capture, PerfectGuard, Chain }

    public sealed class BondDefinition
    {
        public string name,description;public int a,b;public float damage,haste,mana;
        public BondDefinition(string name,int a,int b,string description,float damage=0,float haste=0,float mana=0)
        {this.name=name;this.a=a;this.b=b;this.description=description;this.damage=damage;this.haste=haste;this.mana=mana;}
    }
    public sealed class EncounterDefinition
    {
        public string name,description;
        public EncounterDefinition(string name,string description){this.name=name;this.description=description;}
    }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string id, name, title, description, skill, color;
        public int faction, cost, hits;
        public CombatRole role;
        public HeroPower power;
        public float health, damage, interval, range, speed, splash;
        public HeroDefinition(string id, string name, string title, string description, string skill, string color,
            int faction, CombatRole role, int cost, float health, float damage, float interval, float range, float speed, float splash = 0, int hits = 1, HeroPower power = HeroPower.None)
        {
            this.id=id; this.name=name; this.title=title; this.description=description; this.skill=skill; this.color=color;
            this.faction=faction; this.role=role; this.cost=cost; this.health=health; this.damage=damage;
            this.interval=interval; this.range=range; this.speed=speed; this.splash=splash; this.hits=hits;
            this.power=power;
        }
    }

    public sealed class RelicDefinition
    {
        public string id, name, description, glyph;
        public RelicDefinition(string id, string name, string description, string glyph)
        { this.id=id; this.name=name; this.description=description; this.glyph=glyph; }
    }

    public static class RiftData
    {
        public const string Version = "0.9.0";
        public const int HeroesPerFaction = 6;
        public const int HeroCount = 12;
        public static int FactionStart(int faction) { return faction*HeroesPerFaction; }
        public const int StageCount = 6;
        public const int UnitCap = 18;
        public static readonly string[] ArenaIds={"moonbridge","stormshrine","eclipsecitadel"};
        public static readonly string[] ArenaNames={"월광의 다리","폭풍의 산문","백야의 성채"};
        public static int ArenaForStage(int stage){return Math.Max(0,Math.Min(2,(stage-1)/2));}
        public static readonly string[] BossTechniqueNames={"전열 분쇄","후열 낙뢰","지원 병력 소집"};
        public static readonly string[] Factions = { "나루토 진영", "블리치 진영" };
        public static readonly string[] StageNames = { "달빛 아래의 첫 조우", "흩날리는 칼날", "경계의 수호자", "붉은 달의 추격", "무너지는 경계", "월야의 최후 결전" };
        public static readonly string[] StageHints = {
            "짝을 함께 출격해 조합을 켜세요. 12초 후 중앙 제단이 열립니다.",
            "상단의 적 전술을 확인하세요. 제단을 차지하면 영력과 돌파 버프를 얻습니다.",
            "대장 비기 0.65초 이내에 E! 절묘한 결계로 끊고 약점을 노리세요.",
            "영입·서약·무료 각성으로 편성을 바꿔 보세요. 같은 치유는 겹쳐도 연속 적용되지 않습니다.",
            "다른 적 전술에 맞춰 병종을 바꾸고 완성한 조합으로 제단을 쟁탈하세요.",
            "결계 반격 후 궁극기로 집중 타격하세요. 대장을 쓰러뜨리면 거점 결계가 해제됩니다." };

        public static readonly HeroDefinition[] Heroes = {
            new HeroDefinition("sakura", "사쿠라", "백호의 괴력", "괴력 2연타로 주변 적을 타격합니다. 술사에게 강합니다.", "앵화충", "#FFA5C9", 0, CombatRole.Striker, 28, 185, 15, .9f, 6, 8, 2.5f, 2, HeroPower.ShockPunch),
            new HeroDefinition("tsunade", "츠나데", "5대 호카게", "받는 피해 25% 감소. 괴력으로 밀쳐내고 주변 아군을 회복합니다.", "괴력 · 백호의 술", "#7DE0A5", 0, CombatRole.Vanguard, 38, 420, 23, 1.3f, 5.6f, 4.8f, 0, 1, HeroPower.MedicalGuard),
            new HeroDefinition("hinata", "히나타", "백안의 유권", "팔괘공장으로 원거리 광역 타격. 쌍사권이 적을 잠깐 경직시킵니다.", "팔괘공장 · 유보쌍사권", "#BBC5FF", 0, CombatRole.Mystic, 48, 145, 30, 1.6f, 23, 4.6f, 7, 1, HeroPower.GentleFist),
            new HeroDefinition("ino", "이노", "야마나카의 감지", "심전신으로 적을 0.55초 붙잡고 주변 아군을 조금씩 회복합니다.", "심전신의 술", "#D9B6FF", 0, CombatRole.Mystic, 40, 155, 19, 1.8f, 22, 5.1f, 0, 1, HeroPower.MindTransfer),
            new HeroDefinition("temari", "테마리", "모래의 바람", "거대한 부채로 넓은 범위를 타격하고 적의 전열을 밀어냅니다.", "풍둔 · 대겸이타치", "#B1E6BB", 0, CombatRole.Mystic, 46, 160, 28, 1.8f, 25, 4.9f, 10, 1, HeroPower.WindFan),
            new HeroDefinition("konan", "코난", "아카츠키의 천사", "종이 수리검 2연타가 넓은 범위의 적을 함께 타격합니다.", "식지의 춤", "#D4DEF4", 0, CombatRole.Mystic, 50, 155, 16, 1.65f, 24, 4.9f, 6, 2, HeroPower.PaperStorm),
            new HeroDefinition("rangiku", "란기쿠", "10번대 부대장", "받는 피해 25% 감소. 회묘의 재가 주변 적을 베고 잠깐 붙잡습니다.", "회묘", "#D5B7A6", 1, CombatRole.Vanguard, 38, 390, 25, 1.35f, 6, 5.2f, 3, 1, HeroPower.AshPrison),
            new HeroDefinition("yoruichi", "요루이치", "순신", "순홍의 번개 2연타. 술사에게 강하며 네 번째 공격은 치명타입니다.", "순보 · 순홍", "#D7A0FF", 1, CombatRole.Striker, 28, 175, 11, .85f, 6, 8.3f, 0, 2, HeroPower.FlashStrike),
            new HeroDefinition("unohana", "우노하나", "4번대 대장", "검술과 회복의 귀도. 5초마다 주변 아군을 크게 회복합니다.", "회도 · 미나즈키", "#9AE5C5", 1, CombatRole.Mystic, 48, 155, 33, 1.7f, 22, 4.5f, 4, 1, HeroPower.MedicSword),
            new HeroDefinition("rukia", "루키아", "13번대 대장", "수백설의 검격으로 주변 적을 얼리고 잠시 이동 속도를 낮춥니다.", "수백설", "#B8E9FF", 1, CombatRole.Vanguard, 42, 330, 25, 1.6f, 8, 5.5f, 6, 1, HeroPower.IceSword),
            new HeroDefinition("orihime", "오리히메", "순순육화", "순순육화로 주변 아군을 회복하고 작은 보호막을 부여합니다.", "삼천결순 · 쌍천귀순", "#FFE0A0", 1, CombatRole.Mystic, 50, 145, 20, 1.85f, 23, 4.8f, 3, 1, HeroPower.ShieldHealer),
            new HeroDefinition("soifon", "소이폰", "2번대 대장", "작봉의 표식을 같은 적에게 두 번 맞히면 강화된 타격을 가합니다.", "작봉 · 이격결살", "#F2D678", 1, CombatRole.Striker, 32, 165, 16, .9f, 6, 9.2f, 0, 1, HeroPower.DoubleMark)
        };

        public static readonly RelicDefinition[] Relics = {
            new RelicDefinition("edge", "벚꽃의 칼날", "모든 아군의 공격력 +16%", "blade"),
            new RelicDefinition("heart", "불굴의 매듭", "모든 아군의 최대 체력 +22%", "heart"),
            new RelicDefinition("spring", "차오르는 영맥", "초당 영력 회복 +2.5", "mana"),
            new RelicDefinition("moon", "만월의 서약", "궁극기 피해 +35%, 전투 시작 각성 +15", "moon"),
            new RelicDefinition("echo", "잔향의 방울", "소환 비용 15% 감소", "spark"),
            new RelicDefinition("hunt", "추격자의 표식", "처치할 때마다 영력 +4", "target"),
            new RelicDefinition("bloom", "소생의 꽃", "치유량 +45%, 아군 체력 +8%", "flower"),
            new RelicDefinition("tempo", "바람의 걸음", "아군 공격 속도 +14%, 진격 지속 +2초", "wind"),
            new RelicDefinition("bond", "경계를 넘은 인연", "양 진영을 함께 출격하면 공격력 +20%", "bond"),
            new RelicDefinition("ward", "월광의 부적", "결계 재사용 -6초, 아군 보호막 +50", "shield"),
            new RelicDefinition("cascade", "연쇄의 불꽃", "5연쇄마다 폭발! 주변 적에게 50+경계×7 피해", "spark"),
            new RelicDefinition("riposte", "되받아치는 달", "절묘한 결계의 각성 +10, 대장 약점 +1초", "moon"),
            new RelicDefinition("conquest", "선봉의 깃발", "제단 점령 영력 +15, 돌파 버프 +4초", "target")
        };
        public static readonly BondDefinition[] Bonds = {
            new BondDefinition("백호의 계승",0,1,"사쿠라·츠나데 공격력 +18%",damage:.18f),
            new BondDefinition("감지의 인연",2,3,"히나타·이노 생존 시 영력 +1.2/s",mana:1.2f),
            new BondDefinition("풍지 난무",4,5,"테마리·코난 공격 속도 +18%",haste:.18f),
            new BondDefinition("빙회 연무",6,9,"란기쿠·루키아 공격력 +18%",damage:.18f),
            new BondDefinition("은밀기동",7,11,"요루이치·소이폰 공격 속도 +18%",haste:.18f),
            new BondDefinition("수호의 인연",8,10,"우노하나·오리히메 생존 시 영력 +1.2/s",mana:1.2f)
        };
        public static readonly EncounterDefinition[] Encounters = {
            new EncounterDefinition("정찰대","기본 편성 · 조합과 제단을 익히세요"),
            new EncounterDefinition("속공대","적 격투 이동 +18%, 피해 +12% · 전위로 대응"),
            new EncounterDefinition("중장대","적 전위 체력 +18% · 술사로 대응"),
            new EncounterDefinition("술법대","적 술사 공격 속도 +15% · 격투로 대응"),
            new EncounterDefinition("각성 대장","비기 직전 결계로 반격 · 대장 격파 시 거점 결계 해제")
        };
        public static int RuleForStage(int seed,int stage)
        {return stage==1?0:stage%3==0?4:1+(int)(((uint)seed+(uint)stage)%3);}
        public static string RoleName(CombatRole role)
        { return role==CombatRole.Vanguard ? "전위" : role==CombatRole.Striker ? "격투" : "술사"; }
        public static float Counter(CombatRole attacker, CombatRole target)
        {
            return (attacker==CombatRole.Vanguard && target==CombatRole.Striker) ||
                   (attacker==CombatRole.Striker && target==CombatRole.Mystic) ||
                   (attacker==CombatRole.Mystic && target==CombatRole.Vanguard) ? 1.45f : 1;
        }
    }

    [Serializable]
    public sealed class Fighter
    {
        public int id, hero, attacks, pendingTarget, markSource;
        public bool ally, boss, moving;
        public float x, health, maxHealth, damage, cooldown, windup, animation, stun, shield, shieldTime, healTimer, slowTime, markTime, healLock;
        public bool Alive { get { return health > 0; } }
    }

    public struct BattleCue
    {
        public Cue kind;
        public int source, target, hero;
        public float x, amount;
        public bool ally, critical;
        public BattleCue(Cue kind, int source, int target, int hero, float x, float amount=0, bool ally=true, bool critical=false)
        { this.kind=kind; this.source=source; this.target=target; this.hero=hero; this.x=x; this.amount=amount; this.ally=ally; this.critical=critical; }
    }

    [Serializable]
    public sealed class Checkpoint
    {
        public int schema=2, seed, faction, stage, economy, kills, bestCombo, metaHealth, metaMana;
        public int perfectGuards,shrineCaptures,chainBursts,freeAwakens;
        public int peakBonds;
        public string runId;
        public bool pendingReward,rewardRerolled;
        public int[] rewardChoices,rewardSummons;
        public int[] ranks;
        public string[] relics;
        public RiftLoadout loadout;
        public float baseHealth, elapsed;
    }

    [Serializable]
    public sealed partial class PlayerProfile
    {
        public int schema=1, shards, runs, wins, bestStage, healthUpgrade, manaUpgrade;
        public bool muted, reducedMotion;
        public Checkpoint checkpoint;
        public int UpgradeCost(bool health) { return 35 + (health ? healthUpgrade : manaUpgrade) * 25; }
        public bool BuyUpgrade(bool health)
        {
            int level=health?healthUpgrade:manaUpgrade, cost=UpgradeCost(health);
            if(level>=5 || shards<cost) return false;
            shards-=cost;
            if(health) healthUpgrade++; else manaUpgrade++;
            return true;
        }
    }
}
