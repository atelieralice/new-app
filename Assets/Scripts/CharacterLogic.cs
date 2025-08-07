using meph;
using Godot;

namespace meph {
    public static class CharacterLogic {
        public static void EquipCardToSlot ( CharacterData character, CardData card ) {
            // Check if the slot for the card's type is already occupied
            // TODO: Separate null checks to another function for debugging purposes and future characters
            if ( card == null && character == null ) return;
            if ( character.equippedSlots.TryGetValue ( card.type, out CardData value ) && value != null ) {
                GD.Print ( $"Slot {card.type} is already occupied." );
                return;
            }
            // Equip the card to the slot
            character.equippedSlots[card.type] = card;
            GD.Print($"{character.charName} equipped {card.name} to {card.type} slot.");
        }
        public static void UseSlot ( CharacterData character, CardData.TYPE slotType, Character user, Character target ) {
            
        }

    }
}