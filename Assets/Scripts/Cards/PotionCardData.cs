using Godot;

// Exclusive data class for Potion Cards to reduce editor clutter
namespace meph {
    [GlobalClass]
    public partial class PotionCardData : CardData {
        [Export] public int maxPotion = 1;
    }
}