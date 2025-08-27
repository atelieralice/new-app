using System;
using static meph.Character;

namespace meph {
    public static class EventBus {
        // Turn events
        public static event Action OnAttackerTurn;
        public static event Action OnDefenderTurn;
        public static void RaiseAttackerTurn ( ) => OnAttackerTurn?.Invoke ( );
        public static void RaiseDefenderTurn ( ) => OnDefenderTurn?.Invoke ( );

        // Action lock/unlock events
        public static event Action OnActionLock;
        public static event Action OnActionUnlock;
        public static void RaiseActionLock ( ) => OnActionLock?.Invoke ( );
        public static void RaiseActionUnlock ( ) => OnActionUnlock?.Invoke ( );

        // Turn start/end events
        public static event Action OnTurnStart;
        public static event Action OnTurnEnd;
        public static void RaiseTurnStart ( ) => OnTurnStart?.Invoke ( );
        public static void RaiseTurnEnd ( ) => OnTurnEnd?.Invoke ( );

        // Game start/end events
        public static event Action OnGameStart;
        public static event Action OnGameEnd;
        public static void RaiseGameStart ( ) => OnGameStart?.Invoke ( );
        public static void RaiseGameEnd ( ) => OnGameEnd?.Invoke ( );

        // Card events
        public static event Action<Character, Card> OnCardEquipped;
        public static event Action<Character, Card> OnCardPlayed;
        public static void RaiseCardEquipped ( Character character, Card card ) => OnCardEquipped?.Invoke ( character, card );
        public static void RaiseCardPlayed ( Character character, Card card ) => OnCardPlayed?.Invoke ( character, card );

        // Damage event
        public static event Action<Character, int, DAMAGE_TYPE> OnDamageApplied;
        public static void RaiseDamageApplied ( Character target, int amount, DAMAGE_TYPE type ) => OnDamageApplied?.Invoke ( target, amount, type );

        // Status effect event
        public static event Action<Character, STATUS_EFFECT> OnStatusEffectApplied;
        public static void RaiseStatusEffectApplied ( Character target, STATUS_EFFECT effect ) => OnStatusEffectApplied?.Invoke ( target, effect );
    }
}