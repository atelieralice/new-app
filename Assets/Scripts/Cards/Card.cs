using Godot;
using System.Collections.Generic;

namespace meph {
    public delegate void CardEffect ( Character user, Character target );

    public partial class Card {
        public enum TYPE { NONE, BW, SW, Q, W, E, P, U }

        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public TYPE Type { get; internal set; }
        public string Description { get; internal set; }

        public Dictionary<string, int> Requirements { get; internal set; } = new ( );

        public bool IsSwift { get; internal set; }
        public bool IsFrozen { get; private set; }
        public int FreezeDuration { get; private set; }

        // Assigned in code
        public CardEffect Effect;

        public void Freeze ( int duration ) {
            if ( duration <= 0 ) { Unfreeze ( ); return; }
            IsFrozen = true;
            FreezeDuration = duration;
        }

        public void Unfreeze ( ) {
            IsFrozen = false;
            FreezeDuration = 0;
        }
    }
}