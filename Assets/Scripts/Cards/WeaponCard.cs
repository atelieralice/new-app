using meph;
using System.Collections.Generic;

namespace meph {
    public class WeaponCard : Card {
        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = false;
        public override bool HasPassive { get; protected set; } = true;

        // Constructor
        public WeaponCard ( CardData data ) : base ( data ) { }
    }
}