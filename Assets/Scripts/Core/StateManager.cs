using System;
using Godot;

namespace meph {
    
    /// <summary>
    /// Enumeration defining the two primary turn phases in combat
    /// Used for turn tracking, player identification, and game state management
    /// Provides clear distinction between offensive and defensive positions
    /// </summary>
    public enum TURN {
        /// <summary>The player who initiated combat or has the current offensive turn</summary>
        ATTACKER = 0,
        /// <summary>The player responding to attacks or in defensive position</summary>
        DEFENDER = 1
    }

    /// <summary>
    /// Core game state management system controlling turn flow, action economy, and resource regeneration
    /// Implements turn-based combat mechanics with action limitations and automatic resource recovery
    /// Provides event-driven architecture for UI synchronization and game system coordination
    /// 
    /// Core Responsibilities:
    /// - Turn Management: Controls alternating turns between ATTACKER and DEFENDER
    /// - Action Economy: Tracks and limits actions per turn (1 action default, unlimited Swift Actions)
    /// - Resource Regeneration: Applies automatic EP/MP recovery according to game rules (5% EP, 2% MP)
    /// - State Validation: Prevents invalid actions when turns are locked or actions exhausted
    /// - Event Coordination: Triggers appropriate events for UI updates and system synchronization
    /// 
    /// Game Rule Integration:
    /// - EP Regeneration: 5% of MaxEP per turn start (design document specification)
    /// - MP Regeneration: 2% of MaxMP per turn start (design document specification)
    /// - Action Limitation: 1 standard action per turn, unlimited Swift Actions
    /// - Turn Structure: Automatic resource recovery → turn events → action processing → turn end
    /// 
    /// Event-Driven Architecture:
    /// - Turn lifecycle events with character context for UI and system updates
    /// - Action tracking events for UI feedback and state validation
    /// - Resource regeneration events for combat log and effect tracking
    /// - Integration with GameEvents system for broader game state communication
    /// </summary>
    public class StateManager {
        
        #region Core State Properties
        
        /// <summary>
        /// Current active turn indicating which player can perform actions
        /// Alternates between ATTACKER and DEFENDER with each turn transition
        /// Used for player identification and action validation
        /// </summary>
        public TURN CurrentTurn { get; private set; } = TURN.ATTACKER;
        
        /// <summary>
        /// Indicates whether actions are currently prohibited for the active player
        /// Set to true when action limit is reached or turn is manually ended
        /// Prevents action execution until next turn begins
        /// </summary>
        public bool ActionsLocked { get; set; } = false;
        
        /// <summary>
        /// Number of standard actions remaining for the current turn
        /// Decremented with each non-Swift action, resets to 1 on turn start
        /// Swift Actions do not consume from this counter
        /// </summary>
        public int ActionsRemaining { get; set; } = 1;
        
        #endregion
        
        #region Event System
        
        /// <summary>
        /// Triggered when a new turn begins after resource regeneration and state reset
        /// Provides turn type and active character for UI updates and system initialization
        /// Called after resource regeneration but before any actions can be taken
        /// </summary>
        public event Action<TURN, Character> OnTurnStarted;
        
        /// <summary>
        /// Triggered when a turn ends, either through action exhaustion or manual termination
        /// Provides ending turn type and character for cleanup and transition logic
        /// Called before turn alternation and resource regeneration
        /// </summary>
        public event Action<TURN, Character> OnTurnEnded;
        
        /// <summary>
        /// Triggered when actions become locked due to exhaustion or manual turn end
        /// Signals UI systems to disable action buttons and show turn end options
        /// Indicates player must manually end turn or turn will auto-advance
        /// </summary>
        public event Action OnActionLock;
        
        /// <summary>
        /// Triggered whenever the remaining action count changes during a turn
        /// Provides updated action count for UI displays and action validation
        /// Called after action consumption and on turn start with reset count
        /// </summary>
        public event Action<int> OnActionsChanged;
        
        #endregion
        
        #region External Dependencies
        
        /// <summary>
        /// Function delegate for retrieving the Character instance for a given turn
        /// Injected by GameManager to provide access to current players
        /// Enables StateManager to operate without direct GameManager coupling
        /// Used for resource regeneration and event context
        /// </summary>
        public Func<TURN, Character> GetPlayer { get; set; }
        
