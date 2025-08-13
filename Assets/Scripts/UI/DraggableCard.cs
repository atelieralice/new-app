using Godot;
using System;

namespace meph
{
    public partial class DraggableCard : Control
    {
        public Resource CardResource { get; private set; }

        [Export] private TextureRect cardArt;
        [Export] private Label cardNameLabel;
        [Export] private Label cardDescriptionLabel;
        [Export] private Label cardTypeLabel;
        [Export] private Label requirementsLabel;

        public void Init(Resource resource)
        {
            CardResource = resource;

            if (resource is CardData cardData)
            {
                cardNameLabel.Text = cardData.name;
                cardDescriptionLabel.Text = cardData.description;
                cardTypeLabel.Text = cardData.type.ToString();
                
                // Display requirements
                if (cardData.requirements.Count > 0)
                {
                    string reqText = "Req: ";
                    foreach (var req in cardData.requirements)
                    {
                        reqText += $"{req.Key}:{req.Value} ";
                    }
                    requirementsLabel.Text = reqText.Trim();
                }
                else
                {
                    requirementsLabel.Text = "";
                }
                
                // Mark swift cards
                if (cardData.isSwift)
                {
                    cardTypeLabel.Text += " (Swift)";
                }
            }
            else if (resource is CharmData charmData)
            {
                cardNameLabel.Text = charmData.charmName;
                cardDescriptionLabel.Text = charmData.charmDescription;
                cardTypeLabel.Text = $"Charm ({charmData.slot})";
                
                // Display charm bonuses
                string bonusText = "";
                if (charmData.lpBonus > 0) bonusText += $"LP+{charmData.lpBonus} ";
                if (charmData.epBonus > 0) bonusText += $"EP+{charmData.epBonus} ";
                if (charmData.mpBonus > 0) bonusText += $"MP+{charmData.mpBonus} ";
                if (charmData.defBonus > 0) bonusText += $"DEF+{charmData.defBonus} ";
                
                requirementsLabel.Text = bonusText.Trim();
            }
            else
            {
                GD.PrintErr("Unsupported resource: " + resource.GetType());
            }
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            var preview = Duplicate() as Control;
            preview.Modulate = new Color(1, 1, 1, 0.7f);
            SetDragPreview(preview);
            return CardResource;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            return false; // Cards themselves don't accept drops
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            // Not used here
        }
    }
}
