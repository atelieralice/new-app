using Godot;
using meph;
using System.Linq;

/// <summary>
/// Enumeration defining the primary phases of game progression
/// Controls game flow and determines available actions and UI states
/// Used for phase validation and state transition management
/// </summary>
public enum GamePhase {
    /// <summary>Player selects between Character Battle and Flexible Mode</summary>
    MODE_SELECTION,
    /// <summary>Players select their characters and perform initial equipment setup</summary>
    CHARACTER_SELECTION,
    /// <summary>Active combat phase with turn-based gameplay and action economy</summary>
    BATTLE,
    /// <summary>Game conclusion phase after player defeat, showing results and restart options</summary>
    GAME_OVER
}

/// <summary>
/// Enumeration defining the two primary game modes with different equipment rules
/// Determines timing and cost of card equipment during gameplay
/// Affects strategic depth and accessibility for different player preferences
/// </summary>
public enum GameMode {
    /// <summary>Characters start fully equipped with all cards, focuses on pure combat tactics</summary>
    CHARACTER_BATTLE,
    /// <summary>Players equip cards during battle using action economy, adds strategic equipment timing</summary>
    FLEXIBLE_MODE
}

/// <summary>
/// Central game coordination system managing all major game systems and state transitions
/// Implements comprehensive game flow control from mode selection through combat resolution
/// Provides unified interface for UI, combat systems, and game state management
/// 
/// Core Responsibilities:
/// - Game Flow Management: Controls phase transitions and mode-specific rules
/// - System Coordination: Integrates StateManager, FactorManager, and character systems
/// - Event Hub: Routes events between game systems for loose coupling
/// - Combat Resolution: Manages damage application, action validation, and defeat handling
/// - Equipment Control: Enforces equipment rules based on game mode and phase
/// - Console Integration: Provides comprehensive logging and debugging capabilities
/// 
/// Game Mode Integration:
/// - Character Battle: Pre-equipped characters, immediate combat focus
/// - Flexible Mode: Strategic equipment timing with action economy costs
/// - Phase Validation: Ensures actions are only available in appropriate phases
/// - Rule Enforcement: Validates card ownership and equipment restrictions
/// 
/// System Architecture:
/// - Singleton Pattern: Provides global access for UI and game systems
/// - Event-Driven Design: Loose coupling between systems through GameEvents
/// - Error Handling: Comprehensive validation and graceful failure recovery
/// - Resource Management: Automatic cleanup and memory management
/// 
/// Integration Points:
/// - StateManager: Turn flow and action economy management
/// - FactorManager: Status effect tracking and resolution
/// - Character Systems: Passive abilities and equipment validation
/// - UI Systems: Phase-aware interface updates and action feedback
/// - GameEvents: Centralized event routing for system communication
/// </summary>
public partial class GameManager : Node {
    
    #region Singleton and Core References
    
    /// <summary>
    /// Global singleton instance providing unified access to game management systems
    /// Set during _Ready() and cleared during _ExitTree() for proper lifecycle management
    /// Used by UI systems, combat logic, and utility classes for game state access
    /// </summary>
    public static GameManager Instance { get; private set; }
    
    /// <summary>
    /// Primary game UI controller managing interface updates and user interactions
    /// Handles phase-specific UI states and provides player action interfaces
    /// Integrates with GameEvents for real-time status updates and feedback
    /// </summary>
    private GameUI gameUI;
    
    /// <summary>
    /// External console window for comprehensive game logging and debugging
    /// Created dynamically with fallback to programmatic creation if scene loading fails
    /// Toggleable with F1 key for development and troubleshooting support
    /// </summary>
    private ConsoleWindow consoleWindow;
    
    #endregion
    
    #region Core Game Systems
    
    /// <summary>
    /// Turn flow controller managing action economy and resource regeneration
    /// Handles turn alternation, action validation, and automatic EP/MP recovery
    /// Integrates with character systems for turn-based effect resolution
    /// </summary>
    public StateManager StateManager { get; private set; }
    
    /// <summary>
    /// Status effect manager tracking factor instances and duration handling
    /// Provides centralized storage for Toughness, Healing, Burning, and other factors
    /// Handles automatic expiration and bitfield updates for efficient status queries
    /// </summary>
    public FactorManager FactorManager { get; private set; }
    
    /// <summary>
    /// The player who initiated combat and has the first turn
    /// May be null during setup phases or after game reset
    /// Automatically registered with FactorManager for status effect tracking
    /// </summary>
    public Character Attacker { get; private set; }
    
    /// <summary>
    /// The player responding to combat in the defensive position
    /// May be null during setup phases or after game reset
    /// Automatically registered with FactorManager for status effect tracking
    /// </summary>
    public Character Defender { get; private set; }
    
    #endregion
    
    #region Node References and UI Management
    
    /// <summary>
    /// Root container for game board elements and visual combat representation
    /// Provides organized structure for card display and combat effects
    /// Validated during initialization to ensure proper scene tree setup
    /// </summary>
    private Control boardRoot;
    
    /// <summary>
    /// Root container for user interface elements and control panels
    /// Maintains separation between game board and interface components
    /// Enables independent UI updates without affecting board representation
    /// </summary>
    private Control uiRoot;
    
    #endregion
    
    #region Game State Properties
    
    /// <summary>
    /// Current phase of game progression determining available actions and UI state
    /// Controls phase transitions and validates action availability
    /// Updated through proper transition methods with event notification
    /// </summary>
    public GamePhase CurrentPhase { get; private set; } = GamePhase.MODE_SELECTION;
    
