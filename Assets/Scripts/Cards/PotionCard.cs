using meph;
using System.Collections.Generic;

namespace meph {
    public class PotionCard : Card {
        public override bool IsSwift { get; protected set; } = true;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        public int MaxPotion { get; private set; } = 1;
        public int PotionCount { get; private set; }

        // Constructor
        public PotionCard ( CardData data ) : base ( data ) {
            MaxPotion = data.maxPotion;
            PotionCount = data.maxPotion;
        }
    }
}