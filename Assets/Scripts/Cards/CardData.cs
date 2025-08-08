using Godot;

namespace meph {
    public delegate void CardEffect ( Character user, Character target );

    [GlobalClass]
    public partial class CardData : Resource {
        public enum TYPE { BW, SW, Q, W, E, P, U }

        [Export] public string id;
        [Export] public string name;
        [Export] public TYPE type;
        [Export] public string description;
        [Export] public Godot.Collections.Dictionary<string, int> requirements = new ( );
        [Export] public bool isSwift;

        // Assigned in code
        public CardEffect Effect;
    }
}