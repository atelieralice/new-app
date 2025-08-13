using Godot;
using meph;
using System.Linq;

public enum GamePhase {
    SETUP,
    CHARACTER_SELECTION,
    BATTLE, // Combined equip/battle phase
    GAME_OVER
}

public partial class GameManager : Node {
    public static GameManager Instance { get; private set; }
    
    // Add UI reference
    private GameUI gameUI;

    public StateManager StateManager { get; private set; }
    public FactorManager FactorManager { get; private set; }
    public Character Attacker { get; private set; }
    public Character Defender { get; private set; }

    // Node references for clean separation
    private Control boardRoot;
    private Control uiRoot;
    private RichTextLabel consoleLog;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.SETUP;
    public bool GameInProgress { get; private set; } = false;

    public override void _Ready ( ) {
        Instance = this;

        // Initialize core systems
        StateManager = new StateManager ( );
        FactorManager = new FactorManager ( );

        // Provide player getter to StateManager
        StateManager.GetPlayer = ( turn ) => turn == TURN.ATTACKER ? Attacker : Defender;

        // Get node references
        InitializeNodeReferences ( );
        InitializeConsole ( );
        InitializeEvents ( );

        ConsoleLog.Game ( "GameManager ready." );
        GameEvents.TriggerGameStarted ( );

        // Start the game setup phase
        StartGameSetup();
    }

    private void InitializeNodeReferences ( ) {
        boardRoot = GetNode<Control> ( "%BoardRoot" );
        uiRoot = GetNode<Control> ( "%UI" );
        consoleLog = GetNode<RichTextLabel> ( "%ConsoleLog" );
        
        // Initialize UI
        gameUI = GetNode<GameUI>("%GameUI");
        
        if ( boardRoot == null ) ConsoleLog.Error ( "BoardRoot node not found!" );
        if ( uiRoot == null ) ConsoleLog.Error ( "UI node not found!" );
        if ( consoleLog == null ) ConsoleLog.Error ( "ConsoleLog node not found!" );
        if ( gameUI == null ) ConsoleLog.Error ( "GameUI node not found!" );
    }

    private void InitializeConsole ( ) {
        ConsoleLog.Init ( consoleLog );
    }

    private void InitializeEvents ( ) {
        // Factor events
        FactorManager.OnFactorApplied += ( character, effect, instance ) => {
            ConsoleLog.Factor ( $"Applied {effect} to {character} (dur {instance.Duration})" );
            GameEvents.TriggerFactorApplied ( character, effect, instance.Duration );
        };
        FactorManager.OnFactorRemoved += ( character, effect, instance ) =>
            ConsoleLog.Factor ( $"Removed {effect} from {character}" );
        FactorManager.OnStatusCleared += ( character, effect ) => {
            ConsoleLog.Factor ( $"Status cleared: {effect} on {character}" );
            GameEvents.TriggerFactorExpired ( character, effect );
        };
        FactorManager.OnFactorUpdate += ( ) =>
            ConsoleLog.Factor ( "Factors updated" );

        // State events
        StateManager.OnTurnStarted += ( turn, player ) => {
            ResolveTurnStart ( player, GetOpponent ( player ) );
            GameEvents.TriggerTurnStarted ( player );
        };
        StateManager.OnTurnEnded += ( turn, player ) => {
            ConsoleLog.Game ( $"{player?.CharName ?? turn.ToString ( )}'s turn ended" );
            GameEvents.TriggerTurnEnded ( player );
        };
        StateManager.OnActionLock += ( ) => {
            ConsoleLog.Warn ( "No actions remaining" );
            GameEvents.TriggerActionsLocked ( );
        };
        StateManager.OnActionsChanged += ( remaining ) => {
            ConsoleLog.Action ( $"Actions remaining: {remaining}" );
            GameEvents.TriggerActionsChanged ( remaining );
        };

        // Game events (console logging)
        GameEvents.OnDamageDealt += ( target, damage, remaining ) =>
            ConsoleLog.Combat ( $"{target} took {damage} damage ({remaining} LP remaining)" );
        GameEvents.OnCardUsed += ( user, card, target ) =>
            ConsoleLog.Combat ( $"{user} used {card?.Name ?? "unknown card"} on {target}" );
        GameEvents.OnCardEquipped += ( character, card ) =>
            ConsoleLog.Equip ( $"{character} equipped {card.Name}" );
        GameEvents.OnHealingReceived += ( character, amount ) =>
            ConsoleLog.Combat ( $"{character} healed for {amount} LP" );
        GameEvents.OnResourceStolen += ( from, to, amount, type ) =>
            ConsoleLog.Combat ( $"{to} stole {amount} {type} from {from}" );
        GameEvents.OnResourceRegenerated += ( character, ep, mp ) =>
            ConsoleLog.Resource ( $"{character} regenerated {ep} EP and {mp} MP" );
        GameEvents.OnCardFrozen += ( card, duration ) =>
            ConsoleLog.Factor ( $"{card.Name} was frozen for {duration} turns" );
        GameEvents.OnCardUnfrozen += ( card ) =>
            ConsoleLog.Factor ( $"{card.Name} was unfrozen" );
        GameEvents.OnAttackResolved += ( attacker, target, damage, wasCrit ) => {
            string critText = wasCrit ? " (CRITICAL HIT!)" : "";
            ConsoleLog.Combat ( $"{attacker} dealt {damage} damage to {target}{critText}" );
        };
        GameEvents.OnFactorBlocked += ( character, effect ) =>
            ConsoleLog.Factor ( $"{effect} blocked by Storm on {character}" );
        GameEvents.OnPlayerDefeated += ( character ) => {
            var winner = GetOpponent ( character );
            ConsoleLog.Game ( $"{character} was defeated! {winner} wins!" );
            GameEvents.TriggerPlayerVictory ( winner );
            GameEvents.TriggerGameEnded ( );
        };
        GameEvents.OnResourceGained += ( character, amount, type ) =>
            ConsoleLog.Resource ( $"{character} gained {amount} {type}" );
        GameEvents.OnResourceLost += ( character, amount, type ) =>
            ConsoleLog.Resource ( $"{character} lost {amount} {type}" );
    }

