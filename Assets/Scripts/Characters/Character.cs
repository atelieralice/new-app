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

        public int LP { get; internal set; }
        public int EP { get; internal set; }
        public int MP { get; internal set; }
        public int UP { get; internal set; }

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

            LP = data.maxLP;
            EP = data.maxEP;
            MP = data.maxMP;
            UP = 0;
            StatusEffects = STATUS_EFFECT.NONE;
            EquippedSlots = new Dictionary<Card.TYPE, Card> ( );

            DEF = data.DEF;
            EssenceDEF = data.essenceDEF;

            CritRate = data.critRate;
            CritDamage = data.critDamage;
        }

        private int GetBaseStat ( string statKey ) {
            switch ( statKey ) {
                case ParamKeys.MaxLP: return MaxLP;
                case ParamKeys.MaxEP: return MaxEP;
                case ParamKeys.MaxMP: return MaxMP;
                case ParamKeys.LP: return LP;
                case ParamKeys.EP: return EP;
                case ParamKeys.MP: return MP;
                case ParamKeys.UP: return UP;
                case ParamKeys.DEF: return DEF;
                case ParamKeys.EssenceDEF: return EssenceDEF;
                case ParamKeys.CritRate: return (int)( CritRate * 100 );
                case ParamKeys.CritDamage: return (int)( CritDamage * 100 );
                case ParamKeys.NormalDamage: return NormalDamage;
                case ParamKeys.EarthDamage: return EarthDamage;
                case ParamKeys.WaterDamage: return WaterDamage;
                case ParamKeys.ElectricityDamage: return ElectricityDamage;
                case ParamKeys.NatureDamage: return NatureDamage;
                case ParamKeys.AirDamage: return AirDamage;
                case ParamKeys.FireDamage: return FireDamage;
                case ParamKeys.IceDamage: return IceDamage;
                case ParamKeys.LightDamage: return LightDamage;
                case ParamKeys.DarknessDamage: return DarknessDamage;
                case ParamKeys.AbsoluteDamage: return AbsoluteDamage;
                case ParamKeys.DP: return 0;
                case ParamKeys.DT: return 0;
                case ParamKeys.HA: return 0;
                case ParamKeys.HT: return 0;
                case ParamKeys.RA: return 0;
                case ParamKeys.RT: return 0;
                case ParamKeys.GA: return 0;
                case ParamKeys.GT: return 0;
                case ParamKeys.SD: return 0;
                case ParamKeys.ST: return 0;
                case ParamKeys.BD: return 0;
                case ParamKeys.BT: return 0;
                case ParamKeys.FT: return 0;
                default: return 0;
            }
        }

        public int GetEffectiveStat ( string statKey ) {
            int value = GetBaseStat ( statKey );
            foreach ( var card in EquippedSlots.Values ) {
                if ( !card.IsFrozen && card.StatBonuses.TryGetValue ( statKey, out int bonus ) )
                    value += bonus;
            }
            return value;
        }
    }

    // Helper method to check if a specific status effect is present (use on statusEffects)
    // It ANDs the statusEffects with a specific effect to know if that specific effect is set
    // Example: character.StatusEffects.Has(Character.STATUS_EFFECT.FREEZE)
    public static class StatusEffectResolver {
        public static bool Has ( this Character.STATUS_EFFECT effects, Character.STATUS_EFFECT effect ) {
            return ( effects & effect ) != 0;
        }
    }
}