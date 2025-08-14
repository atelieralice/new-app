using System;
using System.Linq;
using Godot;

namespace meph {
    
    /// <summary>
    /// Static utility class providing comprehensive damage calculation and application mechanics
    /// Implements all damage types, critical hit calculations, and character-specific damage modifiers
    /// Serves as the primary interface for combat damage processing and shield/defense interactions
    /// 
    /// Core Functionality:
    /// - Normal Damage: Physical attacks with weapon bonuses and defense calculations
    /// - Elemental Damage: Essence-based attacks with elemental bonuses and essence defense
    /// - Percentage Damage: MaxLP-based damage that bypasses defense but respects shields
    /// - Direct Damage: Shield-bypassing damage for special abilities like Altering Pyre
    /// - Critical Hit System: Percentage-based absolute damage scaling with target's MaxLP
    /// - Passive Integration: Character-specific damage modifications and special triggers
    /// 
    /// Game Rule Integration:
    /// - Defense Calculation: Damage reduction before shield application
    /// - Critical Hit Mechanics: Absolute damage based on target's maximum life points
    /// - Passive Conversions: Rok's berserker mode converts normal attacks to Fire damage
    /// - Factor Interactions: Burning multipliers, Toughness bonuses, and freeze mechanics
    /// </summary>
    public static class DamageCalculator {
        
        #region Normal Damage System
        
        /// <summary>
        /// Applies standard physical damage with all character modifiers and weapon bonuses
        /// Calculates total damage including ATK stat, weapon specialization, and defense reduction
        /// Handles critical hit calculations and passive damage type conversions
        /// 
        /// Damage Calculation Process:
        /// 1. Base damage + attacker's ATK stat
        /// 2. Add weapon-specific damage bonus (if applicable)
        /// 3. Apply target's DEF reduction (minimum 1 damage)
        /// 4. Roll for critical hit and add absolute damage if successful
        /// 5. Check for passive conversions (Rok's berserker mode → Fire damage)
        /// 6. Apply damage through GameManager shield system
        /// 
        /// Critical Hit Mechanics:
        /// - Calculated as percentage of target's MaxLP (stored as decimal: 0.05 = 5%)
        /// - Applied as absolute damage after defense calculations
        /// - Triggers additional logging for combat feedback
        /// 
        /// Passive Integration:
        /// - Rok's low health mode converts normal attacks to Fire elemental damage
        /// </summary>
        /// <param name="attacker">Character performing the attack</param>
        /// <param name="target">Character receiving the damage</param>
        /// <param name="baseDamage">Base damage value before modifiers</param>
        /// <param name="applyWeaponBonus">Whether to include weapon-specific damage bonuses</param>
        public static void ApplyNormalDamage(Character attacker, Character target, int baseDamage, bool applyWeaponBonus = true) {
            if (attacker == null || target == null) return;

            // Calculate total damage with all modifiers
            int totalDamage = baseDamage + attacker.ATK;
            
            // Add weapon-specific damage bonus if applicable
            if (applyWeaponBonus) {
                totalDamage += attacker.GetWeaponDamageBonus();
            }
            
            // Apply defense reduction
            totalDamage = Math.Max(1, totalDamage - target.DEF);
            
            // Check for critical hit
            bool isCrit = attacker.RollCritical();
            if (isCrit) {
                // CritDamage is stored as decimal (0.05 = 5%, 0.07 = 7%, etc.)
                int critBonus = (int)(target.MaxLP * attacker.CritDamage);
                totalDamage += critBonus;
                
                ConsoleLog.Combat($"Critical hit! {attacker.CharName} deals {critBonus} absolute damage ({attacker.CritDamage * 100f:F1}% of {target.CharName}'s Max LP)");
            }
            
            // Check if Rok is in low health mode - convert to fire damage
            if (attacker.PassiveState.IsLowHealthModeActive) {
                ApplyElementalDamage(attacker, target, totalDamage, Character.ESSENCE_TYPE.FIRE);
                return;
            }
            
            // Apply the damage through GameManager shield system
            GameManager.Instance.ApplyDamage(target, totalDamage);
            GameEvents.TriggerAttackResolved(attacker, target, totalDamage, isCrit);
        }
        
