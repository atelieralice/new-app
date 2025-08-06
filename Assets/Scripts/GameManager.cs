using Godot;
using meph;

public partial class GameManager : Node {
    public override void _Ready ( ) {
        StateManager stateManager = new StateManager ( );
        stateManager.OnAttackerTurn += OnAttackerTurnHandler;
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
    }

    public override void _Process ( double delta ) { }

    private void OnAttackerTurnHandler ( ) { }
    private void OnDefenderTurnHandler ( ) { }

}