using System;


// We could implement an interface like ITargetableEntity for future characters' skills (wink wink!). Just a reminder
// For now I'll just go along with what I wrote, doing stuff will be faster than thinking a very complex system for that
namespace meph {
    public abstract class Character {
        public enum STAR {
            NONE = -1,
            FOUR = 4,
            FIVE = 5,
            SIX = 6
        }

        public enum ESSENCE_TYPE {
            NONE = -1,
            EARTH,
            WATER,
            ELECTRICITY,
            AIR,
            FIRE,
            ICE,
            LIGHT,
            DARKNESS
        }

        public enum WEAPON_TYPE {
            NONE = -1,
            SWORD,
            CLAYMORE,
            POLEARM,
            BOW,
            MAGIC
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
        public int MaxUP { get; internal set; }
        public int MaxPotion { get; internal set; }

        public int LP { get; internal set; }
        public int EP { get; internal set; }
        public int MP { get; internal set; }
        public int UP { get; internal set; }
        public int Potion { get; internal set; }
        // Default crit stats could be init by the game manager and different values could added as a modifier here
        // Emplty until it is decided

        // This variable represents Factors (and other status effects) as a bitfield    
        public STATUS_EFFECT StatusEffects { get; internal set; }

        // |   : Bitwise OR (set flag)              -> statusEffects |= STATUS_EFFECT.BURNING;
        // &= ~: Bitwise AND with NOT (remove flag) -> statusEffects &= ~STATUS_EFFECT.BURNING;
        // &   : Bitwise AND (check flag)           -> (statusEffects & STATUS_EFFECT.BURNING) != 0

    }

    // Helper method to check if a specific status effect is present (use on statusEffects)
    // It ANDs the statusEffects with a specific effect to know if that specific effect is set
    public static class StatusEffectResolver {
        public static bool Has ( this Character.STATUS_EFFECT effects, Character.STATUS_EFFECT effect ) {
            return ( effects & effect ) != 0;
        }
    }
    // Example:
    // if (StatusEffects.Has(Character.STATUS_EFFECT.FREEZE)) {
    // Console.WriteLine("Character is frozen");
}