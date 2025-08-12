using System;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    public static class YuCards {
        public static Card CreateYuCharacterCard() {
            return new Card {
                Id = "yu_character",
                Name = "Yu",
                Type = Card.TYPE.C,
                Description = "At the beginning of your Turns, your opponent takes 100 Ice Damage for each Frozen Card they have.",
                Effect = (user, target) => {
                    // This is handled by character passives
                    CharacterPassives.InitializePassives(user);
                }
            };
        }

        public static Card CreateKatanaOfBlizzard() {
            return new Card {
                Id = "katana_of_blizzard",
                Name = "Katana of Blizzard",
                Type = Card.TYPE.BW,
                Description = "Deals 90 Normal Damage to opponent. If equipped by an Ice ruler, deals 100 Ice Damage instead.",
                Effect = (user, target) => {
                    if (user.EssenceType == Character.ESSENCE_TYPE.ICE) {
                        DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} used Ice ruler bonus - Ice damage instead of Normal");
                    } else {
                        DamageCalculator.ApplyNormalDamage(user, target, 90);
                    }
                }
            };
        }

        public static Card CreateColdBringer() {
            return new Card {
                Id = "cold_bringer",
                Name = "Cold Bringer",
                Type = Card.TYPE.SW,
                Description = "Deals 90 Normal Damage to opponent. If your opponent has any Frozen Cards, deals 100 Ice Damage instead.",
                Effect = (user, target) => {
                    if (DamageCalculator.HasFrozenCards(target)) {
                        DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} deals Ice damage due to opponent's frozen cards");
                    } else {
                        DamageCalculator.ApplyNormalDamage(user, target, 90);
                    }
                }
            };
        }

        public static Card CreateGlacialTrap() {
            return new Card {
                Id = "glacial_trap",
                Name = "Glacial Trap",
                Type = Card.TYPE.E,
                Description = "If your opponent has any Frozen Cards, whenever you deal Ice Damage to your opponent, applies Freeze as well, FT:2 Turns. This Card is a passive.",
                Effect = (user, target) => {
                    user.PassiveState.IsGlacialTrapActive = true;
                    ConsoleLog.Combat($"{user.CharName} activated Glacial Trap - Ice damage will freeze cards when opponent has frozen cards");
                }
            };
        }

        public static Card CreateJudgementOfHailstones() {
            return new Card {
                Id = "judgement_of_hailstones",
                Name = "Judgement of Hailstones",
                Type = Card.TYPE.W,
                Description = "If your opponent doesn't have any Frozen Cards, applies Freeze, FT:2 Turns. If your Opponent has any Frozen Cards, deals 300 Ice Damage to your opponent instead.",
                Requirements = new Dictionary<string, int> { { "MP", 80 }, { "EP", 90 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 80 }, { "EP", 90 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Judgement of Hailstones");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 80);
                    CharacterLogic.SpendResource(user, "EP", 90);
                    
                    if (!DamageCalculator.HasFrozenCards(target)) {
                        // Apply freeze to a random card
                        FactorLogic.FreezeMultipleCards(target, 1, 2, user);
                        ConsoleLog.Combat($"{user.CharName} froze a card with Judgement of Hailstones");
                    } else {
                        // Deal 300 Ice damage
                        DamageCalculator.ApplyElementalDamage(user, target, 300, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} dealt massive Ice damage due to opponent's frozen cards");
                    }
                }
            };
        }

        public static Card CreateFreezingStrike() {
            return new Card {
                Id = "freezing_strike",
                Name = "Freezing Strike",
                Type = Card.TYPE.Q,
                Description = "Choose one of the options: A: Freeze 1 Card, FT:1 Turn and increase FT of all Frozen Cards by 3 Turns. B: Freeze 3 Cards, FT:2 Turns.",
                Requirements = new Dictionary<string, int> { { "MP", 120 }, { "EP", 150 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 120 }, { "EP", 150 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Freezing Strike");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 120);
                    CharacterLogic.SpendResource(user, "EP", 150);
                    
                    // For now, we'll implement option B (freeze 3 cards)
                    // In a full implementation, this would show a choice UI
                    bool useOptionA = DamageCalculator.HasFrozenCards(target); // Use A if target has frozen cards
                    
                    if (useOptionA) {
                        // Option A: Freeze 1 card and extend all frozen cards
                        FactorLogic.FreezeMultipleCards(target, 1, 1, user);
                        FactorLogic.ExtendFreezeOnAllCards(target, 3);
                        ConsoleLog.Combat($"{user.CharName} used Freezing Strike Option A - extended freeze duration");
                    } else {
                        // Option B: Freeze 3 cards
                        FactorLogic.FreezeMultipleCards(target, 3, 2, user);
                        ConsoleLog.Combat($"{user.CharName} used Freezing Strike Option B - froze 3 cards");
                    }
                }
            };
        }

        public static Card CreateUltimateHeadStart() {
            return new Card {
                Id = "ultimate_head_start",
                Name = "Ultimate Head Start",
                Type = Card.TYPE.P,
                Description = "Your character gains 1 UP charge immediately.",
                Effect = (user, target) => {
                    CharacterLogic.GainResource(user, "UP", 1);
                    ConsoleLog.Combat($"{user.CharName} gained 1 UP charge immediately");
                }
            };
        }

        public static Card CreateForbiddenTechniqueOfFrostbite() {
            return new Card {
                Id = "forbidden_technique_of_frostbite",
                Name = "Forbidden Technique of Frostbite",
                Type = Card.TYPE.U,
                Description = "Applies Freeze to all Cards your opponent currently has equipped, FT:3 Turns. (Except Character Card.)",
                Requirements = new Dictionary<string, int> { { "UP", 1 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "UP", 1 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Forbidden Technique of Frostbite");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "UP", 1);
                    
                    // Freeze all equipped cards except character card
                    int frozenCount = 0;
                    int baseDuration = 3;
                    
                    // Check for set bonus - can freeze immune characters
                    bool canFreezeImmune = user.CanUseSetBonus("Crystalized Dreams");
                    
                    foreach (var card in target.EquippedSlots.Values) {
                        if (card != null && card.Type != Card.TYPE.C) {
                            // Check if target is immune (unless we have set bonus)
                            bool targetIsImmune = target.StatusEffects.Has(Character.STATUS_EFFECT.IMMUNE);
                            
                            if (!targetIsImmune || canFreezeImmune) {
                                int duration = baseDuration + user.GetFreezeDurationBonus();
                                card.Freeze(duration);
                                GameEvents.TriggerCardFrozen(card, duration);
                                frozenCount++;
                                
                                if (canFreezeImmune && targetIsImmune) {
                                    ConsoleLog.Combat($"Set bonus allows freezing immune target's {card.Name}");
                                }
                            }
                        }
                    }
                    
                    ConsoleLog.Combat($"{user.CharName} froze {frozenCount} cards with Forbidden Technique");
                    
                    // Note: This ultimate doesn't count toward UP charging
                }
            };
        }
    }
}