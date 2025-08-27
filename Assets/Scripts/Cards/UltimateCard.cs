using meph;
using System.Collections.Generic;

namespace meph {
    public class UltimateCard : Card {
        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        public int MaxUP { get; private set; }

        // Constructor
        public UltimateCard ( CardData data ) : base ( data ) {
            MaxUP = data.maxUP;
        }
    }
}