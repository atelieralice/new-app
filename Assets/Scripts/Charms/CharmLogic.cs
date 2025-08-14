using System;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    
    /// <summary>
    /// Static utility class providing core charm equipment operations and management
    /// Implements charm equipping, unequipping, and character stat integration
    /// Serves as the primary interface for charm-related game logic operations
    /// 
    /// Core Functionality:
    /// - Equipment Management: Charm equipping/unequipping with validation and conflict resolution
    /// - Resource Integration: Automatic resource updates when charm bonuses affect maximum values
    /// - Set Bonus Detection: Complete set validation and set bonus activation events
    /// - Data Conversion: CharmData to runtime Charm instance transformation
    /// - Preset Collections: Predefined charm sets for specific characters (Rok, Yu)
    /// - Event Coordination: GameEvents integration for UI updates and logging
    /// </summary>
    public static class CharmLogic {
        
        #region Equipment Management System
        
        /// <summary>
        /// Equips a charm to the appropriate character slot with comprehensive validation
        /// Prevents slot conflicts, applies resource bonuses, and manages set completion
        /// Triggers equipment events for UI updates and game state synchronization
        /// 
        /// Equipment Process:
        /// 1. Validate character and charm are not null
        /// 2. Check for existing charm in target slot (prevent conflicts)
        /// 3. Equip charm to character's EquippedCharms dictionary
        /// 4. Apply resource bonuses (LP/EP/MP increases affect current values)
        /// 5. Trigger equipment events for UI and logging
        /// 6. Check for complete set bonus activation
        /// 
        /// Game Rules:
        /// - Only one charm per slot type allowed
        /// - Resource bonuses immediately increase current values
        /// - Set bonuses activate when all 5 slots filled with matching set
        /// </summary>
        /// <param name="character">Character receiving the charm equipment</param>
        /// <param name="charm">Charm to be equipped (null check performed)</param>
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

        /// <summary>
        /// Unequips a charm from the specified character slot
        /// Removes charm from equipment but preserves current resource values
        /// Triggers unequipment events for UI updates and game state tracking
        /// 
        /// Unequipment Rules:
        /// - Current resources (LP/EP/MP) are NOT reduced when unequipping
        /// - Only maximum values change through computed properties
        /// - Set bonuses are automatically lost when set becomes incomplete
        /// - Events trigger for UI synchronization and logging
        /// 
        /// Resource Preservation Logic:
        /// Game rule states that current resources should not decrease when unequipping,
        /// only maximum values change, allowing characters to retain gained resources
        /// </summary>
        /// <param name="character">Character unequipping the charm</param>
        /// <param name="slot">Equipment slot to unequip charm from</param>
        public static void UnequipCharm(Character character, CharmSlot slot) {
            if (character == null) return;

            if (character.EquippedCharms.TryGetValue(slot, out Charm charm)) {
                character.EquippedCharms.Remove(slot);
                
                // Note: We don't reduce current resources when unequipping, only max values change
                ConsoleLog.Equip($"{character.CharName} unequipped {charm.Name}");
                GameEvents.TriggerCharmUnequipped(character, charm);
            }
        }

        /// <summary>
        /// Updates character's current resources when charm bonuses increase maximum values
        /// Implements game rule: when MaxLP/EP/MP increases, current values should also increase
        /// Triggers resource gain events for UI updates and consistent game state tracking
        /// 
        /// Resource Update Rules:
        /// - LP Bonus: Increases both MaxLP and current LP simultaneously
        /// - EP Bonus: Increases both MaxEP and current EP simultaneously  
        /// - MP Bonus: Increases both MaxMP and current MP simultaneously
        /// - Only positive bonuses trigger updates (negative values ignored)
        /// 
        /// This ensures characters immediately benefit from resource-enhancing charms
        /// </summary>
        /// <param name="character">Character receiving the resource updates</param>
        /// <param name="charm">Charm providing the resource bonuses</param>
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
        
        #endregion

        #region Data Conversion System
        
        /// <summary>
        /// Creates a runtime Charm instance from CharmData template
        /// Transfers all properties from data template to runtime charm object
        /// Used by charm factories and equipment systems for charm instantiation
        /// 
        /// Conversion Process:
        /// 1. Create new Charm instance
        /// 2. Copy identity properties (ID, name, description, set)
        /// 3. Transfer all stat bonuses and specialized bonuses
        /// 4. Set equipment slot assignment
        /// 5. Return fully configured charm ready for equipment
        /// 
        /// This method bridges the gap between data templates and runtime objects,
        /// enabling data-driven charm configuration and factory pattern usage
        /// </summary>
        /// <param name="data">CharmData template containing charm configuration</param>
        /// <returns>Runtime Charm instance with all properties transferred</returns>
        public static Charm CreateCharmFromData(CharmData data) {
            return new Charm {
                Id = data.charmId,
                Name = data.charmName,
                Description = data.charmDescription,
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
        
        #endregion

        #region Preset Charm Collections
        
        /// <summary>
        /// Static factory class for creating predefined charm sets for specific characters
        /// Provides complete charm set definitions for testing, development, and default loadouts
        /// Enables quick character setup with thematically appropriate and balanced charm builds
        /// 
        /// Character-Specific Sets:
        /// - Rok: "Flames of Crimson Rage" - Burning damage enhancement and Fire essence synergy
        /// - Yu: "Crystalized Dreams" - Freeze control, Ice damage bonuses, and resource sustainability
        /// 
        /// Each set provides complete 5-piece collections with thematic naming and balanced bonuses
        /// </summary>
        public static class PresetCharms {
            
            /// <summary>
            /// Creates Rok's "Flames of Crimson Rage" charm set
            /// Fire essence themed set focused on burning damage enhancement and combat stats
            /// 
            /// Set Features:
            /// - Universal Burning Damage: Each piece provides 1% BD bonus (5% total)
            /// - Balanced Stat Distribution: LP, DEF, Essence DEF, Normal Damage, MP bonuses
            /// - Fire Synergy: Enhances Rok's berserker mode and burning factor applications
            /// - Set Bonus: "Flames of Crimson Rage" doubles BD bonus effectiveness (10% total)
            /// 
            /// Strategic Focus: Aggressive burning builds with enhanced survivability
            /// </summary>
            /// <returns>Complete 5-piece Rok charm set with Fire essence synergy</returns>
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

            /// <summary>
            /// Creates Yu's "Crystalized Dreams" charm set
            /// Ice essence themed set focused on freeze control, Ice damage, and resource sustainability
            /// 
            /// Set Features:
            /// - Ice Damage Specialization: Multiple pieces provide specific Ice damage bonuses
            /// - Freeze Duration Enhancement: +1 turn to all freeze effects (Protection of Fear)
            /// - MP Sustainability: 20 MP recovery per normal attack (Guilt of Betrayal)
            /// - Weapon Synergy: Sword damage bonuses matching Yu's weapon type
            /// - Defensive Balance: LP, EP, DEF, and Essence DEF distribution
            /// 
            /// Strategic Focus: Control-oriented builds with sustained resource management
            /// </summary>
            /// <returns>Complete 5-piece Yu charm set with Ice essence control synergy</returns>
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
        
        #endregion
    }
}