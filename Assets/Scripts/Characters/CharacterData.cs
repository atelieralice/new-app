using System;
using System.Collections.Generic;
using Godot;

namespace meph {

    #region Character Resource Templates
    
    /// <summary>
    /// Godot Resource-based character data template for serialization and editor integration
    /// Provides base character statistics and configuration for character initialization
    /// Supports visual editing in Godot Inspector and .tres file serialization
    /// 
    /// Character Data Components:
    /// - Identity Properties: Name, star rating, essence, and weapon types
    /// - Resource Statistics: Base LP/EP/MP/UP/Potion maximums before charm bonuses
    /// - Equipment Collections: Future card and charm loadout definitions
    /// - Set Bonus Integration: Reference to complete charm set configurations
    /// 
    /// Used by CharacterCreator.InitCharacter() for runtime character instantiation
    /// </summary>
    [GlobalClass]
    public partial class CharacterData : Resource {
        
        #region Core Character Identity
        
        /// <summary>
        /// Character display name and unique identifier
        /// Used for UI display, save/load operations, and character recognition
        /// Should match character names used in AllCards factory methods
        /// </summary>
        [Export] public string charName;
        
        /// <summary>
        /// Character star rating determining power level and stat scaling
        /// Affects base combat statistics through CharacterCreator scaling formulas
        /// 
        /// Star Level Impact:
        /// - 4 Star: 160 ATK/EssenceATK, 40 DEF/EssenceDEF
        /// - 5 Star: 180 ATK/EssenceATK, 50 DEF/EssenceDEF
        /// - 6 Star: 200 ATK/EssenceATK, 60 DEF/EssenceDEF
        /// </summary>
        [Export] public Character.STAR star;
        
        /// <summary>
        /// Character essence type defining elemental affinity and factor access
        /// Determines signature abilities, damage type bonuses, and Factor applications
        /// Must match character's signature card set in AllCards factory
        /// </summary>
        [Export] public Character.ESSENCE_TYPE essenceType;
        
        /// <summary>
        /// Character weapon type defining combat style and synergies
        /// Affects normal attack mechanics and weapon-specific card bonuses
        /// Used for charm weapon damage bonus calculations
        /// </summary>
        [Export] public Character.WEAPON_TYPE weaponType;
        
        #endregion
        
        #region Base Resource Statistics
        
        /// <summary>
        /// Maximum Life Points before charm bonuses
        /// Character is defeated when current LP reaches zero
        /// CharacterCreator initializes current LP to computed MaxLP (base + charm bonuses)
        /// </summary>
        [Export] public int maxLP;
        
        /// <summary>
        /// Maximum Energy Points before charm bonuses
        /// Used for physical abilities and weapon skills
        /// Regenerates 5% of computed MaxEP per turn (base + charm bonuses)
        /// </summary>
        [Export] public int maxEP;
        
        /// <summary>
        /// Maximum Mana Points before charm bonuses
        /// Used for magical abilities and essence skills
        /// Regenerates 2% of computed MaxMP per turn (base + charm bonuses)
        /// </summary>
        [Export] public int maxMP;
        
        /// <summary>
        /// Maximum Ultimate Points (fixed, no charm bonuses)
        /// Required for Ultimate Card activation
        /// Charging conditions defined by character's specific Ultimate Card
        /// CharacterCreator initializes current UP to 0 (must be charged)
        /// </summary>
        [Export] public int maxUP;
        
        /// <summary>
        /// Maximum Potion slots (fixed, no charm bonuses)
        /// Determines how many consumable potions can be carried
        /// Future Swift implementation will affect usage patterns per turn
        /// </summary>
        [Export] public int maxPotion;
        
        #endregion
        
        #region Equipment Collections (Future Implementation)
        
        /// <summary>
        /// Future implementation: Character-specific card loadout definitions
        /// Currently unused but reserved for data-driven card set configuration
        /// Would enable visual card deck editing in Godot Inspector
        /// 
        /// Potential Usage:
        /// - Predefined character builds with specific card combinations
        /// - Campaign mode loadouts with progression unlocks
        /// - AI opponent deck configurations
        /// 
        /// Note: References CardData class (separate file)
        /// </summary>
        [Export] public Godot.Collections.Array<CardData> cards = [];
        
