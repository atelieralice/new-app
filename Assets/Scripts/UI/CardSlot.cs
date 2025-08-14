using Godot;

namespace meph
{
    public partial class CardSlot : Control
    {
        public enum SlotType
        {
            // Skill slots
            SkillQ, SkillW, SkillE,
            // Weapon slots  
            BaseWeapon, SecondaryWeapon,
            // Charm slots
            CharmHelmet, CharmArmor, CharmGloves, CharmBoots, CharmGlow,
            // Special slots
            Ultimate, Character, Potion
        }

        [Export] public SlotType Type;
        [Export] private Label slotLabel;
        [Export] private Control dropZone;
        [Export] private Control equippedCardDisplay;
        
        public Resource EquippedResource { get; private set; }
        private DraggableCard equippedCardUI;

        public override void _Ready()
        {
            UpdateSlotLabel();
            UpdateUI();
        }

        private void UpdateSlotLabel()
        {
            if (slotLabel != null)
            {
                slotLabel.Text = Type switch
                {
                    SlotType.SkillQ => "Q",
                    SlotType.SkillW => "W", 
                    SlotType.SkillE => "E",
                    SlotType.BaseWeapon => "BW",
                    SlotType.SecondaryWeapon => "SW",
                    SlotType.CharmHelmet => "H",
                    SlotType.CharmArmor => "A",
                    SlotType.CharmGloves => "G",
                    SlotType.CharmBoots => "B",
                    SlotType.CharmGlow => "Gl",
                    SlotType.Ultimate => "U",
                    SlotType.Character => "C",
                    SlotType.Potion => "P",
                    _ => "?"
                };
            }
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.VariantType == Variant.Type.Object && data.AsGodotObject() is Resource resource)
            {
                // Check charm slots
                if (resource is CharmData charmData)
                {
                    return Type switch
                    {
                        SlotType.CharmHelmet => charmData.slot == CharmSlot.HELMET,
                        SlotType.CharmArmor => charmData.slot == CharmSlot.ARMOR,
                        SlotType.CharmGloves => charmData.slot == CharmSlot.GLOVES,
                        SlotType.CharmBoots => charmData.slot == CharmSlot.BOOTS,
                        SlotType.CharmGlow => charmData.slot == CharmSlot.GLOW,
                        _ => false
                    };
                }
                
                // Check card slots
                if (resource is CardData cardData)
                {
                    return Type switch
                    {
                        SlotType.SkillQ => cardData.type == Card.TYPE.Q,
                        SlotType.SkillW => cardData.type == Card.TYPE.W,
                        SlotType.SkillE => cardData.type == Card.TYPE.E,
                        SlotType.BaseWeapon => cardData.type == Card.TYPE.BW,
                        SlotType.SecondaryWeapon => cardData.type == Card.TYPE.SW,
                        SlotType.Ultimate => cardData.type == Card.TYPE.U,
                        SlotType.Character => cardData.type == Card.TYPE.C,
                        SlotType.Potion => cardData.type == Card.TYPE.P,
                        _ => false
                    };
                }
            }
            return false;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (data.AsGodotObject() is Resource resource)
            {
                // Unequip previous resource if any
                if (EquippedResource != null)
                {
                    UnequipResource();
                }
                
                EquippedResource = resource;
                EquipResource();
                UpdateUI();
                
                // Trigger game logic
                var gameManager = GameManager.Instance;
                var currentPlayer = gameManager?.StateManager?.GetPlayer?.Invoke(gameManager.StateManager.CurrentTurn);
                
                if (currentPlayer != null)
                {
                    if (resource is CardData cardData)
                    {
                        // Create actual card and equip it
                        var card = CreateCardFromData(cardData);
                        CharacterLogic.EquipCardToSlot(currentPlayer, card);
                    }
                    else if (resource is CharmData charmData)
                    {
                        // Create charm and equip it
                        var charm = CharmLogic.CreateCharmFromData(charmData);
                        CharmLogic.EquipCharm(currentPlayer, charm);
                    }
                }
            }
        }

        private void EquipResource()
        {
            if (equippedCardDisplay != null && EquippedResource != null)
            {
                // Create a visual representation of the equipped resource
                var cardScene = GD.Load<PackedScene>("res://Scenes/UI/DraggableCard.tscn");
                if (cardScene != null)
                {
                    equippedCardUI = cardScene.Instantiate<DraggableCard>();
                    equippedCardUI.Init(EquippedResource);
                    equippedCardDisplay.AddChild(equippedCardUI);
                    
                    // Make it non-draggable while equipped (optional)
                    equippedCardUI.MouseFilter = Control.MouseFilterEnum.Ignore;
                }
            }
        }

        private void UnequipResource()
        {
            if (equippedCardUI != null)
            {
                equippedCardUI.QueueFree();
                equippedCardUI = null;
            }
            
            // Trigger game logic unequip
            var gameManager = GameManager.Instance;
            var currentPlayer = gameManager?.StateManager?.GetPlayer?.Invoke(gameManager.StateManager.CurrentTurn);
            
            if (currentPlayer != null && EquippedResource is CharmData charmData)
            {
                CharmLogic.UnequipCharm(currentPlayer, charmData.slot);
            }
        }

        private void UpdateUI()
        {
            // Update visual feedback based on equipped state
            if (dropZone != null)
            {
                dropZone.Visible = EquippedResource == null;
            }
            
            if (equippedCardDisplay != null)
            {
                equippedCardDisplay.Visible = EquippedResource != null;
            }
        }

        private Card CreateCardFromData(CardData data)
        {
            // This matches the logic from your GameManager
            var card = new Card
            {
                Id = data.id,
                Name = data.name,
                Type = data.type,
                Description = data.description,
                IsSwift = data.isSwift,
                Requirements = new System.Collections.Generic.Dictionary<string, int>()
            };

            // Convert Godot dictionary to C# dictionary
            foreach (var kvp in data.requirements)
            {
                card.Requirements[kvp.Key] = kvp.Value;
            }

            // Card effects will need to be assigned based on the card ID
            // You might want to use your existing card creation logic from RokCards/YuCards
            AssignCardEffect(card);

            return card;
        }

        private void AssignCardEffect(Card card) {
    var effectCard = AllCards.GetCardById(card.Id);
    if (effectCard != null) {
        card.Effect = effectCard.Effect;
    } else {
        ConsoleLog.Warn($"No effect assigned for card: {card.Id}");
    }
}
    }
}
