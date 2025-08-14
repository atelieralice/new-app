using Godot;

namespace meph {

    #region Godot Resource System Integration
    
    /// <summary>
    /// Godot Resource-based card data container for potential future expansion
    /// Provides serializable card properties that can be saved/loaded via Godot's resource system
    /// Currently unused in favor of code-based card definitions but maintained for flexibility
    /// 
    /// This class supports:
    /// - Visual card editor in Godot Inspector
    /// - Resource-based card loading from .tres files
    /// - Runtime card modification and serialization
    /// - Integration with Godot's asset management system
    /// </summary>
    [GlobalClass]
    public partial class CardData : Resource {
        
        #region Core Card Properties
        
        /// <summary>
        /// Unique identifier matching the Id property in Card class
        /// Used for card lookup and reference tracking
        /// Should follow naming convention: "character_cardname" (e.g., "rok_blazing_dash")
        /// </summary>
        [Export] public string id;
        
        /// <summary>
        /// Human-readable card name displayed in UI
        /// Corresponds to Card.Name property for visual representation
        /// </summary>
        [Export] public string name;
        
        /// <summary>
        /// Card type enumeration determining slot and usage rules
        /// Must match Card.TYPE enum values for proper game mechanics
        /// Exported for visual editing in Godot Inspector
        /// </summary>
        [Export] public Card.TYPE type;
        
        /// <summary>
        /// Detailed card description explaining effects and mechanics
        /// Includes damage values, resource costs, and special conditions
        /// Used for tooltips and card information display
        /// </summary>
        [Export] public string description;
        
        #endregion
        
        #region Resource Requirements System
        
        /// <summary>
        /// Resource costs required to use this card's active effect
        /// Uses Godot.Collections.Dictionary for proper serialization support
        /// 
        /// Supported resource types:
        /// - "MP": Mana Points (magical abilities)
        /// - "EP": Energy Points (physical abilities) 
        /// - "UP": Ultimate Points (ultimate abilities)
        /// 
        /// Note: Requirements only apply to active card usage, not normal attacks
        /// </summary>
        [Export] public Godot.Collections.Dictionary<string, int> requirements = new();
        
        #endregion
        
        #region Swift Card System
        
        /// <summary>
        /// Determines if this card can be used as a Swift Action
        /// Swift Cards can be used multiple times per turn without consuming actions
        /// 
        /// Future implementation will make all Potion cards Swift by default
        /// Reserved for special abilities that don't follow normal action economy
        /// </summary>
        [Export] public bool isSwift;
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Converts this CardData resource into a functional Card instance
        /// Bridges the gap between Godot's resource system and game logic
        /// Would require effect assignment from external sources
        /// </summary>
        /// <returns>Card instance with properties copied from this resource</returns>
        public Card ToCard() {
            var card = new Card {
                Id = id,
                Name = name,
                Type = type,
                Description = description,
                IsSwift = isSwift
            };
            
            // Convert Godot dictionary to standard dictionary
            foreach (var kvp in requirements) {
                card.Requirements[kvp.Key] = kvp.Value;
            }
            
            // Note: Effect would need to be assigned separately as it cannot be serialized
            return card;
        }
        
        /// <summary>
        /// Validates that this CardData has all required fields properly set
        /// Used for resource validation and debugging
        /// </summary>
        /// <returns>True if all essential fields are valid, false otherwise</returns>
        public bool IsValid() {
            return !string.IsNullOrEmpty(id) && 
                   !string.IsNullOrEmpty(name) && 
                   type != Card.TYPE.NONE;
        }
        
        #endregion
    }
    
    #endregion
}