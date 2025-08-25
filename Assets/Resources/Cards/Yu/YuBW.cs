namespace meph {
    public static partial class CardEffectRegistry {
        static CardEffectRegistry ( ) {
            EffectRegistry["yu_bw"] = ( fm, user, target ) => {
                int damage;
                DAMAGE_TYPE type;
                if ( user.EssenceType == Character.ESSENCE_TYPE.ICE ) {
                    damage = 100;
                    type = DAMAGE_TYPE.ICE;
                } else {
                    damage = 90;
                    type = DAMAGE_TYPE.NORMAL;
                }
                DamageLogic.ApplyDamage ( fm, target, damage, type );
            };

            PassiveEffectRegistry["yu_bw"] = ( fm, user, target ) => { };
        }
    }
}