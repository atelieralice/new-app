using System;
using System.Collections.Generic;

namespace meph {
    public static class RokCards {
        public static Card CreateRokCharacterCard() {
            return new Card {
                Id = "rok_character",
                Name = "Rok",
                Type = Card.TYPE.C,
                Description = "When LP is below 25% of Max LP, all attacks of Rok deals Fire Damage and gains Burning. BT is increased by 1 Turn.",
                Effect = (user, target) => {
                    // This is handled by character passives, but we can trigger a check here
                    CharacterPassives.InitializePassives(user);
                }
            };
        }

        public static Card CreateAuraOfMagic() {
            return new Card {
                Id = "aura_of_magic",
                Name = "Aura of Magic",
                Type = Card.TYPE.BW,
                Description = "Deals 65 Normal Damage to opponent. If equipped by a Magic wielder, regenerates 20 MP per use.",
                Effect = (user, target) => {
                    DamageCalculator.ApplyNormalDamage(user, target, 65);
                    
                    // Magic wielder bonus
                    if (user.WeaponType == Character.WEAPON_TYPE.MAGIC) {
                        CharacterLogic.GainResource(user, "MP", 20);
                        ConsoleLog.Combat($"{user.CharName} regenerated 20 MP from Magic wielder bonus");
                    }
                }
            };
        }

        public static Card CreateTalismanOfCalamity() {
            return new Card {
                Id = "talisman_of_calamity",
                Name = "Talisman of Calamity",
                Type = Card.TYPE.SW,
                Description = "Deals 80 Normal Damage to opponent. If equipped by a Fire ruler, your character gains a shield which blocks only Essence Damages, DP:80 and DT:2 Turns.",
                Effect = (user, target) => {
                    DamageCalculator.ApplyNormalDamage(user, target, 80);
                    
                    // Fire ruler bonus - essence damage shield
                    if (user.EssenceType == Character.ESSENCE_TYPE.FIRE) {
                        var fm = GameManager.Instance?.FactorManager;
                        if (fm != null) {
                            // Create special essence-only shield (we'll need to modify toughness for this)
                            FactorLogic.AddEssenceShield(fm, user, 2, 80);
                            ConsoleLog.Combat($"{user.CharName} gained essence damage shield (80 DP) from Fire ruler bonus");
                        }
                    }
                }
            };
        }

        public static Card CreateSparkingPunch() {
            return new Card {
                Id = "sparking_punch",
                Name = "Sparking Punch",
                Type = Card.TYPE.E,
                Description = "Deals 60 Normal Damage to opponent. Increases BD by 2% for 6 Turns. Duration of this effect cannot be stacked.",
                Requirements = new Dictionary<string, int> { { "MP", 40 } },
                Effect = (user, target) => {
                    // Check and spend resources
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 40 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Sparking Punch");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 40);
                    
                    DamageCalculator.ApplyNormalDamage(user, target, 60);
                    
                    // Add BD increase effect (non-stacking)
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        // Remove existing BD effects first (non-stacking)
                        fm.RemoveAllFactors(user, Character.STATUS_EFFECT.BURNING_DAMAGE_BOOST);
                        
                        // Add new BD boost
                        FactorLogic.AddBurningDamageBoost(fm, user, 6, 2);
                        ConsoleLog.Combat($"{user.CharName} gained 2% BD boost for 6 turns");
                    }
                }
            };
        }

        public static Card CreateWoundingEmber() {
            return new Card {
                Id = "wounding_ember",
                Name = "Wounding Ember",
                Type = Card.TYPE.W,
                Description = "Deals 100 Fire Damage to opponent. Inflicts Burning.",
                Requirements = new Dictionary<string, int> { { "MP", 90 }, { "EP", 50 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 90 }, { "EP", 50 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Wounding Ember");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "MP", 90);
                    CharacterLogic.SpendResource(user, "EP", 50);
                    
                    DamageCalculator.ApplyElementalDamage(user, target, 100, Character.ESSENCE_TYPE.FIRE);
                    
                    // Apply burning
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        FactorLogic.AddBurning(fm, target, 2, 2, user);
                    }
                }
            };
        }

