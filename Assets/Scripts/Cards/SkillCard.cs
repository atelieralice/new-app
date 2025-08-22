using meph;
using System.Collections.Generic;

namespace meph {
    public class SkillCard : Card {
        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        // Constructor
        public SkillCard ( ) {
        }
    }
}