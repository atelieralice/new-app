using System.Collections.Generic;

namespace meph {
    public class FactorInstance {
        public Character.STATUS_EFFECT Type { get; internal set; } // What kind of factor
        public int Duration { get; internal set; } // How many turns left
        public Dictionary<string, int> Params { get; internal set; } = new ( ); // Extra info
    }

    public class FactorManager {
        // Tracks all factors for each character
        private Dictionary<Character, Dictionary<Character.STATUS_EFFECT, List<FactorInstance>>> characterFactors = new ( );

        // Make sure character is registered before using factors
        public void RegisterCharacter ( Character character ) {
            if ( !characterFactors.ContainsKey ( character ) )
                characterFactors[character] = new Dictionary<Character.STATUS_EFFECT, List<FactorInstance>> ( );
        }

        // Add a factor to a character
        public void ApplyFactor ( Character character, Character.STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null ) {
            RegisterCharacter ( character );

            // Freeze and Storm always overwrite previous instance
            if ( effect == Character.STATUS_EFFECT.FREEZE || effect == Character.STATUS_EFFECT.STORM ) {
                characterFactors[character][effect] = new List<FactorInstance> {
                    new FactorInstance {
                        Type = effect,
                        Duration = duration,
                        Params = parameters ?? new Dictionary<string, int>()
                    }
                };
                character.StatusEffects |= effect;
                return;
            }

            // Factor stacking
            if ( !characterFactors[character].ContainsKey ( effect ) )
                characterFactors[character][effect] = new List<FactorInstance> ( );

            characterFactors[character][effect].Add ( new FactorInstance {
                Type = effect,
                Duration = duration,
                Params = parameters ?? new Dictionary<string, int> ( )
            } );
            character.StatusEffects |= effect;
        }

        // Remove a specific instance of a factor
        public void RemoveFactorInstance ( Character character, Character.STATUS_EFFECT effect, int index ) {
            if ( characterFactors.ContainsKey ( character ) && characterFactors[character].ContainsKey ( effect ) ) {
                var list = characterFactors[character][effect];
                if ( index >= 0 && index < list.Count ) {
                    list.RemoveAt ( index );
                    // Remove status effect if no instances left
                    if ( list.Count == 0 ) {
                        characterFactors[character].Remove ( effect );
                        character.StatusEffects &= ~effect;
                    }
                }
            }
        }

        // Remove all instances of a factor
        public void RemoveAllFactor ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.ContainsKey ( character ) && characterFactors[character].ContainsKey ( effect ) ) {
                characterFactors[character].Remove ( effect );
                character.StatusEffects &= ~effect;
            }
        }

        // Get all instances of a factor
        public List<FactorInstance> GetFactors ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.ContainsKey ( character ) && characterFactors[character].ContainsKey ( effect ) )
                return characterFactors[character][effect];
            return new List<FactorInstance> ( );
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
                var factors = kvp.Value;
                var toRemove = new List<(Character.STATUS_EFFECT, int)> ( );

                foreach ( var effectKvp in factors ) {
                    var effect = effectKvp.Key;
                    var instances = effectKvp.Value;
                    for ( int i = instances.Count - 1; i >= 0; i-- ) {
                        instances[i].Duration--;
                        // Remove expired instances
                        if ( instances[i].Duration <= 0 )
                            toRemove.Add ( (effect, i) );
                    }
                }

                foreach ( var (effect, index) in toRemove )
                    RemoveFactorInstance ( character, effect, index );
            }
        }
    }
}