    // Game logic methods
    public Character GetOpponent ( Character player ) {
        if ( player == null ) return null;
        return player == Attacker ? Defender : Attacker;
    }

    public void SetAttacker ( Character character ) {
        Attacker = character;
        FactorManager.RegisterCharacter ( character );
        ConsoleLog.Game ( $"Attacker set: {character?.CharName ?? "None"}" );
    }

    public void SetDefender ( Character character ) {
        Defender = character;
        FactorManager.RegisterCharacter ( character );
        ConsoleLog.Game ( $"Defender set: {character?.CharName ?? "None"}" );
    }

    public void Reset ( ) {
        if ( Attacker != null ) FactorManager.UnregisterCharacter ( Attacker );
        if ( Defender != null ) FactorManager.UnregisterCharacter ( Defender );
        Attacker = null;
        Defender = null;
        ConsoleLog.Game ( "Game reset" );
    }

    // Main Gameplay Loop Implementation
    public void StartGameSetup() {
        CurrentPhase = GamePhase.SETUP;
        GameInProgress = false;
        
        ConsoleLog.Game("Starting game setup...");
        GameEvents.TriggerGamePhaseChanged("SETUP");
        
        // Clear any existing game state
        Reset();
        
        // Initialize game systems
        StateManager = new StateManager();
        FactorManager = new FactorManager();
        StateManager.GetPlayer = (turn) => turn == TURN.ATTACKER ? Attacker : Defender;
        
        ConsoleLog.Game("Game setup complete. Ready for character selection.");
        TransitionToCharacterSelection();
    }

    public void TransitionToCharacterSelection() {
        CurrentPhase = GamePhase.CHARACTER_SELECTION;
        GameEvents.TriggerGamePhaseChanged("CHARACTER_SELECTION");
        ConsoleLog.Game("Waiting for players to select characters...");
    }

    public void SetPlayers(Character attacker, Character defender) {
        if (CurrentPhase != GamePhase.CHARACTER_SELECTION) {
            ConsoleLog.Warn("Cannot set players - not in character selection phase");
            return;
        }

        SetAttacker(attacker);
        SetDefender(defender);
        
        GameEvents.TriggerPlayersSet(attacker, defender);
        ConsoleLog.Game($"Players set: {attacker.CharName} vs {defender.CharName}");
        
        TransitionToCardEquipment();
    }

    public void TransitionToCardEquipment() {
        // Remove this method since you're using hybrid gameplay
        // Just transition directly to battle
        StartBattle();
    }

