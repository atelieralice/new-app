using meph;
using Godot;

namespace meph {
    public static class CharacterLogic {
        public static void EquipCardToSlot ( Character character, Card card ) {
            // Null checks
            if ( card == null || character == null ) return;
            if ( character.EquippedSlots.TryGetValue ( card.Type, out Card value ) && value != null ) {
                ConsoleLog.Warn ( $"Slot {card.Type} is already occupied." );
                return;
            }
            // Equip the card to the slot
            character.EquippedSlots[card.Type] = card;
            ConsoleLog.Info ( $"{character.CharName} equipped {card.Name} to {card.Type} slot." );
        }

        public static void UseSlot ( Character character, Card.TYPE slotType, Character user, Character target ) {
            if ( character.EquippedSlots.TryGetValue ( slotType, out Card card ) && card != null ) {
                // Execute the card's effect
                card.Effect?.Invoke ( user, target );
                ConsoleLog.Info ( $"{user.CharName} used {card.Name} from {slotType} slot on {target.CharName}." );
            } else {
                ConsoleLog.Warn ( $"No card equipped in {slotType} slot." );
            }
        }
    }
}