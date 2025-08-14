using System.Collections.Generic;

namespace meph {
    
    #region Charm Equipment System
    
    /// <summary>
    /// Charm equipment slot types defining character equipment categories
    /// Each character can equip one charm per slot for a complete 5-piece set
    /// Set bonuses activate when all slots are filled with matching set charms
    /// 
    /// Slot Categories by Equipment Function:
    /// - Helmet/Armor: Primary defensive bonuses (LP, DEF, EssenceDEF)
    /// - Gloves/Boots: Utility and mobility bonuses (MP recovery, freeze duration)
    /// - Glow: Premium slot with strongest bonuses and set-defining effects
    /// 
    /// Slot restrictions prevent multiple charms of the same type being equipped
    /// </summary>
    public enum CharmSlot {
        HELMET,     // Head protection - defensive focus
        ARMOR,      // Body protection - defensive focus  
        GLOVES,     // Hand equipment - utility focus
        BOOTS,      // Foot equipment - mobility focus
        GLOW        // Special enhancement - premium effects
    }

    /// <summary>
    /// Individual charm equipment piece providing passive stat bonuses and set membership
    /// Implements the complete charm bonus system with both general and specialized bonuses
    /// Supports 5-piece set configurations with matching set names for bonus activation
    /// 
    /// Charm System Features:
    /// - Stat Bonuses: LP/EP/MP resource enhancement and combat stat improvements
    /// - Specialized Bonuses: Essence-specific, weapon-specific, and factor-specific enhancements
    /// - Set Integration: Matching set names enable powerful set bonus mechanics
    /// - Slot Management: CharmSlot restrictions ensure balanced equipment distribution
    /// - Character Integration: Bonuses calculated through Character computed properties
    /// 
    /// Bonus Categories:
    /// - General: Universal stat bonuses (LP, EP, MP, ATK, DEF)
    /// - Specialized: Targeted bonuses (essence damage, weapon damage, factor effects)
    /// - Unique: Set-exclusive bonuses (burning damage, freeze duration, MP recovery)
    /// </summary>
    public class Charm {
        
        #region Core Charm Identity
        
        /// <summary>
        /// Unique charm identifier for lookup and reference tracking
        /// Used for save/load operations, inventory management, and charm identification
        /// Should follow naming convention: "set_slot" (e.g., "crimson_helmet")
        /// </summary>
        public string Id { get; internal set; }
        
        /// <summary>
        /// Human-readable charm name displayed in UI
        /// Used for player identification and equipment management interfaces
        /// Should clearly indicate charm function and set membership
        /// </summary>
        public string Name { get; internal set; }
        
        /// <summary>
        /// Detailed charm description explaining effects and bonuses
        /// Includes general bonuses, signature bonuses, and set information
        /// Used for tooltips, equipment comparison, and player understanding
        /// </summary>
        public string Description { get; internal set; }
        
        /// <summary>
        /// Charm set name for set bonus validation and special ability access
        /// Must match across all 5 pieces for complete set bonus activation
        /// 
        /// Known Set Examples:
        /// - "Flames of Crimson Rage": Burning damage enhancement set
        /// - "Crystalized Dreams": Freeze manipulation and immunity bypass set
        /// - "Guilt of Betrayal": MP recovery and resource sustainability set
        /// </summary>
        public string SetName { get; internal set; }
        
        /// <summary>
        /// Equipment slot this charm occupies on the character
        /// Determines where the charm can be equipped and prevents slot conflicts
        /// Used for equipment validation and set completion checking
        /// </summary>
        public CharmSlot Slot { get; internal set; }
        
        #endregion
        
        #region General Stat Bonuses
        
        /// <summary>
        /// Life Points bonus added to character's computed MaxLP
        /// Increases both maximum and current LP when equipped (game rule)
        /// Scales character survivability and health pool capacity
        /// </summary>
        public int LpBonus { get; internal set; }
        
        /// <summary>
        /// Energy Points bonus added to character's computed MaxEP
        /// Increases both maximum and current EP when equipped (game rule)
        /// Enhances physical ability usage and weapon skill capacity
        /// </summary>
        public int EpBonus { get; internal set; }
        
