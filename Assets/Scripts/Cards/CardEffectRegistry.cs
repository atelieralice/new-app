using System.Collections.Generic;

namespace meph {
    public static class CardEffectRegistry {
        public static Dictionary<string, CardEffect> EffectRegistry = new ( ) {
            { "a1", (user, target) => {} },
            { "a2", (user, target) => {} },
        };

        public static Dictionary<string, CardPassiveEffect> PassiveEffectRegistry = new ( ) {
            { "p1", (user, target) => {} },
        };
    }
}