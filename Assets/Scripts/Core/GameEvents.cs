using System;

namespace meph {
    public static class GameEvents {
        // Combat Events (for VFX/UI/SFX)
        public static event Action<Character, int, int> OnDamageDealt; // (target, damage, remainingLP)
        public static event Action<Character, int> OnHealingReceived; // (character, amount)
        public static event Action<Character, Character, int, string> OnResourceStolen; // (from, to, amount, type)

        // Card Events (for UI/VFX)
        public static event Action<Character, Card, Character> OnCardUsed;
        public static event Action<Character, Card> OnCardEquipped;
        public static event Action<Character, Card> OnCardUnequipped;
        public static event Action<Character, Card.TYPE> OnNormalAttack;
        public static event Action<Card, int> OnCardFrozen; // (card, duration)
        public static event Action<Card> OnCardUnfrozen;

        // Turn/Action Events (for UI state)
        public static event Action<TURN, Character> OnTurnChanged;
        public static event Action<Character> OnTurnStarted;
        public static event Action<Character> OnTurnEnded;
        public static event Action<int> OnActionsChanged;
        public static event Action OnActionsLocked;

        // Factor Events (for VFX/SFX)
        public static event Action<Character, Character.STATUS_EFFECT, int> OnFactorApplied; // (character, effect, duration)
        public static event Action<Character, Character.STATUS_EFFECT> OnFactorExpired;
        public static event Action<Character, Character.STATUS_EFFECT> OnFactorBlocked; // Storm blocking other factors

        // Game State Events
        public static event Action<Character> OnPlayerDefeated;
        public static event Action<Character> OnPlayerVictory;
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;

        // Critical Hit Events (for VFX/SFX)
        public static event Action<Character, Character, int, bool> OnAttackResolved; // (attacker, target, damage, wasCrit)

        // Resource Events (for UI updates)
        public static event Action<Character, int, string> OnResourceGained; // (character, amount, type)
        public static event Action<Character, int, string> OnResourceLost; // (character, amount, type)
        public static event Action<Character, int, int> OnResourceRegenerated; // (character, ep, mp)

        // Charm Events
        public static event Action<Character, Charm> OnCharmEquipped;
        public static event Action<Character, Charm> OnCharmUnequipped;
        public static event Action<Character, string> OnSetBonusActivated;
        
        // Character Passive Events
        public static event Action<Character> OnPassiveTriggered;
        public static event Action<Character, string> OnPassiveStateChanged;

        // Trigger methods with better names
        public static void TriggerDamageDealt ( Character target, int damage, int remainingLP )
            => OnDamageDealt?.Invoke ( target, damage, remainingLP );

        public static void TriggerCardUsed ( Character user, Card card, Character target )
            => OnCardUsed?.Invoke ( user, card, target );

        public static void TriggerCardEquipped ( Character character, Card card )
            => OnCardEquipped?.Invoke ( character, card );

        public static void TriggerNormalAttack ( Character attacker, Card.TYPE weaponType )
            => OnNormalAttack?.Invoke ( attacker, weaponType );

        public static void TriggerTurnChanged ( TURN turn, Character character )
            => OnTurnChanged?.Invoke ( turn, character );

        public static void TriggerTurnStarted ( Character character )
            => OnTurnStarted?.Invoke ( character );

        public static void TriggerTurnEnded ( Character character )
            => OnTurnEnded?.Invoke ( character );

        public static void TriggerActionsChanged ( int remaining )
            => OnActionsChanged?.Invoke ( remaining );

        public static void TriggerActionsLocked ( )
            => OnActionsLocked?.Invoke ( );

        public static void TriggerFactorApplied ( Character character, Character.STATUS_EFFECT effect, int duration )
            => OnFactorApplied?.Invoke ( character, effect, duration );

        public static void TriggerFactorExpired ( Character character, Character.STATUS_EFFECT effect )
            => OnFactorExpired?.Invoke ( character, effect );

        public static void TriggerFactorBlocked ( Character character, Character.STATUS_EFFECT effect )
            => OnFactorBlocked?.Invoke ( character, effect );

        public static void TriggerHealingReceived ( Character character, int amount )
            => OnHealingReceived?.Invoke ( character, amount );

        public static void TriggerResourceStolen ( Character from, Character to, int amount, string type )
            => OnResourceStolen?.Invoke ( from, to, amount, type );

        public static void TriggerResourceGained ( Character character, int amount, string type )
            => OnResourceGained?.Invoke ( character, amount, type );

        public static void TriggerResourceLost ( Character character, int amount, string type )
            => OnResourceLost?.Invoke ( character, amount, type );

        public static void TriggerResourceRegenerated ( Character character, int ep, int mp )
            => OnResourceRegenerated?.Invoke ( character, ep, mp );

        public static void TriggerAttackResolved ( Character attacker, Character target, int damage, bool wasCrit )
            => OnAttackResolved?.Invoke ( attacker, target, damage, wasCrit );

        public static void TriggerCardFrozen ( Card card, int duration )
            => OnCardFrozen?.Invoke ( card, duration );

        public static void TriggerCardUnfrozen ( Card card )
            => OnCardUnfrozen?.Invoke ( card );

        public static void TriggerGameStarted ( )
            => OnGameStarted?.Invoke ( );

        public static void TriggerGameEnded ( )
            => OnGameEnded?.Invoke ( );

        public static void TriggerPlayerVictory ( Character winner )
            => OnPlayerVictory?.Invoke ( winner );

        // Fixed: Add missing trigger method
        public static void TriggerPlayerDefeated ( Character character )
            => OnPlayerDefeated?.Invoke ( character );

        // New trigger methods for charms
        public static void TriggerCharmEquipped(Character character, Charm charm)
            => OnCharmEquipped?.Invoke(character, charm);

        public static void TriggerCharmUnequipped(Character character, Charm charm)
            => OnCharmUnequipped?.Invoke(character, charm);

        public static void TriggerSetBonusActivated(Character character, string setName)
            => OnSetBonusActivated?.Invoke(character, setName);

        public static void TriggerPassiveTriggered(Character character)
            => OnPassiveTriggered?.Invoke(character);

        public static void TriggerPassiveStateChanged(Character character, string stateName)
            => OnPassiveStateChanged?.Invoke(character, stateName);
    }
}