        /// <summary>
        /// Future implementation: Character-specific charm loadout definitions
        /// Currently unused but reserved for predefined charm set configurations
        /// Would enable visual charm build editing in Godot Inspector
        /// 
        /// Potential Usage:
        /// - Recommended charm builds for specific strategies
        /// - Campaign progression with charm unlock tracking
        /// - Tournament mode standardized equipment sets
        /// 
        /// Note: References CharmData class (CharmData.cs file)
        /// </summary>
        [Export] public Godot.Collections.Array<CharmData> charms = new();
        
        #endregion
        
        #region Set Bonus Integration
        
        /// <summary>
        /// Reference to complete charm set bonus configuration
        /// Defines special abilities and enhancements available with full charm sets
        /// Used for set bonus validation and special ability access
        /// 
        /// Set Bonus Examples:
        /// - "Flames of Crimson Rage": Enhanced burning damage calculations
        /// - "Crystalized Dreams": Freeze immunity bypass capabilities
        /// </summary>
        [Export] public SetBonusData setBonus;
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Validates that all essential character data fields are properly configured
        /// Used for resource validation and character creation safety checks
        /// Ensures character can be properly initialized by CharacterCreator
        /// </summary>
        /// <returns>True if all required fields are valid, false otherwise</returns>
        public bool IsValid() {
            return !string.IsNullOrEmpty(charName) &&
                   star != Character.STAR.NONE &&
                   essenceType != Character.ESSENCE_TYPE.NONE &&
                   weaponType != Character.WEAPON_TYPE.NONE &&
                   maxLP > 0 && maxEP > 0 && maxMP > 0 && maxUP > 0 && maxPotion > 0;
        }
        
        /// <summary>
        /// Returns character name for debugging and display purposes
        /// Provides safe string representation even for malformed character data
        /// </summary>
        /// <returns>Character name or "Unknown Character Data" if name is null</returns>
        public override string ToString() => charName ?? "Unknown Character Data";
        
        #endregion
    }
    
    #endregion

    #region Set Bonus Resource Templates
    
    /// <summary>
    /// Godot Resource-based set bonus data template for charm set configuration
    /// Defines special abilities and enhancements available with complete charm sets
    /// Supports complex set bonus mechanics and character-specific interactions
    /// 
    /// Set Bonus Mechanics:
    /// - Requires all 5 charm slots (H/A/G/B/Gl) filled with matching set pieces
    /// - Provides enhanced abilities beyond individual charm bonuses
    /// - Can enable special mechanics like immunity bypass or damage multipliers
    /// 
    /// Known Set Examples:
    /// - "Flames of Crimson Rage": Doubles charm BD bonus effectiveness
    /// - "Crystalized Dreams": Allows freezing immune targets
    /// </summary>
    [GlobalClass]
    public partial class SetBonusData : Resource {
        
        #region Set Bonus Properties
        
        /// <summary>
        /// Unique set name identifier matching charm set references
        /// Used for set bonus validation and special ability checks
        /// Must match setName in corresponding CharmData instances
        /// </summary>
        [Export] public string setName;
        
        /// <summary>
        /// Detailed description of set bonus effects and requirements
        /// Explains enhanced abilities, special mechanics, and activation conditions
        /// Used for UI display and player understanding of set benefits
        /// </summary>
        [Export] public string description;
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Validates that set bonus data has all required fields properly configured
        /// Used for resource validation and set bonus system safety
        /// </summary>
        /// <returns>True if all essential fields are valid, false otherwise</returns>
        public bool IsValid() {
            return !string.IsNullOrEmpty(setName) && 
                   !string.IsNullOrEmpty(description);
        }
        
        #endregion
    }
    
    #endregion

    #region Legacy Code Documentation
    
    // NOTE: CharmData class has been moved to its own dedicated resource file
    // The comprehensive CharmData implementation is now in CharmData.cs
    // This provides complete stat bonus system with all charm functionality
    // Including slot management, set bonuses, and runtime conversion methods
    //
    // NOTE: CardData class has been moved to its own dedicated resource file
    // The original CardData implementation provided basic card serialization
    // Now handled by dedicated CardData.cs file with enhanced functionality
    // Including proper Card.TYPE enum integration and effect system support
    
    #endregion
}