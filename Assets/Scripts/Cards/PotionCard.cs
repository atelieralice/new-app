using meph;
using System.Collections.Generic;

namespace meph {
    public class PotionCard : Card {
        public override bool IsSwift { get; protected set; } = true;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        // Constructor
        public PotionCard ( ) {
            Type = TYPE.P;
        }
    }
}