        #endregion
        
        #region Turn Management System
        
        /// <summary>
        /// Advances the game to the next turn with complete state transition and resource regeneration
        /// Implements full turn lifecycle including cleanup, alternation, regeneration, and initialization
        /// Triggers all appropriate events for UI synchronization and system coordination
        /// 
        /// Turn Transition Process:
        /// 1. End current turn: Trigger OnTurnEnded with current player context
        /// 2. Reset turn state: Unlock actions and restore action count to 1
        /// 3. Alternate turn: Switch between ATTACKER and DEFENDER
        /// 4. Resource regeneration: Apply automatic EP/MP recovery for new active player
        /// 5. Begin new turn: Trigger OnTurnStarted and action count events
        /// 6. Game event integration: Notify broader game systems of turn change
        /// 
        /// Automatic Resource Recovery:
        /// - EP Regeneration: 5% of MaxEP (design document specification)
        /// - MP Regeneration: 2% of MaxMP (design document specification)
        /// - Capped Recovery: Cannot exceed maximum resource values
        /// - Event Integration: Triggers resource gained events for tracking and UI
        /// 
        /// Event Coordination:
        /// - Turn lifecycle events provide character context for UI updates
        /// - Action change events update UI button states and counters
        /// - GameEvents integration ensures broader system synchronization
        /// - Console logging provides combat feedback and debugging information
        /// </summary>
        public void NextTurn() {
            // End current turn with character context
            var currentPlayer = GetPlayer?.Invoke(CurrentTurn);
            OnTurnEnded?.Invoke(CurrentTurn, currentPlayer);

            // Reset turn state for new player
            ActionsLocked = false;
            ActionsRemaining = 1; // Reset actions for new turn
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;

            // Get new active player and apply resource regeneration
            var newPlayer = GetPlayer?.Invoke(CurrentTurn);
            if (newPlayer != null) {
                RegenerateResources(newPlayer);
            }

            // Initialize new turn with events and logging
            OnTurnStarted?.Invoke(CurrentTurn, newPlayer);
            OnActionsChanged?.Invoke(ActionsRemaining);
            GameEvents.TriggerTurnChanged(CurrentTurn, newPlayer);

            ConsoleLog.Game($"{newPlayer?.CharName ?? CurrentTurn.ToString()}'s turn started.");
        }

        /// <summary>
        /// Manually ends the current turn, bypassing any remaining actions
        /// Provides player control for early turn termination when no more actions desired
        /// Triggers standard turn transition with resource regeneration and event handling
        /// 
        /// Use Cases:
        /// - Player chooses to end turn early despite having actions remaining
        /// - Strategic turn ending to avoid triggering opponent effects
        /// - UI "End Turn" button functionality
        /// - Forced turn end from special card effects or game conditions
        /// </summary>
        public void EndTurn() {
            NextTurn();
        }
        
        #endregion
        
        #region Action Economy System
        
        /// <summary>
        /// Attempts to execute an action within the current turn's action economy constraints
        /// FIXED: Now properly handles Swift Actions from Potion cards
        /// Validates action availability and applies appropriate action cost based on Swift Action status
        /// 
        /// Action Economy Rules:
        /// - Standard Actions: Cost 1 action, limited to ActionsRemaining count
        /// - Swift Actions: No action cost, unlimited usage per turn
        /// - Action Exhaustion: Turn becomes locked when ActionsRemaining reaches 0
        /// - Validation: Actions blocked when turn is locked or no actions remain
        /// 
        /// FIXED: Swift cards (Potions) can be used multiple times without action cost
        /// </summary>
        /// <param name="action">Action delegate to execute if action economy permits</param>
        /// <param name="isSwift">Whether this is a Swift Action that bypasses action costs</param>
        public void TryAction(Action action, bool isSwift = false) {
            // FIXED: Validate action availability for standard actions only
            if (!CanAct() && !isSwift) {
                ConsoleLog.Warn("Cannot act, no actions remaining.");
                return;
            }

            // Execute the requested action
            action();

            // Apply action cost and state changes for standard actions only
            if (!isSwift) {
                ActionsRemaining--;
                OnActionsChanged?.Invoke(ActionsRemaining);

                // Lock turn if no actions remain
                if (ActionsRemaining <= 0) {
                    LockAction();
                }
            } else {
                // FIXED: Log Swift action usage without consuming actions
                ConsoleLog.Action("Swift action executed - no action cost");
            }
        }