        #endregion
        
        #region Elemental Damage System
        
        /// <summary>
        /// Applies essence-based elemental damage with specialized bonuses and essence defense
        /// Calculates total damage including EssenceATK, element-specific bonuses, and factor interactions
        /// Handles critical hits, passive triggers, and character-specific elemental mechanics
        /// 
        /// Elemental Damage Calculation Process:
        /// 1. Base damage + attacker's EssenceATK stat
        /// 2. Add essence-specific damage bonus for matching element type
        /// 3. Apply factor-based bonuses (Toughness for Earth damage)
        /// 4. Apply target's EssenceDEF reduction (minimum 1 damage)
        /// 5. Roll for critical hit and add absolute damage if successful
        /// 6. Apply damage through GameManager shield system
        /// 7. Trigger character-specific passive effects (Yu's Glacial Trap)
        /// 
        /// Factor Integration:
        /// - Earth Essence: Receives Toughness factor damage bonuses
        /// - Ice Essence: Can trigger Yu's Glacial Trap passive for additional freeze effects
        /// 
        /// Critical Hit Mechanics:
        /// - Same system as normal damage: percentage of target's MaxLP as absolute damage
        /// - Applied after essence defense calculations
        /// </summary>
        /// <param name="attacker">Character performing the elemental attack</param>
        /// <param name="target">Character receiving the elemental damage</param>
        /// <param name="baseDamage">Base damage value before elemental modifiers</param>
        /// <param name="essenceType">Type of elemental damage being applied</param>
        public static void ApplyElementalDamage(Character attacker, Character target, int baseDamage, Character.ESSENCE_TYPE essenceType) {
            if (attacker == null || target == null) return;

            // Calculate total damage with essence modifiers
            int totalDamage = baseDamage + attacker.EssenceATK + attacker.GetEssenceDamageBonus(essenceType);
            
            // Apply Toughness earth damage bonus if this is earth essence
            if (essenceType == Character.ESSENCE_TYPE.EARTH) {
                var fm = GameManager.Instance?.FactorManager;
                if (fm != null) {
                    totalDamage += FactorLogic.GetToughnessEarthBonus(fm, attacker);
                }
            }
            
            // Apply essence defense reduction
            totalDamage = Math.Max(1, totalDamage - target.EssenceDEF);
            
            // Check for critical hit
            bool isCrit = attacker.RollCritical();
            if (isCrit) {
                // CritDamage is stored as decimal (0.05 = 5%, 0.07 = 7%, etc.)
                int critBonus = (int)(target.MaxLP * attacker.CritDamage);
                totalDamage += critBonus; // Applied after defense calculation as absolute damage
                
                ConsoleLog.Combat($"Critical hit! {attacker.CharName} deals {critBonus} absolute damage ({attacker.CritDamage * 100f:F1}% of {target.CharName}'s Max LP)");
            }
            
            // Apply the damage through GameManager shield system
            GameManager.Instance.ApplyDamage(target, totalDamage);
            GameEvents.TriggerAttackResolved(attacker, target, totalDamage, isCrit);
            
            ConsoleLog.Combat($"{attacker.CharName} dealt {totalDamage} {essenceType} damage to {target.CharName}");
            
            // Trigger passive effects for ice damage (Yu's Glacial Trap)
            if (essenceType == Character.ESSENCE_TYPE.ICE && attacker.PassiveState.IsGlacialTrapActive) {
                TriggerGlacialTrap(attacker, target);
            }
        }
        
        #endregion
        
        #region Percentage Damage System
        