    /// <summary>
    /// Selected game mode affecting equipment rules and strategic depth
    /// Character Battle: Pre-equipped focus on combat tactics
    /// Flexible Mode: Strategic equipment timing with action costs
    /// </summary>
    public GameMode CurrentMode { get; private set; } = GameMode.CHARACTER_BATTLE;
    
    /// <summary>
    /// Indicates whether an active battle is in progress
    /// Used for action validation and state transition control
    /// Prevents invalid operations during setup or game over phases
    /// </summary>
    public bool GameInProgress { get; private set; } = false;
    
    #endregion
    
    #region Initialization and Setup
    
    /// <summary>
    /// Primary initialization method setting up all game systems and dependencies
    /// Establishes proper initialization order to prevent dependency issues
    /// Configures event handlers and validates scene tree structure
    /// 
    /// Initialization Sequence:
    /// 1. Set singleton instance for global access
    /// 2. Initialize core game systems (StateManager, FactorManager)
    /// 3. Establish node references and validate scene structure
    /// 4. Configure comprehensive event handling network
    /// 5. Create console window with fallback error handling
    /// 6. Trigger initial game events and start mode selection
    /// 
    /// Error Handling:
    /// - Node reference validation with detailed error logging
    /// - Console window creation with programmatic fallback
    /// - Deferred operations to avoid scene tree conflicts
    /// - Graceful degradation when optional components fail
    /// </summary>
    public override void _Ready() {
        Instance = this;

        // Initialize core systems first
        InitializeCoreSystems();
        
        // Get node references
        InitializeNodeReferences();
        
        // Setup event handlers
        InitializeEvents();

        // CRITICAL: Defer console window creation to avoid scene tree conflicts
        CallDeferred(nameof(CreateConsoleWindowDeferred));

        ConsoleLog.Game("GameManager ready.");
        GameEvents.TriggerGameStarted();

        // Start with mode selection
        StartModeSelection();
    }

    /// <summary>
    /// Initializes core game management systems with proper dependency injection
    /// Establishes StateManager-GameManager coupling for player access
    /// Creates FactorManager instance for centralized status effect management
    /// 
    /// System Configuration:
    /// - StateManager: Configured with player getter delegate for turn-based access
    /// - FactorManager: Initialized for character registration and factor tracking
    /// - Dependency Injection: Provides clean system interaction without tight coupling
    /// </summary>
    private void InitializeCoreSystems() {
        StateManager = new StateManager();
        FactorManager = new FactorManager();
        
        // Provide player getter to StateManager
        StateManager.GetPlayer = (turn) => turn == TURN.ATTACKER ? Attacker : Defender;
    }

    /// <summary>
    /// Establishes references to critical scene tree nodes with comprehensive validation
    /// Retrieves UI components and organizational containers for game management
    /// Performs validation to ensure proper scene structure before proceeding
    /// 
    /// Node Structure Requirements:
    /// - BoardRoot: Container for game board and visual elements
    /// - UI: Root container for interface components
    /// - GameUI: Primary game interface controller
    /// - Console window creation deferred to avoid initialization conflicts
    /// </summary>
    private void InitializeNodeReferences() {
        // Get core UI nodes
        boardRoot = GetNode<Control>("%BoardRoot");
        uiRoot = GetNode<Control>("%UI");
        gameUI = GetNode<GameUI>("%GameUI");
        
        // Validate critical nodes
        ValidateNodeReferences();
        
        // Console window creation is deferred to avoid scene tree conflicts
    }

    /// <summary>
    /// Validates that all critical scene tree nodes are properly accessible
    /// Provides detailed error logging for missing components
    /// Ensures graceful handling of scene structure issues during development
    /// 
    /// Validation Process:
    /// - Check each critical node reference for null values
    /// - Log detailed error messages for missing components
    /// - Continue initialization despite missing optional components
    /// - Enable debugging of scene tree structure issues
    /// </summary>
    private void ValidateNodeReferences() {
        if (boardRoot == null) {
            GD.PrintErr("BoardRoot node not found!");
            ConsoleLog.Error("BoardRoot node not found!");
        }
        if (uiRoot == null) {
            GD.PrintErr("UI node not found!");
            ConsoleLog.Error("UI node not found!");
        }
        if (gameUI == null) {
            GD.PrintErr("GameUI node not found!");
            ConsoleLog.Error("GameUI node not found!");
        }
    }

    /// <summary>
    /// Deferred console window creation wrapper to avoid scene tree conflicts
    /// Uses Godot's CallDeferred system to ensure proper initialization timing
    /// Prevents issues with scene tree modification during _Ready() processing
    /// </summary>
    private void CreateConsoleWindowDeferred() {
        CallDeferred(nameof(CreateConsoleWindow));
    }

    /// <summary>
    /// Creates external console window for comprehensive game logging and debugging
    /// Attempts scene-based creation with fallback to programmatic generation
    /// Provides robust error handling for development and deployment scenarios
    /// 
    /// Creation Strategy:
    /// 1. Attempt to load ConsoleWindow.tscn scene file
    /// 2. Instantiate console window from scene if available
    /// 3. Add to scene tree root safely using deferred operations
    /// 4. Fall back to programmatic creation if scene loading fails
    /// 5. Provide detailed error logging for troubleshooting
    /// 
    /// Error Recovery:
    /// - Scene loading failures trigger programmatic fallback
    /// - Instantiation errors handled gracefully with logging
    /// - Scene tree addition uses deferred operations for safety
    /// - Complete failure falls back to Godot's built-in console only
    /// </summary>
    private void CreateConsoleWindow() {
        if (consoleWindow != null) {
            ConsoleLog.Warn("Console window already exists");
            return;
        }

        try {
            var consoleScene = GD.Load<PackedScene>("res://Assets/Scenes/ConsoleWindow.tscn");
            if (consoleScene == null) {
                GD.PrintErr("Failed to load ConsoleWindow.tscn - scene file not found");
                CreateConsoleWindowProgrammatically();
                return;
            }

            consoleWindow = consoleScene.Instantiate<ConsoleWindow>();
            if (consoleWindow == null) {
                GD.PrintErr("Failed to instantiate ConsoleWindow from scene");
                CreateConsoleWindowProgrammatically();
                return;
            }
            
            // Add to scene tree root safely
            GetTree().Root.CallDeferred("add_child", consoleWindow);
            
            ConsoleLog.Game("External console window created from scene successfully");
        }
        catch (System.Exception ex) {
            GD.PrintErr($"Failed to create console window from scene: {ex.Message}");
            CreateConsoleWindowProgrammatically();
        }
    }