        /// <summary>
        /// Checks whether the current player can perform standard actions
        /// Validates both action lock status and remaining action count
        /// Used for UI state management and action validation
        /// 
        /// Action Availability Rules:
        /// - Must not be action locked (turn not manually ended)
        /// - Must have actions remaining (ActionsRemaining > 0)
        /// - Swift Actions bypass this check completely
        /// - Returns false when turn transition is pending
        /// </summary>
        /// <returns>True if standard actions can be performed, false otherwise</returns>
        public bool CanAct() => !ActionsLocked && ActionsRemaining > 0;

        /// <summary>
        /// Manually locks the current turn to prevent further standard actions
        /// Used when action limit is reached or player chooses to end turn early
        /// Triggers OnActionLock event for UI updates and turn end button display
        /// 
        /// Lock Triggers:
        /// - Automatic: When ActionsRemaining reaches 0 after action execution
        /// - Manual: When player chooses to end turn early
        /// - Forced: From special card effects or game conditions
        /// - Turn End: Always locked during turn transition processing
        /// 
        /// UI Integration:
        /// - OnActionLock event signals UI to show "End Turn" button
        /// - Disables action buttons and card interaction
        /// - Provides visual feedback that turn is complete
        /// </summary>
        public void LockAction() {
            ActionsLocked = true;
            OnActionLock?.Invoke();
        }
        
        #endregion
        
        #region Resource Regeneration System
        
        /// <summary>
        /// Applies automatic resource regeneration for a character at turn start
        /// Implements design document specifications for EP and MP recovery rates
        /// Triggers appropriate resource events for UI updates and effect tracking
        /// 
        /// Regeneration Mechanics (Design Document):
        /// - EP Recovery: 5% of MaxEP per turn (rounded down)
        /// - MP Recovery: 2% of MaxMP per turn (rounded down)
        /// - Maximum Caps: Cannot exceed MaxEP or MaxMP through regeneration
        /// - Turn Timing: Applied immediately after turn alternation, before actions
        /// 
        /// Event Integration:
        /// - Individual resource gained events for each resource type
        /// - Combined regeneration event for specialized tracking
        /// - Console logging for combat feedback and debugging
        /// - UI synchronization through GameEvents system
        /// 
        /// Calculation Process:
        /// 1. Calculate regeneration amounts based on maximum values and percentages
        /// 2. Apply regeneration with maximum value capping
        /// 3. Track actual gained amounts (may be less due to caps)
        /// 4. Trigger resource gained events for each resource type
        /// 5. Trigger combined regeneration event with both amounts
        /// 6. Log regeneration for combat feedback
        /// </summary>
        /// <param name="character">Character receiving automatic resource regeneration</param>
        private void RegenerateResources(Character character) {
            // Calculate regeneration amounts according to design document
            int epRegen = (int)(character.MaxEP * 0.05f); // 5% of Max EP per turn
            int mpRegen = (int)(character.MaxMP * 0.02f); // 2% of Max MP per turn

            // Store old values for event calculation
            int oldEP = character.EP;
            int oldMP = character.MP;

            // Apply regeneration with maximum value caps
            character.EP = Math.Min(character.EP + epRegen, character.MaxEP);
            character.MP = Math.Min(character.MP + mpRegen, character.MaxMP);

            // Trigger individual resource gained events for actual gained amounts
            if (character.EP > oldEP) {
                GameEvents.TriggerResourceGained(character, character.EP - oldEP, "EP");
            }
            if (character.MP > oldMP) {
                GameEvents.TriggerResourceGained(character, character.MP - oldMP, "MP");
            }

            // Trigger combined regeneration event for specialized tracking
            GameEvents.TriggerResourceRegenerated(character, character.EP - oldEP, character.MP - oldMP);

            // Log regeneration for combat feedback
            ConsoleLog.Resource($"{character.CharName} regenerated {character.EP - oldEP} EP and {character.MP - oldMP} MP");
        }
        
        #endregion
    }
}