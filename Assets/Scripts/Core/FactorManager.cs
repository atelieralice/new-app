using System.Collections.Generic;
using System;
using static meph.Character;

namespace meph {
    // A single, concrete effect applied to a character.
    // - Type: which STATUS_EFFECT this is.
    // - Duration: remaining turns (instance-based, decremented by UpdateFactors).
    // - Params: small bag of ints (e.g., DP, HA) keyed via ParamKeys for extensibility.
    public class FactorInstance {
        public STATUS_EFFECT Type { get; internal set; }
        public int Duration { get; internal set; }
        public Dictionary<string, int> Params { get; internal set; } = new ( );
    }

    // Centralized string keys for FactorInstance.Params to avoid magic strings.
    // Keep additions here so FactorLogic and cards stay consistent.
    public static class ParamKeys {
        public const string DP = "DP";   // Damage Prevention / Shield points
        public const string HA = "HA";   // Heal Amount
        public const string BD = "BD";   // Burn Damage percent (of MaxLP)
        public const string FD = "FD";   // Freeze Duration (turns)
        public const string RC = "RC";   // Recharge amount
        public const string SD = "SD";   // Storm damage per tick
        public const string MP = "MP";   // Growth: MP amount
    }

    // FactorManager owns storage of active effects per character.
    // Design:
    // - All durations are instance-based (each application is its own timer).
    // - Some effects are "overwrite-only" (STORM, FREEZE): only one instance exists; re-applying refreshes it.
    // - Character.StatusEffects is a bitfield mirror for quick checks; do not mutate it directly.
    public class FactorManager {
        // Lifecycle events (engine-agnostic; safe for Unity)
        public event Action<Character, STATUS_EFFECT, FactorInstance> OnFactorApplied;
        public event Action<Character, STATUS_EFFECT, FactorInstance> OnFactorRemoved;
        public event Action<Character, STATUS_EFFECT> OnStatusCleared;
        public event Action OnFactorUpdate;

        // Number of single-bit flags in STATUS_EFFECT; computed once so we can pre-size arrays.
        private static readonly int EffectCount = GetEffectCount ( );

        // Shared empty list to return when a character/effect has no entries.
        // Treat as read-only. Do not Add/Remove items to it.
        private static readonly List<FactorInstance> EmptyList = new ( );

        // Each character maps to an array of lists (index-per-flag). Index 0 is NONE and unused.
        // Durations are instance-based for all effects except FREEZE and STORM (overwrite-only).
        private readonly Dictionary<Character, List<FactorInstance>[]> characterFactors = new ( );

        // Determine how many single-bit flags exist in STATUS_EFFECT (ignores combined flags).
        private static int GetEffectCount ( ) {
            int maxIdx = 0;
            foreach ( STATUS_EFFECT e in Enum.GetValues ( typeof ( STATUS_EFFECT ) ) ) {
                if ( e == STATUS_EFFECT.NONE ) continue;
                int v = (int)e;
                // Only consider single-bit flags (v & ( v - 1 ) ) == 0
                if ( ( v & ( v - 1 ) ) != 0 ) continue;
                int idx = 0;
                while ( v > 0 ) { v >>= 1; idx++; }
                if ( idx > maxIdx ) maxIdx = idx;
            }
            return maxIdx;
        }

        // Flag <-> index mapping helpers to address the correct list in the per-character array.
        private static int EffectToIndex ( STATUS_EFFECT effect ) {
            if ( effect == STATUS_EFFECT.NONE ) return 0;
            int val = (int)effect;
            int idx = 1;
            while ( val > 1 ) { val >>= 1; idx++; }
            return idx;
        }
        private static STATUS_EFFECT IndexToEffect ( int idx ) =>
            idx == 0 ? STATUS_EFFECT.NONE : (STATUS_EFFECT)( 1 << ( idx - 1 ) );

        // These effects are single-instance per character; re-applying replaces and refreshes duration.
        private static bool DoesOverwrite ( STATUS_EFFECT effect ) =>
            effect == STATUS_EFFECT.FREEZE || effect == STATUS_EFFECT.STORM;

