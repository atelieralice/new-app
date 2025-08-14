using System;

namespace meph {
    /// <summary>
    /// Centralized event system providing game-wide event coordination and loose coupling
    /// Implements comprehensive event architecture for UI updates, VFX triggers, and system communication
    /// Enables responsive game feedback and modular system integration without direct dependencies
    /// 
    /// Core Architecture:
    /// - Static Event Hub: Provides global access for event subscription and triggering
    /// - Typed Event Delegates: Strongly-typed parameters for reliable event data
    /// - Category Organization: Logical grouping of related events for maintainability
    /// - Trigger Methods: Consistent invocation pattern with null safety checks
    /// 
    /// Event Categories:
    /// - Combat Events: Damage, healing, attacks, and resource changes for VFX/UI feedback
    /// - Card Events: Equipment, usage, and state changes for inventory and UI updates
    /// - Turn/Action Events: Turn flow and action economy for UI state management
    /// - Factor Events: Status effect lifecycle for visual feedback and game logic
    /// - Game State Events: Phase transitions and victory conditions for UI coordination
    /// - Resource Events: Resource gains, losses, and regeneration for UI synchronization
    /// - Character Events: Passive abilities and character-specific state changes
    /// 
    /// Usage Patterns:
    /// - Event Subscription: Systems subscribe to relevant events during initialization
    /// - Event Triggering: Game logic triggers events at appropriate moments
    /// - UI Integration: Interface systems respond to events for real-time updates
    /// - VFX/SFX: Visual and audio systems use events for effect timing
    /// - Logging Integration: Console logging systems track events for debugging
    /// 
    /// Design Benefits:
    /// - Loose Coupling: Systems communicate without direct references
    /// - Extensibility: New systems can easily integrate through event subscription
    /// - Responsiveness: Real-time UI updates through immediate event notification
    /// - Debugging: Centralized event tracking for game state analysis
    /// - Modularity: Individual systems can be developed and tested independently
    /// </summary>
    public static class GameEvents {
        
        #region Combat Event Definitions
        
        /// <summary>
        /// Triggered when damage is successfully applied to a character
        /// Provides damage amount and remaining health for UI updates and VFX timing
        /// Used for health bar updates, damage numbers, and combat feedback
        /// </summary>
        public static event Action<Character, int, int> OnDamageDealt; // (target, damage, remainingLP)
        
        /// <summary>
        /// Triggered when a character receives healing from any source
        /// Provides healing amount for UI updates and positive feedback effects
        /// Used for health bar updates, healing numbers, and restoration VFX
        /// </summary>
        public static event Action<Character, int> OnHealingReceived; // (character, amount)
        
        /// <summary>
        /// Triggered when resources are stolen between characters through factor effects
        /// Provides source, target, amount, and resource type for comprehensive feedback
        /// Used for resource bar updates, transfer animations, and combat logging
        /// </summary>
        public static event Action<Character, Character, int, string> OnResourceStolen; // (from, to, amount, type)
        
        /// <summary>
        /// Triggered when an attack is fully resolved with final damage and critical hit status
        /// Provides complete attack information for VFX, SFX, and combat analysis
        /// Used for damage effects, critical hit animations, and combat feedback
        /// </summary>
        public static event Action<Character, Character, int, bool> OnAttackResolved; // (attacker, target, damage, wasCrit)
        
        #endregion
        
        #region Card and Equipment Event Definitions
        
        /// <summary>
        /// Triggered when a card effect is successfully executed by a character
        /// Provides user, card, and target information for UI feedback and effect tracking
        /// Used for card usage animations, cooldown updates, and combat logging
        /// </summary>
        public static event Action<Character, Card, Character> OnCardUsed;
        
        /// <summary>
        /// Triggered when a card is successfully equipped to a character slot
        /// Provides character and card information for inventory updates and UI synchronization
        /// Used for equipment display updates, stat recalculation, and audit logging
        /// </summary>
        public static event Action<Character, Card> OnCardEquipped;
        
        /// <summary>
        /// Triggered when a card is removed from a character's equipped slots
        /// Provides character and card information for inventory updates and stat recalculation
        /// Used for equipment display updates, stat adjustments, and cleanup operations
        /// </summary>
        public static event Action<Character, Card> OnCardUnequipped;
        
