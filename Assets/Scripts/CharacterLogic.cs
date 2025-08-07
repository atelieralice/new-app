using meph;
using Godot;

namespace meph {
    public static class CharacterLogic {
        public static void EquipCardToSlot ( StateManager stateManager, Character character, CardData card ) {
            // Null checks
            if ( card == null || character == null ) return;
            if ( character.EquippedSlots.TryGetValue ( card.type, out CardData value ) && value != null ) {
                GD.Print ( $"Slot {card.type} is already occupied." );
                return;
            }
            // Equip the card to the slot
            character.EquippedSlots[card.type] = card;
            GD.Print ( $"{character.CharName} equipped {card.name} to {card.type} slot." );

            // All logic regarding action locking is moved to StateManager.cs

            // // Lock actions after equipping
            // if ( stateManager != null ) {
            //     stateManager.LockAction ( );
            // }
        }

        public static void UseSlot ( StateManager stateManager, Character character, CardData.TYPE slotType, Character user, Character target ) {
            if ( character.EquippedSlots.TryGetValue ( slotType, out CardData card ) && card != null ) {
                // Execute the card's effect
                card.Effect?.Invoke ( user, target );
                GD.Print ( $"{user.CharName} used {card.name} from {slotType} slot on {target.CharName}." );
            } else {
                GD.Print ( $"No card equipped in {slotType} slot." );
            }
        }
    }
}