using System;
using System.Collections.Generic;
using System.Linq;

// We could implement an interface like ITargetableEntity for future characters' skills (wink wink!). Just a reminder
// For now I'll just go along with what I wrote, doing stuff will be faster than thinking a very complex system for that
namespace meph {
    public class Character {
        public enum STAR {
            NONE = 0,
            FOUR = 4,
            FIVE = 5,
            SIX = 6
        }

        public enum ESSENCE_TYPE {
            NONE = 0,
            EARTH,
            WATER,
            ELECTRICITY,
            NATURE,
            AIR,
            FIRE,
            ICE,
            LIGHT,
            DARKNESS
        }

        public enum WEAPON_TYPE {
            NONE = 0,
            SWORD,
            CLAYMORE,
            POLEARM,
            BOW,
            MAGIC,
            GUN
        }

        [Flags]
        public enum STATUS_EFFECT {
            NONE = 0,
            TOUGHNESS = 1,
            HEALING = 2,
            RECHARGE = 4,
            GROWTH = 8,
            STORM = 16,
            BURNING = 32,
            FREEZE = 64,
            IMMUNE = 128,
            ESSENCE_SHIELD = 256,        // Special shield that only blocks essence damage
            BURNING_DAMAGE_BOOST = 512,  // BD% increase
            MP_REGEN = 1024             // MP regeneration per turn
        }

        // Basic character properties
        public string CharName { get; internal set; }
        public STAR Star { get; internal set; }
        public ESSENCE_TYPE EssenceType { get; internal set; }
        public WEAPON_TYPE WeaponType { get; internal set; }

        // Base stats (without modifiers)
        public int BaseMaxLP { get; internal set; }
        public int BaseMaxEP { get; internal set; }
        public int BaseMaxMP { get; internal set; }
        public int MaxUP { get; internal set; }
        public int MaxPotion { get; internal set; }

        // Base combat stats
        public int BaseATK { get; internal set; } = 100;
        public int BaseEssenceATK { get; internal set; } = 100;
        public int BaseDEF { get; internal set; } = 0;
        public int BaseEssenceDEF { get; internal set; } = 0;

        // Current resources
        public int LP { get; internal set; }
        public int EP { get; internal set; }
        public int MP { get; internal set; }
        public int UP { get; internal set; }
        public int Potion { get; internal set; }

        // Equipment slots
        public Dictionary<Card.TYPE, Card> EquippedSlots { get; internal set; } = new();
        public Dictionary<CharmSlot, Charm> EquippedCharms { get; internal set; } = new();

        // Status effects bitfield (managed by FactorManager)
        public STATUS_EFFECT StatusEffects { get; internal set; }

        // Update the CritDamage property to store as decimal (0.02 = 2% buff):

        public float CritRate { get; internal set; } = 0.1f; // 10% base from document
        public float CritDamage { get; internal set; } = 0.05f; // 5% base from document (0.05 = 5%)

        // Character-specific passive state
        public CharacterPassiveState PassiveState { get; internal set; } = new();

        // Computed properties with modifiers
        public int MaxLP => BaseMaxLP + GetCharmBonus(c => c.LpBonus);
        public int MaxEP => BaseMaxEP + GetCharmBonus(c => c.EpBonus);
        public int MaxMP => BaseMaxMP + GetCharmBonus(c => c.MpBonus);
        public int ATK => BaseATK + GetCharmBonus(c => c.NormalDamageBonus);
        public int EssenceATK => BaseEssenceATK + GetCharmBonus(c => c.EssenceDamageBonus);
        public int DEF => BaseDEF + GetCharmBonus(c => c.DefBonus);
        public int EssenceDEF => BaseEssenceDEF + GetCharmBonus(c => c.EssenceDefBonus);

        // Get specific damage bonuses
        public int GetWeaponDamageBonus() => GetCharmBonus(c => c.WeaponType == WeaponType ? c.WeaponDamageBonus : 0);
        public int GetEssenceDamageBonus(ESSENCE_TYPE essenceType) => GetCharmBonus(c => c.EssenceType == essenceType ? c.SpecificEssenceDamageBonus : 0);
        
        // Get burning damage multiplier including charm bonuses
        public float GetBurningDamageMultiplier() {
            float baseMultiplier = 1.0f;
            float charmBonus = GetCharmBonus(c => c.BurningDamageBonus);
            
            // Check for set bonus
            if (HasCompleteCharmSet()) {
                var setName = EquippedCharms.Values.FirstOrDefault()?.SetName;
                if (setName == "Flames of Crimson Rage") {
                    // Each charm gives 2% instead of 1%
                    charmBonus *= 2f;
                }
            }
            
            return baseMultiplier + (charmBonus / 100f);
        }

