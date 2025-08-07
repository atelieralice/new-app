using Godot;
using meph;

public partial class Debug : Node {
    public override void _Ready ( ) {
        // Create a test character
        var testCharacter = new CharacterData ( );

        // Create a test card
        var testCard = CreateTestCard ( );

        // Equip the test card
        CharacterLogic.EquipCardToSlot ( testCharacter, testCard );
    }

    // Creates a placeholder card for testing
    private CardData CreateTestCard ( ) {
        var testCard = new CardData {
            id = "test_q",
            name = "Test Q Card",
            type = CardData.TYPE.Q,
            description = "Placeholder Q card for testing.",
            Effect = ( user, target ) => {
                target.LP -= 10;
            }
        };
        return testCard;
    }
}