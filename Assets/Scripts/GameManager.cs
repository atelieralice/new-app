using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;


// Entry point
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

    // When an event is invoked these methods will be called
    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }
    private void OnActionLockHandler ( ) { }

    // Runs once when the game starts
    public override void _Ready ( ) {
        StateManager stateManager = new StateManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler; // += is used to subscribe to events
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock += OnActionLockHandler;
    }

    public override void _Process ( double delta ) { }

}