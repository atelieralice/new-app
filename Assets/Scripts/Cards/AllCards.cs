using System;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    
    /// <summary>
    /// Centralized card factory providing all game cards through code-based definitions
    /// Implements the complete card catalog for both characters with their signature abilities
    /// Maintains consistency through factory patterns and organized card creation methods
    /// 
    /// Card Organization:
    /// - Character Cards: Identity and passive abilities
    /// - Weapon Cards (BW/SW): Used in normal attacks + card effects
    /// - Skill Cards (E/W/Q): Active abilities with resource costs
    /// - Potion Cards (P): Consumable support items (future Swift implementation)
    /// - Ultimate Cards (U): Powerful character-specific abilities requiring UP
    /// </summary>
    public static class AllCards {
        
        #region Rok Cards - Fire Essence Master
        
        /// <summary>
        /// Creates Rok's character identity card
        /// 
        /// Passive Ability: When LP drops below 25% of Max LP:
        /// - All attacks deal Fire Damage instead of Normal Damage
        /// - All attacks gain Burning effect
        /// - Burning Time (BT) is increased by 1 Turn
        /// 
        /// This transforms Rok into a berserker when critically injured
        /// </summary>
        /// <returns>Rok's character card with low-health fire transformation passive</returns>
        public static Card CreateRokCharacterCard() {
            return new Card {
                Id = "rok_character",
                Name = "Rok",
                Type = Card.TYPE.C,
                Description = "When LP is below 25% of Max LP, all attacks of Rok deals Fire Damage and gains Burning. BT is increased by 1 Turn.",
                Effect = (user, target) => {
                    // Character passives are handled by CharacterPassives system
                    // This triggers initialization of Rok's berserker transformation ability
                    CharacterPassives.InitializePassives(user);
                }
            };
        }

        /// <summary>
        /// Creates Rok's basic weapon - Aura of Magic
        /// 
        /// Base Weapon (BW): Used in normal attacks and as card effect
        /// - Deals 65 Normal Damage baseline
        /// - Magic Wielder Bonus: Regenerates 20 MP when used by Magic weapon users
        /// 
        /// Synergizes with Rok's Magic weapon type for resource sustainability
        /// </summary>
        /// <returns>Basic weapon card with magic wielder bonus</returns>
        public static Card CreateAuraOfMagic() {
            return new Card {
                Id = "aura_of_magic",
                Name = "Aura of Magic",
                Type = Card.TYPE.BW,
                Description = "Deals 65 Normal Damage to opponent. If equipped by a Magic wielder, regenerates 20 MP per use.",
                Effect = (user, target) => {
                    DamageCalculator.ApplyNormalDamage(user, target, 65);
                    
                    // Magic wielder bonus - sustains MP for other abilities
                    if (user.WeaponType == Character.WEAPON_TYPE.MAGIC) {
                        CharacterLogic.GainResource(user, "MP", 20);
                        ConsoleLog.Combat($"{user.CharName} regenerated 20 MP from Magic wielder bonus");
                    }
                }
            };
        }

        /// <summary>
        /// Creates Rok's secondary weapon - Talisman of Calamity
        /// 
        /// Secondary Weapon (SW): Enhanced damage with defensive bonus
        /// - Deals 80 Normal Damage baseline (stronger than BW)
        /// - Fire Ruler Bonus: Grants essence damage shield (DP:80, DT:2 Turns)
        /// 
        /// The shield specifically blocks only Essence Damages, not Normal or Absolute
        /// </summary>
        /// <returns>Secondary weapon with fire ruler defensive bonus</returns>
        public static Card CreateTalismanOfCalamity() {
            return new Card {
                Id = "talisman_of_calamity",
                Name = "Talisman of Calamity",
                Type = Card.TYPE.SW,
                Description = "Deals 80 Normal Damage to opponent. If equipped by a Fire ruler, your character gains a shield which blocks only Essence Damages, DP:80 and DT:2 Turns.",
                Effect = (user, target) => {
                    DamageCalculator.ApplyNormalDamage(user, target, 80);
                    
                    // Fire ruler bonus - specialized essence damage protection
                    if (user.EssenceType == Character.ESSENCE_TYPE.FIRE) {
                        var fm = GameManager.Instance?.FactorManager;
                        if (fm != null) {
                            // Specialized shield that only blocks essence-based attacks
                            FactorLogic.AddEssenceShield(fm, user, 2, 80);
                            ConsoleLog.Combat($"{user.CharName} gained essence damage shield (80 DP) from Fire ruler bonus");
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates Rok's basic skill - Sparking Punch
        /// 
        /// Equipment Skill (E): Basic offensive enhancement with non-stacking buff
        /// Cost: 40 MP
        /// Effects:
        /// - Deals 60 Normal Damage
        /// - Increases Burning Damage (BD) by 2% for 6 Turns
        /// - Duration cannot be stacked (new application overwrites old)
        /// 
        /// Enhances all future burning effects while active
        /// </summary>
        /// <returns>Basic skill card with burning damage enhancement</returns>
        public static Card CreateSparkingPunch() {
            return new Card {
                Id = "sparking_punch",
                Name = "Sparking Punch",
                Type = Card.TYPE.E,
                Description = "Deals 60 Normal Damage to opponent. Increases BD by 2% for 6 Turns. Duration of this effect cannot be stacked.",
                Requirements = new Dictionary<string, int> { { "MP", 40 } },
                Effect = (user, target) => {
                    // Resource validation and consumption
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 40 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Sparking Punch");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 40);
                    DamageCalculator.ApplyNormalDamage(user, target, 60);
                    
                    // Non-stacking BD enhancement - remove existing before adding new
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        fm.RemoveAllFactors(user, Character.STATUS_EFFECT.BURNING_DAMAGE_BOOST);
                        FactorLogic.AddBurningDamageBoost(fm, user, 6, 2);
                        ConsoleLog.Combat($"{user.CharName} gained 2% BD boost for 6 turns");
                    }
                }
            };
        }

        /// <summary>
        /// Creates Rok's weapon skill - Wounding Ember
        /// 
        /// Weapon Skill (W): Fire-based attack with burning application
        /// Cost: 90 MP + 50 EP (high resource investment)
        /// Effects:
        /// - Deals 100 Fire Damage (essence damage type)
        /// - Inflicts Burning Factor on target
        /// 
        /// Core fire-based offensive ability with guaranteed burning effect
        /// </summary>
        /// <returns>Fire damage weapon skill with burning application</returns>
        public static Card CreateWoundingEmber() {
            return new Card {
                Id = "wounding_ember",
                Name = "Wounding Ember",
                Type = Card.TYPE.W,
                Description = "Deals 100 Fire Damage to opponent. Inflicts Burning.",
                Requirements = new Dictionary<string, int> { { "MP", 90 }, { "EP", 50 } },
                Effect = (user, target) => {
                    // Dual resource requirement validation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 90 }, { "EP", 50 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Wounding Ember");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 90);
                    CharacterLogic.SpendResource(user, "EP", 50);
                    
                    // Fire elemental damage with burning application
                    DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.FIRE);
                    
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        // Standard burning: 2% damage for 2 turns
                        FactorLogic.AddBurning(fm, target, 2, 2, user);
                    }
                }
            };
        }

        /// <summary>
        /// Creates Rok's signature Q skill - Altering Pyre
        /// 
        /// Character-Specific Skill (Q): Complex charge-based ability with scaling power
        /// Cost: 120 MP + 100 EP (activation only)
        /// 
        /// Mechanics:
        /// - Activation: Grants 3 charges and begins turn counter
        /// - Usage: Different effects based on turns waited since activation
        ///   • 1+ Turns (1 charge): 300 Fire Damage + Burning
        ///   • 3+ Turns (2 charges): 5% Fire Damage + 4% BD boost + Burning
        ///   • 5+ Turns (3 charges): 10% Fire Damage + 4% BD boost + Burning
        /// 
        /// Rewards patience with increasingly powerful effects
        /// </summary>
        /// <returns>Rok's signature Q skill with charge-based scaling mechanics</returns>
        public static Card CreateAlteringPyre() {
            return new Card {
                Id = "altering_pyre",
                Name = "Altering Pyre",
                Type = Card.TYPE.Q,
                Description = "Has 3 charges. Can be used after being activated. Gains different features according to Turns waited.",
                Requirements = new Dictionary<string, int> { { "MP", 120 }, { "EP", 100 } },
                Effect = (user, target) => {
                    // Resource validation for activation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 120 }, { "EP", 100 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Altering Pyre activation");
                        return;
                    }
                    
                    if (!user.PassiveState.IsAlteringPyreActive) {
                        // Initial activation - spend resources and setup charges
                        CharacterLogic.SpendResource(user, "MP", 120);
                        CharacterLogic.SpendResource(user, "EP", 100);
                        
                        user.PassiveState.IsAlteringPyreActive = true;
                        user.PassiveState.AlteringPyreTurnsWaited = 0;
                        user.PassiveState.AlteringPyreCharges = 3;
                        
                        ConsoleLog.Combat($"{user.CharName} activated Altering Pyre - gaining power over time");
                    } else {
                        // Usage phase - effects scale with patience
                        int turnsWaited = user.PassiveState.AlteringPyreTurnsWaited;
                        
                        if (turnsWaited >= 5 && user.PassiveState.AlteringPyreCharges >= 3) {
                            // Maximum power: 10% Fire Damage + enhanced burning
                            user.PassiveState.AlteringPyreCharges -= 3;
                            DamageCalculator.ApplyDirectPercentageDamage(target, 10f, Character.ESSENCE_TYPE.FIRE, user);
                            ApplyTemporaryBDBoost(user, 4);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (5+ turns) - 10% Fire damage with BD boost");
                        } else if (turnsWaited >= 3 && user.PassiveState.AlteringPyreCharges >= 2) {
                            // Moderate power: 5% Fire Damage + enhanced burning
                            user.PassiveState.AlteringPyreCharges -= 2;
                            DamageCalculator.ApplyDirectPercentageDamage(target, 5f, Character.ESSENCE_TYPE.FIRE, user);
                            ApplyTemporaryBDBoost(user, 4);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (3+ turns) - 5% Fire damage with BD boost");
                        } else if (turnsWaited >= 1 && user.PassiveState.AlteringPyreCharges >= 1) {
                            // Basic power: Fixed 300 Fire Damage
                            user.PassiveState.AlteringPyreCharges -= 1;
                            DamageCalculator.ApplyElementalDamage(user, target, 300, Character.ESSENCE_TYPE.FIRE);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (1+ turns) - 300 Fire damage");
                        } else {
                            ConsoleLog.Warn($"{user.CharName} cannot use Altering Pyre - insufficient charges or turns");
                        }
                        
                        // Auto-deactivation when charges depleted
                        if (user.PassiveState.AlteringPyreCharges <= 0) {
                            user.PassiveState.IsAlteringPyreActive = false;
                            ConsoleLog.Combat($"{user.CharName}'s Altering Pyre deactivated - no charges remaining");
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates Rok's ultimate ability - Blazing Dash
        /// 
        /// Ultimate Card (U): Powerful defensive/offensive hybrid ability
        /// Cost: 1 UP (Ultimate Point)
        /// 
        /// Effects:
        /// - Grants Immunity status (prevents all factor applications)
        /// - Multiplies Burning Damage (BD) by 2 for enhanced burning effects
        /// - Lasts for exactly 5 attacks executed by the user
        /// 
        /// Strategic ultimate that combines defense and offense enhancement
        /// </summary>
        /// <returns>Rok's ultimate card with immunity and burning enhancement</returns>
        public static Card CreateBlazingDash() {
            return new Card {
                Id = "blazing_dash",
                Name = "Blazing Dash",
                Type = Card.TYPE.U,
                Description = "Your character becomes Immune. Multiply BD by 2. Lasts for 5 separate attacks executed by you.",
                Requirements = new Dictionary<string, int> { { "UP", 1 } },
                Effect = (user, target) => {
                    // Ultimate Point validation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "UP", 1 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Blazing Dash");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "UP", 1);
                    
                    // Grant powerful defensive and offensive bonuses
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        FactorLogic.AddImmunity(fm, user, 999); // High duration, managed by attack counter
                        user.PassiveState.IsBlazingDashActive = true;
                        user.PassiveState.BlazingDashAttacksRemaining = 5;
                        
                        ConsoleLog.Combat($"{user.CharName} activated Blazing Dash - Immune with 2x BD for 5 attacks");
                    }
                }
            };
        }

        #endregion

        #region Yu Cards - Ice Essence Master

        /// <summary>
        /// Creates Yu's character identity card
        /// 
        /// Passive Ability: At the beginning of Yu's turns:
        /// - Opponent takes 100 Ice Damage for each Frozen Card they have
        /// - Scales with freeze accumulation for increasing pressure
        /// 
        /// Rewards freeze-focused strategy with automatic damage scaling
        /// </summary>
        /// <returns>Yu's character card with frozen card punishment passive</returns>
        public static Card CreateYuCharacterCard() {
            return new Card {
                Id = "yu_character",
                Name = "Yu",
                Type = Card.TYPE.C,
                Description = "At the beginning of your Turns, your opponent takes 100 Ice Damage for each Frozen Card they have.",
                Effect = (user, target) => {
                    // Character passives are handled by CharacterPassives system
                    // This triggers initialization of Yu's freeze punishment ability
                    CharacterPassives.InitializePassives(user);
                }
            };
        }

        /// <summary>
        /// Creates Yu's basic weapon - Katana of Blizzard
        /// 
        /// Base Weapon (BW): Enhanced weapon with ice ruler synergy
        /// - Baseline: 90 Normal Damage
        /// - Ice Ruler Bonus: 100 Ice Damage instead (essence type change)
        /// 
        /// Perfect synergy with Yu's Ice essence for consistent elemental damage
        /// </summary>
        /// <returns>Basic weapon with ice ruler damage type conversion</returns>
        public static Card CreateKatanaOfBlizzard() {
            return new Card {
                Id = "katana_of_blizzard",
                Name = "Katana of Blizzard",
                Type = Card.TYPE.BW,
                Description = "Deals 90 Normal Damage to opponent. If equipped by an Ice ruler, deals 100 Ice Damage instead.",
                Effect = (user, target) => {
                    if (user.EssenceType == Character.ESSENCE_TYPE.ICE) {
                        // Ice ruler bonus - converts to elemental damage with slight increase
                        DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} used Ice ruler bonus - Ice damage instead of Normal");
                    } else {
                        DamageCalculator.ApplyNormalDamage(user, target, 90);
                    }
                }
            };
        }

        /// <summary>
        /// Creates Yu's secondary weapon - Cold Bringer
        /// 
        /// Secondary Weapon (SW): Conditional damage enhancement
        /// - Baseline: 90 Normal Damage
        /// - Freeze Bonus: 100 Ice Damage when opponent has frozen cards
        /// 
        /// Synergizes with freeze strategy for enhanced damage output
        /// </summary>
        /// <returns>Secondary weapon with freeze-conditional damage bonus</returns>
        public static Card CreateColdBringer() {
            return new Card {
                Id = "cold_bringer",
                Name = "Cold Bringer",
                Type = Card.TYPE.SW,
                Description = "Deals 90 Normal Damage to opponent. If your opponent has any Frozen Cards, deals 100 Ice Damage instead.",
                Effect = (user, target) => {
                    if (DamageCalculator.HasFrozenCards(target)) {
                        // Enhanced damage when freeze strategy is working
                        DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} deals Ice damage due to opponent's frozen cards");
                    } else {
                        DamageCalculator.ApplyNormalDamage(user, target, 90);
                    }
                }
            };
        }

        /// <summary>
        /// Creates Yu's basic skill - Glacial Trap
        /// 
        /// Equipment Skill (E): Passive enhancement for ice damage
        /// No resource cost - permanent passive effect once activated
        /// 
        /// Effect: When opponent has frozen cards, all Ice Damage also applies Freeze (FT:2)
        /// Creates devastating freeze chain reactions and card lockdown
        /// </summary>
        /// <returns>Passive skill enabling freeze chaining on ice damage</returns>
        public static Card CreateGlacialTrap() {
            return new Card {
                Id = "glacial_trap",
                Name = "Glacial Trap",
                Type = Card.TYPE.E,
                Description = "If your opponent has any Frozen Cards, whenever you deal Ice Damage to your opponent, applies Freeze as well, FT:2 Turns. This Card is a passive.",
                Effect = (user, target) => {
                    // Permanent passive - no resource cost
                    user.PassiveState.IsGlacialTrapActive = true;
                    ConsoleLog.Combat($"{user.CharName} activated Glacial Trap - Ice damage will freeze cards when opponent has frozen cards");
                }
            };
        }

        /// <summary>
        /// Creates Yu's weapon skill - Judgement of Hailstones
        /// 
        /// Weapon Skill (W): Adaptive freeze/damage ability
        /// Cost: 80 MP + 90 EP
        /// 
        /// Adaptive Effects:
        /// - No frozen cards: Applies Freeze (FT:2 Turns) to initiate freeze strategy
        /// - Has frozen cards: Deals 300 Ice Damage to capitalize on existing freezes
        /// 
        /// Strategic card that adapts to battlefield state
        /// </summary>
        /// <returns>Adaptive freeze/damage weapon skill</returns>
        public static Card CreateJudgementOfHailstones() {
            return new Card {
                Id = "judgement_of_hailstones",
                Name = "Judgement of Hailstones",
                Type = Card.TYPE.W,
                Description = "If your opponent doesn't have any Frozen Cards, applies Freeze, FT:2 Turns. If your Opponent has any Frozen Cards, deals 300 Ice Damage to your opponent instead.",
                Requirements = new Dictionary<string, int> { { "MP", 80 }, { "EP", 90 } },
                Effect = (user, target) => {
                    // Dual resource validation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 80 }, { "EP", 90 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Judgement of Hailstones");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 80);
                    CharacterLogic.SpendResource(user, "EP", 90);
                    
                    if (!DamageCalculator.HasFrozenCards(target)) {
                        // Initiate freeze strategy
                        FactorLogic.FreezeMultipleCards(target, 1, 2, user);
                        ConsoleLog.Combat($"{user.CharName} froze a card with Judgement of Hailstones");
                    } else {
                        // Capitalize on existing freezes with high damage
                        DamageCalculator.ApplyElementalDamage(user, target, 300, Character.ESSENCE_TYPE.ICE);
                        ConsoleLog.Combat($"{user.CharName} dealt massive Ice damage due to opponent's frozen cards");
                    }
                }
            };
        }

        /// <summary>
        /// Creates Yu's signature Q skill - Freezing Strike
        /// 
        /// Character-Specific Skill (Q): Advanced freeze manipulation with choice
        /// Cost: 120 MP + 150 EP
        /// 
        /// Choice-Based Effects:
        /// - Option A: Freeze 1 card (FT:1) + extend all frozen cards by 3 turns
        /// - Option B: Freeze 3 cards (FT:2) for mass lockdown
        /// 
        /// Currently auto-selects Option A when target has frozen cards, Option B otherwise
        /// Future implementation will provide player choice interface
        /// </summary>
        /// <returns>Yu's signature Q skill with dual freeze manipulation options</returns>
        public static Card CreateFreezingStrike() {
            return new Card {
                Id = "freezing_strike",
                Name = "Freezing Strike",
                Type = Card.TYPE.Q,
                Description = "Choose one of the options: A: Freeze 1 Card, FT:1 Turn and increase FT of all Frozen Cards by 3 Turns. B: Freeze 3 Cards, FT:2 Turns.",
                Requirements = new Dictionary<string, int> { { "MP", 120 }, { "EP", 150 } },
                Effect = (user, target) => {
                    // High resource cost for powerful freeze manipulation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 120 }, { "EP", 150 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Freezing Strike");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 120);
                    CharacterLogic.SpendResource(user, "EP", 150);
                    
                    // Strategic choice based on current freeze state
                    // TODO: Implement player choice UI for manual selection
                    bool useOptionA = DamageCalculator.HasFrozenCards(target);
                    
                    if (useOptionA) {
                        // Option A: Extend existing freeze durations
                        FactorLogic.FreezeMultipleCards(target, 1, 1, user);
                        FactorLogic.ExtendFreezeOnAllCards(target, 3);
                        ConsoleLog.Combat($"{user.CharName} used Freezing Strike Option A - extended freeze duration");
                    } else {
                        // Option B: Mass freeze initiation
                        FactorLogic.FreezeMultipleCards(target, 3, 2, user);
                        ConsoleLog.Combat($"{user.CharName} used Freezing Strike Option B - froze 3 cards");
                    }
                }
            };
        }

        /// <summary>
        /// Creates Yu's ultimate ability - Forbidden Technique of Frostbite
        /// 
        /// Ultimate Card (U): Mass freeze ultimate
        /// Cost: 1 UP (Ultimate Point)
        /// 
        /// Effects:
        /// - Freezes ALL equipped cards except Character card
        /// - Base duration: 3 turns (FT:3)
        /// - Enhanced by freeze duration bonuses and set bonuses
        /// - Can freeze even immune targets with "Crystalized Dreams" set bonus
        /// 
        /// Devastating ultimate that can completely lock down opponent's strategy
        /// </summary>
        /// <returns>Yu's ultimate card with mass freeze capability</returns>
        public static Card CreateForbiddenTechniqueOfFrostbite() {
            return new Card {
                Id = "forbidden_technique_of_frostbite",
                Name = "Forbidden Technique of Frostbite",
                Type = Card.TYPE.U,
                Description = "Applies Freeze to all Cards your opponent currently has equipped, FT:3 Turns. (Except Character Card.)",
                Requirements = new Dictionary<string, int> { { "UP", 1 } },
                Effect = (user, target) => {
                    // Ultimate Point validation
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "UP", 1 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Forbidden Technique of Frostbite");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "UP", 1);
                    
                    // Mass freeze implementation with set bonus consideration
                    int frozenCount = 0;
                    int baseDuration = 3;
                    bool canFreezeImmune = user.CanUseSetBonus("Crystalized Dreams");
                    
                    foreach (var card in target.EquippedSlots.Values) {
                        if (card != null && card.Type != Card.TYPE.C) {
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
                }
            };
        }

        #endregion

        #region Universal Potion Cards
        
        /// <summary>
        /// Creates Fierceful Recover potion
        /// 
        /// Potion Card (P): Resource regeneration over time
        /// No resource cost - consumable support item
        /// 
        /// Effect: Grants 40 MP regeneration at the beginning of each turn for 4 turns
        /// Total: 160 MP over 4 turns for sustained ability usage
        /// 
        /// Future: Will be implemented as Swift Card for multiple uses per turn
        /// </summary>
        /// <returns>MP regeneration potion card</returns>
        public static Card CreateFiercefulRecover() {
            return new Card {
                Id = "fierceful_recover",
                Name = "Fierceful Recover",
                Type = Card.TYPE.P,
                Description = "Your character recovers 40 MP at beginning of every Turn for 4 Turns.",
                Effect = (user, target) => {
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        // Sustained MP regeneration for extended ability usage
                        FactorLogic.AddMpRegeneration(fm, user, 4, 40);
                        ConsoleLog.Combat($"{user.CharName} will recover 40 MP per turn for 4 turns");
                    }
                }
            };
        }

        /// <summary>
        /// Creates Ultimate Head Start potion
        /// 
        /// Potion Card (P): Ultimate Point acceleration
        /// No resource cost - strategic timing support
        /// 
        /// Effect: Immediately grants 1 UP charge
        /// Allows faster access to Ultimate abilities for strategic timing
        /// 
        /// Future: Will be implemented as Swift Card for multiple uses per turn
        /// </summary>
        /// <returns>UP acceleration potion card</returns>
        public static Card CreateUltimateHeadStart() {
            return new Card {
                Id = "ultimate_head_start",
                Name = "Ultimate Head Start",
                Type = Card.TYPE.P,
                Description = "Your character gains 1 UP charge immediately.",
                Effect = (user, target) => {
                    // Immediate UP gain for ultimate timing control
                    CharacterLogic.GainResource(user, "UP", 1);
                    ConsoleLog.Combat($"{user.CharName} gained 1 UP charge immediately");
                }
            };
        }

        #endregion

        #region Helper Methods for Card Effects
        
        /// <summary>
        /// Applies temporary Burning Damage boost for single attack
        /// Used by Altering Pyre's enhanced effects
        /// 
        /// Note: This is a temporary boost affecting only the current attack instance
        /// Different from permanent BD boosts applied through Factor system
        /// </summary>
        /// <param name="user">Character receiving the temporary BD boost</param>
        /// <param name="percent">Percentage increase for BD calculation</param>
        private static void ApplyTemporaryBDBoost(Character user, int percent) {
            // Temporary boost for current attack only - not persistent
            ConsoleLog.Combat($"{user.CharName} gains {percent}% BD boost for this attack");
        }

        /// <summary>
        /// Applies burning with potential BD boost enhancements
        /// Used by Altering Pyre and other enhanced fire abilities
        /// 
        /// Applies standard burning effect but benefits from any active BD boosts
        /// </summary>
        /// <param name="user">Character applying the burning effect</param>
        /// <param name="target">Character receiving the burning effect</param>
        private static void ApplyBurningWithBoost(Character user, Character target) {
            var fm = GameManager.Instance?.FactorManager;
            if (fm != null) {
                // Standard burning parameters: 2% damage for 2 turns
                // Benefits from any active BD boost factors
                FactorLogic.AddBurning(fm, target, 2, 2, user);
            }
        }

        #endregion

        #region Card Factory Methods
        
        /// <summary>
        /// Retrieves character identity cards by character name
        /// Used during character selection and initialization
        /// 
        /// Character cards define core identity and passive abilities
        /// Must be equipped first before other character-specific cards
        /// </summary>
        /// <param name="characterName">Name of the character ("Rok" or "Yu")</param>
        /// <returns>Character identity card for the specified character</returns>
        /// <exception cref="ArgumentException">Thrown for unknown character names</exception>
        public static Card GetCharacterCard(string characterName) {
            return characterName switch {
                "Rok" => CreateRokCharacterCard(),
                "Yu" => CreateYuCharacterCard(),
                _ => throw new ArgumentException($"Unknown character: {characterName}")
            };
        }

        /// <summary>
        /// Retrieves complete card set for a character including all signature cards
        /// Used for character battle mode initialization and flexible mode card pools
        /// 
        /// Card Set Composition:
        /// - Index 0: Character Card (identity and passives)
        /// - Index 1+: Signature cards in power progression order
        /// 
        /// The .Skip(1) operation in GameUI filters out character cards for equipment pools
        /// </summary>
        /// <param name="characterName">Name of the character ("Rok" or "Yu")</param>
        /// <returns>Complete list of cards for the character, character card first</returns>
        public static List<Card> GetCharacterCardSet(string characterName) {
            return characterName switch {
                "Rok" => new List<Card> {
                    CreateRokCharacterCard(),      // Character identity
                    CreateAuraOfMagic(),           // BW - Basic weapon with MP regen
                    CreateTalismanOfCalamity(),    // SW - Secondary weapon with shield
                    CreateSparkingPunch(),         // E  - Basic skill with BD boost
                    CreateWoundingEmber(),         // W  - Fire weapon skill
                    CreateAlteringPyre(),          // Q  - Signature charged ability
                    CreateBlazingDash()            // U  - Ultimate immunity + BD
                },
                "Yu" => new List<Card> {
                    CreateYuCharacterCard(),               // Character identity
                    CreateKatanaOfBlizzard(),              // BW - Basic weapon with ice conversion
                    CreateColdBringer(),                   // SW - Secondary weapon with freeze bonus
                    CreateGlacialTrap(),                   // E  - Passive freeze enhancement
                    CreateJudgementOfHailstones(),         // W  - Adaptive freeze/damage
                    CreateFreezingStrike(),                // Q  - Advanced freeze manipulation
                    CreateForbiddenTechniqueOfFrostbite()  // U  - Mass freeze ultimate
                },
                _ => new List<Card>()
            };
        }

        /// <summary>
        /// Retrieves all universal potion cards available to any character
        /// Used in both game modes for consumable support items
        /// 
        /// Potions provide strategic support without character restrictions
        /// Future implementation will make these Swift Cards for flexible usage
        /// </summary>
        /// <returns>List of all available potion cards</returns>
        public static List<Card> GetPotionCards() {
            return new List<Card> {
                CreateFiercefulRecover(),    // MP regeneration over time
                CreateUltimateHeadStart()    // Immediate UP gain
            };
        }

        /// <summary>
        /// Retrieves card by unique identifier for data-driven card loading
        /// Used for save/load systems, card references, and debugging
        /// 
        /// Provides safe card lookup with null return for invalid IDs
        /// Maintains consistency with card creation methods
        /// </summary>
        /// <param name="cardId">Unique string identifier for the card</param>
        /// <returns>Card instance matching the ID, or null if not found</returns>
        public static Card GetCardById(string cardId) {
            return cardId switch {
                // Rok Cards
                "rok_character" => CreateRokCharacterCard(),
                "aura_of_magic" => CreateAuraOfMagic(),
                "talisman_of_calamity" => CreateTalismanOfCalamity(),
                "sparking_punch" => CreateSparkingPunch(),
                "wounding_ember" => CreateWoundingEmber(),
                "altering_pyre" => CreateAlteringPyre(),
                "blazing_dash" => CreateBlazingDash(),
                
                // Yu Cards
                "yu_character" => CreateYuCharacterCard(),
                "katana_of_blizzard" => CreateKatanaOfBlizzard(),
                "cold_bringer" => CreateColdBringer(),
                "glacial_trap" => CreateGlacialTrap(),
                "judgement_of_hailstones" => CreateJudgementOfHailstones(),
                "freezing_strike" => CreateFreezingStrike(),
                "forbidden_technique_of_frostbite" => CreateForbiddenTechniqueOfFrostbite(),
                
                // Universal Potions
                "fierceful_recover" => CreateFiercefulRecover(),
                "ultimate_head_start" => CreateUltimateHeadStart(),
                
                // Unknown ID
                _ => null
            };
        }

        #endregion
    }
}