        /// <summary>
        /// Mana Points bonus added to character's computed MaxMP
        /// Increases both maximum and current MP when equipped (game rule)
        /// Enhances magical ability usage and essence skill capacity
        /// </summary>
        public int MpBonus { get; internal set; }
        
        /// <summary>
        /// Normal defense bonus added to character's computed DEF
        /// Reduces incoming non-essence damage before shield calculations
        /// Stacks with base defense and other charm bonuses
        /// </summary>
        public int DefBonus { get; internal set; }
        
        /// <summary>
        /// Essence defense bonus added to character's computed EssenceDEF
        /// Reduces incoming elemental damage before shield calculations
        /// Provides protection against all essence types equally
        /// </summary>
        public int EssenceDefBonus { get; internal set; }
        
        #endregion
        
        #region General Damage Bonuses
        
        /// <summary>
        /// Normal attack damage bonus added to character's computed ATK
        /// Enhances all non-essence damage output including weapon attacks
        /// Stacks with base attack and other charm bonuses for total damage
        /// </summary>
        public int NormalDamageBonus { get; internal set; }
        
        /// <summary>
        /// Essence attack damage bonus added to character's computed EssenceATK
        /// Enhances all elemental damage output including factor effects
        /// Applies to Fire, Ice, Earth, Water, Electricity, Nature, and Air damage
        /// </summary>
        public int EssenceDamageBonus { get; internal set; }
        
        #endregion
        
        #region Specialized Damage Bonuses
        
        /// <summary>
        /// Bonus damage for a specific essence type
        /// Only applies when using attacks of the matching essence element
        /// Calculated separately from general essence damage bonuses
        /// 
        /// Usage Pattern:
        /// - Set EssenceType to specify which element gets the bonus
        /// - Set SpecificEssenceDamageBonus to the bonus amount
        /// - Character.GetEssenceDamageBonus(essenceType) calculates total
        /// </summary>
        public int SpecificEssenceDamageBonus { get; internal set; }
        
        /// <summary>
        /// Essence type for specific essence damage bonus targeting
        /// Determines which elemental attacks receive the SpecificEssenceDamageBonus
        /// Must match the essence type being used for bonus to apply
        /// </summary>
        public Character.ESSENCE_TYPE EssenceType { get; internal set; }
        
        /// <summary>
        /// Bonus damage for a specific weapon type
        /// Only applies when equipped by characters using the matching weapon
        /// Calculated through Character.GetWeaponDamageBonus() method
        /// 
        /// Usage Pattern:
        /// - Set WeaponType to specify which weapon gets the bonus
        /// - Set WeaponDamageBonus to the bonus amount
        /// - Bonus applies only if character's WeaponType matches
        /// </summary>
        public int WeaponDamageBonus { get; internal set; }
        
        /// <summary>
        /// Weapon type for specific weapon damage bonus targeting
        /// Determines which weapon users receive the WeaponDamageBonus
        /// Must match the character's weapon type for bonus to apply
        /// </summary>
        public Character.WEAPON_TYPE WeaponType { get; internal set; }
        
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
        public float BurningDamageBonus { get; internal set; }
        
        /// <summary>
        /// Additional freeze duration in turns for Freeze Factor applications
        /// Extends card lockdown effects for Ice essence control builds
        /// Applied through Character.GetFreezeDurationBonus() for all freeze effects
        /// 
        /// Example: 1 bonus = +1 turn to all freeze durations applied by this character
        /// </summary>
        public int FreezeDurationBonus { get; internal set; }
        
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
        public int MpRecoveryBonus { get; internal set; }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Returns charm name for debugging and display purposes
        /// Provides safe string representation even for malformed charm data
        /// Used in equipment UI, logging systems, and development tools
        /// </summary>
        /// <returns>Charm name or "Unknown Charm" if name is null</returns>
        public override string ToString() => Name ?? "Unknown Charm";
        
        #endregion
    }
    
    #endregion
}