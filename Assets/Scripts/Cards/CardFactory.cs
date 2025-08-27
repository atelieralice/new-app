using System;
using meph;

namespace meph {
    public static class CardFactory {
        public static Card CreateCard ( CardData data ) {
            switch ( data.type ) {
                case Card.TYPE.BW:
                case Card.TYPE.SW:
                    return new WeaponCard ( data );
                case Card.TYPE.E:
                case Card.TYPE.W:
                case Card.TYPE.Q:
                    return new SkillCard ( data );
                case Card.TYPE.P:
                    return new PotionCard ( data );
                case Card.TYPE.U:
                    return new UltimateCard ( data );
                case Card.TYPE.H:
                case Card.TYPE.A:
                case Card.TYPE.G:
                case Card.TYPE.B:
                case Card.TYPE.Gl:
                    return new CharmCard ( data );
                default:
                    ConsoleLog.Error ( $"Unknown card type: {data.type}" );
                    throw new ArgumentException ( $"Unknown card type: {data.type}" );

            }
        }

        public static CharacterCard CreateCharacterCard ( CharacterData data ) {
            return new CharacterCard ( data );
        }
    }
}