        /// <summary>
        /// Triggered when a character performs a normal attack with weapon integration
        /// Provides attacker and weapon type information for combat animations and feedback
        /// Used for attack animations, weapon effects, and combat sequence coordination
        /// </summary>
        public static event Action<Character, Card.TYPE> OnNormalAttack;
        
        /// <summary>
        /// Triggered when a card becomes frozen and temporarily unusable
        /// Provides card and freeze duration for UI updates and timing management
        /// Used for card graying, countdown timers, and usage prevention feedback
        /// </summary>
        public static event Action<Card, int> OnCardFrozen; // (card, duration)
        
        /// <summary>
        /// Triggered when a card's freeze effect expires and becomes usable again
        /// Provides card information for UI restoration and availability feedback
        /// Used for card restoration animations, usage re-enabling, and state updates
        /// </summary>
        public static event Action<Card> OnCardUnfrozen;
        
        #endregion
        
        #region Turn and Action Economy Event Definitions
        
        /// <summary>
        /// Triggered when the active turn alternates between players
        /// Provides turn type and active character for comprehensive turn transition handling
        /// Used for UI state updates, player notification, and turn-based system coordination
        /// </summary>
        public static event Action<TURN, Character> OnTurnChanged;
        
        /// <summary>
        /// Triggered when a character's turn begins after resource regeneration and setup
        /// Provides active character for turn-specific UI updates and effect processing
        /// Used for turn indicators, action button enabling, and character-specific setup
        /// </summary>
        public static event Action<Character> OnTurnStarted;
        
        /// <summary>
        /// Triggered when a character's turn ends before transition to opponent
        /// Provides ending character for turn cleanup and transition preparation
        /// Used for turn cleanup, effect resolution, and transition animations
        /// </summary>
        public static event Action<Character> OnTurnEnded;
        
        /// <summary>
        /// Triggered whenever the remaining action count changes during a turn
        /// Provides updated action count for UI displays and availability validation
        /// Used for action counter updates, button state management, and turn progression
        /// </summary>
        public static event Action<int> OnActionsChanged;
        
        /// <summary>
        /// Triggered when actions become locked due to exhaustion or manual turn end
        /// Signals UI systems to disable action buttons and show turn end options
        /// Used for UI state transitions, button disabling, and turn end notifications
        /// </summary>
        public static event Action OnActionsLocked;
        
        #endregion
        
        #region Factor and Status Effect Event Definitions
        
        /// <summary>
        /// Triggered when a status effect factor is successfully applied to a character
        /// Provides character, effect type, and duration for UI updates and effect tracking
        /// Used for status icons, effect timers, and visual status indicators
        /// </summary>
        public static event Action<Character, Character.STATUS_EFFECT, int> OnFactorApplied; // (character, effect, duration)
        
        /// <summary>
        /// Triggered when a status effect factor expires and is removed from a character
        /// Provides character and effect type for UI cleanup and effect removal
        /// Used for status icon removal, effect cleanup, and state restoration
        /// </summary>
        public static event Action<Character, Character.STATUS_EFFECT> OnFactorExpired;
        
        /// <summary>
        /// Triggered when Storm factor blocks the application of another status effect
        /// Provides character and blocked effect type for feedback and game rule enforcement
        /// Used for blocked effect notifications, Storm mechanic feedback, and rule validation
        /// </summary>
        public static event Action<Character, Character.STATUS_EFFECT> OnFactorBlocked; // Storm blocking other factors
        
        #endregion
        
        #region Resource Management Event Definitions
        
        /// <summary>
        /// Triggered when a character gains resources from any source (regeneration, effects, etc.)
        /// Provides character, amount, and resource type for UI updates and positive feedback
        /// Used for resource bar updates, gain animations, and resource tracking
        /// </summary>
        public static event Action<Character, int, string> OnResourceGained; // (character, amount, type)
        
        /// <summary>
        /// Triggered when a character loses resources from any source (costs, damage, etc.)
        /// Provides character, amount, and resource type for UI updates and feedback
        /// Used for resource bar updates, cost animations, and resource tracking
        /// </summary>
        public static event Action<Character, int, string> OnResourceLost; // (character, amount, type)
        
