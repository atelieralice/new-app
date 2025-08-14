using System;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    
    /// <summary>
    /// Core character class representing playable fighters in the game system
    /// Implements the complete character mechanics including stats, equipment, and status effects
    /// Supports all character features from basic combat to advanced passive abilities
    /// 
    /// Character System Features:
    /// - Stat Management: LP/EP/MP/UP resource system with base + modifier calculations
    /// - Equipment System: Card slots and Charm equipment with set bonuses
    /// - Status Effects: Bitfield-based factor tracking (Burning, Freeze, Immunity, etc.)
    /// - Passive Abilities: Character-specific state tracking and special mechanics
    /// - Combat Mechanics: Critical hits, damage calculations, and resource management
    /// </summary>
    public class Character {
        
        #region Character Classification System
        
        /// <summary>
        /// Star rating system defining character power level and rarity
        /// Affects base stat scaling and availability in game modes
        /// Higher star characters have enhanced base statistics
        /// </summary>
        public enum STAR {
            NONE = 0,   // Default/unassigned state
            FOUR = 4,   // Basic tier characters
            FIVE = 5,   // Advanced tier characters  
            SIX = 6     // Elite tier characters
        }

        /// <summary>
        /// Essence types defining character elemental affinity and damage bonuses
        /// Each essence (except Light/Darkness) has signature Factor abilities
        /// Light essence characters can control all other essences equally
        /// 
        /// Essence-Specific Factors:
        /// - Earth: Toughness (shields + damage bonus)
        /// - Water: Healing (LP restoration + opponent LP drain)
        /// - Electricity: Recharge (EP steal mechanics)
        /// - Nature: Growth (MP steal mechanics)
        /// - Air: Storm (factor prevention + turn-end damage)
        /// - Fire: Burning (percentage-based damage over time)
        /// - Ice: Freeze (card lockdown mechanics)
        /// </summary>
        public enum ESSENCE_TYPE {
            NONE = 0,
            EARTH,      // Toughness Factor: Shield + Earth damage
            WATER,      // Healing Factor: LP manipulation
            ELECTRICITY,// Recharge Factor: EP manipulation
            NATURE,     // Growth Factor: MP manipulation
            AIR,        // Storm Factor: Factor prevention
            FIRE,       // Burning Factor: DoT percentage damage
            ICE,        // Freeze Factor: Card lockdown
            LIGHT,      // Universal essence control
            DARKNESS    // Future implementation
        }

        /// <summary>
        /// Weapon type system defining combat style and card synergies
        /// Affects normal attack patterns and weapon-specific bonuses
        /// Each type has unique characteristics and strategic applications
        /// 
        /// Weapon Characteristics:
        /// - Sword: Low damage, pairs with supportive weapons
        /// - Claymore: High damage, pairs with damage dealer weapons
        /// - Polearm: Decent damage, pairs with defensive weapons
        /// - Bow: Scattered damage, pairs with buffer weapons
        /// - Magic: Decent damage, pairs with buffer weapons
        /// - Gun: Complex chamber/bullet mechanics (future implementation)
        /// </summary>
        public enum WEAPON_TYPE {
            NONE = 0,
            SWORD,      // Low damage + support synergy
            CLAYMORE,   // High damage + damage dealer synergy
            POLEARM,    // Balanced + defensive synergy
            BOW,        // Scattered + buffer synergy
            MAGIC,      // Balanced + buffer synergy
            GUN         // Complex mechanics (chambers + bullets)
        }

        /// <summary>
        /// Status effect system using bitfield flags for efficient tracking
        /// Managed by FactorManager for consistent application and removal
        /// Supports multiple simultaneous effects through flag combinations
        /// 
        /// Factor Categories:
        /// - Resource Manipulation: Toughness, Healing, Recharge, Growth
        /// - Control Effects: Storm, Burning, Freeze, Immune
        /// - Enhancement Effects: Essence Shield, BD Boost, MP Regen
        /// </summary>
        [Flags]
        public enum STATUS_EFFECT {
            NONE = 0,
            TOUGHNESS = 1,              // Earth shield + damage bonus
            HEALING = 2,                // Water LP restoration over time
            RECHARGE = 4,               // Electricity EP steal over time
            GROWTH = 8,                 // Nature MP steal over time
            STORM = 16,                 // Air factor prevention + damage
            BURNING = 32,               // Fire percentage damage over time
            FREEZE = 64,                // Ice card lockdown (applied to cards, not character)
            IMMUNE = 128,               // Factor application immunity
            ESSENCE_SHIELD = 256,       // Specialized shield blocking only essence damage
            BURNING_DAMAGE_BOOST = 512, // BD percentage increase modifier
            MP_REGEN = 1024            // MP regeneration per turn
        }
        
        #endregion

        #region Core Character Identity
        
        /// <summary>
        /// Character display name and unique identifier
        /// Used for UI display, logging, and character recognition
        /// </summary>
        public string CharName { get; internal set; }
        
        /// <summary>
        /// Character star rating determining power level and stat scaling
        /// Affects base combat statistics and character rarity
        /// </summary>
        public STAR Star { get; internal set; }
        
        /// <summary>
        /// Character essence type defining elemental affinity and factor access
        /// Determines available signature abilities and damage type bonuses
        /// </summary>
        public ESSENCE_TYPE EssenceType { get; internal set; }
        
        /// <summary>
        /// Character weapon type defining combat style and synergies
        /// Affects normal attack mechanics and weapon-specific card bonuses
        /// </summary>
        public WEAPON_TYPE WeaponType { get; internal set; }
        
        #endregion

        #region Base Resource System
        
        /// <summary>
        /// Maximum Life Points without charm bonuses
        /// Character is defeated when current LP reaches zero
        /// Base value increases current LP when modified (game rule)
        /// </summary>
        public int BaseMaxLP { get; internal set; }
        
        /// <summary>
        /// Maximum Energy Points without charm bonuses
        /// Used for physical abilities and weapon skills
        /// Regenerates 5% of Max EP per turn (game rule)
        /// </summary>
        public int BaseMaxEP { get; internal set; }
        
        /// <summary>
        /// Maximum Mana Points without charm bonuses
        /// Used for magical abilities and essence skills
        /// Regenerates 2% of Max MP per turn (game rule)
        /// </summary>
        public int BaseMaxMP { get; internal set; }
        
        /// <summary>
        /// Maximum Ultimate Points (fixed, no charm bonuses)
        /// Required for Ultimate Card activation
        /// Charging conditions defined by character's Ultimate Card
        /// </summary>
        public int MaxUP { get; internal set; }
        
        /// <summary>
        /// Maximum Potion slots (fixed, no charm bonuses)
        /// Determines how many consumable potions can be carried
        /// Future Swift implementation will affect usage patterns
        /// </summary>
        public int MaxPotion { get; internal set; }
        
        #endregion

        #region Base Combat Statistics
        
        /// <summary>
        /// Base Normal Attack damage without modifiers
        /// Enhanced by charm bonuses and weapon synergies
        /// Used for all non-essence damage calculations
        /// </summary>
        public int BaseATK { get; internal set; } = 100;
        
        /// <summary>
        /// Base Essence Attack damage without modifiers
        /// Enhanced by charm bonuses and essence-specific modifiers
        /// Used for elemental damage calculations (Fire, Ice, etc.)
        /// </summary>
        public int BaseEssenceATK { get; internal set; } = 100;
        
        /// <summary>
        /// Base defense against Normal damage without modifiers
        /// Reduces incoming non-essence damage
        /// Enhanced by charm bonuses and defensive abilities
        /// </summary>
        public int BaseDEF { get; internal set; } = 0;
        
        /// <summary>
        /// Base defense against Essence damage without modifiers
        /// Reduces incoming elemental damage from all essence types
        /// Enhanced by charm bonuses and essence-specific resistances
        /// </summary>
        public int BaseEssenceDEF { get; internal set; } = 0;
        
        #endregion

        #region Current Resource Tracking
        
        /// <summary>
        /// Current Life Points - character is defeated when this reaches zero
        /// Cannot exceed MaxLP (computed property with charm bonuses)
        /// Modified by healing effects, damage, and LP manipulation abilities
        /// </summary>
        public int LP { get; internal set; }
        
        /// <summary>
        /// Current Energy Points for physical abilities
        /// Cannot exceed MaxEP (computed property with charm bonuses)
        /// Regenerates 5% of MaxEP per turn automatically
        /// </summary>
        public int EP { get; internal set; }
        
        /// <summary>
        /// Current Mana Points for magical abilities
        /// Cannot exceed MaxMP (computed property with charm bonuses)
        /// Regenerates 2% of MaxMP per turn automatically
        /// </summary>
        public int MP { get; internal set; }
        
        /// <summary>
        /// Current Ultimate Points for Ultimate Card activation
        /// Cannot exceed MaxUP (fixed value, no charm bonuses)
        /// Charging conditions vary by character's Ultimate Card
        /// </summary>
        public int UP { get; internal set; }
        
        /// <summary>
        /// Current Potion count for consumable usage
        /// Cannot exceed MaxPotion (fixed value, no charm bonuses)
        /// Decreased when consuming potion cards
        /// </summary>
        public int Potion { get; internal set; }
        
        #endregion

        #region Equipment System
        
        /// <summary>
        /// Character card equipment slots mapped by card type
        /// Supports all equipment cards except Charm cards
        /// 
        /// Equipment Slots:
        /// - C: Character Card (identity and passive abilities)
        /// - BW: Base Weapon (used in normal attacks + card effects)
        /// - SW: Secondary Weapon (used in normal attacks + card effects)
        /// - E/W/Q: Skill Cards (active abilities with resource costs)
        /// - P: Potion Cards (consumable support items)
        /// - U: Ultimate Card (powerful abilities requiring UP)
        /// </summary>
        public Dictionary<Card.TYPE, Card> EquippedSlots { get; internal set; } = new();
        
        /// <summary>
        /// Charm equipment slots providing passive stat bonuses
        /// Supports 5-piece charm sets with individual and set bonuses
        /// 
        /// Charm Categories:
        /// - Helmet (H): General + signature defensive bonuses
        /// - Armor (A): General + signature defensive bonuses
        /// - Gloves (G): General + signature utility bonuses
        /// - Boots (B): General + signature mobility bonuses
        /// - Glow (Gl): General + signature enhancement bonuses (strongest effects)
        /// </summary>
        public Dictionary<CharmSlot, Charm> EquippedCharms { get; internal set; } = new();
        
        #endregion

        #region Status Effect System
        
        /// <summary>
        /// Current active status effects using bitfield flags
        /// Managed by FactorManager for consistent application and removal
        /// Supports multiple simultaneous effects through flag combinations
        /// Use StatusEffectResolver.Has() extension method for checking specific effects
        /// </summary>
        public STATUS_EFFECT StatusEffects { get; internal set; }
        
        #endregion

        #region Critical Hit System
        
        /// <summary>
        /// Critical hit chance as decimal (0.1 = 10% base from game rules)
        /// Enhanced by charm bonuses and temporary effects
        /// Determines probability of dealing critical damage
        /// </summary>
        public float CritRate { get; internal set; } = 0.1f;
        
        /// <summary>
        /// Critical damage bonus as decimal (0.05 = 5% Absolute Damage from game rules)
        /// Enhanced by charm bonuses and temporary effects
        /// Applied as Absolute Damage when critical hit occurs
        /// </summary>
        public float CritDamage { get; internal set; } = 0.05f;
        
        #endregion

        #region Character-Specific Passive State
        
        /// <summary>
        /// Character-specific passive ability state tracking
        /// Maintains state for complex abilities like Rok's Altering Pyre
        /// Enables character-unique mechanics and ability interactions
        /// </summary>
        public CharacterPassiveState PassiveState { get; internal set; } = new();
        
        #endregion

        #region Computed Properties with Charm Bonuses
        
        /// <summary>
        /// Maximum Life Points including charm bonuses
        /// Automatically increases current LP when modified (game rule)
        /// Used as the reference for percentage-based calculations
        /// </summary>
        public int MaxLP => BaseMaxLP + GetCharmBonus(c => c.LpBonus);
        
        /// <summary>
        /// Maximum Energy Points including charm bonuses
        /// Automatically increases current EP when modified (game rule)
        /// Base for 5% per turn regeneration calculation
        /// </summary>
        public int MaxEP => BaseMaxEP + GetCharmBonus(c => c.EpBonus);
        
        /// <summary>
        /// Maximum Mana Points including charm bonuses
        /// Automatically increases current MP when modified (game rule)
        /// Base for 2% per turn regeneration calculation
        /// </summary>
        public int MaxMP => BaseMaxMP + GetCharmBonus(c => c.MpBonus);
        
        /// <summary>
        /// Total Normal Attack damage including charm bonuses
        /// Used for all non-essence damage calculations
        /// Enhanced by weapon synergies and general damage bonuses
        /// </summary>
        public int ATK => BaseATK + GetCharmBonus(c => c.NormalDamageBonus);
        
        /// <summary>
        /// Total Essence Attack damage including charm bonuses
        /// Used for elemental damage calculations (Fire, Ice, etc.)
        /// Enhanced by essence-specific bonuses and general damage bonuses
        /// </summary>
        public int EssenceATK => BaseEssenceATK + GetCharmBonus(c => c.EssenceDamageBonus);
        
        /// <summary>
        /// Total defense against Normal damage including charm bonuses
        /// Reduces incoming non-essence damage before shield calculations
        /// Enhanced by defensive charm bonuses and set effects
        /// </summary>
        public int DEF => BaseDEF + GetCharmBonus(c => c.DefBonus);
        
        /// <summary>
        /// Total defense against Essence damage including charm bonuses
        /// Reduces incoming elemental damage before shield calculations
        /// Enhanced by essence-specific resistances and general bonuses
        /// </summary>
        public int EssenceDEF => BaseEssenceDEF + GetCharmBonus(c => c.EssenceDefBonus);
        
        #endregion

        #region Specialized Damage Bonus Calculations
        
        /// <summary>
        /// Calculates weapon-specific damage bonus from equipped charms
        /// Only applies bonuses from charms matching this character's weapon type
        /// Used to enhance weapon synergy and specialized combat builds
        /// </summary>
        /// <returns>Additional weapon damage from matching charm bonuses</returns>
        public int GetWeaponDamageBonus() => GetCharmBonus(c => c.WeaponType == WeaponType ? c.WeaponDamageBonus : 0);
        
        /// <summary>
        /// Calculates essence-specific damage bonus for a particular element
        /// Only applies bonuses from charms specifically enhancing the target essence
        /// Used for specialized elemental builds and essence-focused strategies
        /// </summary>
        /// <param name="essenceType">The essence type to calculate bonuses for</param>
        /// <returns>Additional essence damage for the specified element</returns>
        public int GetEssenceDamageBonus(ESSENCE_TYPE essenceType) {
            return EquippedCharms.Values
                .Where(c => c.EssenceType == essenceType)
                .Sum(c => c.SpecificEssenceDamageBonus);
        }
        
        /// <summary>
        /// Calculates total Burning Damage multiplier including charm bonuses and set effects
        /// Base multiplier is 1.0f, enhanced by charm bonuses and special set bonuses
        /// 
        /// Set Bonus Enhancement:
        /// - "Flames of Crimson Rage": Doubles the charm bonus effectiveness (2% instead of 1%)
        /// 
        /// Used for all Burning Factor damage calculations
        /// </summary>
        /// <returns>Burning damage multiplier (1.0 = normal, 1.2 = 20% increase)</returns>
        public float GetBurningDamageMultiplier() {
            float baseMultiplier = 1.0f;
            float charmBonus = GetCharmBonus(c => c.BurningDamageBonus);
            
            // Check for set bonus enhancement
            if (HasCompleteCharmSet()) {
                var setName = EquippedCharms.Values.FirstOrDefault()?.SetName;
                if (setName == "Flames of Crimson Rage") {
                    // Set bonus: Each charm gives 2% instead of 1%
                    charmBonus *= 2f;
                }
            }
            
            return baseMultiplier + (charmBonus / 100f);
        }

        /// <summary>
        /// Calculates additional freeze duration from charm bonuses
        /// Applied to all Freeze Factor applications for enhanced control builds
        /// Used by Ice essence characters for extended card lockdown strategies
        /// </summary>
        /// <returns>Additional turns to add to freeze duration</returns>
        public int GetFreezeDurationBonus() => GetCharmBonus(c => c.FreezeDurationBonus);
        
        /// <summary>
        /// Calculates MP recovery bonus for normal attacks from charm bonuses
        /// Enhanced by weapon-specific charms and MP-focused builds
        /// Applied during normal attack resolution for resource sustainability
        /// </summary>
        /// <returns>Additional MP recovered during normal attacks</returns>
        public int GetMpRecoveryBonus() => GetCharmBonus(c => c.MpRecoveryBonus);
        
        #endregion

        #region Charm System Support Methods
        
        /// <summary>
        /// Helper method to sum integer bonuses from equipped charms
        /// Applies the selector function to each equipped charm and totals the results
        /// Used internally for computed property calculations
        /// </summary>
        /// <param name="selector">Function to extract bonus value from each charm</param>
        /// <returns>Total bonus value from all equipped charms</returns>
        private int GetCharmBonus(Func<Charm, int> selector) {
            return EquippedCharms.Values.Sum(selector);
        }

        /// <summary>
        /// Helper method to sum floating-point bonuses from equipped charms
        /// Applies the selector function to each equipped charm and totals the results
        /// Used for percentage-based bonuses and multiplicative effects
        /// </summary>
        /// <param name="selector">Function to extract bonus value from each charm</param>
        /// <returns>Total bonus value from all equipped charms</returns>
        private float GetCharmBonus(Func<Charm, float> selector) {
            return EquippedCharms.Values.Sum(selector);
        }

        /// <summary>
        /// Checks if character has a complete charm set equipped
        /// Requires all 5 charm slots filled with charms from the same set
        /// Used to determine set bonus eligibility and special ability access
        /// </summary>
        /// <returns>True if all 5 slots have charms from the same named set</returns>
        public bool HasCompleteCharmSet() {
            if (EquippedCharms.Count != 5) return false;
            
            var setNames = EquippedCharms.Values.Select(c => c.SetName).Distinct().ToList();
            return setNames.Count == 1 && !string.IsNullOrEmpty(setNames[0]);
        }

        /// <summary>
        /// Gets the name of the currently equipped charm set
        /// Returns null if no complete set is equipped
        /// Used for set bonus identification and special ability checks
        /// </summary>
        /// <returns>Name of equipped set, or null if incomplete/mixed sets</returns>
        public string GetEquippedSetName() {
            return HasCompleteCharmSet() ? EquippedCharms.Values.First().SetName : null;
        }
        
        #endregion

        #region Combat Mechanics
        
        /// <summary>
        /// Performs critical hit calculation using current CritRate
        /// Uses Godot's random number generator for consistent randomization
        /// Called during damage calculation to determine critical hit occurrence
        /// </summary>
        /// <returns>True if attack should deal critical damage</returns>
        public bool RollCritical() {
            return Godot.GD.Randf() < CritRate;
        }

        /// <summary>
        /// Checks if character is in low health state (below 25% of Max LP)
        /// Used for character passive abilities like Rok's berserker transformation
        /// Triggers special mechanics and enhanced abilities at critical health
        /// </summary>
        /// <returns>True if current LP is 25% or less of Max LP</returns>
        public bool IsLowHealth() {
            return LP <= (MaxLP * 0.25f);
        }

        /// <summary>
        /// Checks if character can use special abilities from specific charm sets
        /// Requires complete charm set matching the specified set name
        /// Used for set-exclusive abilities and enhanced mechanics
        /// </summary>
        /// <param name="setName">Name of the charm set to check for</param>
        /// <returns>True if character has complete matching set equipped</returns>
        public bool CanUseSetBonus(string setName) {
            return HasCompleteCharmSet() && GetEquippedSetName() == setName;
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Returns character name for debugging and display purposes
        /// Provides safe string representation even for malformed characters
        /// </summary>
        /// <returns>Character name or "Unknown Character" if name is null</returns>
        public override string ToString() => CharName ?? "Unknown Character";
        
        #endregion
    }

    #region Character Passive State System
    
    /// <summary>
    /// Character-specific passive ability state tracking system
    /// Maintains state for complex abilities requiring turn-based or condition-based tracking
    /// Enables character-unique mechanics and advanced ability interactions
    /// 
    /// Supported Character Abilities:
    /// - Rok: Berserker mode, Altering Pyre charges, Blazing Dash duration
    /// - Yu: Freeze application counting, Glacial Trap activation state
    /// </summary>
    public class CharacterPassiveState {
        
        #region Rok's Fire Essence Passive States
        
        /// <summary>
        /// Tracks if Rok's low health berserker mode is currently active
        /// Activated when LP drops below 25% of Max LP
        /// Transforms all attacks to Fire Damage with enhanced Burning effects
        /// </summary>
        public bool IsLowHealthModeActive { get; set; }
        
        /// <summary>
        /// Tracks if Rok's Altering Pyre ability is currently active
        /// Set to true on initial activation, false when all charges are consumed
        /// Controls access to the scaling damage/effect system
        /// </summary>
        public bool IsAlteringPyreActive { get; set; }
        
        /// <summary>
        /// Number of turns waited since Altering Pyre activation
        /// Determines which tier of effects can be used (1+, 3+, or 5+ turns)
        /// Incremented automatically during turn progression
        /// </summary>
        public int AlteringPyreTurnsWaited { get; set; }
        
        /// <summary>
        /// Remaining charges for Altering Pyre usage
        /// Starts at 3 on activation, decreases based on effect tier used
        /// Ability deactivates when charges reach zero
        /// </summary>
        public int AlteringPyreCharges { get; set; } = 3;
        
        /// <summary>
        /// Tracks if Rok's Blazing Dash ultimate is currently active
        /// Provides immunity and doubled burning damage effects
        /// Duration managed by attack counter rather than turns
        /// </summary>
        public bool IsBlazingDashActive { get; set; }
        
        /// <summary>
        /// Number of attacks remaining for Blazing Dash duration
        /// Decrements with each attack performed by the user
        /// Ultimate deactivates when counter reaches zero
        /// </summary>
        public int BlazingDashAttacksRemaining { get; set; }
        
        #endregion
        
        #region Yu's Ice Essence Passive States
        
        /// <summary>
        /// Counts successful freeze applications for Yu's UP charging
        /// Incremented each time Yu successfully freezes opponent cards
        /// Used to determine Ultimate Point charge progress
        /// </summary>
        public int FreezeApplicationCount { get; set; }
        
        /// <summary>
        /// Tracks if Yu's Glacial Trap passive enhancement is active
        /// When active, Ice Damage also applies Freeze if opponent has frozen cards
        /// Creates devastating freeze chain reactions and card lockdown
        /// </summary>
        public bool IsGlacialTrapActive { get; set; }
        
        #endregion
    }
    
    #endregion

    #region Status Effect Extension Methods
    
    /// <summary>
    /// Extension methods for efficient status effect checking using bitfield operations
    /// Provides clean syntax for testing specific status effects on characters
    /// Uses bitwise AND operations for optimal performance
    /// 
    /// Usage Examples:
    /// - user.StatusEffects.Has(Character.STATUS_EFFECT.IMMUNE)
    /// - target.StatusEffects.Has(Character.STATUS_EFFECT.BURNING | Character.STATUS_EFFECT.FREEZE)
    /// </summary>
    public static class StatusEffectResolver {
        
        /// <summary>
        /// Checks if specific status effect(s) are present in the effects bitfield
        /// Supports checking multiple effects simultaneously using bitwise OR
        /// Returns true if ANY of the specified effects are present
        /// </summary>
        /// <param name="effects">Current status effects bitfield</param>
        /// <param name="effect">Effect(s) to check for (can be combined with | operator)</param>
        /// <returns>True if any specified effect is present, false otherwise</returns>
        public static bool Has(this Character.STATUS_EFFECT effects, Character.STATUS_EFFECT effect) {
            return (effects & effect) != 0;
        }
    }
    
    #endregion

    #region Character Creation System
    
    /// <summary>
    /// Static factory class for character initialization and setup
    /// Handles character creation from data templates with proper stat scaling
    /// Ensures consistent character initialization across the game system
    /// </summary>
    public static class CharacterCreator {
        
        /// <summary>
        /// Creates and initializes a character from character data template
        /// Sets up all base stats, resources, and computed properties
        /// Applies star-based stat scaling and initializes resources to maximum values
        /// 
        /// Initialization Process:
        /// 1. Copy identity properties (name, star, essence, weapon)
        /// 2. Set base resource maximums from data
        /// 3. Initialize current resources to computed maximums
        /// 4. Apply star-based combat stat scaling
        /// 5. Initialize passive state tracking
        /// </summary>
        /// <param name="data">Character data template containing base statistics</param>
        /// <returns>Fully initialized character ready for game use</returns>
        public static Character InitCharacter(CharacterData data) {
            var character = new Character();
            
            // Set character identity properties
            character.CharName = data.charName;
            character.Star = data.star;
            character.EssenceType = data.essenceType;
            character.WeaponType = data.weaponType;
            
            // Set base resource statistics
            character.BaseMaxLP = data.maxLP;
            character.BaseMaxEP = data.maxEP;
            character.BaseMaxMP = data.maxMP;
            character.MaxUP = data.maxUP;
            character.MaxPotion = data.maxPotion;
            
            // Initialize current resources to computed maximums
            // Note: Uses computed properties to include any initial charm bonuses
            character.LP = character.MaxLP;
            character.EP = character.MaxEP;
            character.MP = character.MaxMP;
            character.UP = 0; // UP starts empty and must be charged
            character.Potion = character.MaxPotion;
            
            // Apply star-based combat stat scaling
            SetDefaultCombatStats(character);
            
            return character;
        }
        
        /// <summary>
        /// Applies star-based scaling to character combat statistics
        /// Higher star characters receive enhanced base attack and defense values
        /// Ensures consistent power scaling across character tiers
        /// 
        /// Scaling Formula:
        /// - ATK/EssenceATK: 80 + (star level * 20)
        /// - DEF/EssenceDEF: star level * 10
        /// 
        /// Star Level Examples:
        /// - 4 Star: 160 ATK, 40 DEF
        /// - 5 Star: 180 ATK, 50 DEF  
        /// - 6 Star: 200 ATK, 60 DEF
        /// </summary>
        /// <param name="character">Character to apply stat scaling to</param>
        private static void SetDefaultCombatStats(Character character) {
            int statMultiplier = (int)character.Star;
            
            // Base attack scales significantly with star level
            character.BaseATK = 80 + (statMultiplier * 20);
            character.BaseEssenceATK = 80 + (statMultiplier * 20);
            
            // Defense scales moderately with star level
            character.BaseDEF = statMultiplier * 10;
            character.BaseEssenceDEF = statMultiplier * 10;
        }
    }
    
    #endregion
}