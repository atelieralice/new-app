using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;

public partial class Debug : Node {
    public override void _Ready ( ) {
        var stateManager = new StateManager ( );

        // Load character resources
        var rokData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Rok.tres" );
        var owawaData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Owawa.tres" );

        // Create attacker and defender from resource data
        var attacker = InitCharacter ( rokData );
        var defender = InitCharacter ( owawaData );

        // Create and equip a test card to attacker
        var testCard = CreateTestCard ( );
        EquipCardToSlot ( stateManager, attacker, testCard );

        // Use the slot and test event triggering
        UseSlot ( stateManager, attacker, CardData.TYPE.Q, attacker, defender );

        GD.Print ( $"Defender LP after effect: {defender.LP}" );
    }



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