using System;

// There are 52980 ways to impelemnt cards, why does this file exist?
// I don't know, maybe what written here will be useful in the future
namespace meph {

    public abstract class Card {
        public string Name;
        public string Description;
    }

    public class WeaponCard : Card {
        public Character.WEAPON_TYPE WeaponType;
        public int AttackPower;
    }

    public class SkillCard : Card {
        public enum Type { Q, W, E, U }
    }

    public class PotionCard : Card {
        public int Amount;
    }

    public class CharmCard : Card {
        public enum Type { Helmet, Armor, Gloves, Boots, Glow }
        public Type CharmType;
    }
}