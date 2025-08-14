using meph;
using Godot;
using System;
using System.Collections.Generic;

namespace meph {
    
    /// <summary>
    /// Static utility class providing core character operations and game mechanics
    /// Implements equipment management, combat actions, and resource manipulation
    /// Serves as the primary interface for character-related game logic operations
    /// 
    /// Core Functionality:
    /// - Equipment System: Card equipping and slot management with validation
    /// - Combat Operations: Normal attacks, card usage, and damage resolution
    /// - Resource Management: MP/EP/UP spending, gaining, and affordability checks
    /// - Action Integration: StateManager coordination for turn-based mechanics
    /// - Event System: Comprehensive game event triggering for UI and logging
    /// </summary>
    public static class CharacterLogic {
        
        #region Equipment Management System
        
        /// <summary>
        /// Equips a card to the appropriate character slot with validation
        /// Prevents slot conflicts and ensures proper equipment tracking
        /// Triggers equipment events for UI updates and game state management
        /// 
        /// Equipment Rules:
        /// - Each card type has a dedicated slot (BW, SW, E, W, Q, P, U, C)
        /// - Existing cards in slots must be unequipped before new equipment
        /// - Character cards (C type) define identity and passive abilities
        /// - Equipment changes trigger GameEvents for UI synchronization
        /// </summary>
        /// <param name="character">Character receiving the equipment</param>
        /// <param name="card">Card to be equipped (null check performed)</param>
        public static void EquipCardToSlot(Character character, Card card) {
            if (card == null || character == null) return;

            if (character.EquippedSlots.TryGetValue(card.Type, out Card existingCard) && existingCard != null) {
                ConsoleLog.Warn($"Slot {card.Type} is already occupied by {existingCard.Name}.");
                return;
            }

            character.EquippedSlots[card.Type] = card;
            GameEvents.TriggerCardEquipped(character, card);
            // Event system handles logging to avoid duplication
        }
        
        #endregion

        #region Card Usage and Action System
        
        /// <summary>
        /// Executes equipped card effects through the action management system
        /// Validates card availability, freeze status, and action economy
        /// Coordinates with StateManager for turn-based action tracking
        /// 
        /// Action Economy Rules:
        /// - Standard cards consume 1 action per turn (managed by StateManager)
        /// - Swift cards bypass action economy restrictions
        /// - Frozen cards cannot be used regardless of action availability
        /// - Card effects are executed through the centralized effect system
        /// </summary>
        /// <param name="user">Character using the card</param>
        /// <param name="slotType">Equipment slot type to activate</param>
        /// <param name="target">Target character for the card effect</param>
        /// <param name="isSwift">Override for swift action mechanics</param>
        public static void UseSlot(Character user, Card.TYPE slotType, Character target, bool isSwift = false) {
            if (user == null) {
                ConsoleLog.Warn("Cannot use slot - user is null");
                return;
            }

            if (!user.EquippedSlots.TryGetValue(slotType, out Card card) || card == null) {
                ConsoleLog.Warn($"No card equipped in {slotType} slot.");
                return;
            }

            if (card.IsFrozen) {
                ConsoleLog.Warn($"{card.Name} is frozen and cannot be used.");
                return;
            }

            // Coordinate with action management system
            var stateManager = GameManager.Instance?.StateManager;
            if (stateManager != null) {
                stateManager.TryAction(() => {
                    ExecuteCardEffect(user, card, target);
                }, isSwift || card.IsSwift);
            } else {
                ExecuteCardEffect(user, card, target);
            }
        }

        /// <summary>
        /// Private helper method for executing card effects with event coordination
        /// Invokes card effect delegates and triggers appropriate game events
        /// Centralizes effect execution for consistent logging and state tracking
        /// 
        /// Effect Execution Flow:
        /// 1. Invoke card's effect delegate with user and target parameters
        /// 2. Trigger CardUsed event for UI updates and logging
        /// 3. Allow GameManager to handle effect resolution and state changes
        /// </summary>
        /// <param name="user">Character using the card</param>
        /// <param name="card">Card whose effect is being executed</param>
        /// <param name="target">Target character for the effect</param>
        private static void ExecuteCardEffect(Character user, Card card, Character target) {
            card.Effect?.Invoke(user, target);
            GameEvents.TriggerCardUsed(user, card, target);
            // Event system handles comprehensive logging
        }
        
