using System.Collections.Generic;
using System;
using static meph.Character;

namespace meph {
    
    /// <summary>
    /// Represents a single factor effect instance applied to a character with duration and parameters
    /// Encapsulates the core data for status effects including timing, type, and effect-specific values
    /// Provides extensible parameter system for varied factor mechanics and character interactions
    /// 
    /// Core Components:
    /// - Type: STATUS_EFFECT enum value identifying the specific factor (Toughness, Burning, etc.)
    /// - Duration: Instance-based timer decremented each turn until expiration
    /// - Params: Flexible parameter dictionary for effect-specific values (DP, HA, BD, etc.)
    /// 
    /// Instance-Based Design:
    /// - Each application creates separate FactorInstance with independent duration
    /// - Multiple instances of same effect can exist simultaneously (except overwrite types)
    /// - Parameter values can vary between instances for scaling and customization
    /// - Duration management handles automatic expiration and cleanup
    /// </summary>
    public class FactorInstance {
        /// <summary>
        /// The specific status effect type this instance represents
        /// Determines behavior, resolution mechanics, and parameter interpretation
        /// </summary>
        public STATUS_EFFECT Type { get; internal set; }
        
        /// <summary>
        /// Remaining turns before this factor instance expires
        /// Decremented automatically by FactorManager.UpdateFactors() each turn
        /// Instance is removed when duration reaches zero
        /// </summary>
        public int Duration { get; internal set; }
        
        /// <summary>
        /// Effect-specific parameter values keyed by ParamKeys constants
        /// Stores values like shield points (DP), heal amounts (HA), damage percentages (BD)
        /// Allows flexible factor customization and character-specific scaling
        /// </summary>
        public Dictionary<string, int> Params { get; internal set; } = new();
    }

    #region Parameter Key Constants
    
    /// <summary>
    /// Centralized string constants for FactorInstance parameter keys
    /// Prevents magic strings and maintains consistency across factor system
    /// Maps implementation keys to game documentation terminology for reference
    /// 
    /// Usage Pattern:
    /// - Use these constants when setting FactorInstance.Params values
    /// - Provides type safety and IDE support for parameter access
    /// - Documents the relationship between code keys and game rule terminology
    /// - Facilitates refactoring and parameter validation
    /// 
    /// Game Documentation Mapping:
    /// - Implementation uses practical short names for efficiency
    /// - Comments show corresponding game document terminology
    /// - Maintains backward compatibility while documenting rule alignment
    /// </summary>
    public static class ParamKeys {
        /// <summary>Damage Prevention / Shield points (game docs: "Durability Power")</summary>
        public const string DP = "DP";
        
        /// <summary>Heal Amount (game docs: "Heal Amount")</summary>
        public const string HA = "HA";
        
        /// <summary>Burn Damage percent (of MaxLP) (game docs: "Burning Damage")</summary>
        public const string BD = "BD";
        
        /// <summary>Freeze Duration (turns) (game docs: "Freeze Time")</summary>
        public const string FD = "FD";
        
        /// <summary>Recharge amount (game docs: "Recharge amount")</summary>
        public const string RC = "RC";
        
        /// <summary>Storm damage per tick (game docs: "Storm Damage")</summary>
        public const string SD = "SD";
        
        /// <summary>Growth: MP amount (game docs: "Growth amount")</summary>
        public const string MP = "MP";
    }
    
    #endregion

    #region Core Factor Management System
    
    /// <summary>
    /// Centralized factor system manager providing storage, application, and resolution for all status effects
    /// Implements instance-based duration tracking with bitfield optimization for quick status queries
    /// Manages factor lifecycles, parameter storage, and automatic expiration handling
    /// 
    /// Core Architecture:
    /// - Instance-Based Duration: Each factor application creates independent FactorInstance with own timer
    /// - Bitfield Mirroring: Character.StatusEffects provides O(1) status checking via bitwise operations
    /// - Event-Driven Updates: Lifecycle events enable UI synchronization and game state tracking
    /// - Parameter Flexibility: Extensible parameter system supports varied factor mechanics
    /// 
    /// Factor Application Patterns:
    /// - Stackable Effects: Most factors allow multiple instances (Toughness, Healing, Burning)
    /// - Overwrite Effects: Some factors replace existing instances (Storm, Freeze)
    /// - Duration Management: Automatic expiration with cleanup and bitfield maintenance
    /// - Parameter Storage: Effect-specific values stored per instance for customization
    /// 
    /// Performance Optimizations:
    /// - Pre-computed effect counts for efficient array sizing
    /// - Bitfield operations for rapid status effect queries
    /// - Shared empty lists to minimize allocation overhead
    /// - Index-based array access for O(1) factor retrieval
    /// 
    /// Integration Points:
    /// - FactorLogic: Uses FactorManager for all factor applications and queries
    /// - Character System: StatusEffects bitfield provides fast status checking
    /// - UI Systems: Events enable real-time status effect display updates
    /// - Combat Resolution: Factor queries support damage calculations and effect processing
    /// </summary>
    public class FactorManager {
        
