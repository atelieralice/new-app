using Godot;
using meph;
using static meph.CharacterLogic;
using static meph.Character;

// Main entry point node
public partial class GameManager : Node {
    StateManager stateManager;
    FactorManager factorManager;
    public Character Attacker { get; private set; }
    public Character Defender { get; private set; }

    public void SetAttacker ( Character character ) => Attacker = character;
    public void SetDefender ( Character character ) => Defender = character;
    public void Reset ( ) {
        Attacker = null;
        Defender = null;
    }

    // When an event is invoked these methods will be called
    private void OnAttackerTurnHandler ( ) { ResolveTurnStart ( Attacker, Defender ); }
    private void OnDefenderTurnHandler ( ) { ResolveTurnStart ( Defender, Attacker ); }
    private void OnActionLockHandler ( ) { }

    // Update factors for both sides
    private void ResolveTurnStart ( Character current, Character other ) {
        if ( current != null && other != null ) {
            FactorLogic.ResolveHealing ( factorManager, current, other );
            FactorLogic.ResolveRecharge ( factorManager, current, other );
            FactorLogic.ResolveGrowth ( factorManager, current, other );
            FactorLogic.ResolveBurning ( factorManager, current );
            FactorLogic.ResolveStorm ( factorManager, current );
            FactorLogic.ResolveCardFreeze ( current );

            FactorLogic.ResolveHealing ( factorManager, other, current );
            FactorLogic.ResolveRecharge ( factorManager, other, current );
            FactorLogic.ResolveGrowth ( factorManager, other, current );
            FactorLogic.ResolveBurning ( factorManager, other );
            FactorLogic.ResolveStorm ( factorManager, other );
            FactorLogic.ResolveCardFreeze ( other );
        }
        factorManager.UpdateFactors ( );
    }

    // Runs once when the game starts
    public override void _Ready ( ) {
        stateManager = new StateManager ( );
        factorManager = new FactorManager ( );

        // Init in-game console
        var console = GetNode<RichTextLabel> ( "%ConsoleLog" );
        ConsoleLog.Init ( console );
        ConsoleLog.Info ( "GameManager ready." );

        // Factor lifecycle logs
        factorManager.OnFactorApplied += ( character, effect, instance ) =>
            ConsoleLog.Info ( $"Applied {effect} (dur {instance.Duration})." );
        factorManager.OnFactorRemoved += ( character, effect, instance ) =>
            ConsoleLog.Info ( $"Removed {effect}." );
        factorManager.OnStatusCleared += ( character, effect ) =>
            ConsoleLog.Info ( $"Status cleared: {effect}." );
        factorManager.OnFactorUpdate += ( ) =>
            ConsoleLog.Info ( "Factors updated." );

        // Global events
        EventBus.OnAttackerTurn += OnAttackerTurnHandler;
        EventBus.OnDefenderTurn += OnDefenderTurnHandler;
        EventBus.OnActionLock += OnActionLockHandler;
    }

    public override void _Process ( double delta ) { }
}