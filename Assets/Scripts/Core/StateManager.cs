using System;
using Godot;

// This file handles the game state
namespace meph {
    public enum TURN {
        ATTACKER = 0,
        DEFENDER = 1
    }

    public class StateManager {
        public TURN CurrentTurn { get; private set; } = TURN.ATTACKER;
        public bool ActionsLocked { get; set; } = false;  // Changed to public set
        public int ActionsRemaining { get; set; } = 1;    // Changed to public set

        // Enhanced events with character context
        public event Action<TURN, Character> OnTurnStarted;
        public event Action<TURN, Character> OnTurnEnded;
        public event Action OnActionLock;
        public event Action<int> OnActionsChanged;

        // Reference to get current players
        public Func<TURN, Character> GetPlayer { get; set; }

        public void NextTurn ( ) {
            var currentPlayer = GetPlayer?.Invoke ( CurrentTurn );
            OnTurnEnded?.Invoke ( CurrentTurn, currentPlayer );

            ActionsLocked = false;
            ActionsRemaining = 1; // Reset actions for new turn
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;

            var newPlayer = GetPlayer?.Invoke ( CurrentTurn );

            // Apply turn-based regeneration according to design document
            if ( newPlayer != null ) {
                RegenerateResources ( newPlayer );
            }

            OnTurnStarted?.Invoke ( CurrentTurn, newPlayer );
            OnActionsChanged?.Invoke ( ActionsRemaining );
            GameEvents.TriggerTurnChanged ( CurrentTurn, newPlayer );

            ConsoleLog.Game ( $"{newPlayer?.CharName ?? CurrentTurn.ToString ( )}'s turn started." );
        }

        private void RegenerateResources ( Character character ) {
            // According to design document:
            // EP: 5% of Max EP per turn
            // MP: 2% of Max MP per turn
            int epRegen = (int)( character.MaxEP * 0.05f );
            int mpRegen = (int)( character.MaxMP * 0.02f );

            int oldEP = character.EP;
            int oldMP = character.MP;

            character.EP = Math.Min ( character.EP + epRegen, character.MaxEP );
            character.MP = Math.Min ( character.MP + mpRegen, character.MaxMP );

            // Trigger individual resource gained events
            if ( character.EP > oldEP ) {
                GameEvents.TriggerResourceGained ( character, character.EP - oldEP, "EP" );
            }
            if ( character.MP > oldMP ) {
                GameEvents.TriggerResourceGained ( character, character.MP - oldMP, "MP" );
            }

            // Also trigger the combined regeneration event
            GameEvents.TriggerResourceRegenerated ( character, character.EP - oldEP, character.MP - oldMP );

            ConsoleLog.Resource ( $"{character.CharName} regenerated {character.EP - oldEP} EP and {character.MP - oldMP} MP" );
        }

        public void TryAction ( Action action, bool isSwift = false ) {
            if ( !CanAct ( ) && !isSwift ) {
                ConsoleLog.Warn ( "Cannot act, no actions remaining." );
                return;
            }

            action ( );

            if ( !isSwift ) {
                ActionsRemaining--;
                OnActionsChanged?.Invoke ( ActionsRemaining );

                if ( ActionsRemaining <= 0 ) {
                    LockAction ( );
                }
            }
        }

        public bool CanAct ( ) => !ActionsLocked && ActionsRemaining > 0;

        public void LockAction ( ) {
            ActionsLocked = true;
            OnActionLock?.Invoke ( );
        }

        public void EndTurn ( ) {
            // Manual turn end - useful for when player wants to end early
            NextTurn ( );
        }
    }
}