    // Fix StartBattle method
    public void StartBattle() {
        // Only check for CHARACTER_SELECTION (remove CARD_EQUIPMENT reference)
        if (CurrentPhase != GamePhase.CHARACTER_SELECTION) {
            ConsoleLog.Warn("Cannot start battle - not in character selection phase");
            return;
        }

        if (!ValidateGameStart()) {
            ConsoleLog.Error("Cannot start battle - validation failed");
            return;
        }

        CurrentPhase = GamePhase.BATTLE;
        GameInProgress = true;
        
        GameEvents.TriggerGamePhaseChanged("BATTLE");
        ConsoleLog.Game("BATTLE BEGINS! Players can equip cards and attack!");
        
        // Initialize character passives
        CharacterPassives.InitializePassives(Attacker);
        CharacterPassives.InitializePassives(Defender);
        
        // FIXED LINE 229: Use GameEvents instead of direct event invocation
        GameEvents.TriggerTurnStarted(GetCurrentPlayer());
    }

    private bool ValidateGameStart() {
        if (Attacker == null || Defender == null) {
            ConsoleLog.Error("Both players must be set before starting battle");
            return false;
        }

        // Validate character cards are equipped
        if (!Attacker.EquippedSlots.ContainsKey(Card.TYPE.C)) {
            ConsoleLog.Error($"{Attacker.CharName} must have a character card equipped");
            return false;
        }

        if (!Defender.EquippedSlots.ContainsKey(Card.TYPE.C)) {
            ConsoleLog.Error($"{Defender.CharName} must have a character card equipped");
            return false;
        }

        return true;
    }

    // Fixed turn resolution
    private void ResolveTurnStart(Character current, Character other) {
        if (current == null) {
            FactorManager.UpdateFactors();
            return;
        }

        ConsoleLog.Game($"{current}'s turn started");

        // Execute character-specific turn start effects
        CharacterPassives.ExecuteTurnStartEffects(current);

        // Resolve per-turn effects in correct order
        FactorLogic.ResolveHealing(FactorManager, current, other);
        if (other != null) {
            FactorLogic.ResolveRecharge(FactorManager, current, other);
            FactorLogic.ResolveGrowth(FactorManager, current, other);
        }
        FactorLogic.ResolveBurning(FactorManager, current);
        FactorLogic.ResolveStorm(FactorManager, current);
        
        // Age all factors
        FactorManager.UpdateFactors();

        // Regenerate resources (5% EP, 2% MP per game rules)
        RegenerateResources(current);
        
        // Reset actions for new turn - use proper StateManager method
        StateManager.ActionsRemaining = 1; // Reset to 1 action per turn
        StateManager.ActionsLocked = false;
    }

    // Add missing RegenerateResources method
    private void RegenerateResources(Character character) {
        if (character == null) return;

        int oldEP = character.EP;
        int oldMP = character.MP;

        // 5% EP regeneration per turn
        int epRegen = Mathf.RoundToInt(character.MaxEP * 0.05f);
        character.EP = Mathf.Min(character.EP + epRegen, character.MaxEP);

        // 2% MP regeneration per turn  
        int mpRegen = Mathf.RoundToInt(character.MaxMP * 0.02f);
        character.MP = Mathf.Min(character.MP + mpRegen, character.MaxMP);

        // Trigger events for actual regeneration
        if (character.EP > oldEP || character.MP > oldMP) {
            GameEvents.TriggerResourceRegenerated(character, character.EP - oldEP, character.MP - oldMP);
        }
    }

    // Fixed damage application - make non-static to access instance
    public void ApplyDamage(Character character, int damage, bool isAbsolute = false) {
        if (damage <= 0 || character == null) return;

        int finalDamage = damage;
        
        // Handle absolute damage (bypasses DEF but respects shields)
        if (!isAbsolute) {
            // Apply DEF reduction for non-absolute damage
            finalDamage = Mathf.Max(damage - character.DEF, 0);
        }

        // Always check shields regardless of damage type
        int remaining = FactorLogic.ResolveToughness(FactorManager, character, finalDamage);

        if (remaining > 0) {
            int oldLP = character.LP;
            character.LP = Mathf.Max(character.LP - remaining, 0);

            // Trigger resource loss event
            if (oldLP > character.LP) {
                GameEvents.TriggerResourceLost(character, oldLP - character.LP, "LP");
            }
        }

        GameEvents.TriggerDamageDealt(character, damage, character.LP);

        // Check for defeat and end game if necessary
        if (character.LP <= 0) {
            HandlePlayerDefeat(character);
        }
    }