        #endregion

        #region Combat System Operations
        
        /// <summary>
        /// Executes normal attack using both equipped weapon cards simultaneously
        /// Combines Base Weapon (BW) and Secondary Weapon (SW) effects
        /// Integrates with passive abilities and character-specific mechanics
        /// 
        /// Normal Attack Mechanics:
        /// - Requires both BW and SW cards to be equipped
        /// - Executes both weapon effects in sequence
        /// - Applies charm-based MP recovery bonuses
        /// - Manages Blazing Dash attack counter for Rok's ultimate
        /// - Consumes 1 action through StateManager integration
        /// </summary>
        /// <param name="attacker">Character performing the normal attack</param>
        /// <param name="target">Character receiving the attack effects</param>
        public static void PerformNormalAttack(Character attacker, Character target) {
            if (attacker == null) {
                ConsoleLog.Warn("Cannot perform normal attack - attacker is null");
                return;
            }

            if (!attacker.EquippedSlots.TryGetValue(Card.TYPE.BW, out Card baseWeapon) ||
                !attacker.EquippedSlots.TryGetValue(Card.TYPE.SW, out Card secondaryWeapon)) {
                ConsoleLog.Warn($"{attacker.CharName} cannot perform normal attack - missing weapons.");
                return;
            }

            var stateManager = GameManager.Instance?.StateManager;
            stateManager?.TryAction(() => {
                // Execute both weapon effects simultaneously
                baseWeapon?.Effect?.Invoke(attacker, target);
                secondaryWeapon?.Effect?.Invoke(attacker, target);

                // Apply charm-based MP recovery (e.g., Yu's Guilt of Betrayal set)
                int mpRecovery = attacker.GetMpRecoveryBonus();
                if (mpRecovery > 0) {
                    CharacterLogic.GainResource(attacker, "MP", mpRecovery);
                    ConsoleLog.Resource($"{attacker.CharName} recovered {mpRecovery} MP from normal attack");
                }

                // Handle Rok's Blazing Dash ultimate attack counting
                if (attacker.PassiveState.IsBlazingDashActive) {
                    attacker.PassiveState.BlazingDashAttacksRemaining--;
                    if (attacker.PassiveState.BlazingDashAttacksRemaining <= 0) {
                        attacker.PassiveState.IsBlazingDashActive = false;
                        // Remove immunity when ultimate expires
                        var fm = GameManager.Instance?.FactorManager;
                        if (fm != null) {
                            fm.RemoveAllFactors(attacker, Character.STATUS_EFFECT.IMMUNE);
                        }
                        ConsoleLog.Combat($"{attacker.CharName}'s Blazing Dash expired");
                    }
                }

                GameEvents.TriggerNormalAttack(attacker, Card.TYPE.BW);
                // Event system handles detailed combat logging
            });
        }

        /// <summary>
        /// Resolves attack damage with critical hit calculation and application
        /// Handles critical hit probability, damage bonuses, and final damage resolution
        /// Integrates with game damage system and event tracking
        /// 
        /// Damage Resolution Process:
        /// 1. Roll for critical hit using attacker's CritRate
        /// 2. Calculate critical bonus as percentage of base damage (CritDamage)
        /// 3. Apply final damage through GameManager damage system
        /// 4. Trigger attack resolution events for UI and logging
        /// </summary>
        /// <param name="attacker">Character dealing the damage</param>
        /// <param name="target">Character receiving the damage</param>
        /// <param name="baseDamage">Base damage before critical hit calculation</param>
        public static void ResolveAttackDamage(Character attacker, Character target, int baseDamage) {
            if (attacker == null || target == null) {
                ConsoleLog.Warn("Cannot resolve attack damage - attacker or target is null");
                return;
            }

            bool isCrit = attacker.RollCritical();
            int finalDamage = baseDamage;

            if (isCrit) {
                // Critical hit bonus as Absolute Damage (game rule: 5% default)
                int critBonus = (int)(baseDamage * attacker.CritDamage);
                finalDamage += critBonus;
            }

            // Apply damage through centralized damage system
            GameManager.Instance.ApplyDamage(target, finalDamage);

            // Trigger comprehensive attack resolution event
            GameEvents.TriggerAttackResolved(attacker, target, finalDamage, isCrit);
        }
        
