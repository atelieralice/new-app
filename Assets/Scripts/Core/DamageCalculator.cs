using System;
using System.Linq;
using Godot;

namespace meph {
    public static class DamageCalculator {
        // Apply normal damage with all modifiers including weapon bonuses
        public static void ApplyNormalDamage(Character attacker, Character target, int baseDamage, bool applyWeaponBonus = true) {
            if (attacker == null || target == null) return;

            // Calculate total damage with all modifiers
            int totalDamage = baseDamage + attacker.ATK;
            
            // Add weapon-specific damage bonus if applicable
            if (applyWeaponBonus) {
                totalDamage += attacker.GetWeaponDamageBonus();
            }
            
            // Apply Toughness earth damage bonus ONLY if this is earth essence damage
            // (Normal damage doesn't get this bonus according to document)
            
            // Apply defense reduction
            totalDamage = Math.Max(1, totalDamage - target.DEF);
            
            // Check for critical hit - FIXED
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
            
            // Apply the damage through GameManager
            GameManager.ApplyDamage(GameManager.Instance.FactorManager, target, totalDamage);
            GameEvents.TriggerAttackResolved(attacker, target, totalDamage, isCrit);
        }

        // Apply elemental damage with essence-specific bonuses
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
            
            // Check for critical hit - FIXED
            bool isCrit = attacker.RollCritical();
            if (isCrit) {
                // CritDamage is stored as decimal (0.05 = 5%, 0.07 = 7%, etc.)
                int critBonus = (int)(target.MaxLP * attacker.CritDamage);
                totalDamage += critBonus; // Applied after defense calculation as absolute damage
                
                ConsoleLog.Combat($"Critical hit! {attacker.CharName} deals {critBonus} absolute damage ({attacker.CritDamage * 100f:F1}% of {target.CharName}'s Max LP)");
            }
            
            // Apply the damage through GameManager
            GameManager.ApplyDamage(GameManager.Instance.FactorManager, target, totalDamage);
            GameEvents.TriggerAttackResolved(attacker, target, totalDamage, isCrit);
            
            ConsoleLog.Combat($"{attacker.CharName} dealt {totalDamage} {essenceType} damage to {target.CharName}");
            
            // Trigger passive effects for ice damage (Yu's Glacial Trap)
            if (essenceType == Character.ESSENCE_TYPE.ICE && attacker.PassiveState.IsGlacialTrapActive) {
                TriggerGlacialTrap(attacker, target);
            }
        }

        // FIXED: Apply percentage-based damage that bypasses DEF but respects shields
        public static void ApplyPercentageDamage(Character target, float percentage, Character.ESSENCE_TYPE? essenceType = null, Character source = null) {
            if (target == null) return;

            int damage = (int)(target.MaxLP * percentage / 100f);
            
            // Apply burning damage multiplier if this is burning damage and source has modifiers
            if (essenceType == Character.ESSENCE_TYPE.FIRE && source != null) {
                float multiplier = source.GetBurningDamageMultiplier();
                damage = (int)(damage * multiplier);
            }
            
            // FIXED: Burning damage bypasses DEF and Essence DEF according to document
            // Only apply through shield system
            GameManager.ApplyDamage(GameManager.Instance.FactorManager, target, damage);
            
            if (essenceType.HasValue) {
                ConsoleLog.Combat($"{target.CharName} takes {damage} {essenceType} percentage damage ({percentage}% of Max LP) - bypasses defense");
            } else {
                ConsoleLog.Combat($"{target.CharName} takes {damage} percentage damage ({percentage}% of Max LP)");
            }
        }

        // Special damage that bypasses shields (for Rok's Altering Pyre percentage damage)
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
                GameEvents.TriggerPlayerDefeated(target);
            }
        }

        // Helper method for Yu's Glacial Trap passive
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

        // Check if target has frozen cards (helper for card effects)
        public static bool HasFrozenCards(Character character) {
            return character.EquippedSlots.Values.Any(card => card != null && card.IsFrozen);
        }

        // Count frozen cards (helper for card effects)
        public static int CountFrozenCards(Character character) {
            return character.EquippedSlots.Values.Count(card => card != null && card.IsFrozen);
        }
    }
}