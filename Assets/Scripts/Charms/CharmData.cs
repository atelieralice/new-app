using Godot;

namespace meph {
    
    #region Charm Resource Templates
    
    /// <summary>
    /// Godot Resource-based charm data template for equipment serialization and editor integration
    /// Provides complete charm statistics and configuration for runtime charm instantiation
    /// Supports visual editing in Godot Inspector with .tres file serialization
    /// 
    /// Charm Data Components:
    /// - Identity Properties: Unique ID, display name, description, and set membership
    /// - Equipment Classification: Slot assignment and equipment restrictions
    /// - Stat Bonuses: General resource and combat stat enhancements
    /// - Specialized Bonuses: Essence-specific, weapon-specific, and factor-specific bonuses
    /// - Set Integration: Set name matching for complete set bonus activation
    /// 
    /// Used by charm factories and equipment systems for runtime charm creation
    /// Enables data-driven charm configuration without code modifications
    /// </summary>
    [GlobalClass]
    public partial class CharmData : Resource {
        
        #region Core Charm Identity
        
        /// <summary>
        /// Unique charm identifier for lookup and reference tracking
        /// Used for save/load operations, inventory management, and charm identification
        /// Should follow naming convention: "set_slot" (e.g., "crimson_helmet")
        /// 
        /// Note: Property renamed from 'id' to avoid Godot Resource conflicts
        /// </summary>
        [Export] public string charmId;
        
        /// <summary>
        /// Human-readable charm name displayed in UI
        /// Used for player identification and equipment management interfaces
        /// Should clearly indicate charm function and set membership
        /// 
        /// Note: Property renamed from 'name' to avoid Godot Resource conflicts
        /// </summary>
        [Export] public string charmName;
        
        /// <summary>
        /// Detailed charm description explaining effects and bonuses
        /// Includes general bonuses, signature bonuses, and set information
        /// Used for tooltips, equipment comparison, and player understanding
        /// 
        /// Note: Property renamed from 'description' to avoid Godot Resource conflicts
        /// </summary>
        [Export] public string charmDescription;
        
        /// <summary>
        /// Charm set name for set bonus validation and special ability access
        /// Must match across all 5 pieces for complete set bonus activation
        /// 
        /// Known Set Examples:
        /// - "Flames of Crimson Rage": Burning damage enhancement set
        /// - "Crystalized Dreams": Freeze manipulation and immunity bypass set
        /// - "Guilt of Betrayal": MP recovery and resource sustainability set
        /// </summary>
        [Export] public string setName;
        
        /// <summary>
        /// Equipment slot this charm occupies on the character
        /// Determines where the charm can be equipped and prevents slot conflicts
        /// Used for equipment validation and set completion checking
        /// </summary>
        [Export] public CharmSlot slot;
        
        #endregion
        
        #region General Stat Bonuses
        
        /// <summary>
        /// Life Points bonus added to character's computed MaxLP
        /// Increases both maximum and current LP when equipped (game rule)
        /// Scales character survivability and health pool capacity
        /// </summary>
        [Export] public int lpBonus;
        
        /// <summary>
        /// Energy Points bonus added to character's computed MaxEP
        /// Increases both maximum and current EP when equipped (game rule)
        /// Enhances physical ability usage and weapon skill capacity
        /// </summary>
        [Export] public int epBonus;
        
        /// <summary>
        /// Mana Points bonus added to character's computed MaxMP
        /// Increases both maximum and current MP when equipped (game rule)
        /// Enhances magical ability usage and essence skill capacity
        /// </summary>
        [Export] public int mpBonus;
        
        /// <summary>
        /// Normal defense bonus added to character's computed DEF
        /// Reduces incoming non-essence damage before shield calculations
        /// Stacks with base defense and other charm bonuses
        /// </summary>
        [Export] public int defBonus;
        
        /// <summary>
        /// Essence defense bonus added to character's computed EssenceDEF
        /// Reduces incoming elemental damage before shield calculations
        /// Provides protection against all essence types equally
        /// </summary>
        [Export] public int essenceDefBonus;
        
        #endregion
        
        #region General Damage Bonuses
        
        /// <summary>
        /// Normal attack damage bonus added to character's computed ATK
        /// Enhances all non-essence damage output including weapon attacks
        /// Stacks with base attack and other charm bonuses for total damage
        /// </summary>
        [Export] public int normalDamageBonus;
        
        /// <summary>
        /// Essence attack damage bonus added to character's computed EssenceATK
        /// Enhances all elemental damage output including factor effects
        /// Applies to Fire, Ice, Earth, Water, Electricity, Nature, and Air damage
        /// </summary>
        [Export] public int essenceDamageBonus;
        
        #endregion
        
        #region Specialized Damage Bonuses
        
        /// <summary>
        /// Bonus damage for a specific essence type
        /// Only applies when using attacks of the matching essence element
        /// Calculated separately from general essence damage bonuses
        /// 
        /// Usage Pattern:
        /// - Set essenceType to specify which element gets the bonus
        /// - Set specificEssenceDamageBonus to the bonus amount
        /// - Character.GetEssenceDamageBonus(essenceType) calculates total
        /// </summary>
        [Export] public int specificEssenceDamageBonus;
        