        #endregion

        #region Resource Management System
        
        /// <summary>
        /// Spends character resources with validation and event integration
        /// Supports all resource types: EP (Energy), MP (Mana), UP (Ultimate)
        /// Prevents overspending and triggers resource loss events for UI updates
        /// 
        /// Resource Types:
        /// - EP: Physical abilities and weapon skills (5% regeneration per turn)
        /// - MP: Magical abilities and essence skills (2% regeneration per turn)
        /// - UP: Ultimate abilities (charged through specific conditions)
        /// 
        /// Validation ensures sufficient resources before consumption
        /// </summary>
        /// <param name="character">Character spending the resource</param>
        /// <param name="resourceType">Type of resource (EP/MP/UP, case insensitive)</param>
        /// <param name="amount">Amount to spend (validated against current resources)</param>
        public static void SpendResource(Character character, string resourceType, int amount) {
            if (character == null) {
                ConsoleLog.Warn("Cannot spend resource - character is null");
                return;
            }

            switch (resourceType.ToUpper()) {
                case "EP":
                    if (character.EP >= amount) {
                        character.EP -= amount;
                        GameEvents.TriggerResourceLost(character, amount, "EP");
                        ConsoleLog.Resource($"{character.CharName} spent {amount} EP");
                    } else {
                        ConsoleLog.Warn($"{character.CharName} doesn't have enough EP ({character.EP}/{amount})");
                    }
                    break;
                case "MP":
                    if (character.MP >= amount) {
                        character.MP -= amount;
                        GameEvents.TriggerResourceLost(character, amount, "MP");
                        ConsoleLog.Resource($"{character.CharName} spent {amount} MP");
                    } else {
                        ConsoleLog.Warn($"{character.CharName} doesn't have enough MP ({character.MP}/{amount})");
                    }
                    break;
                case "UP":
                    if (character.UP >= amount) {
                        character.UP -= amount;
                        GameEvents.TriggerResourceLost(character, amount, "UP");
                        ConsoleLog.Resource($"{character.CharName} spent {amount} UP");
                    } else {
                        ConsoleLog.Warn($"{character.CharName} doesn't have enough UP ({character.UP}/{amount})");
                    }
                    break;
                default:
                    ConsoleLog.Error($"Unknown resource type: {resourceType}");
                    break;
            }
        }