        #region Events and Lifecycle Management
        
        /// <summary>
        /// Triggered when a new factor instance is applied to a character
        /// Provides factor details for UI updates, logging, and effect tracking
        /// </summary>
        public event Action<Character, STATUS_EFFECT, FactorInstance> OnFactorApplied;
        
        /// <summary>
        /// Triggered when a factor instance is removed (expiration or manual removal)
        /// Enables cleanup logic and UI synchronization for effect removal
        /// </summary>
        public event Action<Character, STATUS_EFFECT, FactorInstance> OnFactorRemoved;
        
        /// <summary>
        /// Triggered when all instances of a status effect are cleared from a character
        /// Signals complete removal of effect type for comprehensive status updates
        /// </summary>
        public event Action<Character, STATUS_EFFECT> OnStatusCleared;
        
        /// <summary>
        /// Triggered after each factor update cycle (turn-based duration decrements)
        /// Enables synchronized updates for UI, AI, and game state management
        /// </summary>
        public event Action OnFactorUpdate;
        
        #endregion
        
        #region Internal Storage and Configuration
        
        /// <summary>
        /// Pre-computed count of single-bit STATUS_EFFECT flags for efficient array allocation
        /// Calculated once at startup to optimize storage array sizing
        /// </summary>
        private static readonly int EffectCount = GetEffectCount();

        /// <summary>
        /// Shared empty list returned when no factors exist for a character/effect combination
        /// Reduces allocation overhead and provides consistent read-only empty collection
        /// </summary>
        private static readonly List<FactorInstance> EmptyList = new();

        /// <summary>
        /// Core storage mapping characters to arrays of factor instance lists
        /// Each array index corresponds to a specific STATUS_EFFECT flag for O(1) access
        /// Supports efficient factor queries, additions, and removals per character
        /// </summary>
        private readonly Dictionary<Character, List<FactorInstance>[]> characterFactors = new();
        
        #endregion
        
        #region Internal Helper Methods
        
        /// <summary>
        /// Calculates the number of distinct single-bit flags in STATUS_EFFECT enum
        /// Used for pre-sizing storage arrays and ensuring proper index mapping
        /// Ignores combined flags to focus on individual effect types
        /// 
        /// Algorithm:
        /// 1. Iterate through all STATUS_EFFECT enum values
        /// 2. Filter to single-bit flags using bitwise operations
        /// 3. Calculate bit position for maximum flag value
        /// 4. Return highest bit position for array sizing
        /// </summary>
        /// <returns>Maximum bit index for STATUS_EFFECT single-bit flags</returns>
        private static int GetEffectCount() {
            int maxIdx = 0;
            foreach (STATUS_EFFECT e in Enum.GetValues(typeof(STATUS_EFFECT))) {
                if (e == STATUS_EFFECT.NONE) continue;
                int v = (int)e;
                // Only consider single-bit flags (v & (v - 1)) == 0
                if ((v & (v - 1)) != 0) continue;
                int idx = 0;
                while (v > 0) { v >>= 1; idx++; }
                if (idx > maxIdx) maxIdx = idx;
            }
            return maxIdx;
        }

        /// <summary>
        /// Converts STATUS_EFFECT flag to array index for storage system access
        /// Maps bitfield values to sequential array indices for efficient lookup
        /// Returns 0 for NONE flag, sequential indices for valid single-bit flags
        /// </summary>
        /// <param name="effect">STATUS_EFFECT flag to convert</param>
        /// <returns>Array index for the specified effect (0 for NONE)</returns>
        private static int EffectToIndex(STATUS_EFFECT effect) {
            if (effect == STATUS_EFFECT.NONE) return 0;
            int val = (int)effect;
            int idx = 1;
            while (val > 1) { val >>= 1; idx++; }
            return idx;
        }

