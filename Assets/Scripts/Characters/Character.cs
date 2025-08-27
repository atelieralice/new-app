using System;
using System.Collections.Generic;

namespace meph {
    public class Character : Card {
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

        public string CharName { get; private set; }
        public STAR Star { get; private set; }
        public ESSENCE_TYPE EssenceType { get; private set; }
        public WEAPON_TYPE WeaponType { get; private set; }

        // Custom getters for automatically getting the effective value without making a big change.
        // So basically we add GetEffectiveStat method to getters and be saved from adding it everywhere else.
        private int _maxLP;
        public int MaxLP { get => GetEffectiveStat ( ParamKeys.MaxLP ); internal set => _maxLP = value; }

        private int _maxEP;
        public int MaxEP { get => GetEffectiveStat ( ParamKeys.MaxEP ); internal set => _maxEP = value; }

        private int _maxMP;
        public int MaxMP { get => GetEffectiveStat ( ParamKeys.MaxMP ); internal set => _maxMP = value; }

        private int _lp;
        public int LP { get => GetEffectiveStat ( ParamKeys.LP ); internal set => _lp = value; }

        private int _ep;
        public int EP { get => GetEffectiveStat ( ParamKeys.EP ); internal set => _ep = value; }

        private int _mp;
        public int MP { get => GetEffectiveStat ( ParamKeys.MP ); internal set => _mp = value; }

        private int _up;
        public int UP { get => GetEffectiveStat ( ParamKeys.UP ); internal set => _up = value; }

        private int _def;
        public int DEF { get => GetEffectiveStat ( ParamKeys.DEF ); internal set => _def = value; }

        private int _essenceDEF;
        public int EssenceDEF { get => GetEffectiveStat ( ParamKeys.EssenceDEF ); internal set => _essenceDEF = value; }

        private float _critRate;
        public float CritRate { get => GetEffectiveStat ( ParamKeys.CritRate ); internal set => _critRate = value; }

        private float _critDamage; // % absolute damage
        public float CritDamage { get => GetEffectiveStat ( ParamKeys.CritDamage ); internal set => _critDamage = value; }

        private int _normalDamage;
        public int NormalDamage { get => GetEffectiveStat ( ParamKeys.NormalDamage ); internal set => _normalDamage = value; }

        private int _earthDamage;
        public int EarthDamage { get => GetEffectiveStat ( ParamKeys.EarthDamage ); internal set => _earthDamage = value; }

        private int _waterDamage;
        public int WaterDamage { get => GetEffectiveStat ( ParamKeys.WaterDamage ); internal set => _waterDamage = value; }

        private int _electricityDamage;
        public int ElectricityDamage { get => GetEffectiveStat ( ParamKeys.ElectricityDamage ); internal set => _electricityDamage = value; }

        private int _natureDamage;
        public int NatureDamage { get => GetEffectiveStat ( ParamKeys.NatureDamage ); internal set => _natureDamage = value; }

        private int _airDamage;
        public int AirDamage { get => GetEffectiveStat ( ParamKeys.AirDamage ); internal set => _airDamage = value; }

        private int _fireDamage;
        public int FireDamage { get => GetEffectiveStat ( ParamKeys.FireDamage ); internal set => _fireDamage = value; }

        private int _iceDamage;
        public int IceDamage { get => GetEffectiveStat ( ParamKeys.IceDamage ); internal set => _iceDamage = value; }

        private int _lightDamage;
        public int LightDamage { get => GetEffectiveStat ( ParamKeys.LightDamage ); internal set => _lightDamage = value; }

        private int _darknessDamage;
        public int DarknessDamage { get => GetEffectiveStat ( ParamKeys.DarknessDamage ); internal set => _darknessDamage = value; }

        private int _absoluteDamage;
        public int AbsoluteDamage { get => GetEffectiveStat ( ParamKeys.AbsoluteDamage ); internal set => _absoluteDamage = value; }

        // Equiped cards are stored in a dictionary with their type as the key
        public Dictionary<TYPE, Card> EquippedSlots { get; internal set; } = new ( );

        // This variable represents Factors (and other status effects) as a bitfield 
        // Do NOT use directly to apply effects, use FactorManager instead   
        public STATUS_EFFECT StatusEffects { get; internal set; }

        // |   : Bitwise OR (set flag)              -> statusEffects |= STATUS_EFFECT.BURNING;
        // &= ~: Bitwise AND with NOT (remove flag) -> statusEffects &= ~STATUS_EFFECT.BURNING;
        // &   : Bitwise AND (check flag)           -> (statusEffects & STATUS_EFFECT.BURNING) != 0

        // Constructor
        public Character ( CharacterData data ) : base ( data ) {
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
            EquippedSlots = new Dictionary<TYPE, Card> ( );

            DEF = data.DEF;
            EssenceDEF = data.essenceDEF;

            CritRate = data.critRate;
            CritDamage = data.critDamage;

            // Card constructor overwrites
            OwnerCharacter = CharName;
            Id = CharName.Replace ( " ", "_" ).ToLowerInvariant ( );
            Name = CharName;
            Type = TYPE.C;

        }

        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = false;
        public override bool HasPassive { get; protected set; } = true;

        private int GetBaseStat ( string paramKey ) {
            return paramKey switch {
                ParamKeys.MaxLP => _maxLP,
                ParamKeys.MaxEP => _maxEP,
                ParamKeys.MaxMP => _maxMP,
                ParamKeys.LP => _lp,
                ParamKeys.EP => _ep,
                ParamKeys.MP => _mp,
                ParamKeys.UP => _up,
                ParamKeys.DEF => _def,
                ParamKeys.EssenceDEF => _essenceDEF,
                ParamKeys.CritRate => (int)( _critRate * 100 ),
                ParamKeys.CritDamage => (int)( _critDamage * 100 ),
                ParamKeys.NormalDamage => _normalDamage,
                ParamKeys.EarthDamage => _earthDamage,
                ParamKeys.WaterDamage => _waterDamage,
                ParamKeys.ElectricityDamage => _electricityDamage,
                ParamKeys.NatureDamage => _natureDamage,
                ParamKeys.AirDamage => _airDamage,
                ParamKeys.FireDamage => _fireDamage,
                ParamKeys.IceDamage => _iceDamage,
                ParamKeys.LightDamage => _lightDamage,
                ParamKeys.DarknessDamage => _darknessDamage,
                ParamKeys.AbsoluteDamage => _absoluteDamage,
                ParamKeys.DP => 0,
                ParamKeys.DT => 0,
                ParamKeys.HA => 0,
                ParamKeys.HT => 0,
                ParamKeys.RA => 0,
                ParamKeys.RT => 0,
                ParamKeys.GA => 0,
                ParamKeys.GT => 0,
                ParamKeys.SD => 0,
                ParamKeys.ST => 0,
                ParamKeys.BD => 0,
                ParamKeys.BT => 0,
                ParamKeys.FT => 0,
                _ => 0,
            };

        }

        public int GetEffectiveStat ( string paramKey ) {
            int value = GetBaseStat ( paramKey );
            foreach ( var card in EquippedSlots.Values ) {
                if ( !card.IsFrozen && card.StatBonuses.TryGetValue ( paramKey, out int bonus ) )
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