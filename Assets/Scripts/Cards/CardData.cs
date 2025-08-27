using Godot;

// Every card is defined in this format (only exception is Character cards)
// -Key fields are used to tie the card to its logic
namespace meph {
    [GlobalClass]
    public partial class CardData : Resource {
        [Export] public string ownerCharacter;
        [Export] public string id;
        [Export] public string name;
        [Export] public Card.TYPE type;
        [Export] public string description;

        [Export] public Godot.Collections.Dictionary<string, int> requirements = new ( );
        [Export] public Godot.Collections.Dictionary<string, int> statBonuses = new ( );

        [ExportGroup ( "Card Specific" )]
        [Export] public int maxPotion;
        [Export] public int maxUP;

        [ExportCategory ( "Flags" )]
        [Export] public bool isSwift;
        [Export] public bool isUsable;
        [Export] public bool hasPassive;

        // Used as reference to logic
        [ExportCategory ( "Effect Keys" )]
        [Export] public string effectKey;
        [Export] public string passiveEffectKey;
    }
}