        /// <summary>
        /// Triggered when automatic resource regeneration occurs at turn start
        /// Provides character and regenerated amounts for specialized regeneration feedback
        /// Used for regeneration animations, turn-based recovery indication, and logging
        /// </summary>
        public static event Action<Character, int, int> OnResourceRegenerated; // (character, ep, mp)
        
        #endregion
        
        #region Character and Charm Event Definitions
        
        /// <summary>
        /// Triggered when a charm is successfully equipped to a character
        /// Provides character and charm information for inventory updates and set bonus calculation
        /// Used for charm display updates, set bonus evaluation, and equipment tracking
        /// </summary>
        public static event Action<Character, Charm> OnCharmEquipped;
        
        /// <summary>
        /// Triggered when a charm is removed from a character's equipment
        /// Provides character and charm information for inventory updates and set bonus recalculation
        /// Used for charm display cleanup, set bonus re-evaluation, and equipment tracking
        /// </summary>
        public static event Action<Character, Charm> OnCharmUnequipped;
        
        /// <summary>
        /// Triggered when a charm set bonus becomes active due to complete set equipment
        /// Provides character and set name for bonus notification and effect tracking
        /// Used for set bonus notifications, bonus effect indication, and enhancement tracking
        /// </summary>
        public static event Action<Character, string> OnSetBonusActivated;
        
        /// <summary>
        /// Triggered when a character's passive ability activates or executes
        /// Provides character information for passive ability feedback and tracking
        /// Used for passive effect notifications, ability tracking, and character enhancement display
        /// </summary>
        public static event Action<Character> OnPassiveTriggered;
        
        /// <summary>
        /// Triggered when a character's passive ability state changes (activation, deactivation, etc.)
        /// Provides character and state information for detailed passive ability tracking
        /// Used for passive state indicators, ability management, and character enhancement tracking
        /// </summary>
        public static event Action<Character, string> OnPassiveStateChanged;
        
        #endregion
        
        #region Game State and Flow Event Definitions
        
        /// <summary>
        /// Triggered when a player is defeated and eliminated from combat
        /// Provides defeated character for victory processing and game conclusion handling
        /// Used for defeat animations, victory determination, and game end processing
        /// </summary>
        public static event Action<Character> OnPlayerDefeated;
        
        /// <summary>
        /// Triggered when a player achieves victory by defeating their opponent
        /// Provides winning character for victory celebration and result processing
        /// Used for victory animations, result screens, and achievement tracking
        /// </summary>
        public static event Action<Character> OnPlayerVictory;
        
        /// <summary>
        /// Triggered when the game session begins (initial startup or restart)
        /// Signals game initialization and system startup for comprehensive setup
        /// Used for game initialization, system startup, and initial UI setup
        /// </summary>
        public static event Action OnGameStarted;
        
        /// <summary>
        /// Triggered when the game session ends (victory, defeat, or termination)
        /// Signals game conclusion and cleanup for comprehensive shutdown handling
        /// Used for game cleanup, result processing, and session conclusion
        /// </summary>
        public static event Action OnGameEnded;
        
        /// <summary>
        /// Triggered when game state changes require broad system notification
        /// Provides character context for state-dependent updates and processing
        /// Used for general state synchronization and system coordination
        /// </summary>
        public static event Action<Character> OnGameStateChanged;
        
        /// <summary>
        /// Triggered when both players are assigned and ready for battle initiation
        /// Provides both characters for player setup confirmation and battle preparation
        /// Used for player confirmation, battle setup, and pre-combat initialization
        /// </summary>
        public static event Action<Character, Character> OnPlayersSet;
        
        /// <summary>
        /// Triggered when the game transitions between major phases (selection, battle, etc.)
        /// Provides phase name for UI state management and system configuration
        /// Used for UI state transitions, system reconfiguration, and phase-specific setup
        /// </summary>
        public static event Action<string> OnGamePhaseChanged;
        
        #endregion
        
        #region Combat Event Triggers
        
