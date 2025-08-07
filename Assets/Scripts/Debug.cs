using Godot;
using meph;

public partial class Debug : Node {
    // Minimal concrete class for testing card effects
    private class TestCharacter : Character {
        public TestCharacter ( string name, int lp ) {
            CharName = name;
            LP = lp;
        }
    }

    public override void _Ready ( ) {
        // Create StateManager and subscribe to events for debug output
        var stateManager = new StateManager ( );
        stateManager.OnActionLock += ( ) => GD.Print ( "Action locked!" );
        stateManager.OnAttackerTurn += ( ) => GD.Print ( "Attacker's turn!" );
        stateManager.OnDefenderTurn += ( ) => GD.Print ( "Defender's turn!" );
        // Create a test character data
        var testCharacter = new CharacterData ( );

        // Create a test card
        var testCard = CreateTestCard ( );

        // Equip the test card
        CharacterLogic.EquipCardToSlot ( stateManager, testCharacter, testCard );

        // Create attacker and defender for testing card effect
        var attacker = new TestCharacter ( "Attacker", 100 );
        var defender = new TestCharacter ( "Defender", 100 );

        // Use the slot and test event triggering
        CharacterLogic.UseSlot ( stateManager, testCharacter, CardData.TYPE.Q, attacker, defender );

        GD.Print ( $"Defender LP after effect: {defender.LP}" );
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