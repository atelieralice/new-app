using meph;
using Godot;
using System;
using System.Collections.Generic;

namespace meph {
    public static class CharacterLogic {
        public static void EquipCardToSlot ( Character character, Card card ) {
            if ( card == null || character == null ) return;

            if ( character.EquippedSlots.TryGetValue ( card.Type, out Card existingCard ) && existingCard != null ) {
                ConsoleLog.Warn ( $"Slot {card.Type} is already occupied by {existingCard.Name}." );
                return;
            }

            character.EquippedSlots[card.Type] = card;
            GameEvents.TriggerCardEquipped ( character, card );
            // Removed duplicate logging since GameManager handles it via events
        }

        public static void UseSlot ( Character user, Card.TYPE slotType, Character target, bool isSwift = false ) {
            if ( user == null ) {
                ConsoleLog.Warn ( "Cannot use slot - user is null" );
                return;
            }

            if ( !user.EquippedSlots.TryGetValue ( slotType, out Card card ) || card == null ) {
                ConsoleLog.Warn ( $"No card equipped in {slotType} slot." );
                return;
            }

            if ( card.IsFrozen ) {
                ConsoleLog.Warn ( $"{card.Name} is frozen and cannot be used." );
                return;
            }

            // Check if this should be handled as an action
            var stateManager = GameManager.Instance?.StateManager;
            if ( stateManager != null ) {
                stateManager.TryAction ( ( ) => {
                    ExecuteCardEffect ( user, card, target );
                }, isSwift || card.IsSwift );
            } else {
                ExecuteCardEffect ( user, card, target );
            }
        }

        private static void ExecuteCardEffect ( Character user, Card card, Character target ) {
            card.Effect?.Invoke ( user, target );
            GameEvents.TriggerCardUsed ( user, card, target );
            // Removed duplicate logging since GameManager handles it via events
        }

        public static void PerformNormalAttack ( Character attacker, Character target ) {
            if ( attacker == null ) {
                ConsoleLog.Warn ( "Cannot perform normal attack - attacker is null" );
                return;
            }

            if ( !attacker.EquippedSlots.TryGetValue ( Card.TYPE.BW, out Card baseWeapon ) ||
                !attacker.EquippedSlots.TryGetValue ( Card.TYPE.SW, out Card secondaryWeapon ) ) {
                ConsoleLog.Warn ( $"{attacker.CharName} cannot perform normal attack - missing weapons." );
                return;
            }

            var stateManager = GameManager.Instance?.StateManager;
            stateManager?.TryAction ( ( ) => {
                // Check for critical hit
                bool isCrit = attacker.RollCritical ( );

                // Execute both weapon effects
                baseWeapon?.Effect?.Invoke ( attacker, target );
                secondaryWeapon?.Effect?.Invoke ( attacker, target );

                GameEvents.TriggerNormalAttack ( attacker, Card.TYPE.BW );
                // Removed duplicate logging since GameManager handles it via events
            } );
        }

        // Enhanced resource spending with events - FIXED NULL CHECKING
        public static void SpendResource ( Character character, string resourceType, int amount ) {
            // Add null check at the beginning
            if ( character == null ) {
                ConsoleLog.Warn ( "Cannot spend resource - character is null" );
                return;
            }

            switch ( resourceType.ToUpper ( ) ) {
                case "EP":
                    if ( character.EP >= amount ) {
                        character.EP -= amount;
                        GameEvents.TriggerResourceLost ( character, amount, "EP" );
                        ConsoleLog.Resource ( $"{character.CharName} spent {amount} EP" );
                    } else {
                        ConsoleLog.Warn ( $"{character.CharName} doesn't have enough EP ({character.EP}/{amount})" );
                    }
                    break;
                case "MP":
                    if ( character.MP >= amount ) {
                        character.MP -= amount;
                        GameEvents.TriggerResourceLost ( character, amount, "MP" );
                        ConsoleLog.Resource ( $"{character.CharName} spent {amount} MP" );
                    } else {
                        ConsoleLog.Warn ( $"{character.CharName} doesn't have enough MP ({character.MP}/{amount})" );
                    }
                    break;
                case "UP":
                    if ( character.UP >= amount ) {
                        character.UP -= amount;
                        GameEvents.TriggerResourceLost ( character, amount, "UP" );
                        ConsoleLog.Resource ( $"{character.CharName} spent {amount} UP" );
                    } else {
                        ConsoleLog.Warn ( $"{character.CharName} doesn't have enough UP ({character.UP}/{amount})" );
                    }
                    break;
                default:
                    ConsoleLog.Error ( $"Unknown resource type: {resourceType}" );
                    break;
            }
        }