        /// <summary>
        /// Converts array index back to corresponding STATUS_EFFECT flag
        /// Reverse mapping for storage system to enum value conversion
        /// Used during factor updates and enumeration operations
        /// </summary>
        /// <param name="idx">Array index to convert (0 returns NONE)</param>
        /// <returns>STATUS_EFFECT flag corresponding to the array index</returns>
        private static STATUS_EFFECT IndexToEffect(int idx) =>
            idx == 0 ? STATUS_EFFECT.NONE : (STATUS_EFFECT)(1 << (idx - 1));

        /// <summary>
        /// Determines if a status effect uses overwrite behavior instead of stacking
        /// Overwrite effects replace existing instances rather than adding new ones
        /// Currently applies to STORM and FREEZE for single-instance semantics
        /// 
        /// Overwrite vs Stacking:
        /// - Overwrite: New application replaces all existing instances
        /// - Stacking: New application adds to existing instances
        /// - Duration refresh: Overwrite effects reset duration on reapplication
        /// </summary>
        /// <param name="effect">STATUS_EFFECT to check for overwrite behavior</param>
        /// <returns>True if effect overwrites existing instances, false if it stacks</returns>
        private static bool DoesOverwrite(STATUS_EFFECT effect) =>
            effect == STATUS_EFFECT.FREEZE || effect == STATUS_EFFECT.STORM;
        
        #endregion
        
        #region Character Registration and Lifecycle
        
        /// <summary>
        /// Initializes factor storage for a character if not already present
        /// Creates properly sized storage arrays for all possible STATUS_EFFECT types
        /// Must be called before applying factors to ensure storage availability
        /// 
        /// Storage Initialization:
        /// - Creates array of lists sized for all possible STATUS_EFFECT flags
        /// - Each array index corresponds to specific STATUS_EFFECT for O(1) access
        /// - Pre-allocates all lists to avoid null reference exceptions
        /// - Idempotent operation: safe to call multiple times for same character
        /// </summary>
        /// <param name="character">Character to initialize factor storage for</param>
        public void RegisterCharacter(Character character) {
            if (!characterFactors.ContainsKey(character)) {
                var arr = new List<FactorInstance>[EffectCount + 1];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = new List<FactorInstance>();
                characterFactors[character] = arr;
            }
        }

        /// <summary>
        /// Removes all factor storage for a character and optionally clears status flags
        /// Used for cleanup when character leaves battle or is defeated
        /// Prevents memory leaks and ensures clean character state transitions
        /// 
        /// Cleanup Process:
        /// 1. Remove all factor instances from storage
        /// 2. Optionally clear Character.StatusEffects bitfield
        /// 3. Free storage arrays for garbage collection
        /// 4. Ensure no lingering factor references remain
        /// </summary>
        /// <param name="character">Character to remove factor storage for</param>
        /// <param name="clearStatusFlags">Whether to reset Character.StatusEffects to NONE</param>
        public void UnregisterCharacter(Character character, bool clearStatusFlags = true) {
            if (characterFactors.Remove(character) && clearStatusFlags) {
                character.StatusEffects = STATUS_EFFECT.NONE;
            }
        }
        
        #endregion
        
        #region Factor Application and Removal
        
        /// <summary>
        /// Applies a new factor instance to a character with specified duration and parameters
        /// Handles both stacking and overwrite behaviors based on effect type
        /// Updates Character.StatusEffects bitfield and triggers application events
        /// 
        /// Application Process:
        /// 1. Validate effect type and ensure character storage exists
        /// 2. Create new FactorInstance with specified parameters
        /// 3. Apply stacking or overwrite logic based on effect type
        /// 4. Update Character.StatusEffects bitfield for quick queries
        /// 5. Trigger OnFactorApplied event for external systems
        /// 
        /// Stacking vs Overwrite Behavior:
        /// - Stacking: Adds new instance alongside existing ones (Toughness, Healing)
        /// - Overwrite: Replaces all existing instances with new one (Storm, Freeze)
        /// - Duration: Each instance tracks own duration independently
        /// - Parameters: Each instance maintains separate parameter values
        /// </summary>
        /// <param name="character">Character receiving the factor effect</param>
        /// <param name="effect">Type of status effect to apply</param>
        /// <param name="duration">Number of turns the effect will remain active</param>
        /// <param name="parameters">Effect-specific parameters (DP, HA, BD, etc.)</param>
        public void ApplyFactor(Character character, STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null) {
            if (effect == STATUS_EFFECT.NONE) return;
            RegisterCharacter(character);
            int idx = EffectToIndex(effect);
            var arr = characterFactors[character];

            var factor = new FactorInstance {
                Type = effect,
                Duration = duration,
                Params = parameters ?? new Dictionary<string, int>()
            };

            if (DoesOverwrite(effect)) {
                arr[idx].Clear();
                arr[idx].Add(factor);
            } else {
                arr[idx].Add(factor);
            }
            character.StatusEffects |= effect;

            OnFactorApplied?.Invoke(character, effect, factor);
        }

