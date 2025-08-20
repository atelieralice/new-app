using Godot;

namespace meph {
    [GlobalClass]
    public partial class CardData : Resource {
        [Export] public string id;
        [Export] public string name;
        [Export] public Card.TYPE type;
        [Export] public string description;
        [Export] public Godot.Collections.Dictionary<string, int> requirements = new ( );
        [Export] public bool isSwift;
    }
}