        // Ensure storage exists for a character before applying/reading factors.
        public void RegisterCharacter ( Character character ) {
            if ( !characterFactors.ContainsKey ( character ) ) {
                var arr = new List<FactorInstance>[EffectCount + 1];
                for ( int i = 0; i < arr.Length; i++ )
                    arr[i] = new List<FactorInstance> ( );
                characterFactors[character] = arr;
            }
        }

        // Optional cleanup hook (e.g., when removing a character from battle).
        public void UnregisterCharacter ( Character character, bool clearStatusFlags = true ) {
            if ( characterFactors.Remove ( character ) && clearStatusFlags ) {
                character.StatusEffects = STATUS_EFFECT.NONE;
            }
        }

        // Core entry to apply an effect:
        // - Instance-based: appends a new FactorInstance with its own timer.
        // - Overwrite-only: clears existing instances and inserts a single refreshed one.
        // Also mirrors the bit in Character.StatusEffects.
        public void ApplyFactor ( Character character, STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null ) {
            if ( effect == STATUS_EFFECT.NONE ) return;
            RegisterCharacter ( character );
            int idx = EffectToIndex ( effect );
            var arr = characterFactors[character];

            var factor = new FactorInstance {
                Type = effect,
                Duration = duration,
                Params = parameters ?? new Dictionary<string, int> ( )
            };

            if ( DoesOverwrite ( effect ) ) {
                arr[idx].Clear ( );
                arr[idx].Add ( factor );
            } else {
                arr[idx].Add ( factor );
            }
            character.StatusEffects |= effect;

            OnFactorApplied?.Invoke ( character, effect, factor );
        }

        // Remove a single instance (by index) and clear the bit if that was the last.
        public void RemoveFactorInstance ( Character character, STATUS_EFFECT effect, int idx ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                var list = arr[EffectToIndex ( effect )];
                if ( idx >= 0 && idx < list.Count ) {
                    var removed = list[idx];
                    list.RemoveAt ( idx );
                    OnFactorRemoved?.Invoke ( character, effect, removed );
                    if ( list.Count == 0 ) {
                        character.StatusEffects &= ~effect;
                        OnStatusCleared?.Invoke ( character, effect );
                    }
                }
            }
        }

        // Remove all instances of an effect and clear the bit.
        public void RemoveAllFactors ( Character character, STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                var list = arr[EffectToIndex ( effect )];
                if ( list.Count > 0 ) {
                    // emit removals, then clear
                    for ( int i = 0; i < list.Count; i++ )
                        OnFactorRemoved?.Invoke ( character, effect, list[i] );
                    list.Clear ( );
                    character.StatusEffects &= ~effect;
                    OnStatusCleared?.Invoke ( character, effect );
                }
            }
        }

        // Return the live list for a given character/effect.
        // Note: This is the internal list; do not add/remove externally—use Apply/Remove methods.
        public List<FactorInstance> GetFactors ( Character character, STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                return arr[idx];
            }
            return EmptyList;
        }

        // Example helper for callers that need a quick shield reference.
        public FactorInstance GetFirstShield ( Character character ) {
            var shields = GetFactors ( character, STATUS_EFFECT.TOUGHNESS );
            return shields.Count > 0 ? shields[0] : null;
        }

        // Decrement timers and prune expired instances. If a list becomes empty,
        // unset the corresponding bit flag on the character.
        private static void UpdateEffectList ( List<FactorInstance> instances, STATUS_EFFECT effect, Character character, int idx ) {
            for ( int i = instances.Count - 1; i >= 0; i-- ) {
                instances[i].Duration--;
                if ( instances[i].Duration <= 0 ) instances.RemoveAt ( i );
            }
            if ( instances.Count == 0 && idx != 0 )
                character.StatusEffects &= ~effect;
        }

        // Call once per end-of-turn to age all effects for all characters managed here.
        public void UpdateFactors ( ) {
            foreach ( var kvp in characterFactors ) {
                var character = kvp.Key;
                var arr = kvp.Value;
                for ( int idx = 0; idx < arr.Length; idx++ ) {
                    var instances = arr[idx];
                    UpdateEffectList ( instances, IndexToEffect ( idx ), character, idx );
                }
            }
            OnFactorUpdate?.Invoke ( );
        }
    }
}