        /// <summary>
        /// Essence type for specific essence damage bonus targeting
        /// Determines which elemental attacks receive the specificEssenceDamageBonus
        /// Must match the essence type being used for bonus to apply
        /// </summary>
        [Export] public Character.ESSENCE_TYPE essenceType;
        
        /// <summary>
        /// Bonus damage for a specific weapon type
        /// Only applies when equipped by characters using the matching weapon
        /// Calculated through Character.GetWeaponDamageBonus() method
        /// 
        /// Usage Pattern:
        /// - Set weaponType to specify which weapon gets the bonus
        /// - Set weaponDamageBonus to the bonus amount
        /// - Bonus applies only if character's WeaponType matches
        /// </summary>
        [Export] public int weaponDamageBonus;
        
        /// <summary>
        /// Weapon type for specific weapon damage bonus targeting
        /// Determines which weapon users receive the weaponDamageBonus
        /// Must match the character's weapon type for bonus to apply
        /// </summary>
        [Export] public Character.WEAPON_TYPE weaponType;
        
        #endregion
        
        #region Factor-Specific Bonuses
        
        /// <summary>
        /// Burning damage multiplier bonus as percentage increase
        /// Enhances Burning Factor damage calculations for Fire essence builds
        /// 
        /// Application:
        /// - Applied through Character.GetBurningDamageMultiplier()
        /// - Converts to decimal multiplier (10.0f = 10% increase = 1.1x multiplier)
        /// - Set bonus "Flames of Crimson Rage" doubles effectiveness
        /// 
        /// Example: 10.0f bonus = 10% increase in burning damage
        /// </summary>
        [Export] public float burningDamageBonus;
        
        /// <summary>
        /// Additional freeze duration in turns for Freeze Factor applications
        /// Extends card lockdown effects for Ice essence control builds
        /// Applied through Character.GetFreezeDurationBonus() for all freeze effects
        /// 
        /// Example: 1 bonus = +1 turn to all freeze durations applied by this character
        /// </summary>
        [Export] public int freezeDurationBonus;
        
        /// <summary>
        /// MP recovery bonus during normal attacks
        /// Provides resource sustainability for mana-intensive builds
        /// Applied through Character.GetMpRecoveryBonus() during normal attack resolution
        /// 
        /// Usage Pattern:
        /// - Triggers during CharacterLogic.PerformNormalAttack()
        /// - Grants bonus MP after weapon effects are executed
        /// - Supports sustained ability usage strategies
        /// </summary>
        [Export] public int mpRecoveryBonus;
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Validates that all essential charm data fields are properly configured
        /// Used for resource validation and equipment system safety checks
        /// Ensures charm can be properly instantiated by charm factories
        /// </summary>
        /// <returns>True if all required fields are valid, false otherwise</returns>
        public bool IsValid() {
            return !string.IsNullOrEmpty(charmId) &&
                   !string.IsNullOrEmpty(charmName) &&
                   !string.IsNullOrEmpty(charmDescription) &&
                   !string.IsNullOrEmpty(setName);
        }
        
        /// <summary>
        /// Creates a runtime Charm instance from this data template
        /// Transfers all properties from data template to runtime charm object
        /// Used by charm factories and equipment systems for charm instantiation
        /// 
        /// Conversion Process:
        /// 1. Create new Charm instance
        /// 2. Copy identity properties (ID, name, description, set)
        /// 3. Transfer all stat bonuses and specialized bonuses
        /// 4. Set equipment slot assignment
        /// 5. Return fully configured charm ready for equipment
        /// </summary>
        /// <returns>Runtime Charm instance with all properties transferred</returns>
        public Charm ToCharm() {
            return new Charm {
                Id = charmId,
                Name = charmName,
                Description = charmDescription,
                SetName = setName,
                Slot = slot,
                
                // General stat bonuses
                LpBonus = lpBonus,
                EpBonus = epBonus,
                MpBonus = mpBonus,
                DefBonus = defBonus,
                EssenceDefBonus = essenceDefBonus,
                
                // General damage bonuses
                NormalDamageBonus = normalDamageBonus,
                EssenceDamageBonus = essenceDamageBonus,
                
                // Specialized damage bonuses
                SpecificEssenceDamageBonus = specificEssenceDamageBonus,
                EssenceType = essenceType,
                WeaponDamageBonus = weaponDamageBonus,
                WeaponType = weaponType,
                
                // Factor-specific bonuses
                BurningDamageBonus = burningDamageBonus,
                FreezeDurationBonus = freezeDurationBonus,
                MpRecoveryBonus = mpRecoveryBonus
            };
        }
        
        /// <summary>
        /// Returns charm name for debugging and display purposes
        /// Provides safe string representation even for malformed charm data
        /// </summary>
        /// <returns>Charm name or "Unknown Charm Data" if name is null</returns>
        public override string ToString() => charmName ?? "Unknown Charm Data";
        
        #endregion
    }
    
    #endregion
}