using Godot;
using System.Collections.Generic;


namespace meph {
    public delegate void CardEffect ( Character user, Character target );

    public class Card {
        public enum TYPE {
            NONE = 0,
            C,      // Character
            BW,     // Base Weapon
            SW,     // Secondary Weapon
            E,      // E Skill
            W,      // W Skill
            Q,      // Q Skill
            P,      // Potion
            U,      // Ultimate
            H,      // Helmet
            A,      // Armor
            G,      // Gloves
            B,      // Boots
            Gl      // Glow
        }

        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public TYPE Type { get; internal set; }
        public string Description { get; internal set; }
        public Dictionary<string, int> Requirements { get; internal set; } = new ( );
        public bool IsSwift { get; internal set; }
        public CardEffect Effect { get; internal set; }

        // Freeze functionality for cards
        public bool IsFrozen { get; private set; }
        public int FreezeDuration { get; private set; }

        public void Freeze ( int duration ) {
            IsFrozen = true;
            FreezeDuration = duration;
        }

        public void Unfreeze ( ) {
            IsFrozen = false;
            FreezeDuration = 0;
        }

        public void UpdateFreeze ( ) {
            if ( IsFrozen ) {
                FreezeDuration--;
                if ( FreezeDuration <= 0 ) {
                    Unfreeze ( );
                    GameEvents.TriggerCardUnfrozen ( this );
                }
            }
        }

        public override string ToString ( ) => Name ?? "Unknown Card";
    }
}