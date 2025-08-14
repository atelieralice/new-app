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
    
    // UI references
    private GameUI gameUI;
    private ConsoleWindow consoleWindow;

    // Core game systems
    public StateManager StateManager { get; private set; }
    public FactorManager FactorManager { get; private set; }
    public Character Attacker { get; private set; }
    public Character Defender { get; private set; }

    // Node references for clean separation
    private Control boardRoot;
    private Control uiRoot;

    // Game state
    public GamePhase CurrentPhase { get; private set; } = GamePhase.SETUP;
    public bool GameInProgress { get; private set; } = false;

    public override void _Ready() {
        Instance = this;

        // Initialize core systems first
        InitializeCoreSystems();
        
        // Get node references
        InitializeNodeReferences();
        
        // Setup event handlers
        InitializeEvents();

        ConsoleLog.Game("GameManager ready.");
        GameEvents.TriggerGameStarted();

        // Start the game setup phase
        StartGameSetup();
    }

    private void InitializeCoreSystems() {
        StateManager = new StateManager();
        FactorManager = new FactorManager();
        
        // Provide player getter to StateManager
        StateManager.GetPlayer = (turn) => turn == TURN.ATTACKER ? Attacker : Defender;
    }

    private void InitializeNodeReferences() {
        // Get core UI nodes
        boardRoot = GetNode<Control>("%BoardRoot");
        uiRoot = GetNode<Control>("%UI");
        gameUI = GetNode<GameUI>("%GameUI");
        
        // Validate critical nodes
        ValidateNodeReferences();
        
        // Create external console window
        CreateConsoleWindow();
    }

    private void ValidateNodeReferences() {
        if (boardRoot == null) ConsoleLog.Error("BoardRoot node not found!");
        if (uiRoot == null) ConsoleLog.Error("UI node not found!");
        if (gameUI == null) ConsoleLog.Error("GameUI node not found!");
    }

    private void CreateConsoleWindow() {
        try {
            var consoleScene = GD.Load<PackedScene>("res://Scenes/ConsoleWindow.tscn");
            if (consoleScene == null) {
                ConsoleLog.Error("Failed to load ConsoleWindow.tscn - scene file not found");
                return;
            }

            consoleWindow = consoleScene.Instantiate<ConsoleWindow>();
            if (consoleWindow == null) {
                ConsoleLog.Error("Failed to instantiate ConsoleWindow");
                return;
            }
            
            // Add to main window for native OS window behavior
            GetWindow().AddChild(consoleWindow);
            
            // Show as popup centered window
            consoleWindow.PopupCentered(new Vector2I(800, 600));
            
            ConsoleLog.Game("External console window created successfully");
        }
        catch (System.Exception ex) {
            ConsoleLog.Error($"Failed to create console window: {ex.Message}");
        }
    }

    private void InitializeEvents() {
        InitializeFactorEvents();
        InitializeStateEvents();
        InitializeGameEvents();
    }

    private void InitializeFactorEvents() {
        FactorManager.OnFactorApplied += (character, effect, instance) => {
            ConsoleLog.Factor($"Applied {effect} to {character} (dur {instance.Duration})");
            GameEvents.TriggerFactorApplied(character, effect, instance.Duration);
        };
        
        FactorManager.OnFactorRemoved += (character, effect, instance) =>
            ConsoleLog.Factor($"Removed {effect} from {character}");
            
        FactorManager.OnStatusCleared += (character, effect) => {
            ConsoleLog.Factor($"Status cleared: {effect} on {character}");
            GameEvents.TriggerFactorExpired(character, effect);
        };
        
        FactorManager.OnFactorUpdate += () =>
            ConsoleLog.Factor("Factors updated");
    }

    private void InitializeStateEvents() {
        StateManager.OnTurnStarted += (turn, player) => {
            ResolveTurnStart(player, GetOpponent(player));
            GameEvents.TriggerTurnStarted(player);
        };
        
        StateManager.OnTurnEnded += (turn, player) => {
            ConsoleLog.Game($"{player?.CharName ?? turn.ToString()}'s turn ended");
            GameEvents.TriggerTurnEnded(player);
        };
        
        StateManager.OnActionLock += () => {
            ConsoleLog.Warn("No actions remaining");
            GameEvents.TriggerActionsLocked();
        };
        
        StateManager.OnActionsChanged += (remaining) => {
            ConsoleLog.Action($"Actions remaining: {remaining}");
            GameEvents.TriggerActionsChanged(remaining);
        };
    }

    private void InitializeGameEvents() {
        // Combat events
        GameEvents.OnDamageDealt += (target, damage, remaining) =>
            ConsoleLog.Combat($"{target} took {damage} damage ({remaining} LP remaining)");
            
        GameEvents.OnCardUsed += (user, card, target) =>
            ConsoleLog.Combat($"{user} used {card?.Name ?? "unknown card"} on {target}");
            
        GameEvents.OnHealingReceived += (character, amount) =>
            ConsoleLog.Combat($"{character} healed for {amount} LP");
            
        GameEvents.OnAttackResolved += (attacker, target, damage, wasCrit) => {
            string critText = wasCrit ? " (CRITICAL HIT!)" : "";
            ConsoleLog.Combat($"{attacker} dealt {damage} damage to {target}{critText}");
        };

        // Equipment events
        GameEvents.OnCardEquipped += (character, card) =>
            ConsoleLog.Equip($"{character} equipped {card.Name}");
            
        GameEvents.OnCardFrozen += (card, duration) =>
            ConsoleLog.Factor($"{card.Name} was frozen for {duration} turns");
            
        GameEvents.OnCardUnfrozen += (card) =>
            ConsoleLog.Factor($"{card.Name} was unfrozen");

        // Resource events
        GameEvents.OnResourceStolen += (from, to, amount, type) =>
            ConsoleLog.Combat($"{to} stole {amount} {type} from {from}");
            
        GameEvents.OnResourceRegenerated += (character, ep, mp) =>
            ConsoleLog.Resource($"{character} regenerated {ep} EP and {mp} MP");
            
        GameEvents.OnResourceGained += (character, amount, type) =>
            ConsoleLog.Resource($"{character} gained {amount} {type}");
            
        GameEvents.OnResourceLost += (character, amount, type) =>
            ConsoleLog.Resource($"{character} lost {amount} {type}");

        // Factor events
        GameEvents.OnFactorBlocked += (character, effect) =>
            ConsoleLog.Factor($"{effect} blocked by Storm on {character}");

        // Game state events
        GameEvents.OnPlayerDefeated += (character) => {
            var winner = GetOpponent(character);
            ConsoleLog.Game($"{character} was defeated! {winner} wins!");
            GameEvents.TriggerPlayerVictory(winner);
            GameEvents.TriggerGameEnded();
        };
    }

    // Game logic methods
    public Character GetOpponent(Character player) {
        if (player == null) return null;
        return player == Attacker ? Defender : Attacker;
    }

    public Character GetCurrentPlayer() => StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);

    public void SetAttacker(Character character) {
        if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
        
        Attacker = character;
        if (character != null) {
            FactorManager.RegisterCharacter(character);
        }
        ConsoleLog.Game($"Attacker set: {character?.CharName ?? "None"}");
    }

    public void SetDefender(Character character) {
        if (Defender != null) FactorManager.UnregisterCharacter(Defender);
        
        Defender = character;
        if (character != null) {
            FactorManager.RegisterCharacter(character);
        }
        ConsoleLog.Game($"Defender set: {character?.CharName ?? "None"}");
    }

    public void Reset() {
        if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
        if (Defender != null) FactorManager.UnregisterCharacter(Defender);
        
        Attacker = null;
        Defender = null;
        CurrentPhase = GamePhase.SETUP;
        GameInProgress = false;
        
        ConsoleLog.Game("Game reset");
    }

    // Main Gameplay Loop Implementation
    public void StartGameSetup() {
        CurrentPhase = GamePhase.SETUP;
        GameInProgress = false;
        
        ConsoleLog.Game("Starting game setup...");
        GameEvents.TriggerGamePhaseChanged("SETUP");
        
        // Clear any existing game state
        Reset();
        
        // Reinitialize game systems
        InitializeCoreSystems();
        
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

        if (attacker == null || defender == null) {
            ConsoleLog.Error("Both attacker and defender must be provided");
            return;
        }

        SetAttacker(attacker);
        SetDefender(defender);
        
        GameEvents.TriggerPlayersSet(attacker, defender);
        ConsoleLog.Game($"Players set: {attacker.CharName} vs {defender.CharName}");
        
        StartBattle();
    }

    public void StartBattle() {
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
        
        // Start first turn
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

    // Turn resolution with proper error handling
    private void ResolveTurnStart(Character current, Character other) {
        if (current == null) {
            ConsoleLog.Warn("Turn started with null player - updating factors only");
            FactorManager.UpdateFactors();
            return;
        }

        ConsoleLog.Game($"{current.CharName}'s turn started");

        try {
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

            // Regenerate resources
            RegenerateResources(current);
            
            // Reset actions for new turn
            StateManager.ActionsRemaining = 1;
            StateManager.ActionsLocked = false;
        }
        catch (System.Exception ex) {
            ConsoleLog.Error($"Error during turn start resolution: {ex.Message}");
        }
    }

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

        // Trigger events only for actual regeneration
        if (character.EP > oldEP || character.MP > oldMP) {
            GameEvents.TriggerResourceRegenerated(character, character.EP - oldEP, character.MP - oldMP);
        }
    }

    // Enhanced damage application with better error handling
    public void ApplyDamage(Character character, int damage, bool isAbsolute = false) {
        if (character == null) {
            ConsoleLog.Error("Cannot apply damage - character is null");
            return;
        }
        
        if (damage <= 0) {
            ConsoleLog.Warn($"Invalid damage amount: {damage}");
            return;
        }

        int finalDamage = damage;
        
        // Handle absolute damage (bypasses DEF but respects shields)
        if (!isAbsolute) {
            finalDamage = Mathf.Max(damage - character.DEF, 0);
        }

        // Check shields regardless of damage type
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

        // Check for defeat
        if (character.LP <= 0) {
            HandlePlayerDefeat(character);
        }
    }

    // Combat actions with proper validation
    public void PerformNormalAttack(Character target) {
        var current = GetCurrentPlayer();
        if (!ValidateAction(current, target, "normal attack")) return;

        current.EquippedSlots.TryGetValue(Card.TYPE.BW, out Card baseWeapon);
        current.EquippedSlots.TryGetValue(Card.TYPE.SW, out Card secondaryWeapon);

        if (baseWeapon?.IsFrozen == true || secondaryWeapon?.IsFrozen == true) {
            ConsoleLog.Warn("Cannot perform normal attack - weapon is frozen");
            return;
        }

        StateManager.TryAction(() => {
            baseWeapon?.Effect?.Invoke(current, target);
            secondaryWeapon?.Effect?.Invoke(current, target);
            
            GameEvents.TriggerNormalAttack(current, Card.TYPE.BW);
            ConsoleLog.Combat($"{current.CharName} performed normal attack on {target.CharName}");
        });
    }

    public void UseCard(Card.TYPE slotType, Character target) {
        var current = GetCurrentPlayer();
        if (!ValidateAction(current, target, "use card")) return;

        CharacterLogic.UseSlot(current, slotType, target);
    }

    private bool ValidateAction(Character current, Character target, string actionName) {
        if (current == null) {
            ConsoleLog.Warn($"Cannot {actionName} - no current player");
            return false;
        }

        if (target == null) {
            ConsoleLog.Warn($"Cannot {actionName} - no target specified");
            return false;
        }

        if (!StateManager.CanAct()) {
            ConsoleLog.Warn($"Cannot {actionName} - no actions remaining");
            return false;
        }

        return true;
    }

    // Equipment with phase validation
    public void EquipCard(Character character, Card card) {
        if (character == null || card == null) {
            ConsoleLog.Warn("Cannot equip card - character or card is null");
            return;
        }

        if (!CanEquipCards()) {
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

        if (!CanEquipCards()) {
            ConsoleLog.Warn("Cannot equip charms outside of active gameplay");
            return;
        }

        CharmLogic.EquipCharm(character, charm);
    }

    // API for game state queries
    public bool CanEquipCards() => CurrentPhase == GamePhase.CHARACTER_SELECTION || CurrentPhase == GamePhase.BATTLE;
    public bool CanPerformActions() => CurrentPhase == GamePhase.BATTLE && GameInProgress;

    // Turn management
    public void EndTurn() {
        if (!GameInProgress || CurrentPhase != GamePhase.BATTLE) {
            ConsoleLog.Warn("Cannot end turn - game not in progress");
            return;
        }

        var currentPlayer = GetCurrentPlayer();
        ConsoleLog.Game($"{currentPlayer?.CharName ?? "Unknown"} ended their turn");
        
        GameEvents.TriggerTurnEnded(currentPlayer);
        StateManager.NextTurn();
    }

    // Game end handling
    public void HandlePlayerDefeat(Character defeated) {
        if (defeated == null) return;
        
        var winner = GetOpponent(defeated);
        CurrentPhase = GamePhase.GAME_OVER;
        GameInProgress = false;
        
        ConsoleLog.Game($"{defeated.CharName} was defeated! {winner?.CharName ?? "Unknown"} wins!");
        
        GameEvents.TriggerPlayerDefeated(defeated);
        if (winner != null) {
            GameEvents.TriggerPlayerVictory(winner);
        }
        GameEvents.TriggerGameEnded();
        GameEvents.TriggerGamePhaseChanged("GAME_OVER");
    }

    public void RestartGame() {
        ConsoleLog.Game("Restarting game...");
        StartGameSetup();
    }

    // Input handling for console toggle
    public override void _UnhandledKeyInput(InputEvent @event) {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.F1) {
                consoleWindow?.ToggleVisibility();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // Cleanup
    public override void _ExitTree() {
        Reset();
        consoleWindow?.QueueFree();
        Instance = null;
    }
}