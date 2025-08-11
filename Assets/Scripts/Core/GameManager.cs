using Godot;
using meph;
using static meph.CharacterCreator;
using static meph.CharacterLogic;
using static meph.Character;


// Main entry point node
public partial class GameManager : Node {
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

    // When an event is invoked these methods will be called
    private void OnAttackerTurnHandler ( ) { ResolveTurnStart ( Attacker, Defender ); }
    private void OnDefenderTurnHandler ( ) { ResolveTurnStart ( Defender, Attacker ); }
    private void OnActionLockHandler ( ) { }

    // Apply per-turn effects, then age/prune factors
    private void ResolveTurnStart ( Character current, Character other ) {
        if ( current == null ) { factorManager.UpdateFactors ( ); return; }

        // Per-turn ticks use current instances
        FactorLogic.ResolveHealing  ( factorManager, current, other );
        if ( other != null ) {
            FactorLogic.ResolveRecharge( factorManager, current, other );
            FactorLogic.ResolveGrowth  ( factorManager, current, other );
        }
        FactorLogic.ResolveBurning   ( factorManager, current ); // tick on current
        FactorLogic.ResolveStorm     ( factorManager, current ); // tick on current

        // Then decrement durations and remove expired
        factorManager.UpdateFactors ( );
    }

    // Runs once when the game starts
    public override void _Ready ( ) {
        stateManager = new StateManager ( );   // assign to fields (no var → no shadowing)
        factorManager = new FactorManager ( );

        stateManager.OnAttackerTurn += OnAttackerTurnHandler;
        stateManager.OnDefenderTurn += OnDefenderTurnHandler;
        stateManager.OnActionLock   += OnActionLockHandler;
    }

    public override void _Process ( double delta ) { }

}