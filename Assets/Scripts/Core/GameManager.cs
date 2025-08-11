using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;
using static meph.Character;


// Main entry point node
public partial class GameManager : Node {
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

    // When an event is invoked these methods will be called
    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }
    private void OnActionLockHandler ( ) { }

    // Runs once when the game starts
    public override void _Ready ( ) {
        var stateManager = new StateManager ( );
        var factorManager = new FactorManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler; // += is used to subscribe to events
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock += OnActionLockHandler;
    }

    public override void _Process ( double delta ) { }

}