using System;

namespace meph {
    
    /// <summary>
    /// Static management system for character-specific passive abilities and automated behaviors
    /// Implements event-driven passive mechanics that trigger during gameplay without player input
    /// Coordinates with GameEvents system for seamless integration with turn management and combat
    /// 
    /// Passive System Features:
    /// - Event-Driven Architecture: Subscribes to game events for automatic passive triggering
    /// - Character-Specific Logic: Tailored passive implementations for each character's unique abilities
    /// - State Management: Tracks passive conditions and triggers for consistent behavior
    /// - Resource Integration: Handles UP charging, resource generation, and ability state changes
    /// - Combat Integration: Applies passive effects during attacks, turns, and special conditions
    /// 
    /// Supported Characters:
    /// - Rok: Low health berserker mode, Altering Pyre management, attack-triggered burning
    /// - Yu: Frozen card punishment, freeze-based UP charging, turn-start damage scaling
    /// </summary>
    public static class CharacterPassives {
        
        #region Passive Initialization System
        
        /// <summary>
        /// Initializes all passive abilities for a character through event subscription
        /// Sets up character-specific event handlers for automated passive triggering
        /// Called during character creation to establish passive behavior patterns
        /// 
        /// Initialization Process:
        /// 1. Identify character by name
        /// 2. Subscribe to relevant game events (OnTurnStarted, OnAttackResolved, etc.)
        /// 3. Setup character-specific passive state tracking
        /// 4. Register event handlers for automated passive execution
        /// 
        /// Event subscription ensures passives trigger automatically during gameplay
        /// </summary>
        /// <param name="character">Character whose passives should be initialized</param>
        public static void InitializePassives(Character character) {
            if (character == null) {
                ConsoleLog.Warn("Cannot initialize passives - character is null");
                return;
            }

            switch (character.CharName) {
                case "Rok":
                    InitializeRokPassives(character);
                    break;
                case "Yu":
                    InitializeYuPassives(character);
                    break;
                default:
                    ConsoleLog.Info($"No passive abilities defined for {character.CharName}");
                    break;
            }
        }
        
        #endregion

        #region Rok Fire Essence Passive System
        
        /// <summary>
        /// Initializes Rok's Fire Essence passive abilities through event subscription
        /// Establishes automated berserker mode detection and attack enhancement systems
        /// 
        /// Rok's Passive Abilities:
        /// - Low Health Berserker: When LP < 25% MaxLP, all attacks become Fire damage with Burning
        /// - Extended Burning: Low health mode increases Burning Time (BT) by 1 turn
        /// - State Monitoring: Automatic detection of health threshold crossings
        /// 
        /// Event Integration:
        /// - OnTurnStarted: Monitors health percentage for berserker mode activation
        /// - OnAttackResolved: Applies enhanced burning effects during low health
        /// </summary>
        /// <param name="character">Rok character to initialize passives for</param>
        private static void InitializeRokPassives(Character character) {
            // Monitor health status changes each turn for berserker mode
            GameEvents.OnTurnStarted += (c) => {
                if (c == character) {
                    CheckRokLowHealthMode(character);
                }
            };

            // Apply enhanced burning during low health attacks
            GameEvents.OnAttackResolved += (attacker, target, damage, wasCrit) => {
                if (attacker == character && character.IsLowHealth()) {
                    // Transform attack effects: Fire damage + enhanced burning
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        // Base burning: 2% for 2 turns, +1 turn from low health passive
                        FactorLogic.AddBurning(fm, target, 2, 3, character); // Extended duration
                        ConsoleLog.Combat($"{character.CharName}'s berserker mode triggered - enemy gains burning with extended duration");
                    }
                }
            };

            ConsoleLog.Info($"Initialized passive abilities for {character.CharName} - Fire Essence berserker mode");
        }

