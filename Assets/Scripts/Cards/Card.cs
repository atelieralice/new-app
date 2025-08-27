using Godot;
using System.Collections.Generic;

// Base class for all cards. Should not be instantiated directly
namespace meph {
    public delegate void CardEffect ( FactorManager fm, Character user, Character target );
    public delegate void CardPassiveEffect ( FactorManager fm, Character user, Character target );

    public class Card {
        public enum TYPE {
            NONE,
            C,              // Character
            BW, SW,         // Base Weapon & Second Weapon
            E, W, Q, P, U,  // Skills and Potion
            H, A, G, B, Gl  // Charms
        }

        public string OwnerCharacter { get; internal set; }
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public TYPE Type { get; internal set; }
        public string Description { get; internal set; }

        public Dictionary<string, int> Requirements { get; internal set; } = new ( );
        public Dictionary<string, int> StatBonuses { get; internal set; } = new ( );

        public virtual bool IsSwift { get; protected set; }
        public virtual bool IsUsable { get; protected set; }
        public virtual bool HasPassive { get; protected set; }

        public bool IsFrozen { get; private set; }
        public int FreezeDuration { get; private set; }

        // Assigned in code
        public CardEffect Effect;
        public CardPassiveEffect PassiveEffect;

        public Card ( CardData data ) {
            OwnerCharacter = data.ownerCharacter;
            Id = data.id;
            Name = data.name;
            Type = data.type;
            Description = data.description;
            Requirements = new Dictionary<string, int> ( data.requirements );
            StatBonuses = new Dictionary<string, int> ( data.statBonuses );
            IsSwift = data.isSwift;
            IsUsable = data.isUsable;
            HasPassive = data.hasPassive;
            var effectKey = string.IsNullOrEmpty ( data.effectKey ) ? data.id : data.effectKey;
            var passiveEffectKey = string.IsNullOrEmpty ( data.passiveEffectKey ) ? data.id : data.passiveEffectKey;
            Effect = CardEffectRegistry.EffectRegistry.TryGetValue ( effectKey, out var effect ) ? effect : null;
            PassiveEffect = CardEffectRegistry.PassiveEffectRegistry.TryGetValue ( passiveEffectKey, out var passive ) ? passive : null;
        }

        public void Freeze ( int duration ) {
            if ( duration <= 0 ) { Unfreeze ( ); return; }
            IsFrozen = true;
            FreezeDuration = duration;
        }

        public void Unfreeze ( ) {
            IsFrozen = false;
            FreezeDuration = 0;
        }

        public void TickFreeze ( ) {
            if ( IsFrozen ) {
                FreezeDuration--;
                if ( FreezeDuration <= 0 )
                    Unfreeze ( );
            }
        }
    }
}