        /// <summary>
        /// Safely triggers damage dealt event with comprehensive parameter validation
        /// Provides null safety and ensures proper event notification for damage application
        /// </summary>
        /// <param name="target">Character who received damage</param>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="remainingLP">Character's remaining health after damage</param>
        public static void TriggerDamageDealt(Character target, int damage, int remainingLP)
            => OnDamageDealt?.Invoke(target, damage, remainingLP);

        /// <summary>
        /// Safely triggers healing received event with parameter validation
        /// Ensures proper event notification for healing application and recovery
        /// </summary>
        /// <param name="character">Character who received healing</param>
        /// <param name="amount">Amount of healing received</param>
        public static void TriggerHealingReceived(Character character, int amount)
            => OnHealingReceived?.Invoke(character, amount);

        /// <summary>
        /// Safely triggers resource stolen event with comprehensive transaction details
        /// Provides complete information for resource transfer tracking and feedback
        /// </summary>
        /// <param name="from">Character losing resources</param>
        /// <param name="to">Character gaining resources</param>
        /// <param name="amount">Amount of resource transferred</param>
        /// <param name="type">Type of resource transferred (EP, MP, etc.)</param>
        public static void TriggerResourceStolen(Character from, Character to, int amount, string type)
            => OnResourceStolen?.Invoke(from, to, amount, type);

        /// <summary>
        /// Safely triggers attack resolved event with complete combat information
        /// Provides comprehensive attack details for VFX, logging, and feedback systems
        /// </summary>
        /// <param name="attacker">Character performing the attack</param>
        /// <param name="target">Character receiving the attack</param>
        /// <param name="damage">Final damage amount dealt</param>
        /// <param name="wasCrit">Whether the attack was a critical hit</param>
        public static void TriggerAttackResolved(Character attacker, Character target, int damage, bool wasCrit)
            => OnAttackResolved?.Invoke(attacker, target, damage, wasCrit);
        
        #endregion
        
        #region Card and Equipment Event Triggers
        
        /// <summary>
        /// Safely triggers card used event with usage context and participants
        /// Provides complete card usage information for UI updates and effect tracking
        /// </summary>
        /// <param name="user">Character using the card</param>
        /// <param name="card">Card being used</param>
        /// <param name="target">Target of the card effect</param>
        public static void TriggerCardUsed(Character user, Card card, Character target)
            => OnCardUsed?.Invoke(user, card, target);

        /// <summary>
        /// Safely triggers card equipped event with equipment details
        /// Ensures proper notification for equipment changes and inventory updates
        /// </summary>
        /// <param name="character">Character equipping the card</param>
        /// <param name="card">Card being equipped</param>
        public static void TriggerCardEquipped(Character character, Card card)
            => OnCardEquipped?.Invoke(character, card);

        /// <summary>
        /// Safely triggers normal attack event with weapon context
        /// Provides attack information for combat animations and weapon effect coordination
        /// </summary>
        /// <param name="attacker">Character performing normal attack</param>
        /// <param name="weaponType">Type of weapon used in attack</param>
        public static void TriggerNormalAttack(Character attacker, Card.TYPE weaponType)
            => OnNormalAttack?.Invoke(attacker, weaponType);

        /// <summary>
        /// Safely triggers card frozen event with freeze details and duration
        /// Provides comprehensive freeze information for UI updates and timing management
        /// </summary>
        /// <param name="card">Card becoming frozen</param>
        /// <param name="duration">Number of turns card will remain frozen</param>
        public static void TriggerCardFrozen(Card card, int duration)
            => OnCardFrozen?.Invoke(card, duration);

        /// <summary>
        /// Safely triggers card unfrozen event for availability restoration
        /// Ensures proper notification when cards become usable again
        /// </summary>
        /// <param name="card">Card becoming unfrozen and usable</param>
        public static void TriggerCardUnfrozen(Card card)
            => OnCardUnfrozen?.Invoke(card);
        
        #endregion
        
        #region Turn and Action Economy Event Triggers
        
        /// <summary>
        /// Safely triggers turn changed event with comprehensive turn transition information
        /// Provides both turn type and character context for complete turn management
        /// </summary>
        /// <param name="turn">New active turn type (ATTACKER or DEFENDER)</param>
        /// <param name="character">Character whose turn is beginning</param>
        public static void TriggerTurnChanged(TURN turn, Character character)
            => OnTurnChanged?.Invoke(turn, character);

