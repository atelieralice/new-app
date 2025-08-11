using System.Collections.Generic;
using System;
using static meph.Character;

namespace meph {
    public class FactorInstance {
        public STATUS_EFFECT Type { get; internal set; }
        public int Duration { get; internal set; }
        public Dictionary<string, int> Params { get; internal set; } = new ( );
    }

    public class FactorManager {
        // Calculates effect count dynamically
        private static readonly int EffectCount = Enum.GetValues ( typeof ( STATUS_EFFECT ) ).Length - 1; // Exclude NONE

        // Static empty list to avoid repeated allocations
        private static readonly List<FactorInstance> EmptyList = new ( );

        // Each character has an array of lists, indexed by effect
        private Dictionary<Character, List<FactorInstance>[]> characterFactors = new ( );

        // Convert flag to index
        private int EffectToIndex ( STATUS_EFFECT effect ) {
            return effect switch {
                STATUS_EFFECT.NONE => 0,
                STATUS_EFFECT.TOUGHNESS => 1,
                STATUS_EFFECT.HEALING => 2,
                STATUS_EFFECT.RECHARGE => 3,
                STATUS_EFFECT.GROWTH => 4,
                STATUS_EFFECT.STORM => 5,
                STATUS_EFFECT.BURNING => 6,
                STATUS_EFFECT.FREEZE => 7,
                STATUS_EFFECT.IMMUNE => 8,
                _ => 0
            };
        }

        // Convert index to flag
        private STATUS_EFFECT IndexToEffect ( int index ) =>
            index == 0 ? STATUS_EFFECT.NONE : (STATUS_EFFECT)( 1 << ( index - 1 ) );

        private bool DoesOverwrite ( STATUS_EFFECT effect ) =>
            effect == STATUS_EFFECT.FREEZE || effect == STATUS_EFFECT.STORM;

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
        public void ApplyFactor ( Character character, STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null ) {
            RegisterCharacter ( character );
            int idx = EffectToIndex ( effect );
            var arr = characterFactors[character];
            if ( DoesOverwrite ( effect ) ) {
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
        public void RemoveFactorInstance ( Character character, STATUS_EFFECT effect, int index ) {
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
        public void RemoveAllFactors ( Character character, STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                arr[idx].Clear ( );
                character.StatusEffects &= ~effect;
            }
        }

        // Get all instances of a factor
        public List<FactorInstance> GetFactors ( Character character, STATUS_EFFECT effect ) {
            if ( characterFactors.TryGetValue ( character, out var arr ) ) {
                int idx = EffectToIndex ( effect );
                return arr[idx];
            }
            return EmptyList;
        }

        // Get the first shield (for damage logic)
        // May need some refactoring in the future
        public FactorInstance GetFirstShield ( Character character ) {
            var shields = GetFactors ( character, STATUS_EFFECT.TOUGHNESS );
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
                        character.StatusEffects &= ~IndexToEffect ( effectIdx );
                }
            }
        }
    }
}