        /// <summary>
        /// Monitors and updates Rok's low health berserker mode state
        /// Detects health threshold crossings and applies appropriate state changes
        /// Triggers passive state events for UI updates and game state synchronization
        /// 
        /// Berserker Mode Mechanics:
        /// - Activation Threshold: LP drops below 25% of MaxLP
        /// - Deactivation Threshold: LP rises above 25% of MaxLP (via healing)
        /// - State Persistence: Mode remains active until threshold is crossed again
        /// - Combat Effects: All attacks deal Fire damage and apply enhanced Burning
        /// 
        /// State tracking prevents redundant activations and provides clear transitions
        /// </summary>
        /// <param name="character">Rok character to check health status for</param>
        private static void CheckRokLowHealthMode(Character character) {
            bool wasLowHealth = character.PassiveState.IsLowHealthModeActive;
            bool isLowHealth = character.IsLowHealth();

            if (isLowHealth != wasLowHealth) {
                character.PassiveState.IsLowHealthModeActive = isLowHealth;
                
                if (isLowHealth) {
                    ConsoleLog.Combat($"{character.CharName} entered berserker mode - all attacks now deal Fire damage with enhanced Burning");
                } else {
                    ConsoleLog.Combat($"{character.CharName} exited berserker mode - attacks return to normal damage type");
                }
                
                GameEvents.TriggerPassiveStateChanged(character, "LowHealthMode");
            }
        }

        /// <summary>
        /// Updates Rok's Altering Pyre ability state and UP charging mechanics
        /// Manages turn counting and Ultimate Point generation for extended Altering Pyre usage
        /// Called during turn progression to maintain Altering Pyre timing mechanics
        /// 
        /// Altering Pyre UP Charging:
        /// - Condition: Altering Pyre must be active for 6+ consecutive turns
        /// - Reward: Grants 1 UP charge for sustained strategic patience
        /// - Reset: Turn counter resets after UP gain to enable repeated charging
        /// - Limit: UP cannot exceed character's MaxUP value
        /// 
        /// Encourages strategic timing and extended ability usage planning
        /// </summary>
        /// <param name="character">Rok character to update Altering Pyre state for</param>
        public static void UpdateAlteringPyre(Character character) {
            if (character?.CharName != "Rok") return;

            if (character.PassiveState.IsAlteringPyreActive) {
                character.PassiveState.AlteringPyreTurnsWaited++;
                
                // UP charging for extended Altering Pyre usage (6+ turns)
                if (character.PassiveState.AlteringPyreTurnsWaited >= 6) {
                    if (character.UP < character.MaxUP) {
                        character.UP++;
                        ConsoleLog.Resource($"{character.CharName} gained 1 UP from extended Altering Pyre usage");
                        GameEvents.TriggerResourceGained(character, 1, "UP");
                    }
                    
                    // Reset counter to enable repeated UP charging
                    character.PassiveState.AlteringPyreTurnsWaited = 0;
                }
            }
        }
        
        #endregion

        #region Yu Ice Essence Passive System
        
        /// <summary>
        /// Initializes Yu's Ice Essence passive abilities through event subscription
        /// Establishes automated frozen card punishment and freeze-based resource generation
        /// 
        /// Yu's Passive Abilities:
        /// - Frozen Card Punishment: 100 Ice damage per frozen card at turn start
        /// - Freeze-Based UP Charging: Every 10 freeze applications grants 1 UP
        /// - Escalating Pressure: Damage scales with freeze accumulation
        /// 
        /// Event Integration:
        /// - OnTurnStarted: Executes frozen card damage calculation
        /// - OnCardFrozen: Tracks freeze applications for UP charging system
        /// </summary>
        /// <param name="character">Yu character to initialize passives for</param>
        private static void InitializeYuPassives(Character character) {
            // Execute frozen card punishment at turn start
            GameEvents.OnTurnStarted += (c) => {
                if (c == character) {
                    ExecuteYuTurnStartPassive(character);
                }
            };

            // Track freeze applications for UP charging system
            GameEvents.OnCardFrozen += (card, duration) => {
                // Note: In full implementation, would verify Yu caused the freeze
                character.PassiveState.FreezeApplicationCount++;
                
                // UP charging: Every 10 freeze applications = 1 UP
                if (character.PassiveState.FreezeApplicationCount >= 10) {
                    int upGain = Math.Min(1, character.MaxUP - character.UP);
                    if (upGain > 0) {
                        character.UP += upGain;
                        character.PassiveState.FreezeApplicationCount = 0;
                        ConsoleLog.Resource($"{character.CharName} gained {upGain} UP from freeze applications");
                        GameEvents.TriggerResourceGained(character, upGain, "UP");
                    }
                }
            };

            ConsoleLog.Info($"Initialized passive abilities for {character.CharName} - Ice Essence freeze control");
        }

