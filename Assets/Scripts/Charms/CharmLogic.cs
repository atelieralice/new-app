using System;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    public static class CharmLogic {
        public static void EquipCharm(Character character, Charm charm) {
            if (character == null || charm == null) {
                ConsoleLog.Warn("Cannot equip charm - character or charm is null");
                return;
            }

            // Check if slot is already occupied
            if (character.EquippedCharms.ContainsKey(charm.Slot)) {
                var existing = character.EquippedCharms[charm.Slot];
                ConsoleLog.Warn($"Charm slot {charm.Slot} is already occupied by {existing.Name}");
                return;
            }

            // Equip the charm
            character.EquippedCharms[charm.Slot] = charm;
            
            // Update current resources if they were increased by max increases
            UpdateResourcesAfterCharmEquip(character, charm);
            
            ConsoleLog.Equip($"{character.CharName} equipped {charm.Name}");
            GameEvents.TriggerCharmEquipped(character, charm);
            
            // Check for set bonus
            if (character.HasCompleteCharmSet()) {
                var setName = character.GetEquippedSetName();
                ConsoleLog.Equip($"{character.CharName} completed the {setName} set!");
                GameEvents.TriggerSetBonusActivated(character, setName);
            }
        }

        public static void UnequipCharm(Character character, CharmSlot slot) {
            if (character == null) return;

            if (character.EquippedCharms.TryGetValue(slot, out Charm charm)) {
                character.EquippedCharms.Remove(slot);
                
                // Note: We don't reduce current resources when unequipping, only max values change
                ConsoleLog.Equip($"{character.CharName} unequipped {charm.Name}");
                GameEvents.TriggerCharmUnequipped(character, charm);
            }
        }

        private static void UpdateResourcesAfterCharmEquip(Character character, Charm charm) {
            // When max LP/EP/MP increases, current values should also increase
            if (charm.LpBonus > 0) {
                character.LP += charm.LpBonus;
                GameEvents.TriggerResourceGained(character, charm.LpBonus, "LP");
            }
            if (charm.EpBonus > 0) {
                character.EP += charm.EpBonus;
                GameEvents.TriggerResourceGained(character, charm.EpBonus, "EP");
            }
            if (charm.MpBonus > 0) {
                character.MP += charm.MpBonus;
                GameEvents.TriggerResourceGained(character, charm.MpBonus, "MP");
            }
        }

        public static Charm CreateCharmFromData(CharmData data) {
            return new Charm {
                Id = data.charmId,           // Fixed: use charmId
                Name = data.charmName,       // Fixed: use charmName
                Description = data.charmDescription, // Fixed: use charmDescription
                SetName = data.setName,
                Slot = data.slot,
                LpBonus = data.lpBonus,
                EpBonus = data.epBonus,
                MpBonus = data.mpBonus,
                DefBonus = data.defBonus,
                EssenceDefBonus = data.essenceDefBonus,
                NormalDamageBonus = data.normalDamageBonus,
                EssenceDamageBonus = data.essenceDamageBonus,
                SpecificEssenceDamageBonus = data.specificEssenceDamageBonus,
                EssenceType = data.essenceType,
                WeaponDamageBonus = data.weaponDamageBonus,
                WeaponType = data.weaponType,
                BurningDamageBonus = data.burningDamageBonus,
                FreezeDurationBonus = data.freezeDurationBonus,
                MpRecoveryBonus = data.mpRecoveryBonus
            };
        }

        // Create preset charms for Rok and Yu
        public static class PresetCharms {
            public static List<Charm> CreateRokCharms() {
                return new List<Charm> {
                    new Charm {
                        Id = "rok_helmet",
                        Name = "Helmet of Crimson Rage",
                        Description = "Increases BD by 1%. (+100 Essence DEF)",
                        SetName = "Flames of Crimson Rage",
                        Slot = CharmSlot.HELMET,
                        BurningDamageBonus = 1f,
                        EssenceDefBonus = 100
                    },
                    new Charm {
                        Id = "rok_armor",
                        Name = "Armor of Crimson Rage",
                        Description = "Increases BD by 1%. (+300 LP)",
                        SetName = "Flames of Crimson Rage",
                        Slot = CharmSlot.ARMOR,
                        BurningDamageBonus = 1f,
                        LpBonus = 300
                    },
                    new Charm {
                        Id = "rok_gloves",
                        Name = "Gloves of Crimson Rage",
                        Description = "Increases BD by 1%. (+150 Normal Damage)",
                        SetName = "Flames of Crimson Rage",
                        Slot = CharmSlot.GLOVES,
                        BurningDamageBonus = 1f,
                        NormalDamageBonus = 150
                    },
                    new Charm {
                        Id = "rok_boots",
                        Name = "Boots of Crimson Rage",
                        Description = "Increases BD by 1%. (+100 DEF)",
                        SetName = "Flames of Crimson Rage",
                        Slot = CharmSlot.BOOTS,
                        BurningDamageBonus = 1f,
                        DefBonus = 100
                    },
                    new Charm {
                        Id = "rok_glow",
                        Name = "Glow of Crimson Rage",
                        Description = "Increases BD by 1%. (+150 MP)",
                        SetName = "Flames of Crimson Rage",
                        Slot = CharmSlot.GLOW,
                        BurningDamageBonus = 1f,
                        MpBonus = 150
                    }
                };
            }

            public static List<Charm> CreateYuCharms() {
                return new List<Charm> {
                    new Charm {
                        Id = "yu_memories",
                        Name = "Memories of Past",
                        Description = "+50 Essence DEF. (+100 EP)",
                        SetName = "Crystalized Dreams",
                        Slot = CharmSlot.HELMET,
                        EssenceDefBonus = 50,
                        EpBonus = 100
                    },
                    new Charm {
                        Id = "yu_protection",
                        Name = "Protection of Fear",
                        Description = "Increases all FT by 1 Turn. (+100 LP)",
                        SetName = "Crystalized Dreams",
                        Slot = CharmSlot.ARMOR,
                        FreezeDurationBonus = 1,
                        LpBonus = 100
                    },
                    new Charm {
                        Id = "yu_teachings",
                        Name = "Teachings of Ice",
                        Description = "+50 Ice Damage. (+20 Sword Damage)",
                        SetName = "Crystalized Dreams",
                        Slot = CharmSlot.GLOVES,
                        SpecificEssenceDamageBonus = 50,
                        EssenceType = Character.ESSENCE_TYPE.ICE,
                        WeaponDamageBonus = 20,
                        WeaponType = Character.WEAPON_TYPE.SWORD
                    },
                    new Charm {
                        Id = "yu_anchor",
                        Name = "Anchor of Mind",
                        Description = "+30 Ice Damage. (+70 DEF)",
                        SetName = "Crystalized Dreams",
                        Slot = CharmSlot.BOOTS,
                        SpecificEssenceDamageBonus = 30,
                        EssenceType = Character.ESSENCE_TYPE.ICE,
                        DefBonus = 70
                    },
                    new Charm {
                        Id = "yu_guilt",
                        Name = "Guilt of Betrayal",
                        Description = "Recovers 20 MP whenever you initiate a Normal Attack. (+50 Ice Damage)",
                        SetName = "Crystalized Dreams",
                        Slot = CharmSlot.GLOW,
                        MpRecoveryBonus = 20,
                        SpecificEssenceDamageBonus = 50,
                        EssenceType = Character.ESSENCE_TYPE.ICE
                    }
                };
            }
        }
    }
}