using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;

// Basically GameManager.cs but for debugging purposes
public partial class Debug : Node {
    public Character Attacker { get; private set; }
    public Character Defender { get; private set; }

    public void SetAttacker ( Character character ) {
        Attacker = character;
    }
    public void SetDefender ( Character character ) {
        Defender = character;
    }
    public void Reset ( ) {
        Attacker = null;
        Defender = null;
    }

    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }
    private void OnActionLockHandler ( ) { }

    public override void _Ready ( ) {
        var stateManager = new StateManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler;
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock += OnActionLockHandler;

        // Load character resources
        var rokData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Rok.tres" );
        var owawaData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Owawa.tres" );

        // Create characters from data and set their sides
        SetAttacker ( InitCharacter ( rokData ) );
        SetDefender ( InitCharacter ( owawaData ) );

        // Create and equip a test card to attacker
        var testCard = CreateTestCard ( );
        stateManager.TryAction ( ( ) => EquipCardToSlot ( stateManager, Attacker, testCard ) );
        stateManager.TryAction ( ( ) => UseSlot ( stateManager, Attacker, CardData.TYPE.Q, Attacker, Defender ) );

        GD.Print ( $"Defender LP after effect: {Defender.LP}" );
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