        /// <summary>
        /// Executes Yu's turn-start passive ability: frozen card punishment damage
        /// Calculates and applies Ice damage based on opponent's frozen card count
        /// Scales damage linearly with freeze accumulation for escalating pressure
        /// 
        /// Frozen Card Punishment Mechanics:
        /// - Base Damage: 100 Ice damage per frozen card
        /// - Damage Type: Essence damage (affected by EssenceDEF)
        /// - Scaling: Linear scaling encourages freeze accumulation strategies
        /// - Timing: Executes at the beginning of Yu's turns automatically
        /// 
        /// Creates strategic pressure for opponents to manage frozen cards
        /// </summary>
        /// <param name="character">Yu character executing the turn-start passive</param>
        private static void ExecuteYuTurnStartPassive(Character character) {
            var opponent = GameManager.Instance?.GetOpponent(character);
            if (opponent == null) return;

            // Count opponent's currently frozen cards
            int frozenCardCount = 0;
            foreach (var card in opponent.EquippedSlots.Values) {
                if (card != null && card.IsFrozen) {
                    frozenCardCount++;
                }
            }

            if (frozenCardCount > 0) {
                int damage = frozenCardCount * 100; // 100 Ice damage per frozen card
                ConsoleLog.Combat($"{character.CharName}'s passive: {opponent.CharName} takes {damage} Ice damage from {frozenCardCount} frozen cards");
                
                // Apply essence-type Ice damage
                DamageCalculator.ApplyElementalDamage(character, opponent, damage, Character.ESSENCE_TYPE.ICE);
                GameEvents.TriggerPassiveTriggered(character);
            }
        }
        
        #endregion

        #region Universal Passive Management
        
        /// <summary>
        /// Executes all turn-start passive effects for a character
        /// Centralizes turn-start passive triggering for consistent execution timing
        /// Called by turn management system to ensure all passives activate appropriately
        /// 
        /// Turn-Start Passive Categories:
        /// - Damage Effects: Yu's frozen card punishment
        /// - State Monitoring: Rok's health threshold detection
        /// - Resource Generation: Character-specific UP charging mechanics
        /// - Condition Checks: Passive activation/deactivation triggers
        /// 
        /// Provides single entry point for turn-based passive execution
        /// </summary>
        /// <param name="character">Character whose turn-start passives should execute</param>
        public static void ExecuteTurnStartEffects(Character character) {
            if (character == null) {
                ConsoleLog.Warn("Cannot execute turn-start effects - character is null");
                return;
            }
            
            switch (character.CharName) {
                case "Yu":
                    ExecuteYuTurnStartPassive(character);
                    break;
                case "Rok":
                    // Rok's primary passive is attack-based, but check health mode
                    CheckRokLowHealthMode(character);
                    UpdateAlteringPyre(character);
                    break;
                default:
                    // Future characters can be added here as they're implemented
                    break;
            }
        }

        /// <summary>
        /// Executes character-specific turn-end passive effects and state updates
        /// Handles passive mechanics that trigger at the conclusion of character turns
        /// Future implementation for turn-end based passive abilities
        /// 
        /// Potential Turn-End Effects:
        /// - Resource regeneration bonuses
        /// - Delayed passive triggers
        /// - State expiration management
        /// - Condition-based passive activations
        /// </summary>
        /// <param name="character">Character whose turn-end passives should execute</param>
        public static void ExecuteTurnEndEffects(Character character) {
            if (character == null) return;

            // Currently no turn-end passives implemented
            // Reserved for future character abilities requiring turn-end timing
            
            switch (character.CharName) {
                case "Rok":
                    // Future: Turn-end Altering Pyre effects
                    break;
                case "Yu":
                    // Future: Turn-end freeze consolidation effects
                    break;
                default:
                    break;
            }
        }
        
        #endregion
    }
}