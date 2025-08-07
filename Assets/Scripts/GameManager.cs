using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;

// Entry point
public partial class GameManager : Node {
    public override void _Ready ( ) {
        StateManager stateManager = new StateManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler;
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock += OnActionLockHandler;
    }

    public override void _Process ( double delta ) { }

    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }
    private void OnActionLockHandler ( ) { }


}