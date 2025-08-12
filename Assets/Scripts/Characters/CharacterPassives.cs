using System;

namespace meph {
    public static class CharacterPassives {
        public static void InitializePassives(Character character) {
            switch (character.CharName) {
                case "Rok":
                    InitializeRokPassives(character);
                    break;
                case "Yu":
                    InitializeYuPassives(character);
                    break;
            }
        }

        private static void InitializeRokPassives(Character character) {
            // Subscribe to relevant events for Rok's passive
            GameEvents.OnTurnStarted += (c) => {
                if (c == character) {
                    CheckRokLowHealthMode(character);
                }
            };

            // Subscribe to attack events to check for low health burning
            GameEvents.OnAttackResolved += (attacker, target, damage, wasCrit) => {
                if (attacker == character && character.IsLowHealth()) {
                    // All attacks deal fire damage and gain burning when low health
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        // Apply normal burning - let AddBurning handle the duration increase
                        FactorLogic.AddBurning(fm, target, 2, 2, character); // Base: 2% for 2 turns, +1 from low health
                        ConsoleLog.Combat($"{character.CharName}'s low health passive triggered - enemy gains burning with extended duration");
                    }
                }
            };
        }

        private static void InitializeYuPassives(Character character) {
            // Yu's passive: At beginning of turns, opponent takes 100 Ice Damage per frozen card
            GameEvents.OnTurnStarted += (c) => {
                if (c == character) {
                    ExecuteYuTurnStartPassive(character);
                }
            };

            // Track freeze applications for UP charging (every 10 freeze applications = 1 UP)
            GameEvents.OnCardFrozen += (card, duration) => {
                // Only count if Yu caused the freeze (we'd need to track this in freeze logic)
                character.PassiveState.FreezeApplicationCount++;
                
                if (character.PassiveState.FreezeApplicationCount >= 10) {
                    character.UP = Math.Min(character.UP + 1, character.MaxUP);
                    character.PassiveState.FreezeApplicationCount = 0;
                    ConsoleLog.Resource($"{character.CharName} gained 1 UP from freeze applications");
                    GameEvents.TriggerResourceGained(character, 1, "UP");
                }
            };
        }

        private static void CheckRokLowHealthMode(Character character) {
            bool wasLowHealth = character.PassiveState.IsLowHealthModeActive;
            bool isLowHealth = character.IsLowHealth();

            if (isLowHealth != wasLowHealth) {
                character.PassiveState.IsLowHealthModeActive = isLowHealth;
                if (isLowHealth) {
                    ConsoleLog.Combat($"{character.CharName} entered low health mode - all attacks now deal Fire damage and gain Burning");
                } else {
                    ConsoleLog.Combat($"{character.CharName} exited low health mode");
                }
                GameEvents.TriggerPassiveStateChanged(character, "LowHealthMode");
            }
        }

        private static void ExecuteYuTurnStartPassive(Character character) {
            var opponent = GameManager.Instance?.GetOpponent(character);
            if (opponent == null) return;

            // Count frozen cards
            int frozenCardCount = 0;
            foreach (var card in opponent.EquippedSlots.Values) {
                if (card != null && card.IsFrozen) {
                    frozenCardCount++;
                }
            }

            if (frozenCardCount > 0) {
                int damage = frozenCardCount * 100;
                ConsoleLog.Combat($"{character.CharName}'s passive: {opponent.CharName} takes {damage} Ice damage from {frozenCardCount} frozen cards");
                
                // Apply ice damage
                DamageCalculator.ApplyElementalDamage(character, opponent, damage, Character.ESSENCE_TYPE.ICE);
                GameEvents.TriggerPassiveTriggered(character);
            }
        }

        // Handle Altering Pyre passive state for Rok
        public static void UpdateAlteringPyre(Character character) {
            if (character.CharName != "Rok") return;

            if (character.PassiveState.IsAlteringPyreActive) {
                character.PassiveState.AlteringPyreTurnsWaited++;
                
                // Check for UP charging (every 6+ turns)
                if (character.PassiveState.AlteringPyreTurnsWaited >= 6) {
                    if (character.UP < character.MaxUP) {
                        character.UP++;
                        ConsoleLog.Resource($"{character.CharName} gained 1 UP from Altering Pyre");
                        GameEvents.TriggerResourceGained(character, 1, "UP");
                    }
                    character.PassiveState.AlteringPyreTurnsWaited = 0; // Reset for next charge
                }
            }
        }
    }
}