        /// <summary>
        /// Applies percentage-based damage calculated from target's maximum life points
        /// Bypasses defense calculations but still respects shield systems and damage reduction
        /// Primarily used for factor effects like Burning damage and special abilities
        /// 
        /// Percentage Damage Mechanics:
        /// - Damage = target's MaxLP × percentage ÷ 100
        /// - Bypasses both normal DEF and EssenceDEF completely
        /// - Still affected by shield systems and damage reduction factors
        /// - Can be enhanced by character-specific multipliers (Burning damage bonus)
        /// 
        /// Burning Damage Enhancement:
        /// - Fire essence percentage damage applies burning damage multipliers
        /// - Character's GetBurningDamageMultiplier() scales the final damage
        /// - Set bonuses and charm effects can enhance burning effectiveness
        /// 
        /// Usage Examples:
        /// - Burning Factor: 2% Fire damage per turn
        /// - Special abilities requiring fixed percentage damage
        /// - Effects that should ignore armor but respect shields
        /// </summary>
        /// <param name="target">Character receiving the percentage damage</param>
        /// <param name="percentage">Percentage of MaxLP to deal as damage</param>
        /// <param name="essenceType">Optional essence type for damage enhancement and logging</param>
        /// <param name="source">Optional source character for damage multiplier calculations</param>
        public static void ApplyPercentageDamage(Character target, float percentage, Character.ESSENCE_TYPE? essenceType = null, Character source = null) {
            if (target == null) return;

            int damage = (int)(target.MaxLP * percentage / 100f);
            
            // Apply burning damage multiplier if this is burning damage and source has modifiers
            if (essenceType == Character.ESSENCE_TYPE.FIRE && source != null) {
                float multiplier = source.GetBurningDamageMultiplier();
                damage = (int)(damage * multiplier);
            }
            
            // Burning damage bypasses DEF and Essence DEF according to game rules
            // Apply through shield system with absolute damage flag
            GameManager.Instance.ApplyDamage(target, damage, true); // true = isAbsolute (bypasses DEF)
            
            if (essenceType.HasValue) {
                ConsoleLog.Combat($"{target.CharName} takes {damage} {essenceType} percentage damage ({percentage}% of Max LP) - bypasses defense");
            } else {
                ConsoleLog.Combat($"{target.CharName} takes {damage} percentage damage ({percentage}% of Max LP)");
            }
        }

        /// <summary>
        /// Applies direct percentage damage that completely bypasses all protection systems
        /// Used for special abilities that ignore both defense and shields (e.g., Rok's Altering Pyre)
        /// Still applies essence defense but bypasses all other damage reduction mechanisms
        /// 
        /// Direct Damage Mechanics:
        /// - Damage = target's MaxLP × percentage ÷ 100
        /// - Enhanced by burning damage multipliers if source character has them
        /// - Reduced by target's EssenceDEF (still elemental damage)
        /// - Bypasses all shield systems and damage reduction effects
        /// - Directly modifies target's LP with defeat checking
        /// 
        /// Special Use Cases:
        /// - Rok's Altering Pyre: Direct Fire damage that ignores shields
        /// - Ultimate abilities requiring guaranteed damage application
        /// - Abilities that specifically state "ignores all protection"
        /// 
        /// Safety Features:
        /// - Automatic defeat detection and handling
        /// - Resource change events for UI synchronization
        /// - Damage application events for combat logging
        /// </summary>
        /// <param name="target">Character receiving the direct damage</param>
        /// <param name="percentage">Percentage of MaxLP to deal as direct damage</param>
        /// <param name="essenceType">Essence type for damage enhancement and logging</param>
        /// <param name="source">Optional source character for burning damage multipliers</param>
        public static void ApplyDirectPercentageDamage(Character target, float percentage, Character.ESSENCE_TYPE essenceType, Character source = null) {
            if (target == null) return;

            int damage = (int)(target.MaxLP * percentage / 100f);
            
            // Apply burning damage multiplier if source has modifiers
            if (source != null) {
                float multiplier = source.GetBurningDamageMultiplier();
                damage = (int)(damage * multiplier);
            }
            
            // Apply essence defense (this is still elemental damage)
            damage = Math.Max(1, damage - target.EssenceDEF);
            
            // Bypass shields - apply damage directly
            int oldLP = target.LP;
            target.LP = Math.Max(target.LP - damage, 0);
            
            if (oldLP > target.LP) {
                GameEvents.TriggerResourceLost(target, oldLP - target.LP, "LP");
            }
            
            GameEvents.TriggerDamageDealt(target, damage, target.LP);
            ConsoleLog.Combat($"{target.CharName} takes {damage} direct {essenceType} percentage damage ({percentage}% of Max LP)");
            
            // Check for defeat
            if (target.LP <= 0) {
                GameManager.Instance.HandlePlayerDefeat(target);
            }
        }
        
