using Godot;

namespace meph {
    [GlobalClass]
    public partial class PotionCardData : CardData {
        [Export] public int maxPotion = 1;
    }
}