using Godot;

// Every card is defined in this format
// -Key fields are used to tie the card to its logic
// For character cards, see CharacterCard.cs
namespace meph {
    [GlobalClass]
    public partial class CardData : Resource {
        [Export] public string ownerCharacter;
        [Export] public string id;
        [Export] public string name;
        [Export] public Card.TYPE type;
        [Export] public string description;

        [Export] public Godot.Collections.Dictionary<string, int> requirements = new ( );

        [Export] public bool isSwift;
        [Export] public bool isUsable;
        [Export] public bool hasPassive;

        // Used as reference to logic
        [Export] public string effectKey;
        [Export] public string passiveEffectKey;
    }
}