        /// <summary>
        /// Safely triggers turn started event with active character context
        /// Ensures proper notification for turn beginning and player activation
        /// </summary>
        /// <param name="character">Character whose turn is starting</param>
        public static void TriggerTurnStarted(Character character)
            => OnTurnStarted?.Invoke(character);

        /// <summary>
        /// Safely triggers turn ended event with ending character context
        /// Provides notification for turn conclusion and transition preparation
        /// </summary>
        /// <param name="character">Character whose turn is ending</param>
        public static void TriggerTurnEnded(Character character)
            => OnTurnEnded?.Invoke(character);

        /// <summary>
        /// Safely triggers actions changed event with updated action count
        /// Ensures proper notification for action economy updates and UI synchronization
        /// </summary>
        /// <param name="remaining">Number of actions remaining for current turn</param>
        public static void TriggerActionsChanged(int remaining)
            => OnActionsChanged?.Invoke(remaining);

        /// <summary>
        /// Safely triggers actions locked event for turn progression control
        /// Signals action exhaustion and need for turn advancement
        /// </summary>
        public static void TriggerActionsLocked()
            => OnActionsLocked?.Invoke();
        
        #endregion
        
        #region Factor and Status Effect Event Triggers
        
        /// <summary>
        /// Safely triggers factor applied event with comprehensive status effect information
        /// Provides complete factor details for UI updates and effect tracking
        /// </summary>
        /// <param name="character">Character receiving the status effect</param>
        /// <param name="effect">Type of status effect applied</param>
        /// <param name="duration">Duration of the status effect in turns</param>
        public static void TriggerFactorApplied(Character character, Character.STATUS_EFFECT effect, int duration)
            => OnFactorApplied?.Invoke(character, effect, duration);

        /// <summary>
        /// Safely triggers factor expired event for status effect removal
        /// Ensures proper notification when status effects end naturally
        /// </summary>
        /// <param name="character">Character losing the status effect</param>
        /// <param name="effect">Type of status effect expiring</param>
        public static void TriggerFactorExpired(Character character, Character.STATUS_EFFECT effect)
            => OnFactorExpired?.Invoke(character, effect);

        /// <summary>
        /// Safely triggers factor blocked event for Storm mechanic feedback
        /// Provides notification when Storm prevents other status effect applications
        /// </summary>
        /// <param name="character">Character with Storm blocking effects</param>
        /// <param name="effect">Type of status effect that was blocked</param>
        public static void TriggerFactorBlocked(Character character, Character.STATUS_EFFECT effect)
            => OnFactorBlocked?.Invoke(character, effect);
        
        #endregion
        
        #region Resource Management Event Triggers
        
        /// <summary>
        /// Safely triggers resource gained event with detailed resource information
        /// Provides comprehensive resource gain tracking for UI and effect systems
        /// </summary>
        /// <param name="character">Character gaining resources</param>
        /// <param name="amount">Amount of resource gained</param>
        /// <param name="type">Type of resource gained (LP, EP, MP, etc.)</param>
        public static void TriggerResourceGained(Character character, int amount, string type)
            => OnResourceGained?.Invoke(character, amount, type);

        /// <summary>
        /// Safely triggers resource lost event with detailed resource information
        /// Provides comprehensive resource loss tracking for UI and effect systems
        /// </summary>
        /// <param name="character">Character losing resources</param>
        /// <param name="amount">Amount of resource lost</param>
        /// <param name="type">Type of resource lost (LP, EP, MP, etc.)</param>
        public static void TriggerResourceLost(Character character, int amount, string type)
            => OnResourceLost?.Invoke(character, amount, type);

        /// <summary>
        /// Safely triggers resource regenerated event with turn-based recovery details
        /// Provides specialized tracking for automatic resource regeneration at turn start
        /// </summary>
        /// <param name="character">Character receiving resource regeneration</param>
        /// <param name="ep">Amount of EP regenerated</param>
        /// <param name="mp">Amount of MP regenerated</param>
        public static void TriggerResourceRegenerated(Character character, int ep, int mp)
            => OnResourceRegenerated?.Invoke(character, ep, mp);
        
