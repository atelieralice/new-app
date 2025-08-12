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
        public int ActionsRemaining { get; private set; } = 1;

        // Enhanced events with character context
        public event Action<TURN, Character> OnTurnStarted;
        public event Action<TURN, Character> OnTurnEnded;
        public event Action OnActionLock;
        public event Action<int> OnActionsChanged;

        // Reference to get current players
        public Func<TURN, Character> GetPlayer { get; set; }

        public void NextTurn() {
            var currentPlayer = GetPlayer?.Invoke(CurrentTurn);
            OnTurnEnded?.Invoke(CurrentTurn, currentPlayer);
            
            ActionsLocked = false;
            ActionsRemaining = 1; // Reset actions for new turn
            CurrentTurn = CurrentTurn == TURN.ATTACKER ? TURN.DEFENDER : TURN.ATTACKER;
            
            var newPlayer = GetPlayer?.Invoke(CurrentTurn);
            
            // Apply turn-based regeneration
            if (newPlayer != null) {
                RegenerateResources(newPlayer);
            }
            
            OnTurnStarted?.Invoke(CurrentTurn, newPlayer);
            OnActionsChanged?.Invoke(ActionsRemaining);
            GameEvents.TriggerTurnChanged(CurrentTurn, newPlayer);
            
            ConsoleLog.Game($"{newPlayer?.CharName ?? CurrentTurn.ToString()}'s turn started.");
        }

        private void RegenerateResources(Character character) {
            int epRegen = (int)(character.MaxEP * 0.05f);
            int mpRegen = (int)(character.MaxMP * 0.02f);
            
            int oldEP = character.EP;
            int oldMP = character.MP;
            
            character.EP = Math.Min(character.EP + epRegen, character.MaxEP);
            character.MP = Math.Min(character.MP + mpRegen, character.MaxMP);
            
            // Trigger individual resource gained events
            if (character.EP > oldEP) {
                GameEvents.TriggerResourceGained(character, character.EP - oldEP, "EP");
            }
            if (character.MP > oldMP) {
                GameEvents.TriggerResourceGained(character, character.MP - oldMP, "MP");
            }
            
            // Also trigger the combined regeneration event
            GameEvents.TriggerResourceRegenerated(character, character.EP - oldEP, character.MP - oldMP);
        }

        public void TryAction(Action action, bool isSwift = false) {
            if (!CanAct() && !isSwift) {
                ConsoleLog.Warn("Cannot act, no actions remaining.");
                return;
            }

            action();
            
            if (!isSwift) {
                ActionsRemaining--;
                OnActionsChanged?.Invoke(ActionsRemaining);
                
                if (ActionsRemaining <= 0) {
                    LockAction();
                }
            }
        }

        public bool CanAct() => !ActionsLocked && ActionsRemaining > 0;
        
        public void LockAction() {
            ActionsLocked = true;
            OnActionLock?.Invoke();
        }

        public void EndTurn() {
            // Manual turn end - useful for when player wants to end early
            NextTurn();
        }
    }
}