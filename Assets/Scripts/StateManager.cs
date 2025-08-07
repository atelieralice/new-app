using System;

// This file handles the game state
namespace meph {

    public enum TURN {
        ATTACKER = 0,
        DEFENDER = 1
    }

    public class StateManager {
        public Character Attacker { get; private set; }
        public Character Defender { get; private set; }
        public TURN CurrentTurn { get; private set; } = TURN.ATTACKER;

        // We use events so anything we add in the future will be easier
        // These events are handled in GameManager.cs
        public event Action OnAttackerTurn;
        public event Action OnDefenderTurn;
        public event Action OnActionLock;

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

        // We invoke events whenever a side gets the turn
        public void NextTurn ( ) {
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;
            if ( CurrentTurn == TURN.ATTACKER ) {
                OnAttackerTurn?.Invoke ( );
            } else {
                OnDefenderTurn?.Invoke ( );
            }
        }
        // Helper method to invoke the event from outside this class
        public void LockAction ( ) {
            OnActionLock?.Invoke ( );
        }
    }
}