        // Enhanced resource gaining with events - FIXED NULL CHECKING
        public static void GainResource ( Character character, string resourceType, int amount ) {
            // Add null check at the beginning
            if ( character == null ) {
                ConsoleLog.Warn ( "Cannot gain resource - character is null" );
                return;
            }

            switch ( resourceType.ToUpper ( ) ) {
                case "LP":
                    int oldLP = character.LP;
                    character.LP = Math.Min ( character.LP + amount, character.MaxLP );
                    int actualGain = character.LP - oldLP;
                    if ( actualGain > 0 ) {
                        GameEvents.TriggerResourceGained ( character, actualGain, "LP" );
                    }
                    break;
                case "EP":
                    int oldEP = character.EP;
                    character.EP = Math.Min ( character.EP + amount, character.MaxEP );
                    int actualEPGain = character.EP - oldEP;
                    if ( actualEPGain > 0 ) {
                        GameEvents.TriggerResourceGained ( character, actualEPGain, "EP" );
                    }
                    break;
                case "MP":
                    int oldMP = character.MP;
                    character.MP = Math.Min ( character.MP + amount, character.MaxMP );
                    int actualMPGain = character.MP - oldMP;
                    if ( actualMPGain > 0 ) {
                        GameEvents.TriggerResourceGained ( character, actualMPGain, "MP" );
                    }
                    break;
                case "UP":
                    int oldUP = character.UP;
                    character.UP = Math.Min ( character.UP + amount, character.MaxUP );
                    int actualUPGain = character.UP - oldUP;
                    if ( actualUPGain > 0 ) {
                        GameEvents.TriggerResourceGained ( character, actualUPGain, "UP" );
                    }
                    break;
                default:
                    ConsoleLog.Error ( $"Unknown resource type: {resourceType}" );
                    break;
            }
        }

        public static bool CanAfford ( Character character, Dictionary<string, int> requirements ) {
            if ( character == null ) return false;

            foreach ( var req in requirements ) {
                switch ( req.Key.ToUpper ( ) ) {
                    case "EP":
                        if ( character.EP < req.Value ) return false;
                        break;
                    case "MP":
                        if ( character.MP < req.Value ) return false;
                        break;
                    case "UP":
                        if ( character.UP < req.Value ) return false;
                        break;
                }
            }
            return true;
        }

        // Helper method to resolve attack damage with critical hit calculation - FIXED NULL CHECKING
        public static void ResolveAttackDamage ( Character attacker, Character target, int baseDamage ) {
            if ( attacker == null || target == null ) {
                ConsoleLog.Warn ( "Cannot resolve attack damage - attacker or target is null" );
                return;
            }

            bool isCrit = attacker.RollCritical ( );
            int finalDamage = baseDamage;

            if ( isCrit ) {
                int critBonus = (int)( baseDamage * attacker.CritDamage );
                finalDamage += critBonus;
            }

            // Apply the damage
            GameManager.ApplyDamage ( GameManager.Instance.FactorManager, target, finalDamage );

            // Trigger attack resolved event
            GameEvents.TriggerAttackResolved ( attacker, target, finalDamage, isCrit );
        }
    }
}