        /// <summary>
        /// Removes a specific factor instance by index and cleans up empty effect lists
        /// Provides precise control over factor removal for abilities and expiration handling
        /// Updates Character.StatusEffects bitfield when all instances of an effect are removed
        /// 
        /// Removal Process:
        /// 1. Locate factor instance by character, effect type, and index
        /// 2. Remove instance from storage and trigger removal event
        /// 3. Check if effect list is now empty
        /// 4. Clear corresponding StatusEffects bit if no instances remain
        /// 5. Trigger status cleared event for complete effect removal
        /// 
        /// Index Safety:
        /// - Validates index bounds before removal attempt
        /// - Handles invalid indices gracefully without exceptions
        /// - Maintains list integrity during removal operations
        /// </summary>
        /// <param name="character">Character to remove factor instance from</param>
        /// <param name="effect">Type of status effect to remove instance from</param>
        /// <param name="idx">Index of specific instance to remove</param>
        public void RemoveFactorInstance(Character character, STATUS_EFFECT effect, int idx) {
            if (characterFactors.TryGetValue(character, out var arr)) {
                var list = arr[EffectToIndex(effect)];
                if (idx >= 0 && idx < list.Count) {
                    var removed = list[idx];
                    list.RemoveAt(idx);
                    OnFactorRemoved?.Invoke(character, effect, removed);
                    if (list.Count == 0) {
                        character.StatusEffects &= ~effect;
                        OnStatusCleared?.Invoke(character, effect);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all instances of a specific status effect from a character
        /// Provides bulk removal for abilities that clear entire effect types
        /// Ensures proper event triggering and bitfield cleanup for complete removal
        /// 
        /// Bulk Removal Process:
        /// 1. Locate all instances of the specified effect type
        /// 2. Trigger individual removal events for each instance
        /// 3. Clear entire effect list in single operation
        /// 4. Update Character.StatusEffects bitfield
        /// 5. Trigger status cleared event for effect type removal
        /// 
        /// Use Cases:
        /// - Cleansing abilities that remove specific debuffs
        /// - Character defeat cleanup
        /// - Manual factor removal for testing or debugging
        /// </summary>
        /// <param name="character">Character to remove all factor instances from</param>
        /// <param name="effect">Type of status effect to completely remove</param>
        public void RemoveAllFactors(Character character, STATUS_EFFECT effect) {
            if (characterFactors.TryGetValue(character, out var arr)) {
                var list = arr[EffectToIndex(effect)];
                if (list.Count > 0) {
                    // Emit individual removal events, then clear
                    for (int i = 0; i < list.Count; i++)
                        OnFactorRemoved?.Invoke(character, effect, list[i]);
                    list.Clear();
                    character.StatusEffects &= ~effect;
                    OnStatusCleared?.Invoke(character, effect);
                }
            }
        }
        
        #endregion
        
        #region Factor Queries and Access
        
        /// <summary>
        /// Retrieves all active factor instances of a specific type for a character
        /// Returns the live internal list for efficient iteration and parameter access
        /// Provides read-only access pattern with shared empty list for missing entries
        /// 
        /// Usage Pattern:
        /// - FactorLogic uses this for damage calculations and effect resolution
        /// - Combat system queries factors for conditional logic
        /// - UI systems access factors for status display
        /// - Returns internal list: DO NOT modify externally (use Apply/Remove methods)
        /// 
        /// Performance Considerations:
        /// - O(1) access through pre-computed array indexing
        /// - Returns same EmptyList instance for missing entries (allocation-free)
        /// - Live list enables efficient iteration without copying
        /// - Direct access avoids unnecessary list allocation and copying
        /// </summary>
        /// <param name="character">Character to query factor instances for</param>
        /// <param name="effect">Type of status effect to retrieve instances for</param>
        /// <returns>Live list of factor instances (read-only access recommended)</returns>
        public List<FactorInstance> GetFactors(Character character, STATUS_EFFECT effect) {
            if (characterFactors.TryGetValue(character, out var arr)) {
                int idx = EffectToIndex(effect);
                return arr[idx];
            }
            return EmptyList;
        }

        /// <summary>
        /// Convenience method for quick access to the first Toughness shield instance
        /// Provides simplified access pattern for damage calculation systems
        /// Returns null if no shields exist rather than requiring list iteration
        /// 
        /// Shield Access Pattern:
        /// - Used by damage resolution for shield absorption calculations
        /// - Provides immediate access to primary shield for damage reduction
        /// - Null return indicates no active shields for damage bypass
        /// - Simplifies common shield checking logic in combat systems
        /// </summary>
        /// <param name="character">Character to check for active shield instances</param>
        /// <returns>First Toughness factor instance if available, null otherwise</returns>
        public FactorInstance GetFirstShield(Character character) {
            var shields = GetFactors(character, STATUS_EFFECT.TOUGHNESS);
            return shields.Count > 0 ? shields[0] : null;
        }
        
        #endregion
        
        #region Duration Management and Updates
        
        /// <summary>
        /// Updates duration for a specific effect list and removes expired instances
        /// Handles automatic expiration and bitfield cleanup for empty effect lists
        /// Maintains storage integrity during duration decrements and removals
        /// 
        /// Update Process:
        /// 1. Decrement duration for all instances in the list
        /// 2. Remove instances that reach zero duration (reverse iteration for safety)
        /// 3. Clear corresponding StatusEffects bit if list becomes empty
        /// 4. Maintain list integrity during modification operations
        /// 
        /// Reverse Iteration Safety:
        /// - Iterates backwards to safely remove items during loop
        /// - Prevents index shifting issues during removal operations
        /// - Ensures all instances are processed even when multiple expire
        /// </summary>
        /// <param name="instances">List of factor instances to update durations for</param>
        /// <param name="effect">Status effect type for bitfield clearing</param>
        /// <param name="character">Character owning the instances for bitfield updates</param>
        /// <param name="idx">Array index for bitfield clearing logic</param>
        private static void UpdateEffectList(List<FactorInstance> instances, STATUS_EFFECT effect, Character character, int idx) {
            for (int i = instances.Count - 1; i >= 0; i--) {
                instances[i].Duration--;
                if (instances[i].Duration <= 0) instances.RemoveAt(i);
            }
            if (instances.Count == 0 && idx != 0)
                character.StatusEffects &= ~effect;
        }

        /// <summary>
        /// Processes duration updates for all active factors across all registered characters
        /// Called once per turn to handle automatic factor expiration and cleanup
        /// Triggers factor update event for synchronized system updates
        /// 
        /// Turn-Based Update Process:
        /// 1. Iterate through all registered characters
        /// 2. Process duration updates for each status effect type
        /// 3. Remove expired instances and update bitfields
        /// 4. Trigger OnFactorUpdate event for external synchronization
        /// 
        /// Integration Points:
        /// - GameManager calls this during turn resolution
        /// - UI systems respond to OnFactorUpdate for status display refresh
        /// - AI systems use this for turn-based factor considerations
        /// - Combat resolution depends on this for accurate factor state
        /// 
        /// Performance Notes:
        /// - Single pass through all characters and effects
        /// - Efficient array iteration with minimal allocation
        /// - Automatic cleanup prevents memory accumulation
        /// - Event-driven updates enable responsive UI synchronization
        /// </summary>
        public void UpdateFactors() {
            foreach (var kvp in characterFactors) {
                var character = kvp.Key;
                var arr = kvp.Value;
                for (int idx = 0; idx < arr.Length; idx++) {
                    var instances = arr[idx];
                    UpdateEffectList(instances, IndexToEffect(idx), character, idx);
                }
            }
            OnFactorUpdate?.Invoke();
        }
        
        #endregion
    }
    
    #endregion
}