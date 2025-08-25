using System.Collections.Generic;

// All logic is defined near their respective .tres file as partial classes
namespace meph {
    public static partial class CardEffectRegistry {
        public static readonly Dictionary<string, CardEffect> EffectRegistry = new ( ) { };

        public static readonly Dictionary<string, CardPassiveEffect> PassiveEffectRegistry = new ( ) { };
    }
}