        #endregion
        
        #region Character-Specific Passive Effects
        
        /// <summary>
        /// Triggers Yu's Glacial Trap passive effect during Ice damage application
        /// Automatically freezes additional opponent cards when conditions are met
        /// Implements cascading freeze mechanics for Ice essence control strategies
        /// 
        /// Glacial Trap Mechanics:
        /// - Triggers when Yu deals Ice damage and Glacial Trap passive is active
        /// - Requires opponent to have at least one frozen card as activation condition
        /// - Targets random non-frozen, non-C type card from opponent's equipped slots
        /// - Applies 2 + freeze duration bonus turns of freeze effect
        /// 
        /// Strategic Impact:
        /// - Escalating freeze control: More frozen cards → more opportunities to freeze more cards
        /// - Pressure opponent to manage frozen cards or face cascading lockdown
        /// - Synergizes with freeze duration bonuses from charms and set effects
        /// 
        /// Targeting Rules:
        /// - Excludes already frozen cards to prevent redundant applications
        /// - Excludes C-type cards (Charm cards cannot be frozen)
        /// - Random selection among valid targets for unpredictability
        /// </summary>
        /// <param name="attacker">Yu character triggering the Glacial Trap effect</param>
        /// <param name="target">Target character (used to identify opponent)</param>
        private static void TriggerGlacialTrap(Character attacker, Character target) {
            var opponent = GameManager.Instance?.GetOpponent(attacker);
            if (opponent == null) return;

            // Check if opponent has any frozen cards
            bool hasFrozenCards = false;
            foreach (var card in opponent.EquippedSlots.Values) {
                if (card != null && card.IsFrozen) {
                    hasFrozenCards = true;
                    break;
                }
            }

            if (hasFrozenCards) {
                // Apply freeze to a random non-frozen card
                var availableCards = opponent.EquippedSlots.Values.Where(c => c != null && !c.IsFrozen && c.Type != Card.TYPE.C).ToList();
                if (availableCards.Count > 0) {
                    var randomCard = availableCards[(int)(GD.Randi() % availableCards.Count)];
                    int freezeDuration = 2 + attacker.GetFreezeDurationBonus();
                    FactorLogic.FreezeCard(randomCard, freezeDuration);
                    ConsoleLog.Combat($"Glacial Trap triggered - {randomCard.Name} frozen for {freezeDuration} turns");
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Checks if a character has any frozen cards in their equipped slots
        /// Used for card effect conditions and passive ability triggers
        /// Provides fast boolean check without detailed frozen card analysis
        /// 
        /// Usage Examples:
        /// - Card effects requiring frozen cards as activation conditions
        /// - Passive abilities that trigger based on freeze status
        /// - UI indicators showing freeze-dependent states
        /// </summary>
        /// <param name="character">Character to check for frozen cards</param>
        /// <returns>True if character has at least one frozen card, false otherwise</returns>
        public static bool HasFrozenCards(Character character) {
            return character.EquippedSlots.Values.Any(card => card != null && card.IsFrozen);
        }

        /// <summary>
        /// Counts the total number of frozen cards in a character's equipped slots
        /// Used for scaling damage effects and passive ability calculations
        /// Provides precise count for effects that scale with freeze accumulation
        /// 
        /// Usage Examples:
        /// - Yu's turn-start passive: 100 damage per frozen card
        /// - Scaling effects that increase with freeze count
        /// - Strategic analysis for freeze-based deck archetypes
        /// </summary>
        /// <param name="character">Character to count frozen cards for</param>
        /// <returns>Total number of frozen cards currently equipped</returns>
        public static int CountFrozenCards(Character character) {
            return character.EquippedSlots.Values.Count(card => card != null && card.IsFrozen);
        }
        
        #endregion
    }
}