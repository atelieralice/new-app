using meph;
using System.Collections.Generic;

namespace meph {
    public class PotionCard : Card {
        public override bool IsSwift { get; protected set; } = true;
        public override bool IsUsable { get; protected set; } = true;
        public override bool HasPassive { get; protected set; } = false;

        public int MaxPotion { get; private set; }
        public int PotionCount { get; private set; }

        // Constructor
        public PotionCard ( CardData data ) {
            OwnerCharacter = data.ownerCharacter;
            Id = data.id;
            Name = data.name;
            Type = data.type;
            Description = data.description;
            Requirements = new Dictionary<string, int> ( data.requirements );
            IsSwift = data.isSwift;
            IsUsable = data.isUsable;
            HasPassive = data.hasPassive;
            MaxPotion = data.maxPotion;
            PotionCount = data.maxPotion;
            // Need a way to set effectKey
        }
    }
}