    /// <summary>
    /// Fallback console window creation using programmatic UI construction
    /// Creates fully functional console when scene loading fails
    /// Provides essential debugging capabilities in all deployment scenarios
    /// 
    /// Programmatic Construction:
    /// - Create ConsoleWindow instance directly
    /// - Configure RichTextLabel with appropriate settings for logging
    /// - Set up proper layout and styling for readability
    /// - Add to scene tree with safe deferred operations
    /// - Enable full console functionality without scene dependencies
    /// 
    /// Configuration Details:
    /// - BBCode enabled for formatted logging output
    /// - Scroll following for automatic log tracking
    /// - Selection and context menu enabled for log analysis
    /// - Full rectangle layout with appropriate padding
    /// - Proper anchoring for responsive window behavior
    /// </summary>
    private void CreateConsoleWindowProgrammatically() {
        try {
            consoleWindow = new ConsoleWindow();
            
            // Create and configure the console log RichTextLabel
            var richTextLabel = new RichTextLabel {
                Name = "ConsoleLog",
                BbcodeEnabled = true,
                ScrollFollowing = true,
                SelectionEnabled = true,
                ContextMenuEnabled = true,
                FitContent = true,
                ScrollActive = true
            };
            
            // Set to fill the entire window with padding
            richTextLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            richTextLabel.OffsetLeft = 8;
            richTextLabel.OffsetRight = -8;
            richTextLabel.OffsetTop = 8;
            richTextLabel.OffsetBottom = -8;
            
            // Add to console window
            consoleWindow.AddChild(richTextLabel);
            
            // Add to scene tree root safely
            GetTree().Root.CallDeferred("add_child", consoleWindow);
            
            ConsoleLog.Game("Console window created programmatically");
        }
        catch (System.Exception ex) {
            GD.PrintErr($"Failed to create console window programmatically: {ex.Message}");
            // Last resort: log to Godot's built-in console only
        }
    }
    
    #endregion
    
    #region Event System Integration
    
    /// <summary>
    /// Configures comprehensive event handling network for all game systems
    /// Establishes event routing between systems for loose coupling and responsiveness
    /// Enables real-time UI updates and proper system coordination
    /// 
    /// Event Categories:
    /// - Factor Events: Status effect application, removal, and expiration
    /// - State Events: Turn transitions, action changes, and resource updates
    /// - Game Events: Combat resolution, equipment changes, and victory conditions
    /// 
    /// Integration Benefits:
    /// - Loose coupling between game systems
    /// - Real-time UI synchronization
    /// - Comprehensive logging and debugging
    /// - Extensible event architecture for future features
    /// </summary>
    private void InitializeEvents() {
        InitializeFactorEvents();
        InitializeStateEvents();
        InitializeGameEvents();
    }

    /// <summary>
    /// Configures factor system event handlers for status effect tracking and logging
    /// Provides real-time feedback for factor applications, removals, and updates
    /// Integrates with GameEvents system for broader system notification
    /// 
    /// Factor Event Integration:
    /// - Application: Logs factor details and triggers GameEvents for UI updates
    /// - Removal: Provides factor expiration feedback and cleanup notifications
    /// - Status Clearing: Handles complete effect removal with proper event routing
    /// - Update Cycles: Tracks factor aging and duration management
    /// </summary>
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

    /// <summary>
    /// Configures state management event handlers for turn flow and action economy tracking
    /// Provides turn transition handling with factor resolution and resource management
    /// Integrates with GameEvents for UI updates and system synchronization
    /// 
    /// State Event Integration:
    /// - Turn Started: Triggers factor resolution and resource regeneration
    /// - Turn Ended: Provides transition logging and cleanup handling
    /// - Action Lock: Signals UI to show turn end options and disable actions
    /// - Action Changes: Updates action count displays and validates availability
    /// </summary>
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

    /// <summary>
    /// Configures game event handlers for combat, equipment, and resource tracking
    /// Provides comprehensive logging for all major game actions and state changes
    /// Enables detailed combat feedback and debugging capabilities
    /// 
    /// Game Event Categories:
    /// - Combat Events: Damage, healing, attacks, and critical hits
    /// - Equipment Events: Card equipping, freezing, and unfreezing
    /// - Resource Events: Stealing, regeneration, gains, and losses
    /// - Factor Events: Blocking, application, and expiration
    /// - Game State Events: Player defeat, victory, and game conclusion
    /// 
    /// Logging Integration:
    /// - Detailed combat feedback with damage and health tracking
    /// - Equipment changes with character and card identification
    /// - Resource transactions with amounts and participants
    /// - Factor interactions with blocking and application details
    /// - Game state transitions with winner determination and cleanup
    /// </summary>
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
    
    #endregion
    
    #region Player and Character Management
    
