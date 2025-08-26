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
        public bool ActionsLocked { get; private set; } = false;

        // We invoke an event whenever a side gets the turn
        public void NextTurn ( ) {
            ActionsLocked = false; // Clear lock at the start of each turn
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;
            if ( CurrentTurn == TURN.ATTACKER ) {
                EventBus.RaiseAttackerTurn ( );
                GD.Print ( "Attacker's turn." );
            } else {
                EventBus.RaiseDefenderTurn ( );
                GD.Print ( "Defender's turn." );
            }
        }

        // Helper method to invoke the event from outside this class
        public void LockAction ( ) {
            ActionsLocked = true;
            EventBus.RaiseActionLock ( );
        }

        public bool CanAct ( ) => !ActionsLocked; // For convenience

        // "Action" keyword is a built-in delegate type, which basically lets us to not 
        // worry about defining a custom delegate. Delegates take functions as parameters
        // Here we encapsulate the game's action logic in this method and ensure actions are only performed when allowed.
        public void TryAction ( Action action ) {
            if ( CanAct ( ) ) {
                action ( );  // TODO: Check for failing actions that continue
                LockAction ( );
            } else {
                GD.Print ( "Cannot act, actions are locked." );
            }
        }
    }
}