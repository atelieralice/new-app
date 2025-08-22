using System;
using System.Collections.Generic;

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
            IMMUNE = 128
        }

        public string CharName { get; internal set; }
        public STAR Star { get; internal set; }
        public ESSENCE_TYPE EssenceType { get; internal set; }
        public WEAPON_TYPE WeaponType { get; internal set; }

        public int MaxLP { get; internal set; }
        public int MaxEP { get; internal set; }
        public int MaxMP { get; internal set; }
        public int MaxPotion { get; internal set; }

        public int LP { get; internal set; }
        public int EP { get; internal set; }
        public int MP { get; internal set; }
        public int UP { get; internal set; }
        public int Potion { get; internal set; }

        public int DEF { get; internal set; } = 50;
        public int EssenceDEF { get; internal set; } = 50;

        public float CritRate { get; internal set; } = 0.10f;
        public float CritDamage { get; internal set; } = 0.05f; // % absolute damage

        public int NormalDamage { get; internal set; }
        public int EarthDamage { get; internal set; }
        public int WaterDamage { get; internal set; }
        public int ElectricityDamage { get; internal set; }
        public int NatureDamage { get; internal set; }
        public int AirDamage { get; internal set; }
        public int FireDamage { get; internal set; }
        public int IceDamage { get; internal set; }
        public int LightDamage { get; internal set; }
        public int DarknessDamage { get; internal set; }
        public int AbsoluteDamage { get; internal set; }

        // Equiped cards are stored in a dictionary with their type as the key
        public Dictionary<Card.TYPE, Card> EquippedSlots { get; internal set; } = new ( );

        // This variable represents Factors (and other status effects) as a bitfield 
        // Do NOT use directly to apply effects, use FactorManager instead   
        public STATUS_EFFECT StatusEffects { get; internal set; }

        // |   : Bitwise OR (set flag)              -> statusEffects |= STATUS_EFFECT.BURNING;
        // &= ~: Bitwise AND with NOT (remove flag) -> statusEffects &= ~STATUS_EFFECT.BURNING;
        // &   : Bitwise AND (check flag)           -> (statusEffects & STATUS_EFFECT.BURNING) != 0

        // Constructor
        public Character ( CharacterData data ) {
            CharName = data.charName;
            Star = data.star;
            EssenceType = data.essenceType;
            WeaponType = data.weaponType;
            MaxLP = data.maxLP;
            MaxEP = data.maxEP;
            MaxMP = data.maxMP;
            MaxPotion = data.maxPotion;

            LP = data.maxLP;
            EP = data.maxEP;
            MP = data.maxMP;
            UP = 0;
            Potion = data.maxPotion;
            StatusEffects = STATUS_EFFECT.NONE;
            EquippedSlots = new Dictionary<Card.TYPE, Card> ( );

            DEF = data.DEF;
            EssenceDEF = data.EssenceDEF;

            CritRate = data.CritRate;
            CritDamage = data.CritDamage;
        }
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
}