using System.Collections.Generic;
using System;

namespace meph {
    public class FactorInstance {
        public Character.STATUS_EFFECT Type { get; internal set; }
        public int Duration { get; internal set; }
        public Dictionary<string, int> Params { get; internal set; } = new ( );
    }

    public class FactorManager {
        // Calculate effect count dynamically for future-proofing
        private static readonly int EffectCount = Enum.GetValues ( typeof ( Character.STATUS_EFFECT ) ).Length - 1; // Exclude NONE

        // Static empty list to avoid repeated allocations
        private static readonly List<FactorInstance> EmptyList = new ( );

        // Each character has an array of lists, indexed by effect
        private Dictionary<Character, List<FactorInstance>[]> characterFactors = new ( );

        // Helper: Convert STATUS_EFFECT to array index
        private int EffectToIndex ( Character.STATUS_EFFECT effect ) {
            return effect switch {
                Character.STATUS_EFFECT.NONE => 0,
                Character.STATUS_EFFECT.TOUGHNESS => 1,
                Character.STATUS_EFFECT.HEALING => 2,
                Character.STATUS_EFFECT.RECHARGE => 3,
                Character.STATUS_EFFECT.GROWTH => 4,
                Character.STATUS_EFFECT.STORM => 5,
                Character.STATUS_EFFECT.BURNING => 6,
                Character.STATUS_EFFECT.FREEZE => 7,
                Character.STATUS_EFFECT.IMMUNE => 8,
                _ => 0
            };
        }

        // Helper: Convert array index back to flag
        private Character.STATUS_EFFECT EffectIndexToFlag ( int index ) =>
            index == 0 ? Character.STATUS_EFFECT.NONE : (Character.STATUS_EFFECT)( 1 << ( index - 1 ) );

        private bool OverwritesPreviousInstances ( Character.STATUS_EFFECT effect ) =>
            effect == Character.STATUS_EFFECT.FREEZE || effect == Character.STATUS_EFFECT.STORM;

        // Make sure character is registered before using factors
        public void RegisterCharacter ( Character character ) {
            if ( !characterFactors.ContainsKey ( character ) ) {
                var arr = new List<FactorInstance>[EffectCount + 1];
                for ( int i = 0; i < arr.Length; i++ )
                    arr[i] = new List<FactorInstance> ( );
                characterFactors[character] = arr;
            }
        }

        // Add a factor to a character
        public void ApplyFactor ( Character character, Character.STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null ) {
            RegisterCharacter ( character );
            int idx = EffectToIndex ( effect );
            var arr = characterFactors[character];

            if ( OverwritesPreviousInstances ( effect ) ) {
                arr[idx].Clear ( );
                arr[idx].Add ( new FactorInstance {
                    Type = effect,
                    Duration = duration,
                    Params = parameters ?? new Dictionary<string, int> ( )
                } );
                character.StatusEffects |= effect;
                return;
            }

            arr[idx].Add ( new FactorInstance {
                Type = effect,
                Duration = duration,
                Params = parameters ?? new Dictionary<string, int> ( )
            } );
            character.StatusEffects |= effect;
        }

        // Remove a specific instance of a factor
        public void RemoveFactorInstance ( Character character, Character.STATUS_EFFECT effect, int index ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                var list = arr[idx];
                if ( index >= 0 && index < list.Count ) {
                    list.RemoveAt ( index );
                    if ( list.Count == 0 )
                        character.StatusEffects &= ~effect;
                }
            }
        }

        // Remove all instances of a factor
        public void RemoveAllFactors ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                arr[idx].Clear ( );
                character.StatusEffects &= ~effect;
            }
        }

        // Get all instances of a factor
        public List<FactorInstance> GetFactors ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                return arr[idx];
            }
            return EmptyList;
        }

        // Get the first shield (for damage logic)
        public FactorInstance GetFirstShield ( Character character ) {
            var shields = GetFactors ( character, Character.STATUS_EFFECT.TOUGHNESS );
            return shields.Count > 0 ? shields[0] : null;
        }

        // Update all factors at end of turn
        public void UpdateFactors ( ) {
            foreach ( var kvp in characterFactors ) {
                var character = kvp.Key;
                var arr = kvp.Value;
                for ( int effectIdx = 0; effectIdx < arr.Length; effectIdx++ ) {
                    var instances = arr[effectIdx];
                    for ( int i = instances.Count - 1; i >= 0; i-- ) {
                        instances[i].Duration--;
                        if ( instances[i].Duration <= 0 )
                            instances.RemoveAt ( i );
                    }
                    // Remove bitfield if no instances left
                    if ( instances.Count == 0 && effectIdx != 0 )
                        character.StatusEffects &= ~EffectIndexToFlag ( effectIdx );
                }
            }
        }
    }
}