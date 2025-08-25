using meph;
using Godot;

namespace meph {
    public static class CharacterLogic {
        public static void EquipCardToSlot ( Character character, Card card ) {
            // Null checks
            if ( card == null || character == null ) return;

            // Restrict Q/U cards to their owner
            // If the OwnerCharacter is null the card can be equipped by anyone regardless of its type
            if ( ( card.Type == Card.TYPE.Q || card.Type == Card.TYPE.U ) && !string.IsNullOrEmpty ( card.OwnerCharacter ) ) {
                if ( character.CharName != card.OwnerCharacter ) {
                    ConsoleLog.Warn ( $"{character.CharName} cannot equip {card.Name} (only {card.OwnerCharacter} can equip this card)." );
                    return;
                }
            }

            // Check if the slot is already occupied
            if ( character.EquippedSlots.TryGetValue ( card.Type, out Card value ) && value != null ) {
                ConsoleLog.Warn ( $"Slot {card.Type} is already occupied." );
                return;
            }

            // Equip the card to the slot
            character.EquippedSlots[card.Type] = card;
            ConsoleLog.Info ( $"{character.CharName} equipped {card.Name} to {card.Type} slot." );
        }

        public static void UseSlot ( FactorManager fm, Card.TYPE slotType, Character user, Character target ) {
            // Check and get equipped card
            if ( user.EquippedSlots.TryGetValue ( slotType, out Card card ) && card != null ) {
                // Execute the card's effect
                card.Effect?.Invoke ( fm, user, target );
                ConsoleLog.Info ( $"{user.CharName} used {card.Name} from {slotType} slot on {target.CharName}." );
            } else {
                ConsoleLog.Warn ( $"No card equipped in {slotType} slot." );
            }
        }
    }
}