        /// <summary>
        /// Grants character resources with maximum limit enforcement and event integration
        /// Supports all resource types with proper capping and actual gain calculation
        /// Triggers resource gain events only for meaningful increases (non-zero gains)
        /// 
        /// Resource Capping Rules:
        /// - LP: Capped at computed MaxLP (base + charm bonuses)
        /// - EP: Capped at computed MaxEP (base + charm bonuses)
        /// - MP: Capped at computed MaxMP (base + charm bonuses)
        /// - UP: Capped at MaxUP (fixed value, no charm bonuses)
        /// 
        /// Only triggers events for actual resource gains to avoid spam
        /// </summary>
        /// <param name="character">Character receiving the resource</param>
        /// <param name="resourceType">Type of resource (LP/EP/MP/UP, case insensitive)</param>
        /// <param name="amount">Amount to grant (capped at maximum values)</param>
        public static void GainResource(Character character, string resourceType, int amount) {
            if (character == null) {
                ConsoleLog.Warn("Cannot gain resource - character is null");
                return;
            }

            switch (resourceType.ToUpper()) {
                case "LP":
                    int oldLP = character.LP;
                    character.LP = Math.Min(character.LP + amount, character.MaxLP);
                    int actualGain = character.LP - oldLP;
                    if (actualGain > 0) {
                        GameEvents.TriggerResourceGained(character, actualGain, "LP");
                    }
                    break;
                case "EP":
                    int oldEP = character.EP;
                    character.EP = Math.Min(character.EP + amount, character.MaxEP);
                    int actualEPGain = character.EP - oldEP;
                    if (actualEPGain > 0) {
                        GameEvents.TriggerResourceGained(character, actualEPGain, "EP");
                    }
                    break;
                case "MP":
                    int oldMP = character.MP;
                    character.MP = Math.Min(character.MP + amount, character.MaxMP);
                    int actualMPGain = character.MP - oldMP;
                    if (actualMPGain > 0) {
                        GameEvents.TriggerResourceGained(character, actualMPGain, "MP");
                    }
                    break;
                case "UP":
                    int oldUP = character.UP;
                    character.UP = Math.Min(character.UP + amount, character.MaxUP);
                    int actualUPGain = character.UP - oldUP;
                    if (actualUPGain > 0) {
                        GameEvents.TriggerResourceGained(character, actualUPGain, "UP");
                    }
                    break;
                default:
                    ConsoleLog.Error($"Unknown resource type: {resourceType}");
                    break;
            }
        }
        
        #endregion

        #region Resource Validation System
        
        /// <summary>
        /// Validates character can afford multiple resource requirements
        /// Used for complex abilities requiring multiple resource types simultaneously
        /// Performs complete affordability check without consuming resources
        /// 
        /// Common Usage Patterns:
        /// - Skill cards requiring both MP and EP (e.g., Wounding Ember: 90 MP + 50 EP)
        /// - Ultimate abilities with UP requirements
        /// - Complex abilities with scaling resource costs
        /// 
        /// Returns false immediately if any requirement cannot be met
        /// </summary>
        /// <param name="character">Character whose resources are being checked</param>
        /// <param name="requirements">Dictionary of resource type to required amount</param>
        /// <returns>True if character can afford all requirements, false otherwise</returns>
        public static bool CanAfford(Character character, Dictionary<string, int> requirements) {
            if (character == null) return false;

            foreach (var req in requirements) {
                switch (req.Key.ToUpper()) {
                    case "EP":
                        if (character.EP < req.Value) return false;
                        break;
                    case "MP":
                        if (character.MP < req.Value) return false;
                        break;
                    case "UP":
                        if (character.UP < req.Value) return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Comprehensive card usage validation including resource requirements
        /// Validates both resource affordability and card-specific restrictions
        /// Provides detailed logging for failed validation attempts
        /// 
        /// Validation Process:
        /// 1. Check if card has resource requirements
        /// 2. Validate each requirement against current character resources
        /// 3. Log specific resource deficiencies for debugging
        /// 4. Return true only if all requirements are satisfied
        /// 
        /// Used before executing card effects to prevent invalid usage
        /// </summary>
        /// <param name="user">Character attempting to use the card</param>
        /// <param name="card">Card being validated for usage</param>
        /// <param name="target">Target character (for potential future validation)</param>
        /// <returns>True if card can be used, false if requirements are not met</returns>
        public static bool CanAffordAndUseCard(Character user, Card card, Character target) {
            // Validate card has resource requirements
            if (card.Requirements != null && card.Requirements.Count > 0) {
                foreach (var requirement in card.Requirements) {
                    int currentAmount = requirement.Key switch {
                        "MP" => user.MP,
                        "EP" => user.EP,
                        "UP" => user.UP,
                        _ => 0
                    };
                    
                    if (currentAmount < requirement.Value) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford {card.Name} - insufficient {requirement.Key}");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        #endregion
    }
}