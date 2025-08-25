using Godot;
using meph;
using static meph.CharacterLogic;
using static meph.Character;

// Basically GameManager.cs but for debugging purposes
public partial class Debug : Node {
    StateManager stateManager;
    FactorManager factorManager;
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

    public static void ApplyDamage ( FactorManager factorManager, Character character, int damage ) {
        if ( damage <= 0 || character == null ) return;
        int remaining = FactorLogic.ResolveToughness ( factorManager, character, damage );
        if ( remaining > 0 )
            character.LP = Mathf.Max ( character.LP - remaining, 0 );
    }

    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }
    private void OnActionLockHandler ( ) { }

    public override void _Ready ( ) {
        var stateManager = new StateManager ( );
        var factorManager = new FactorManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler;
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock += OnActionLockHandler;

        // Load character resources
        var rokData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Rok.tres" );
        var owawaData = GD.Load<CharacterData> ( "res://Assets/Resources/Characters/Owawa.tres" );

        // Create characters from data and set their sides
        SetAttacker ( new Character ( rokData ) );
        SetDefender ( new Character ( owawaData ) );

        // Create and equip a test card to attacker
        var testCard = CreateTestCard ( );
        stateManager.TryAction ( ( ) => EquipCardToSlot ( Attacker, testCard ) );
        stateManager.TryAction ( ( ) => UseSlot ( Attacker, Card.TYPE.Q, Attacker, Defender ) );
        stateManager.NextTurn ( );
        stateManager.TryAction ( ( ) => UseSlot ( Attacker, Card.TYPE.Q, Attacker, Defender ) );
        GD.Print ( $"Defender LP after effect: {Defender.LP}" );
        GD.Print ( $"{Attacker.CharName} has STORM: {Attacker.StatusEffects.Has ( STATUS_EFFECT.STORM )}" );
    }

    private Card CreateTestCard ( ) {
        var testCard = new Card {
            Id = "test_q",
            Name = "Test Q Card",
            Type = Card.TYPE.Q,
            Description = "Placeholder Q card for testing.",
            Effect = (fm, user, target ) => {
                target.LP -= 10;
                user.StatusEffects |= STATUS_EFFECT.STORM;
            }
        };
        return testCard;
    }
}