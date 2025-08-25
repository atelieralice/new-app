using Godot;

namespace meph {
    public enum DAMAGE_TYPE {
        NORMAL,
        EARTH,
        WATER,
        ELECTRICITY,
        NATURE,
        AIR,
        FIRE,
        ICE,
        LIGHT,
        DARKNESS,
        ABSOLUTE
    }

    // Calculates reduced damage based on damage type
    public static class DamageLogic {
        public static void ApplyDamage ( FactorManager factorManager, Character character, int damage, DAMAGE_TYPE type ) {
            if ( character == null || damage <= 0 ) return;

            int reducedDamage = damage;
            switch ( type ) {
                case DAMAGE_TYPE.NORMAL:
                    reducedDamage -= character.DEF;
                    break;
                case DAMAGE_TYPE.EARTH:
                case DAMAGE_TYPE.WATER:
                case DAMAGE_TYPE.ELECTRICITY:
                case DAMAGE_TYPE.NATURE:
                case DAMAGE_TYPE.AIR:
                case DAMAGE_TYPE.FIRE:
                case DAMAGE_TYPE.ICE:
                case DAMAGE_TYPE.LIGHT:
                case DAMAGE_TYPE.DARKNESS:
                    reducedDamage -= character.EssenceDEF;
                    break;
                case DAMAGE_TYPE.ABSOLUTE:
                    break;
            }
            reducedDamage = Mathf.Max ( reducedDamage, 0 );

            int remaining = FactorLogic.ResolveToughness ( factorManager, character, reducedDamage );
            if ( remaining > 0 )
                character.LP = Mathf.Max ( character.LP - remaining, 0 );
        }
    }
}