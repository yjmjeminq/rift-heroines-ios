namespace RiftHeroines
{
    public sealed partial class RiftBattle
    {
        RiftLoadout loadout;
        public int EquippedAmulet {get{return loadout.amulet;}}
        public int EquippedFrame {get{return loadout.frame;}}
        public int TraitChoice(int hero){return loadout.traits[hero];}
        public int StyleChoice(int hero){return loadout.styles[hero];}
        public int TraitCount {get{int n=0;foreach(int choice in loadout.traits)if(choice>0)n++;return n;}}
        public RiftLoadout CaptureLoadout(){return RiftLoadout.Copy(loadout);}
        HeroTrait TraitOf(Fighter fighter){return fighter.ally?RiftWorkshop.Trait(fighter.hero,loadout.traits[fighter.hero]):RiftWorkshop.Basic;}
        float RangeOf(Fighter fighter){return RiftData.Heroes[fighter.hero].range+TraitOf(fighter).range;}
        public float OrderDuration {get{return 6+Count("tempo")*2+(EquippedAmulet==3?2:0);}}
        public float GuardFury {get{return 15+Count("riposte")*10+(EquippedAmulet==2?8:0);}}
        public float ShieldStrength {get{return (110+Count("ward")*50)*(EquippedAmulet==2?.75f:1);}}
        public float ShrineBuffDuration {get{return (EquippedAmulet==1?9:12)+Count("conquest")*4;}}
    }
}
