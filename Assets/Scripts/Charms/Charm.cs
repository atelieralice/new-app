using System.Collections.Generic;

namespace meph {
    public enum CharmSlot {
        HELMET,
        ARMOR, 
        GLOVES,
        BOOTS,
        GLOW
    }

    public class Charm {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string SetName { get; internal set; }
        public CharmSlot Slot { get; internal set; }
        
        // All the modifiers from CharmData
        public int LpBonus { get; internal set; }
        public int EpBonus { get; internal set; }
        public int MpBonus { get; internal set; }
        public int DefBonus { get; internal set; }
        public int EssenceDefBonus { get; internal set; }
        public int NormalDamageBonus { get; internal set; }
        public int EssenceDamageBonus { get; internal set; }
        public int SpecificEssenceDamageBonus { get; internal set; }
        public Character.ESSENCE_TYPE EssenceType { get; internal set; }
        public int WeaponDamageBonus { get; internal set; }
        public Character.WEAPON_TYPE WeaponType { get; internal set; }
        public float BurningDamageBonus { get; internal set; }
        public int FreezeDurationBonus { get; internal set; }
        public int MpRecoveryBonus { get; internal set; }

        public override string ToString() => Name ?? "Unknown Charm";
    }
}