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

        // We use events so anything we add in the future will be easier
        // These events are handled in GameManager.cs
        public event Action OnAttackerTurn;
        public event Action OnDefenderTurn;
        public event Action OnActionLock;

        public bool ActionsLocked { get; private set; } = false;



        // We invoke events whenever a side gets the turn
        public void NextTurn ( ) {
            ActionsLocked = false; // Clear lock at the start of each turn
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;
            if ( CurrentTurn == TURN.ATTACKER ) {
                OnAttackerTurn?.Invoke ( );
                GD.Print ( "Attacker's turn." );
            } else {
                OnDefenderTurn?.Invoke ( );
                GD.Print ( "Defender's turn." );
            }
        }
        // Helper method to invoke the event from outside this class
        public void LockAction ( ) {
            ActionsLocked = true; // Lock actions for the rest of the turn
            OnActionLock?.Invoke ( );
        }

        public bool CanAct ( ) => !ActionsLocked;

        public void TryAction ( Action action ) {
            if ( CanAct ( ) ) {
                action ( );
                LockAction ( );
            } else {
                GD.Print ( "Cannot act, actions are locked." );
            }
        }
    }
}