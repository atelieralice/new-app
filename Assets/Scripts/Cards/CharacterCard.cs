using meph;
using System.Collections.Generic;

// Card wrapper for character
// CharacterData -> Character -> CharacterCard
namespace meph {
    public class CharacterCard : Card {
        private readonly Character _character;

        public Character Character => _character;

        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = false;
        public override bool HasPassive { get; protected set; } = true;

        // Properties to mirror Character class' functionality
        public string CharName => _character.CharName;
        public Character.STAR Star => _character.Star;
        public Character.ESSENCE_TYPE EssenceType => _character.EssenceType;
        public Character.WEAPON_TYPE WeaponType => _character.WeaponType;

        public int MaxLP => _character.MaxLP;
        public int MaxEP => _character.MaxEP;
        public int MaxMP => _character.MaxMP;

        public int LP {
            get => _character.LP;
            set => _character.LP = value;
        }
        public int EP {
            get => _character.EP;
            set => _character.EP = value;
        }
        public int MP {
            get => _character.MP;
            set => _character.MP = value;
        }
        public int UP {
            get => _character.UP;
            set => _character.UP = value;
        }

        public int DEF {
            get => _character.DEF;
            set => _character.DEF = value;
        }
        public int EssenceDEF {
            get => _character.EssenceDEF;
            set => _character.EssenceDEF = value;
        }

        public float CritRate {
            get => _character.CritRate;
            set => _character.CritRate = value;
        }
        public float CritDamage {
            get => _character.CritDamage;
            set => _character.CritDamage = value;
        }

        public int NormalDamage {
            get => _character.NormalDamage;
            set => _character.NormalDamage = value;
        }
        public int EarthDamage {
            get => _character.EarthDamage;
            set => _character.EarthDamage = value;
        }
        public int WaterDamage {
            get => _character.WaterDamage;
            set => _character.WaterDamage = value;
        }
        public int ElectricityDamage {
            get => _character.ElectricityDamage;
            set => _character.ElectricityDamage = value;
        }
        public int NatureDamage {
            get => _character.NatureDamage;
            set => _character.NatureDamage = value;
        }
        public int AirDamage {
            get => _character.AirDamage;
            set => _character.AirDamage = value;
        }
        public int FireDamage {
            get => _character.FireDamage;
            set => _character.FireDamage = value;
        }
        public int IceDamage {
            get => _character.IceDamage;
            set => _character.IceDamage = value;
        }
        public int LightDamage {
            get => _character.LightDamage;
            set => _character.LightDamage = value;
        }
        public int DarknessDamage {
            get => _character.DarknessDamage;
            set => _character.DarknessDamage = value;
        }
        public int AbsoluteDamage {
            get => _character.AbsoluteDamage;
            set => _character.AbsoluteDamage = value;
        }

        public Dictionary<TYPE, Card> EquippedSlots => _character.EquippedSlots;

        public Character.STATUS_EFFECT StatusEffects {
            get => _character.StatusEffects;
            set => _character.StatusEffects = value;
        }

        // Constructor
        // Add a isUsable field to CharacterData.cs if a future character comes with an active effect
        // IsUsabe = data.isUsable
        public CharacterCard ( CharacterData data ) {
            Id = data.charName;
            Name = data.charName;
            Type = TYPE.C;
            Description = $"Character Card for {data.charName}";
            Requirements = new Dictionary<string, int> ( );
            IsSwift = false;
            IsUsable = false;
            HasPassive = true;
            _character = new Character ( data );
        }

        // IMPORTANT! Has shorter syntax from its Character counterpart
        // Example: user.Has(Character.STATUS_EFFECT.FREEZE)
        public bool Has ( Character.STATUS_EFFECT effect ) => _character.StatusEffects.Has ( effect );

        public void EquipCardToSlot ( Card card ) => CharacterLogic.EquipCardToSlot ( _character, card );

        public void UseSlot ( FactorManager fm, TYPE slotType, CharacterCard target ) {
            CharacterLogic.UseSlot ( _character, fm, slotType, target.Character );
        }
    }
}