        // Get freeze duration bonus from charms
        public int GetFreezeDurationBonus() => GetCharmBonus(c => c.FreezeDurationBonus);
        
        // Get MP recovery bonus from charms (for normal attacks)
        public int GetMpRecoveryBonus() => GetCharmBonus(c => c.MpRecoveryBonus);

        // Helper method to sum charm bonuses
        private int GetCharmBonus(Func<Charm, int> selector) {
            return EquippedCharms.Values.Sum(selector);
        }

        private float GetCharmBonus(Func<Charm, float> selector) {
            return EquippedCharms.Values.Sum(selector);
        }

        // Check if character has complete charm set
        public bool HasCompleteCharmSet() {
            if (EquippedCharms.Count != 5) return false;
            
            var setNames = EquippedCharms.Values.Select(c => c.SetName).Distinct().ToList();
            return setNames.Count == 1 && !string.IsNullOrEmpty(setNames[0]);
        }

        // Get the name of the equipped charm set (if complete)
        public string GetEquippedSetName() {
            return HasCompleteCharmSet() ? EquippedCharms.Values.First().SetName : null;
        }

        // Method to check if an attack crits
        public bool RollCritical() {
            return Godot.GD.Randf() < CritRate;
        }

        // Check if character is below 25% LP (for Rok's passive)
        public bool IsLowHealth() {
            return LP <= (MaxLP * 0.25f);
        }

        // Check if character can use special set bonus abilities
        public bool CanUseSetBonus(string setName) {
            return HasCompleteCharmSet() && GetEquippedSetName() == setName;
        }

        public override string ToString() => CharName ?? "Unknown Character";
    }

    // Character-specific passive state tracking
    public class CharacterPassiveState {
        // Rok's passive state
        public bool IsLowHealthModeActive { get; set; }
        
        // Yu's passive state
        public int FreezeApplicationCount { get; set; } // For UP charging
        
        // Rok's Altering Pyre state
        public bool IsAlteringPyreActive { get; set; }
        public int AlteringPyreTurnsWaited { get; set; }
        public int AlteringPyreCharges { get; set; } = 3;
        
        // Rok's Blazing Dash state
        public bool IsBlazingDashActive { get; set; }
        public int BlazingDashAttacksRemaining { get; set; }
        
        // Yu's Glacial Trap state
        public bool IsGlacialTrapActive { get; set; }
    }

    // Helper method to check if a specific status effect is present (use on statusEffects)
    // It ANDs the statusEffects with a specific effect to know if that specific effect is set
    public static class StatusEffectResolver {
        public static bool Has ( this Character.STATUS_EFFECT effects, Character.STATUS_EFFECT effect ) {
            return ( effects & effect ) != 0;
        }
    }
    // Example:
    // user.StatusEffects.Has(Character.STATUS_EFFECT.FREEZE)
    // We may omit the "Character." in files that have a static using statement for it

    public static class CharacterCreator {
        public static Character InitCharacter(CharacterData data) {
            var character = new Character();
            character.CharName = data.charName;
            character.Star = data.star;
            character.EssenceType = data.essenceType;
            character.WeaponType = data.weaponType;
            
            // Set base stats
            character.BaseMaxLP = data.maxLP;
            character.BaseMaxEP = data.maxEP;
            character.BaseMaxMP = data.maxMP;
            character.MaxUP = data.maxUP;
            character.MaxPotion = data.maxPotion;
            
            // Initialize current resources to max (computed properties will apply modifiers)
            character.LP = character.MaxLP;
            character.EP = character.MaxEP;
            character.MP = character.MaxMP;
            character.UP = 0;
            character.Potion = character.MaxPotion;
            
            // Set default combat stats based on star level
            SetDefaultCombatStats(character);
            
            return character;
        }
        
        private static void SetDefaultCombatStats(Character character) {
            // Base stats scale with star level
            int statMultiplier = (int)character.Star;
            
            character.BaseATK = 80 + (statMultiplier * 20);
            character.BaseEssenceATK = 80 + (statMultiplier * 20);
            character.BaseDEF = statMultiplier * 10;
            character.BaseEssenceDEF = statMultiplier * 10;
        }
    }
}