        public static Card CreateAlteringPyre() {
            return new Card {
                Id = "altering_pyre",
                Name = "Altering Pyre",
                Type = Card.TYPE.Q,
                Description = "Has 3 charges. Can be used after being activated. Gains different features according to Turns waited.",
                Requirements = new Dictionary<string, int> { { "MP", 120 }, { "EP", 100 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "MP", 120 }, { "EP", 100 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Altering Pyre activation");
                        return;
                    }
                    
                    if (!user.PassiveState.IsAlteringPyreActive) {
                        // Activate Altering Pyre
                        CharacterLogic.SpendResource(user, "MP", 120);
                        CharacterLogic.SpendResource(user, "EP", 100);
                        
                        user.PassiveState.IsAlteringPyreActive = true;
                        user.PassiveState.AlteringPyreTurnsWaited = 0;
                        user.PassiveState.AlteringPyreCharges = 3;
                        
                        ConsoleLog.Combat($"{user.CharName} activated Altering Pyre - gaining power over time");
                    } else {
                        // Use Altering Pyre based on turns waited
                        int turnsWaited = user.PassiveState.AlteringPyreTurnsWaited;
                        
                        if (turnsWaited >= 5 && user.PassiveState.AlteringPyreCharges >= 3) {
                            // 5+ Turns: 10% Fire Damage + 4% BD boost
                            user.PassiveState.AlteringPyreCharges -= 3;
                            DamageCalculator.ApplyDirectPercentageDamage(target, 10f, Character.ESSENCE_TYPE.FIRE, user);
                            ApplyTemporaryBDBoost(user, 4);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (5+ turns) - 10% Fire damage with BD boost");
                        } else if (turnsWaited >= 3 && user.PassiveState.AlteringPyreCharges >= 2) {
                            // 3+ Turns: 5% Fire Damage + 4% BD boost
                            user.PassiveState.AlteringPyreCharges -= 2;
                            DamageCalculator.ApplyDirectPercentageDamage(target, 5f, Character.ESSENCE_TYPE.FIRE, user);
                            ApplyTemporaryBDBoost(user, 4);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (3+ turns) - 5% Fire damage with BD boost");
                        } else if (turnsWaited >= 1 && user.PassiveState.AlteringPyreCharges >= 1) {
                            // 1+ Turns: 300 Fire Damage
                            user.PassiveState.AlteringPyreCharges -= 1;
                            DamageCalculator.ApplyElementalDamage(user, target, 300, Character.ESSENCE_TYPE.FIRE);
                            ApplyBurningWithBoost(user, target);
                            ConsoleLog.Combat($"{user.CharName} used Altering Pyre (1+ turns) - 300 Fire damage");
                        } else {
                            ConsoleLog.Warn($"{user.CharName} cannot use Altering Pyre - insufficient charges or turns");
                        }
                        
                        // Deactivate if no charges left
                        if (user.PassiveState.AlteringPyreCharges <= 0) {
                            user.PassiveState.IsAlteringPyreActive = false;
                            ConsoleLog.Combat($"{user.CharName}'s Altering Pyre deactivated - no charges remaining");
                        }
                    }
                }
            };
        }

        public static Card CreateFiercefulRecover() {
            return new Card {
                Id = "fierceful_recover",
                Name = "Fierceful Recover",
                Type = Card.TYPE.P,
                Description = "Your character recovers 40 MP at beginning of every Turn for 4 Turns.",
                Effect = (user, target) => {
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        FactorLogic.AddMpRegeneration(fm, user, 4, 40);
                        ConsoleLog.Combat($"{user.CharName} will recover 40 MP per turn for 4 turns");
                    }
                }
            };
        }

        public static Card CreateBlazingDash() {
            return new Card {
                Id = "blazing_dash",
                Name = "Blazing Dash",
                Type = Card.TYPE.U,
                Description = "Your character becomes Immune. Multiply BD by 2. Lasts for 5 separate attacks executed by you.",
                Requirements = new Dictionary<string, int> { { "UP", 1 } },
                Effect = (user, target) => {
                    if (!CharacterLogic.CanAfford(user, new Dictionary<string, int> { { "UP", 1 } })) {
                        ConsoleLog.Warn($"{user.CharName} cannot afford Blazing Dash");
                        return;
                    }
                    
                    CharacterLogic.SpendResource(user, "UP", 1);
                    
                    // Grant immunity and BD multiplier
                    var fm = GameManager.Instance?.FactorManager;
                    if (fm != null) {
                        FactorLogic.AddImmunity(fm, user, 999); // Long duration, will be removed after 5 attacks
                        user.PassiveState.IsBlazingDashActive = true;
                        user.PassiveState.BlazingDashAttacksRemaining = 5;
                        
                        ConsoleLog.Combat($"{user.CharName} activated Blazing Dash - Immune with 2x BD for 5 attacks");
                    }
                }
            };
        }

        // Helper methods
        private static void ApplyTemporaryBDBoost(Character user, int percent) {
            // This is a temporary boost for this instance only
            ConsoleLog.Combat($"{user.CharName} gains {percent}% BD boost for this attack");
        }

        private static void ApplyBurningWithBoost(Character user, Character target) {
            var fm = GameManager.Instance?.FactorManager;
            if (fm != null) {
                FactorLogic.AddBurning(fm, target, 2, 2, user);
            }
        }
    }
}