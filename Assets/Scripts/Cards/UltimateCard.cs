using meph;
using System.Collections.Generic;

namespace meph {
    public class UltimateCard : Card {
        public override bool IsSwift { get; protected set; } = false;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        public int MaxUP { get; private set; }

        // Constructor
        public UltimateCard ( CardData data ) {
            OwnerCharacter = data.ownerCharacter;
            Id = data.id;
            Name = data.name;
            Type = data.type;
            Description = data.description;
            Requirements = new Dictionary<string, int> ( data.requirements );
            IsSwift = data.isSwift;
            IsUsable = data.isUsable;
            HasPassive = data.hasPassive;
            MaxUP = data.maxUP;
            Effect = CardEffectRegistry.EffectRegistry.TryGetValue ( data.effectKey, out var effect ) ? effect : null;
            PassiveEffect = CardEffectRegistry.PassiveEffectRegistry.TryGetValue ( data.passiveEffectKey, out var passive ) ? passive : null;
        }
    }
}