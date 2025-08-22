using meph;
using System.Collections.Generic;

namespace meph {
    public class UltimateCard : Card {
        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        // Constructor
        public UltimateCard ( ) {
            Type = TYPE.U;
        }
    }
}