    // Fixed normal attack with proper dictionary access
    public void PerformNormalAttack(Character target) {
        var current = StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);
        if (current == null || target == null) return;

        if (!StateManager.CanAct()) {
            ConsoleLog.Warn("Cannot perform normal attack - no actions remaining");
            return;
        }

        // Use TryGetValue instead of GetValueOrDefault
        current.EquippedSlots.TryGetValue(Card.TYPE.BW, out Card baseWeapon);
        current.EquippedSlots.TryGetValue(Card.TYPE.SW, out Card secondaryWeapon);

        if (baseWeapon?.IsFrozen == true || secondaryWeapon?.IsFrozen == true) {
            ConsoleLog.Warn("Cannot perform normal attack - weapon is frozen");
            return;
        }

        StateManager.TryAction(() => {
            // Execute weapon effects
            baseWeapon?.Effect?.Invoke(current, target);
            secondaryWeapon?.Effect?.Invoke(current, target);
            
            GameEvents.TriggerNormalAttack(current, Card.TYPE.BW);
            ConsoleLog.Combat($"{current.CharName} performed normal attack on {target.CharName}");
        });
    }

    // Enhanced equipment - allow during battle phase
    public void EquipCard(Character character, Card card) {
        if (character == null || card == null) {
            ConsoleLog.Warn("Cannot equip card - character or card is null");
            return;
        }

        // Allow equipment during character selection and battle (hybrid gameplay)
        if (CurrentPhase != GamePhase.CHARACTER_SELECTION && CurrentPhase != GamePhase.BATTLE) {
            ConsoleLog.Warn("Cannot equip cards outside of active gameplay");
            return;
        }

        CharacterLogic.EquipCardToSlot(character, card);
    }

    public void EquipCharm(Character character, Charm charm) {
        if (character == null || charm == null) {
            ConsoleLog.Warn("Cannot equip charm - character or charm is null");
            return;
        }

        // Allow charm equipping during battle phase too
        if (CurrentPhase != GamePhase.CHARACTER_SELECTION && CurrentPhase != GamePhase.BATTLE) {
            ConsoleLog.Warn("Cannot equip charms outside of active gameplay");
            return;
        }

        CharmLogic.EquipCharm(character, charm);
    }

    // Updated API for hybrid gameplay
    public bool CanEquipCards() => CurrentPhase == GamePhase.CHARACTER_SELECTION || CurrentPhase == GamePhase.BATTLE;
    public bool CanPerformActions() => CurrentPhase == GamePhase.BATTLE && GameInProgress;

    // Add EndTurn method for manual turn ending
    public void EndTurn() {
        if (!GameInProgress || CurrentPhase != GamePhase.BATTLE) {
            ConsoleLog.Warn("Cannot end turn - game not in progress");
            return;
        }

        var currentPlayer = GetCurrentPlayer();
        ConsoleLog.Game($"{currentPlayer?.CharName ?? "Unknown"} ended their turn");
        
        // FIXED LINE 413: Use GameEvents instead of direct event invocation
        GameEvents.TriggerTurnEnded(currentPlayer);
        
        // Switch to next turn
        StateManager.NextTurn();
    }

    // Add missing GetCurrentPlayer method
    public Character GetCurrentPlayer() => StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);

    // Enhanced card usage with validation
    public void UseCard(Card.TYPE slotType, Character target) {
        var current = GetCurrentPlayer();
        if (current == null) {
            ConsoleLog.Warn("No current player to use card");
            return;
        }

        if (!StateManager.CanAct()) {
            ConsoleLog.Warn("Cannot use card - no actions remaining");
            return;
        }

        CharacterLogic.UseSlot(current, slotType, target);
    }

    // Game end handling
    public void HandlePlayerDefeat(Character defeated) {
        var winner = GetOpponent(defeated);
        CurrentPhase = GamePhase.GAME_OVER;
        GameInProgress = false;
        
        ConsoleLog.Game($"{defeated.CharName} was defeated! {winner.CharName} wins!");
        
        GameEvents.TriggerPlayerDefeated(defeated);
        GameEvents.TriggerPlayerVictory(winner);
        GameEvents.TriggerGameEnded();
        GameEvents.TriggerGamePhaseChanged("GAME_OVER");
    }

    public void RestartGame() {
        ConsoleLog.Game("Restarting game...");
        StartGameSetup();
    }
}