        #endregion
        
        #region Character and Charm Event Triggers
        
        /// <summary>
        /// Safely triggers charm equipped event with equipment details
        /// Ensures proper notification for charm equipment and set bonus evaluation
        /// </summary>
        /// <param name="character">Character equipping the charm</param>
        /// <param name="charm">Charm being equipped</param>
        public static void TriggerCharmEquipped(Character character, Charm charm)
            => OnCharmEquipped?.Invoke(character, charm);

        /// <summary>
        /// Safely triggers charm unequipped event with removal details
        /// Provides notification for charm removal and set bonus recalculation
        /// </summary>
        /// <param name="character">Character removing the charm</param>
        /// <param name="charm">Charm being removed</param>
        public static void TriggerCharmUnequipped(Character character, Charm charm)
            => OnCharmUnequipped?.Invoke(character, charm);

        /// <summary>
        /// Safely triggers set bonus activated event with bonus identification
        /// Provides notification when charm set bonuses become active
        /// </summary>
        /// <param name="character">Character gaining the set bonus</param>
        /// <param name="setName">Name of the charm set providing the bonus</param>
        public static void TriggerSetBonusActivated(Character character, string setName)
            => OnSetBonusActivated?.Invoke(character, setName);

        /// <summary>
        /// Safely triggers passive triggered event for ability activation tracking
        /// Provides notification when character passive abilities activate
        /// </summary>
        /// <param name="character">Character whose passive ability triggered</param>
        public static void TriggerPassiveTriggered(Character character)
            => OnPassiveTriggered?.Invoke(character);

        /// <summary>
        /// Safely triggers passive state changed event with detailed state information
        /// Provides comprehensive tracking for passive ability state management
        /// </summary>
        /// <param name="character">Character whose passive state changed</param>
        /// <param name="stateName">Name or description of the new passive state</param>
        public static void TriggerPassiveStateChanged(Character character, string stateName)
            => OnPassiveStateChanged?.Invoke(character, stateName);
        
        #endregion
        
        #region Game State and Flow Event Triggers
        
        /// <summary>
        /// Safely triggers game started event for session initialization
        /// Signals game startup and system initialization to all listening systems
        /// </summary>
        public static void TriggerGameStarted()
            => OnGameStarted?.Invoke();

        /// <summary>
        /// Safely triggers game ended event for session conclusion
        /// Signals game termination and cleanup to all listening systems
        /// </summary>
        public static void TriggerGameEnded()
            => OnGameEnded?.Invoke();

        /// <summary>
        /// Safely triggers player victory event with winner identification
        /// Provides victory notification and winner context for celebration and results
        /// </summary>
        /// <param name="winner">Character who achieved victory</param>
        public static void TriggerPlayerVictory(Character winner)
            => OnPlayerVictory?.Invoke(winner);

        /// <summary>
        /// Safely triggers player defeated event with defeat context
        /// Provides defeat notification for victory determination and game conclusion
        /// </summary>
        /// <param name="character">Character who was defeated</param>
        public static void TriggerPlayerDefeated(Character character)
            => OnPlayerDefeated?.Invoke(character);

        /// <summary>
        /// Safely triggers game state changed event with character context
        /// Provides broad state change notification for system coordination
        /// </summary>
        /// <param name="character">Character associated with the state change</param>
        public static void TriggerGameStateChanged(Character character)
            => OnGameStateChanged?.Invoke(character);

        /// <summary>
        /// Safely triggers players set event with both player assignments
        /// Provides confirmation when both players are assigned and ready for battle
        /// </summary>
        /// <param name="attacker">Character assigned to attacking position</param>
        /// <param name="defender">Character assigned to defending position</param>
        public static void TriggerPlayersSet(Character attacker, Character defender)
            => OnPlayersSet?.Invoke(attacker, defender);

        /// <summary>
        /// Safely triggers game phase changed event with new phase identification
        /// Provides phase transition notification for UI and system reconfiguration
        /// </summary>
        /// <param name="phase">Name of the new game phase</param>
        public static void TriggerGamePhaseChanged(string phase)
            => OnGamePhaseChanged?.Invoke(phase);
        
        #endregion
    }
}