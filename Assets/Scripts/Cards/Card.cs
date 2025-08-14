using Godot;
using System.Collections.Generic;

namespace meph {
    
    #region Delegates
    
    /// <summary>
    /// Delegate for card effects that target another character
    /// Used by all card types to define their active abilities and effects
    /// </summary>
    /// <param name="user">The character using the card</param>
    /// <param name="target">The target character receiving the effect</param>
    public delegate void CardEffect(Character user, Character target);
    
    #endregion

    /// <summary>
    /// Core card class representing all playable cards in the game system
    /// Implements the complete card mechanics including effects, requirements, and freeze system
    /// Supports all card types from Character cards to Ultimate abilities
    /// </summary>
    public class Card {
        
        #region Card Type System
        
        /// <summary>
        /// Complete enumeration of all card types in the game system
        /// Defines equipment slots, usage rules, and functional categories
        /// Each type has specific mechanics and restrictions as per game rules
        /// </summary>
        public enum TYPE {
            NONE = 0,   // Default/unassigned state
            
            // Core Game Cards
            C,          // Character Card: Identity card defining character stats and essence
            BW,         // Base Weapon: Primary weapon used in normal attacks + card effects
            SW,         // Secondary Weapon: Secondary weapon used in normal attacks + card effects
            
            // Skill Card Categories
            E,          // Equipment/Basic Skill: Basic functions with minimal requirements
            W,          // Weapon Skill: Support functions with moderate requirements  
            Q,          // Character-Specific Skill: Advanced abilities restricted by character
            
            // Special Cards
            P,          // Potion: Swift cards, consumable support items, once per match
            U,          // Ultimate: Powerful character-specific abilities requiring UP resources
            
            // Charm Card System (Future Expansion)
            H,          // Helmet: Defensive charm with general + signature buffs
            A,          // Armor: Defensive charm with general + signature buffs
            G,          // Gloves: Utility charm with general + signature buffs
            B,          // Boots: Mobility charm with general + signature buffs
            Gl          // Glow: Special enhancement charm with strongest effects
        }
        
        #endregion

        #region Core Card Properties
        
        /// <summary>
        /// Unique string identifier for this card instance
        /// Used for card lookup, save/load operations, and debugging
        /// </summary>
        public string Id { get; internal set; }
        
        /// <summary>
        /// Human-readable display name shown in UI
        /// Represents the card's identity to players
        /// </summary>
        public string Name { get; internal set; }
        
        /// <summary>
        /// Card type determining equipment slot and usage mechanics
        /// BW/SW cards function in both normal attacks and active card usage
        /// Q/U cards are restricted to their original character owners
        /// </summary>
        public TYPE Type { get; internal set; }
        
        /// <summary>
        /// Detailed description explaining card mechanics and effects
        /// Includes damage values, factor applications, and special conditions
        /// </summary>
        public string Description { get; internal set; }
        
        #endregion

        #region Resource System
        
        /// <summary>
        /// Resource costs required to actively use this card's effect
        /// Supports MP (Mana Points), EP (Energy Points), UP (Ultimate Points)
        /// Note: Normal attacks using BW/SW cards do not consume these resources
        /// Only active card usage (via "Use Card" action) consumes requirements
        /// </summary>
        public Dictionary<string, int> Requirements { get; internal set; } = new();
        
        #endregion

        #region Swift Card Mechanics
        
        /// <summary>
        /// Whether this card can be used as a Swift Action (no action cost)
        /// Swift Cards can be used multiple times per turn without consuming actions
        /// Currently reserved for future implementation - all Potion cards will be Swift
        /// </summary>
        public bool IsSwift { get; internal set; }
        
        #endregion

        #region Card Effect System
        
        /// <summary>
        /// The primary effect function executed when this card is actively used
        /// 
        /// Usage contexts:
        /// - BW/SW: Triggered during normal attacks AND when used as card effect
        /// - E/W/Q: Only triggered when actively used via "Use Card" action  
        /// - P: Triggered when consumed (Swift Action)
        /// - U: Triggered when Ultimate is activated with UP cost
        /// - C: Triggered during character initialization for passive setup
        /// </summary>
        public CardEffect Effect { get; internal set; }
        
        #endregion

        #region Freeze Mechanics System
        
        /// <summary>
        /// Current freeze status of this card
        /// Frozen cards cannot be used in any capacity:
        /// - BW/SW: Cannot be used in normal attacks or card effects
        /// - All others: Cannot be activated until unfrozen
        /// Applied by Ice-based abilities and Freeze Factor
        /// </summary>
        public bool IsFrozen { get; private set; }
        
        /// <summary>
        /// Number of turns remaining until this card automatically unfreezes
        /// Decremented at the end of each turn during status effect updates
        /// Card becomes usable again when this reaches zero
        /// </summary>
        public int FreezeDuration { get; private set; }

        /// <summary>
        /// Applies freeze effect to this card for the specified duration
        /// Prevents all usage of the card until the freeze expires or is removed
        /// Used by Ice essence abilities and Freeze Factor applications
        /// </summary>
        /// <param name="duration">Number of turns to freeze this card (FT - Freeze Time)</param>
        public void Freeze(int duration) {
            IsFrozen = true;
            FreezeDuration = duration;
            
            // Trigger freeze event for game system tracking
            GameEvents.TriggerCardFrozen(this, duration);
        }

        /// <summary>
        /// Immediately removes freeze effect from this card
        /// Restores full functionality regardless of remaining duration
        /// Can be triggered by unfreeze abilities or immunity effects
        /// </summary>
        public void Unfreeze() {
            IsFrozen = false;
            FreezeDuration = 0;
            
            // Trigger unfreeze event for game system tracking
            GameEvents.TriggerCardUnfrozen(this);
        }

        /// <summary>
        /// Updates freeze duration at the end of each turn
        /// Automatically unfreezes the card when duration expires
        /// Called by the game state manager during turn transitions
        /// </summary>
        public void UpdateFreeze() {
            if (IsFrozen) {
                FreezeDuration--;
                if (FreezeDuration <= 0) {
                    Unfreeze();
                }
            }
        }

        /// <summary>
        /// Extends current freeze duration by additional turns
        /// Only affects cards that are already frozen
        /// Used by abilities that enhance existing freeze effects
        /// </summary>
        /// <param name="additionalTurns">Additional turns to add to current freeze duration</param>
        public void ExtendFreezeDuration(int additionalTurns) {
            if (IsFrozen) {
                FreezeDuration += additionalTurns;
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Returns the card's display name for debugging and UI purposes
        /// Provides safe string representation even for malformed cards
        /// </summary>
        /// <returns>Card name or "Unknown Card" if name is null or empty</returns>
        public override string ToString() => Name ?? "Unknown Card";
        
        /// <summary>
        /// Determines if this card can currently be used based on freeze status
        /// Used by UI systems to enable/disable card interaction
        /// </summary>
        /// <returns>True if card is not frozen, false if frozen</returns>
        public bool CanBeUsed() => !IsFrozen;
        
        /// <summary>
        /// Checks if this card has any resource requirements for activation
        /// Used to determine if resource validation is needed before use
        /// </summary>
        /// <returns>True if card has resource costs, false if free to use</returns>
        public bool HasRequirements() => Requirements.Count > 0;
        
        #endregion
    }
}