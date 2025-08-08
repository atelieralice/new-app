using System.Collections.Generic;

namespace meph {
    // Holds extra info for each active factor
    public class FactorInstance {
        public Character.STATUS_EFFECT Type { get; internal set; }
        public int Duration { get; internal set; }
        public Dictionary<string, int> Params { get; internal set; } = new ( );
    }

    // Manages factors for all characters
    public class FactorManager {
        // Each character has a dictionary of active factors
        private Dictionary<Character, Dictionary<Character.STATUS_EFFECT, FactorInstance>> characterFactors = new ( );

        // Register a character to track its factors
        public void RegisterCharacter ( Character character ) {
            if ( !characterFactors.ContainsKey ( character ) )
                characterFactors[character] = new Dictionary<Character.STATUS_EFFECT, FactorInstance> ( );
        }

        // Apply a factor to a character
        public void ApplyFactor ( Character character, Character.STATUS_EFFECT effect, int duration, Dictionary<string, int> parameters = null ) {
            RegisterCharacter ( character );
            character.StatusEffects |= effect;
            characterFactors[character][effect] = new FactorInstance {
                Type = effect,
                Duration = duration,
                Params = parameters ?? new Dictionary<string, int> ( )
            };
        }

        // Remove a factor from a character
        public void RemoveFactor ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.ContainsKey ( character ) ) {
                character.StatusEffects &= ~effect;
                characterFactors[character].Remove ( effect );
            }
        }

        // Get info about a factor
        public FactorInstance GetFactor ( Character character, Character.STATUS_EFFECT effect ) {
            if ( characterFactors.ContainsKey ( character ) && characterFactors[character].ContainsKey ( effect ) )
                return characterFactors[character][effect];
            return null;
        }

        // Update all factors (e.g., at end of turn)
        public void UpdateFactors ( ) {
            foreach ( var kvp in characterFactors ) {
                var character = kvp.Key;
                var factors = kvp.Value;
                var toRemove = new List<Character.STATUS_EFFECT> ( );
                foreach ( var factor in factors.Values ) {
                    factor.Duration--;
                    if ( factor.Duration <= 0 )
                        toRemove.Add ( factor.Type );
                }
                foreach ( var effect in toRemove )
                    RemoveFactor ( character, effect );
            }
        }
    }
}