    /// <summary>
    /// Retrieves the opponent character for a given player
    /// Provides essential player relationship queries for combat and targeting
    /// Returns null for invalid input to prevent null reference exceptions
    /// 
    /// Usage Examples:
    /// - Combat targeting: Find opponent for damage application
    /// - Resource stealing: Identify source and target for factor effects
    /// - Victory conditions: Determine winner when player is defeated
    /// - Turn context: Provide opponent information during turn resolution
    /// </summary>
    /// <param name="player">Player to find opponent for</param>
    /// <returns>Opponent character or null if player is invalid</returns>
    public Character GetOpponent(Character player) {
        if (player == null) return null;
        return player == Attacker ? Defender : Attacker;
    }

    /// <summary>
    /// Retrieves the currently active player based on StateManager turn tracking
    /// Provides unified access to current player for action validation and UI updates
    /// Returns null during setup phases or when no players are set
    /// </summary>
    /// <returns>Currently active player character or null if none set</returns>
    public Character GetCurrentPlayer() => StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);

    /// <summary>
    /// Sets the attacking player with proper FactorManager integration
    /// Handles character registration for status effect tracking and cleanup
    /// Provides detailed logging for player assignment and game state changes
    /// 
    /// Character Registration Process:
    /// 1. Unregister previous attacker if exists (cleanup old factors)
    /// 2. Set new attacker reference
    /// 3. Register new attacker with FactorManager for status tracking
    /// 4. Log assignment for debugging and game state tracking
    /// 
    /// Integration Points:
    /// - FactorManager: Automatic registration for status effect tracking
    /// - Cleanup: Proper cleanup of previous character's factors
    /// - Logging: Detailed assignment tracking for debugging
    /// </summary>
    /// <param name="character">Character to set as attacker</param>
    public void SetAttacker(Character character) {
        if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
        
        Attacker = character;
        if (character != null) {
            FactorManager.RegisterCharacter(character);
        }
        ConsoleLog.Game($"Attacker set: {character?.CharName ?? "None"}");
    }

    /// <summary>
    /// Sets the defending player with proper FactorManager integration
    /// Handles character registration for status effect tracking and cleanup
    /// Provides detailed logging for player assignment and game state changes
    /// 
    /// Character Registration Process:
    /// 1. Unregister previous defender if exists (cleanup old factors)
    /// 2. Set new defender reference
    /// 3. Register new defender with FactorManager for status tracking
    /// 4. Log assignment for debugging and game state tracking
    /// 
    /// Integration Points:
    /// - FactorManager: Automatic registration for status effect tracking
    /// - Cleanup: Proper cleanup of previous character's factors
    /// - Logging: Detailed assignment tracking for debugging
    /// </summary>
    /// <param name="character">Character to set as defender</param>
    public void SetDefender(Character character) {
        if (Defender != null) FactorManager.UnregisterCharacter(Defender);
        
        Defender = character;
        if (character != null) {
            FactorManager.RegisterCharacter(character);
        }
        ConsoleLog.Game($"Defender set: {character?.CharName ?? "None"}");
    }

    /// <summary>
    /// Resets game state to initial conditions with comprehensive cleanup
    /// Clears all character assignments and factor tracking
    /// Returns game to mode selection phase for fresh game start
    /// 
    /// Reset Process:
    /// 1. Unregister characters from FactorManager (cleanup all factors)
    /// 2. Clear character references
    /// 3. Reset phase to MODE_SELECTION
    /// 4. Clear game progress flag
    /// 5. Log reset completion for debugging
    /// 
    /// Use Cases:
    /// - Game restart after completion
    /// - Error recovery during setup
    /// - Manual reset from UI
    /// - Cleanup during scene transitions
    /// </summary>
    public void Reset() {
        if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
        if (Defender != null) FactorManager.UnregisterCharacter(Defender);
        
        Attacker = null;
        Defender = null;
        CurrentPhase = GamePhase.MODE_SELECTION;
        GameInProgress = false;
        
        ConsoleLog.Game("Game reset");
    }
    
    #endregion
    
    #region Game Flow Management
    
    /// <summary>
    /// Initiates mode selection phase with proper state reset and event notification
    /// Provides entry point for new games and post-completion restart
    /// Ensures clean state for mode selection UI and player interaction
    /// 
    /// Mode Selection Setup:
    /// - Reset to MODE_SELECTION phase
    /// - Clear game progress flag
    /// - Trigger phase change events for UI updates
    /// - Log phase transition for debugging
    /// </summary>
    public void StartModeSelection() {
        CurrentPhase = GamePhase.MODE_SELECTION;
        GameInProgress = false;
        
        ConsoleLog.Game("Select game mode...");
        GameEvents.TriggerGamePhaseChanged("MODE_SELECTION");
    }

    /// <summary>
    /// Sets game mode with phase validation and automatic transition to character selection
    /// Enforces mode selection timing and provides logging for selected mode
    /// Automatically progresses game flow to next appropriate phase
    /// 
    /// Mode Setting Process:
    /// 1. Validate current phase allows mode selection
    /// 2. Set selected game mode
    /// 3. Log mode selection with human-readable name
    /// 4. Automatically transition to character selection phase
    /// 
    /// Game Mode Effects:
    /// - Character Battle: Pre-equipped characters for immediate combat
    /// - Flexible Mode: Strategic equipment during battle with action costs
    /// </summary>
    /// <param name="mode">Game mode to set for the current session</param>
    public void SetGameMode(GameMode mode) {
        if (CurrentPhase != GamePhase.MODE_SELECTION) {
            ConsoleLog.Warn("Cannot set game mode - not in mode selection phase");
            return;
        }

        CurrentMode = mode;
        string modeName = mode == GameMode.CHARACTER_BATTLE ? "Character Battle" : "Flexible Mode";
        ConsoleLog.Game($"Game mode set to: {modeName}");
        
        TransitionToCharacterSelection();
    }

    /// <summary>
    /// Transitions game to character selection phase with event notification
    /// Enables character selection UI and provides appropriate player guidance
    /// Sets up phase for character assignment and initial equipment
    /// </summary>
    public void TransitionToCharacterSelection() {
        CurrentPhase = GamePhase.CHARACTER_SELECTION;
        GameEvents.TriggerGamePhaseChanged("CHARACTER_SELECTION");
        ConsoleLog.Game("Waiting for players to select characters...");
    }

    /// <summary>
    /// Sets both players simultaneously with validation and automatic battle start
    /// Ensures both characters are provided before proceeding to battle phase
    /// Handles character assignment and triggers immediate battle initiation
    /// 
    /// Player Setup Process:
    /// 1. Validate phase allows player assignment
    /// 2. Ensure both characters are provided (prevent null assignments)
    /// 3. Set attacker and defender with FactorManager registration
    /// 4. Trigger player assignment events for UI updates
    /// 5. Log assignment with character names
    /// 6. Automatically start battle with validation
    /// </summary>
    /// <param name="attacker">Character for attacking position</param>
    /// <param name="defender">Character for defending position</param>
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

    /// <summary>
    /// Initiates battle phase with comprehensive validation and system initialization
    /// Validates game state readiness and initializes character passive abilities
    /// Provides mode-specific setup and guidance for players
    /// 
    /// Battle Initialization Process:
    /// 1. Validate phase transition is appropriate
    /// 2. Perform comprehensive game start validation
    /// 3. Set battle phase and game progress flags
    /// 4. Trigger phase change events for UI updates
    /// 5. Provide mode-specific guidance and logging
    /// 6. Initialize character passive abilities
    /// 7. Start first turn with proper event notification
    /// 
    /// Mode-Specific Behavior:
    /// - Character Battle: Characters start fully equipped, focus on combat
    /// - Flexible Mode: Strategic equipment timing with action economy
    /// 
    /// Character Passive Integration:
    /// - Initialize passive abilities for both characters
    /// - Enable turn-based passive effect processing
    /// - Provide character-specific enhancement tracking
    /// </summary>
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
        
        if (CurrentMode == GameMode.CHARACTER_BATTLE) {
            ConsoleLog.Game("CHARACTER BATTLE MODE: Characters start fully equipped!");
        } else {
            ConsoleLog.Game("FLEXIBLE MODE: Equip cards strategically during battle!");
        }
        
        // Initialize character passives
        CharacterPassives.InitializePassives(Attacker);
        CharacterPassives.InitializePassives(Defender);
        
        // Start first turn
        GameEvents.TriggerTurnStarted(GetCurrentPlayer());
    }

    /// <summary>
    /// Validates game state readiness for battle initiation
    /// Ensures both players are assigned and properly equipped based on game mode
    /// Provides detailed error logging for validation failures
    /// 
    /// Validation Rules:
    /// - Both attacker and defender must be assigned
    /// - Character Battle Mode: Character cards must be equipped
    /// - Flexible Mode: Only character cards should be equipped initially
    /// 
    /// Character Card Validation:
    /// - Ensures Character cards are equipped in equipped slots
    /// - Provides mode-specific equipment validation
    /// - Logs appropriate guidance for setup completion
    /// </summary>
    /// <returns>True if game is ready to start, false if validation fails</returns>
    private bool ValidateGameStart() {
        if (Attacker == null || Defender == null) {
            ConsoleLog.Error("Both players must be set before starting battle");
            return false;
        }

        // In Character Battle mode, validate character cards are equipped
        if (CurrentMode == GameMode.CHARACTER_BATTLE) {
            if (!Attacker.EquippedSlots.ContainsKey(Card.TYPE.C)) {
                ConsoleLog.Error($"{Attacker.CharName} must have a character card equipped");
                return false;
            }

            if (!Defender.EquippedSlots.ContainsKey(Card.TYPE.C)) {
                ConsoleLog.Error($"{Defender.CharName} must have a character card equipped");
                return false;
            }
        } else {
            // In Flexible Mode, only character cards should be equipped initially
            ConsoleLog.Game("Flexible Mode: Players start with only character cards equipped");
        }

        return true;
    }
    
    #endregion
    
    #region Turn Resolution and Factor Processing
    
    /// <summary>
    /// Comprehensive turn start resolution with factor processing and resource management
    /// Handles all turn-based effects in proper order with error handling
    /// Integrates character passives, factor resolution, and resource regeneration
    /// 
    /// Turn Start Sequence:
    /// 1. Log turn start with character identification
    /// 2. Execute character-specific turn start effects (passives)
    /// 3. Resolve factor effects in proper order:
    ///    - Healing: LP recovery and opponent LP loss
    ///    - Recharge: EP stealing from opponent
    ///    - Growth: MP stealing from opponent
    ///    - Burning: Percentage-based damage over time
    ///    - Storm: Defense-bypassing damage
    /// 4. Age all factors (duration decrements and expiration)
    /// 5. Regenerate resources according to game rules (5% EP, 2% MP)
    /// 6. Reset action economy for new turn
    /// 
    /// Error Handling:
    /// - Null player protection with warning logs
    /// - Exception catching with detailed error reporting
    /// - Graceful continuation despite individual effect failures
    /// - Comprehensive logging for debugging and analysis
    /// 
    /// Factor Resolution Order:
    /// - Beneficial effects first (Healing, Recharge, Growth)
    /// - Damage effects second (Burning, Storm)
    /// - Factor aging last to prevent premature expiration
    /// - Resource regeneration after all effects for clean state
    /// </summary>
    /// <param name="current">Character whose turn is starting</param>
    /// <param name="other">Opponent character for factor interactions</param>
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

    /// <summary>
    /// Applies automatic resource regeneration according to game design document
    /// Implements 5% EP and 2% MP recovery with maximum value capping
    /// Triggers appropriate events for UI updates and effect tracking
    /// 
    /// Regeneration Rules (Design Document):
    /// - EP Recovery: 5% of MaxEP per turn (rounded to nearest integer)
    /// - MP Recovery: 2% of MaxMP per turn (rounded to nearest integer)
    /// - Maximum Caps: Cannot exceed MaxEP or MaxMP through regeneration
    /// - Event Integration: Triggers resource regenerated events for UI sync
    /// 
    /// Event Notification:
    /// - Triggers regeneration events only for actual resource gains
    /// - Provides before/after values for accurate tracking
    /// - Integrates with GameEvents system for UI and logging
    /// - Supports detailed resource tracking and analysis
    /// </summary>
    /// <param name="character">Character receiving automatic resource regeneration</param>
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
    
    #endregion
    
    #region Combat and Damage Resolution
    
    /// <summary>
    /// Enhanced damage application with comprehensive validation and shield integration
    /// Handles both normal and absolute damage types with proper defense calculations
    /// Integrates shield resolution and provides detailed event tracking
    /// 
    /// Damage Resolution Process:
    /// 1. Validate character reference and damage value
    /// 2. Calculate final damage after defense (if not absolute)
    /// 3. Apply shield resolution through FactorLogic.ResolveToughness
    /// 4. Apply remaining damage to character LP
    /// 5. Trigger resource loss events for UI updates
    /// 6. Trigger damage dealt events for combat tracking
    /// 7. Check for player defeat and handle game end
    /// 
    /// Damage Types:
    /// - Normal Damage: Reduced by character DEF, then processed through shields
    /// - Absolute Damage: Bypasses DEF but still processed through shields
    /// - Shield Integration: All damage types processed through Toughness shields
    /// 
    /// Error Handling:
    /// - Null character protection with detailed error logging
    /// - Invalid damage value validation with warnings
    /// - Graceful handling of edge cases and boundary conditions
    /// 
    /// Event Integration:
    /// - Resource loss events for LP reduction tracking
    /// - Damage dealt events for combat logging and UI updates
    /// - Player defeat events for game conclusion handling
    /// </summary>
    /// <param name="character">Character receiving damage</param>
    /// <param name="damage">Amount of damage to apply</param>
    /// <param name="isAbsolute">Whether damage bypasses DEF calculation</param>
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

    /// <summary>
    /// Executes normal attack with weapon integration and freeze validation
    /// Validates action availability and weapon status before execution
    /// Triggers both base weapon and secondary weapon effects simultaneously
    /// 
    /// Normal Attack Process:
    /// 1. Validate action prerequisites (current player, target, action availability)
    /// 2. Retrieve equipped base weapon and secondary weapon
    /// 3. Check weapon freeze status to prevent frozen weapon usage
    /// 4. Execute action through StateManager for proper action economy
    /// 5. Trigger weapon effects for both base and secondary weapons
    /// 6. Log attack execution with participant details
    /// 
    /// Weapon Integration:
    /// - Base Weapon (BW): Primary weapon effect triggered during normal attack
    /// - Secondary Weapon (SW): Secondary weapon effect triggered simultaneously
    /// - Freeze Checking: Prevents usage of frozen weapons
    /// - Effect Execution: Both weapons execute effects on specified target
    /// 
    /// Action Economy:
    /// - Consumes 1 standard action through StateManager.TryAction
    /// - Validates action availability before execution
    /// - Handles action exhaustion and turn locking automatically
    /// </summary>
    /// <param name="target">Character to target with normal attack</param>
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

    /// <summary>
    /// Executes card usage from equipped slot with validation and action economy
    /// Delegates to CharacterLogic for card effect execution and freeze checking
    /// Integrates with StateManager for proper action counting and turn management
    /// 
    /// Card Usage Process:
    /// 1. Validate action prerequisites (current player, target, action availability)
    /// 2. Delegate to CharacterLogic.UseSlot for card-specific logic
    /// 3. Handle action economy through StateManager integration
    /// 4. Provide error handling and validation feedback
    /// 
    /// Integration Points:
    /// - CharacterLogic: Handles card effect execution and freeze validation
    /// - StateManager: Manages action economy and turn progression
    /// - GameEvents: Provides card usage tracking and UI updates
    /// </summary>
    /// <param name="slotType">Equipment slot containing card to use</param>
    /// <param name="target">Character to target with card effect</param>
    public void UseCard(Card.TYPE slotType, Character target) {
        var current = GetCurrentPlayer();
        if (!ValidateAction(current, target, "use card")) return;

        CharacterLogic.UseSlot(current, slotType, target);
    }

    /// <summary>
    /// Validates action prerequisites for combat operations
    /// Provides comprehensive validation with detailed error feedback
    /// Used by combat methods to ensure proper game state before execution
    /// 
    /// Validation Criteria:
    /// - Current player must be available (not null)
    /// - Target must be specified (not null)
    /// - Action availability through StateManager.CanAct()
    /// - Detailed error logging for each validation failure
    /// 
    /// Error Feedback:
    /// - Specific error messages for each validation failure
    /// - Consistent warning log format for debugging
    /// - Boolean return for simple validation checking
    /// </summary>
    /// <param name="current">Current player attempting action</param>
    /// <param name="target">Target character for action</param>
    /// <param name="actionName">Name of action for error reporting</param>
    /// <returns>True if action can proceed, false if validation fails</returns>
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
    
    #endregion
    
    #region Equipment Management
    
    /// <summary>
    /// Manages card equipment with phase validation and game mode integration
    /// Enforces character-specific card restrictions and action economy costs
    /// Provides different behavior based on game mode and current phase
    /// 
    /// Equipment Rules:
    /// - Phase Validation: Equipment only allowed during appropriate phases
    /// - Character Restrictions: Q and U cards restricted to original character
    /// - Action Economy: Flexible Mode equipment consumes actions during battle
    /// - Free Equipment: Character Battle mode and setup phases allow free equipment
    /// 
    /// Game Mode Integration:
    /// - Character Battle: Free equipment during setup, focus on combat tactics
    /// - Flexible Mode: Strategic equipment timing with action costs during battle
    /// - Phase Awareness: Different rules during CHARACTER_SELECTION vs BATTLE
    /// 
    /// Character Card Restrictions:
    /// - Q Skill Cards: Can only be equipped by original character
    /// - Ultimate Cards: Can only be equipped by original character
    /// - Other Cards: Can be equipped by any character
    /// - Validation through CanCharacterEquipCard method
    /// 
    /// Action Economy Integration:
    /// - Flexible Mode + Battle Phase: Equipment consumes 1 action
    /// - Other Modes/Phases: Free equipment for setup and strategy
    /// - StateManager integration for proper action tracking
    /// </summary>
    /// <param name="character">Character equipping the card</param>
    /// <param name="card">Card to equip to character</param>
    public void EquipCard(Character character, Card card) {
        if (character == null || card == null) {
            ConsoleLog.Warn("Cannot equip card - character or card is null");
            return;
        }

        if (!CanEquipCards()) {
            ConsoleLog.Warn("Cannot equip cards outside of active gameplay");
            return;
        }

        // Check card ownership restrictions for Q and U cards
        if (!CanCharacterEquipCard(character, card)) {
            ConsoleLog.Warn($"{character.CharName} cannot equip {card.Name} - character restriction");
            return;
        }

        // In Flexible Mode, equipping a card consumes an action
        if (CurrentMode == GameMode.FLEXIBLE_MODE && CurrentPhase == GamePhase.BATTLE) {
            if (!StateManager.CanAct()) {
                ConsoleLog.Warn("Cannot equip card - no actions remaining");
                return;
            }

            StateManager.TryAction(() => {
                CharacterLogic.EquipCardToSlot(character, card);
                ConsoleLog.Action($"{character.CharName} spent an action to equip {card.Name}");
            });
        } else {
            // Character Battle mode or setup phase - free equipment
            CharacterLogic.EquipCardToSlot(character, card);
        }
    }

    /// <summary>
    /// Validates character-specific card ownership restrictions
    /// Enforces game rules for Q Skill and Ultimate card equipment limitations
    /// Provides consistent validation for equipment operations
    /// 
    /// Character Restriction Rules:
    /// - Q Skill Cards: Can only be equipped by their original character
    /// - Ultimate Cards: Can only be equipped by their original character
    /// - All Other Cards: Can be equipped by any character
    /// - Validation through character card set checking
    /// 
    /// Implementation Details:
    /// - Uses AllCards.GetCharacterCardSet for character card identification
    /// - Compares card ID for exact matching
    /// - Returns true for unrestricted card types
    /// - Enables flexible equipment while maintaining character identity
    /// </summary>
    /// <param name="character">Character attempting to equip card</param>
    /// <param name="card">Card being equipped</param>
    /// <returns>True if character can equip card, false if restricted</returns>
    public bool CanCharacterEquipCard(Character character, Card card) {
        // Q and U cards can only be equipped by their original character
        if (card.Type == Card.TYPE.Q || card.Type == Card.TYPE.U) {
            return IsCardFromCharacter(card, character.CharName);
        }
        
        // All other cards can be equipped by anyone
        return true;
    }

    /// <summary>
    /// Checks if a card belongs to a specific character's card set
    /// Used for enforcing character-specific equipment restrictions
    /// Provides reliable card ownership validation for game rules
    /// 
    /// Ownership Validation:
    /// - Retrieves character's complete card set from AllCards
    /// - Compares card ID for exact matching
    /// - Handles character name case sensitivity
    /// - Returns false for characters with no card sets
    /// </summary>
    /// <param name="card">Card to check ownership for</param>
    /// <param name="characterName">Character name to validate against</param>
    /// <returns>True if card belongs to character, false otherwise</returns>
    private bool IsCardFromCharacter(Card card, string characterName) {
        var characterCards = AllCards.GetCharacterCardSet(characterName);
        return characterCards.Any(c => c.Id == card.Id);
    }

    /// <summary>
    /// Manages charm equipment with phase validation and action economy integration
    /// Enforces equipment timing rules based on game mode and current phase
    /// Provides mode-specific behavior for charm equipment strategies
    /// 
    /// Charm Equipment Rules:
    /// - Phase Validation: Equipment only allowed during appropriate phases
    /// - Action Economy: Flexible Mode equipment consumes actions during battle
    /// - Free Equipment: Character Battle mode and setup phases allow free equipment
    /// - Set Bonus Integration: Automatic set bonus calculation through CharmLogic
    /// 
    /// Game Mode Integration:
    /// - Character Battle: Free charm equipment during setup
    /// - Flexible Mode: Strategic charm timing with action costs during battle
    /// - Phase Awareness: Different rules during CHARACTER_SELECTION vs BATTLE
    /// 
    /// CharmLogic Integration:
    /// - Delegates to CharmLogic.EquipCharm for charm-specific logic
    /// - Handles set bonus calculations and equipment validation
    /// - Provides consistent charm management across game modes
    /// </summary>
    /// <param name="character">Character equipping the charm</param>
    /// <param name="charm">Charm to equip to character</param>
    public void EquipCharm(Character character, Charm charm) {
        if (character == null || charm == null) {
            ConsoleLog.Warn("Cannot equip charm - character or charm is null");
            return;
        }

        if (!CanEquipCards()) {
            ConsoleLog.Warn("Cannot equip charms outside of active gameplay");
            return;
        }

        // In Flexible Mode, equipping a charm consumes an action
        if (CurrentMode == GameMode.FLEXIBLE_MODE && CurrentPhase == GamePhase.BATTLE) {
            if (!StateManager.CanAct()) {
                ConsoleLog.Warn("Cannot equip charm - no actions remaining");
                return;
            }

            StateManager.TryAction(() => {
                CharmLogic.EquipCharm(character, charm);
                ConsoleLog.Action($"{character.CharName} spent an action to equip charm {charm.Name}");
            });
        } else {
            // Character Battle mode or setup phase - free equipment
            CharmLogic.EquipCharm(character, charm);
        }
    }
    
    #endregion
    
    #region Game State Queries and Turn Management
    
    /// <summary>
    /// Determines if card and charm equipment is currently allowed
    /// Validates current phase against equipment-enabled phases
    /// Used for UI state management and action validation
    /// </summary>
    /// <returns>True if equipment is allowed in current phase</returns>
    public bool CanEquipCards() => CurrentPhase == GamePhase.CHARACTER_SELECTION || CurrentPhase == GamePhase.BATTLE;
    
    /// <summary>
    /// Determines if combat actions can be performed in current game state
    /// Validates both phase and game progress for action availability
    /// Used for UI state management and action validation
    /// </summary>
    /// <returns>True if combat actions are allowed</returns>
    public bool CanPerformActions() => CurrentPhase == GamePhase.BATTLE && GameInProgress;
    
    /// <summary>
    /// Checks if game is currently in Flexible Mode
    /// Provides mode-specific logic branching for equipment and strategy
    /// </summary>
    /// <returns>True if current mode is Flexible Mode</returns>
    public bool IsFlexibleMode() => CurrentMode == GameMode.FLEXIBLE_MODE;
    
    /// <summary>
    /// Checks if game is currently in Character Battle Mode
    /// Provides mode-specific logic branching for pre-equipped combat
    /// </summary>
    /// <returns>True if current mode is Character Battle Mode</returns>
    public bool IsCharacterBattleMode() => CurrentMode == GameMode.CHARACTER_BATTLE;

    /// <summary>
    /// Manually ends the current turn with validation and state management
    /// Provides player control for early turn termination when desired
    /// Triggers standard turn transition through StateManager
    /// 
    /// Turn End Validation:
    /// - Game must be in progress (active battle)
    /// - Must be in BATTLE phase
    /// - Logs turn end with current player identification
    /// - Triggers appropriate events for UI and system updates
    /// 
    /// Integration Points:
    /// - GameEvents: Triggers turn ended event for UI updates
    /// - StateManager: Handles turn transition and resource regeneration
    /// - Logging: Provides detailed turn end tracking
    /// </summary>
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
    
    #endregion
    
    #region Game End Handling and Restart
    
    /// <summary>
    /// Handles player defeat with comprehensive game end processing
    /// Determines winner, updates game state, and triggers victory events
    /// Provides complete game conclusion handling with proper event sequencing
    /// 
    /// Defeat Processing:
    /// 1. Determine opponent as winner
    /// 2. Set game phase to GAME_OVER
    /// 3. Clear game progress flag
    /// 4. Log defeat and victory with character details
    /// 5. Trigger sequential events for proper UI updates:
    ///    - Player defeated event
    ///    - Player victory event for winner
    ///    - Game ended event for conclusion
    ///    - Phase changed event for UI transitions
    /// 
    /// Event Sequencing:
    /// - Player defeat event provides immediate defeat feedback
    /// - Player victory event celebrates winner
    /// - Game ended event triggers conclusion logic
    /// - Phase change event updates UI for game over state
    /// 
    /// State Management:
    /// - Sets GAME_OVER phase for UI state management
    /// - Clears GameInProgress for action validation
    /// - Maintains character references for victory display
    /// </summary>
    /// <param name="defeated">Character who was defeated</param>
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

    /// <summary>
    /// Restarts the game by returning to mode selection phase
    /// Provides simple restart functionality for post-game options
    /// Maintains character and factor cleanup through StartModeSelection
    /// 
    /// Restart Process:
    /// - Logs restart initiation for debugging
    /// - Delegates to StartModeSelection for proper state reset
    /// - Enables fresh game start with clean state
    /// </summary>
    public void RestartGame() {
        ConsoleLog.Game("Restarting game...");
        StartModeSelection();
    }
    
    #endregion
    
    #region Input Handling and Cleanup
    
    /// <summary>
    /// Handles global input events for console window toggling
    /// Provides F1 key functionality for console window visibility
    /// Ensures input is properly handled and marked as consumed
    /// 
    /// Input Handling:
    /// - Listens for F1 key press events
    /// - Toggles console window visibility if available
    /// - Marks input as handled to prevent further processing
    /// - Provides essential debugging access during gameplay
    /// </summary>
    /// <param name="event">Input event to process</param>
    public override void _UnhandledKeyInput(InputEvent @event) {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.F1) {
                consoleWindow?.ToggleVisibility();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Comprehensive cleanup when GameManager is removed from scene tree
    /// Ensures proper resource cleanup and prevents memory leaks
    /// Provides graceful shutdown for all managed systems
    /// 
    /// Cleanup Process:
    /// - Reset game state and clear character references
    /// - Free console window resources
    /// - Clear singleton instance reference
    /// - Ensure all managed resources are properly disposed
    /// </summary>
    public override void _ExitTree() {
        Reset();
        consoleWindow?.QueueFree();
        Instance = null;
    }
    
    #endregion
}