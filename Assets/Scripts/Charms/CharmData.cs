using Godot;

namespace meph {
    public partial class CharmData : Resource {
        [Export] public string charmId; // Changed from 'id' to avoid conflict
        [Export] public string charmName; // Changed from 'name' to avoid conflict  
        [Export] public string charmDescription; // Changed from 'description' to avoid conflict
        [Export] public string setName; // For set bonuses
        [Export] public CharmSlot slot;
        
        // Stat modifiers
        [Export] public int lpBonus;
        [Export] public int epBonus;
        [Export] public int mpBonus;
        [Export] public int defBonus;
        [Export] public int essenceDefBonus;
        [Export] public int normalDamageBonus;
        [Export] public int essenceDamageBonus;
        [Export] public int specificEssenceDamageBonus; // For Ice/Fire/etc specific damage
        [Export] public Character.ESSENCE_TYPE essenceType; // Which essence this bonus applies to
        [Export] public int weaponDamageBonus; // For Sword/Magic/etc specific damage
        [Export] public Character.WEAPON_TYPE weaponType; // Which weapon this bonus applies to
        
        // Factor modifiers
        [Export] public float burningDamageBonus; // BD% increase
        [Export] public int freezeDurationBonus; // FT increase
        [Export] public int mpRecoveryBonus; // MP recovery per normal attack
    }
}