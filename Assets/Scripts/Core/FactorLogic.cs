using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static meph.Character;

namespace meph {
    public static class FactorLogic {

        private static int GetParamOrDefault(FactorInstance factor, string key, int defVal = 0) =>
            factor.Params != null && factor.Params.TryGetValue(key, out var v) ? v : defVal;

        private static bool IsStormed(FactorManager fm, Character target) =>
            fm.GetFactors(target, STATUS_EFFECT.STORM).Count > 0;

        // Enhanced Toughness according to design document: +150 Earth Damage and shield
        public static void AddToughness(FactorManager fm, Character character, int duration = 2, int dp = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.TOUGHNESS);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.DP, dp } };
            fm.ApplyFactor(character, STATUS_EFFECT.TOUGHNESS, duration, parameters);

            ConsoleLog.Factor($"{character.CharName} gained Toughness: +150 Earth Damage and {dp} DP shield for {duration} turns");
        }

        public static void AddHealing(FactorManager fm, Character character, int duration = 2, int ha = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.HEALING);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.HA, ha } };
            fm.ApplyFactor(character, STATUS_EFFECT.HEALING, duration, parameters);
        }

        public static void AddRecharge(FactorManager fm, Character character, int duration = 2, int recharge = 150) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.RECHARGE);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.RC, recharge } };
            fm.ApplyFactor(character, STATUS_EFFECT.RECHARGE, duration, parameters);
        }

        public static void AddGrowth(FactorManager fm, Character character, int duration = 2, int growthMp = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.GROWTH);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.MP, growthMp } };
            fm.ApplyFactor(character, STATUS_EFFECT.GROWTH, duration, parameters);
        }

        // Enhanced Storm according to design document: prevents opponent from activating factors
        public static void AddStorm(FactorManager fm, Character character, int duration = 2, int stormDamage = 50) {
            var parameters = new Dictionary<string, int> { { ParamKeys.SD, stormDamage } };
            fm.ApplyFactor(character, STATUS_EFFECT.STORM, duration, parameters);

            ConsoleLog.Factor($"{character.CharName} is affected by Storm - cannot activate factors and takes {stormDamage} damage per turn for {duration} turns");
        }

        // Enhanced Burning according to design document: percentage-based damage
        public static void AddBurning(FactorManager fm, Character character, int duration = 2, int bdPercent = 2) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, bdPercent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING, duration, parameters);
        }

        public static void AddFreeze(FactorManager fm, Character character, int duration = 2) {
            var parameters = new Dictionary<string, int> { { ParamKeys.FD, duration } };
            fm.ApplyFactor(character, STATUS_EFFECT.FREEZE, duration, parameters );
        }

        public static void FreezeCard(Card card, int duration) {
            card.Freeze(duration);
            GameEvents.TriggerCardFrozen(card, duration);
        }
        public static void UnfreezeCard(Card card) {
            card.Unfreeze();
            GameEvents.TriggerCardUnfrozen(card);
        }

        public static int ResolveToughness(FactorManager fm, Character character, int damage) {
            if (damage <= 0) return 0;

            var shields = fm.GetFactors(character, STATUS_EFFECT.TOUGHNESS);
            if (shields.Count == 0) return damage;

            int totalDP = 0;
            for (int i = 0; i < shields.Count; i++)
                totalDP += GetParamOrDefault(shields[i], ParamKeys.DP, 0);

            if (totalDP == 0) return damage;

            if (damage >= totalDP) {
                int remaining = damage - totalDP;
                // Shield breaks - remove all toughness instances
                for (int i = 0; i < shields.Count; i++) {
                    shields[i].Duration = 0;
                    if (shields[i].Params != null && shields[i].Params.ContainsKey(ParamKeys.DP))
                        shields[i].Params[ParamKeys.DP] = 0;
                }
                ConsoleLog.Combat($"{character.CharName}'s shield absorbed {totalDP} damage and broke");
                return remaining;
            }

            // Shield absorbs all damage
            int toConsume = damage;
            for (int i = 0; i < shields.Count && toConsume > 0; i++) {
                var s = shields[i];
                int dp = GetParamOrDefault(s, ParamKeys.DP, 0);
                if (dp <= 0) continue;

                if (toConsume >= dp) {
                    toConsume -= dp;
                    s.Duration = 0;
                    if (s.Params != null) s.Params[ParamKeys.DP] = 0;
                } else {
                    s.Params[ParamKeys.DP] = dp - toConsume;
                    toConsume = 0;
                }
            }
            ConsoleLog.Combat($"{character.CharName}'s shield absorbed {damage} damage");
            return 0;
        }

        public static int GetToughnessEarthBonus(FactorManager fm, Character character) =>
            fm.GetFactors(character, STATUS_EFFECT.TOUGHNESS).Count > 0 ? 150 : 0;

        // FIXED: Enhanced Healing resolution according to design document
        public static void ResolveHealing(FactorManager fm, Character character, Character target) {
            var heals = fm.GetFactors(character, STATUS_EFFECT.HEALING);
            for (int i = 0; i < heals.Count; i++) {
                int ha = GetParamOrDefault(heals[i], ParamKeys.HA, 100);

                // Character heals (capped at max)
                int oldLP = character.LP;
                character.LP = Math.Min(character.LP + ha, character.MaxLP);
                int actualHealing = character.LP - oldLP;

                if (actualHealing > 0) {
                    GameEvents.TriggerHealingReceived(character, actualHealing);
                }

                // FIXED: Opponent loses LP (half the heal amount) - this is NOT damage according to design
                if (target != null) {
                    int lpLoss = ha / 2;
                    int oldTargetLP = target.LP;
                    target.LP = Math.Max(target.LP - lpLoss, 0);
                    int actualLoss = oldTargetLP - target.LP;
                    
                    if (actualLoss > 0) {
                        GameEvents.TriggerResourceLost(target, actualLoss, "LP");
                        ConsoleLog.Combat($"{target.CharName} lost {actualLoss} LP from {character.CharName}'s healing (not damage)");
                    }

                    // Check for defeat from LP loss
                    if (target.LP <= 0) {
                        GameEvents.TriggerPlayerDefeated(target);
                    }
                }
            }
        }

        public static void ResolveRecharge(FactorManager fm, Character character, Character target) {
            var recharges = fm.GetFactors(character, STATUS_EFFECT.RECHARGE);
            for (int i = 0; i < recharges.Count; i++) {
                int amount = GetParamOrDefault(recharges[i], ParamKeys.RC, 150);
                int steal = Math.Min(amount, target.EP);
                character.EP = Math.Min(character.EP + steal, character.MaxEP);
                target.EP -= steal;

                if (steal > 0) {
                    GameEvents.TriggerResourceStolen(target, character, steal, "EP");
                }
            }
        }

        public static void ResolveGrowth(FactorManager fm, Character character, Character target) {
            var growths = fm.GetFactors(character, STATUS_EFFECT.GROWTH);
            for (int i = 0; i < growths.Count; i++) {
                int amount = GetParamOrDefault(growths[i], ParamKeys.MP, 100);
                int steal = Math.Min(amount, target.MP);
                character.MP = Math.Min(character.MP + steal, character.MaxMP);
                target.MP -= steal;

                if (steal > 0) {
                    GameEvents.TriggerResourceStolen(target, character, steal, "MP");
                }
            }
        }

        // FIXED: Storm damage bypasses DEF according to design document
        public static void ResolveStorm(FactorManager fm, Character target) {
            var storms = fm.GetFactors(target, STATUS_EFFECT.STORM);
            if (storms.Count == 0) return;

            int totalDmg = 0;
            for (int i = 0; i < storms.Count; i++)
                totalDmg += GetParamOrDefault(storms[i], ParamKeys.SD, 50);

            if (totalDmg > 0) {
                ConsoleLog.Combat($"{target.CharName} takes {totalDmg} Storm damage (bypasses DEF)");
                
                // FIXED: Storm damage bypasses DEF but goes through shield system
                // Apply damage directly through shield resolution only
                int remaining = ResolveToughness(fm, target, totalDmg);
                
                if (remaining > 0) {
                    int oldLP = target.LP;
                    target.LP = Math.Max(target.LP - remaining, 0);
                    
                    if (oldLP > target.LP) {
                        GameEvents.TriggerResourceLost(target, oldLP - target.LP, "LP");
                    }
                    
                    GameEvents.TriggerDamageDealt(target, totalDmg, target.LP);
                    
                    if (target.LP <= 0) {
                        GameEvents.TriggerPlayerDefeated(target);
                    }
                }
            }
        }

        // Enhanced Burning resolution according to design document
        public static void ResolveBurning(FactorManager fm, Character target) {
            var burns = fm.GetFactors(target, STATUS_EFFECT.BURNING );
            if ( burns.Count == 0 ) return;

            int totalPercent = 0;
            for ( int i = 0; i < burns.Count; i++ )
                totalPercent += GetParamOrDefault ( burns[i], ParamKeys.BD, 2 );

            if ( totalPercent <= 0 ) return;

            // Find the source character who applied burning (for modifiers)
            var gameManager = GameManager.Instance;
            Character source = null;
            if ( gameManager != null ) {
                source = gameManager.GetOpponent ( target );
            }

            // Apply burning damage with character modifiers
            DamageCalculator.ApplyPercentageDamage ( target, totalPercent, Character.ESSENCE_TYPE.FIRE, source );
        }

        // Enhanced burning application with character modifiers
        public static void AddBurning(FactorManager fm, Character character, int duration = 2, int bdPercent = 2, Character source = null) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING);
                return;
            }

            // Apply Rok's low health passive - increase BT by 1
            if (source != null && source.PassiveState.IsLowHealthModeActive) {
                duration += 1;
                ConsoleLog.Combat($"Burning duration increased by 1 due to {source.CharName}'s low health state");
            }

            var parameters = new Dictionary<string, int> { { ParamKeys.BD, bdPercent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING, duration, parameters);
        }

        // Enhanced freeze application with character modifiers
        public static void FreezeCard(Card card, int duration, Character source = null) {
            if (source != null) {
                duration += source.GetFreezeDurationBonus();
            }

            card.Freeze(duration);
            GameEvents.TriggerCardFrozen(card, duration);

            // Track freeze applications for Yu's UP charging
            if (source != null && source.CharName == "Yu") {
                source.PassiveState.FreezeApplicationCount++;

                if (source.PassiveState.FreezeApplicationCount >= 10) {
                    source.UP = Math.Min(source.UP + 1, source.MaxUP);
                    source.PassiveState.FreezeApplicationCount = 0;
                    ConsoleLog.Resource($"{source.CharName} gained 1 UP from freeze applications");
                    GameEvents.TriggerResourceGained(source, 1, "UP");
                }
            }
        }

        // Method to freeze multiple cards
        public static void FreezeMultipleCards(Character target, int cardCount, int duration, Character source = null) {
            var availableCards = target.EquippedSlots.Values
                .Where(c => c != null && !c.IsFrozen && c.Type != Card.TYPE.C)
                .ToList();

            int cardsToFreeze = Math.Min(cardCount, availableCards.Count);

            for (int i = 0; i < cardsToFreeze; i++) {
                var randomIndex = (int)(GD.Randi() % availableCards.Count);
                var cardToFreeze = availableCards[randomIndex];
                availableCards.RemoveAt(randomIndex);

                FreezeCard(cardToFreeze, duration, source);
            }
        }

        // Method to extend freeze duration on all frozen cards
        public static void ExtendFreezeOnAllCards(Character target, int additionalDuration) {
            foreach (var card in target.EquippedSlots.Values) {
                if (card != null && card.IsFrozen) {
                    card.ExtendFreezeDuration(additionalDuration);
                    ConsoleLog.Combat($"{card.Name}'s freeze duration extended by {additionalDuration} turns");
                }
            }
        }

        // Special essence-only shield for Rok's Talisman of Calamity
        public static void AddEssenceShield(FactorManager fm, Character character, int duration = 2, int dp = 80) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.ESSENCE_SHIELD);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.DP, dp } };
            fm.ApplyFactor(character, STATUS_EFFECT.ESSENCE_SHIELD, duration, parameters);
        }

        // Burning damage boost effect for Sparking Punch
        public static void AddBurningDamageBoost(FactorManager fm, Character character, int duration = 6, int percent = 2) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING_DAMAGE_BOOST);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, percent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING_DAMAGE_BOOST, duration, parameters);
        }

        // MP regeneration effect for Fierceful Recover
        public static void AddMpRegeneration(FactorManager fm, Character character, int duration = 4, int amount = 40) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.MP_REGEN);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.MP, amount } };
            fm.ApplyFactor(character, STATUS_EFFECT.MP_REGEN, duration, parameters);
        }

        // Immunity effect for Blazing Dash
        public static void AddImmunity(FactorManager fm, Character character, int duration = 999) {
            var parameters = new Dictionary<string, int>();
            fm.ApplyFactor(character, STATUS_EFFECT.IMMUNE, duration, parameters);
        }

        // Resolve MP regeneration
        public static void ResolveMpRegeneration(FactorManager fm, Character character) {
            var regens = fm.GetFactors(character, STATUS_EFFECT.MP_REGEN);
            for (int i = 0; i < regens.Count; i++) {
                int amount = GetParamOrDefault(regens[i], ParamKeys.MP, 40);
                CharacterLogic.GainResource(character, "MP", amount);
                ConsoleLog.Resource($"{character.CharName} regenerated {amount} MP from effect");
            }
        }
    }
}