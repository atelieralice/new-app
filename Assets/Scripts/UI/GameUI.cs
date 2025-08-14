using Godot;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    [GlobalClass]
    public partial class GameUI : Control {
        // UI Containers
        private Control modeSelectionPanel;
        private Control characterSelectionPanel;
        private Control cardSelectionPanel;
        private Control gamePlayPanel;
        private Control charmSelectionPanel;

        // Mode Selection
        private Button characterBattleModeButton;
        private Button flexibleModeButton;

        // Character Selection
        private VBoxContainer player1CharacterContainer;
        private VBoxContainer player2CharacterContainer;
        private Button startBattleButton;

        // Card Selection (visible during battle)
        private GridContainer availableCardsContainer;
        private GridContainer availableCharmsContainer;

        // Game Play UI
        private Label player1NameLabel;
        private Label player2NameLabel;
        
        // Resource Bars
        private ProgressBar player1LPBar;
        private ProgressBar player2LPBar;
        private ProgressBar player1EPBar;
        private ProgressBar player2EPBar;
        private ProgressBar player1MPBar;
        private ProgressBar player2MPBar;
        
        // Resource Labels
        private Label player1LPLabel;
        private Label player2LPLabel;
        private Label player1EPLabel;
        private Label player2EPLabel;
        private Label player1MPLabel;
        private Label player2MPLabel;
        private Label player1UPLabel;
        private Label player2UPLabel;
        
        private Button endTurnButton;
        private Button normalAttackButton;
        private Button useCardButton;
        private Label currentTurnLabel;
        private Label gameModeLabel;

        // Equipment Display
        private VBoxContainer player1EquipmentContainer;
        private VBoxContainer player2EquipmentContainer;
        private VBoxContainer player1CharmsContainer;
        private VBoxContainer player2CharmsContainer;

        // Current State
        private Character selectedPlayer1;
        private Character selectedPlayer2;

        public override void _Ready() {
            CreateUIStructure();
            SubscribeToEvents();
            ShowModeSelection();
        }

        private void CreateUIStructure() {
            // Set main container properties
            this.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            
            // Create main panels
            CreateModeSelectionPanel();
            CreateCharacterSelectionPanel();
            CreateGamePlayPanel();
            CreateCardSelectionPanel();
            CreateCharmSelectionPanel();
        }

        private void CreateModeSelectionPanel() {
            modeSelectionPanel = new Control();
            modeSelectionPanel.Name = "ModeSelectionPanel";
            modeSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(modeSelectionPanel);

            // Main container for mode selection
            var mainVBox = new VBoxContainer();
            mainVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            modeSelectionPanel.AddChild(mainVBox);

            // Title
            var titleLabel = new Label();
            titleLabel.Text = "SELECT GAME MODE";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            mainVBox.AddChild(titleLabel);

            // Add spacer
            var spacer1 = new Control();
            spacer1.CustomMinimumSize = new Vector2(0, 50);
            mainVBox.AddChild(spacer1);

            // Mode buttons container
            var modeButtonsVBox = new VBoxContainer();
            modeButtonsVBox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            mainVBox.AddChild(modeButtonsVBox);

            // Character Battle Mode button
            characterBattleModeButton = new Button();
            characterBattleModeButton.Text = "CHARACTER BATTLE\n\nCharacters start fully equipped\nwith their signature cards.\nFast-paced combat!";
            characterBattleModeButton.CustomMinimumSize = new Vector2(300, 120);
            characterBattleModeButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            characterBattleModeButton.Pressed += () => OnModeSelected(GameMode.CHARACTER_BATTLE);
            modeButtonsVBox.AddChild(characterBattleModeButton);

            // Add spacer between buttons
            var spacer2 = new Control();
            spacer2.CustomMinimumSize = new Vector2(0, 20);
            modeButtonsVBox.AddChild(spacer2);

            // Flexible Mode button
            flexibleModeButton = new Button();
            flexibleModeButton.Text = "FLEXIBLE MODE\n\nEquip any cards to any character!\nEquipping uses actions.\nStrategic gameplay!";
            flexibleModeButton.CustomMinimumSize = new Vector2(300, 120);
            flexibleModeButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            flexibleModeButton.Pressed += () => OnModeSelected(GameMode.FLEXIBLE_MODE);
            modeButtonsVBox.AddChild(flexibleModeButton);
        }

        private void CreateCharacterSelectionPanel() {
            characterSelectionPanel = new Control();
            characterSelectionPanel.Name = "CharacterSelectionPanel";
            characterSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            characterSelectionPanel.Visible = false;
            AddChild(characterSelectionPanel);

            // Main container for character selection
            var mainVBox = new VBoxContainer();
            mainVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            characterSelectionPanel.AddChild(mainVBox);

            // Title
            var titleLabel = new Label();
            titleLabel.Text = "SELECT CHARACTERS";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            mainVBox.AddChild(titleLabel);

            // Add spacer
            var spacer1 = new Control();
            spacer1.CustomMinimumSize = new Vector2(0, 20);
            mainVBox.AddChild(spacer1);

            // Player selection container
            var playersHBox = new HBoxContainer();
            playersHBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            mainVBox.AddChild(playersHBox);

            // Player 1 section
            var player1VBox = new VBoxContainer();
            player1VBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            playersHBox.AddChild(player1VBox);

            var player1Label = new Label();
            player1Label.Text = "PLAYER 1";
            player1Label.HorizontalAlignment = HorizontalAlignment.Center;
            player1VBox.AddChild(player1Label);

            player1CharacterContainer = new VBoxContainer();
            player1CharacterContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            player1VBox.AddChild(player1CharacterContainer);

            // Separator
            var separator = new VSeparator();
            separator.CustomMinimumSize = new Vector2(20, 0);
            playersHBox.AddChild(separator);

            // Player 2 section
            var player2VBox = new VBoxContainer();
            player2VBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            playersHBox.AddChild(player2VBox);

            var player2Label = new Label();
            player2Label.Text = "PLAYER 2";
            player2Label.HorizontalAlignment = HorizontalAlignment.Center;
            player2VBox.AddChild(player2Label);

            player2CharacterContainer = new VBoxContainer();
            player2CharacterContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            player2VBox.AddChild(player2CharacterContainer);

            // Add spacer
            var spacer2 = new Control();
            spacer2.CustomMinimumSize = new Vector2(0, 20);
            mainVBox.AddChild(spacer2);

            // Start Battle button
            startBattleButton = new Button();
            startBattleButton.Text = "START BATTLE";
            startBattleButton.CustomMinimumSize = new Vector2(200, 50);
            startBattleButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            startBattleButton.Disabled = true;
            startBattleButton.Pressed += OnStartBattlePressed;
            mainVBox.AddChild(startBattleButton);
        }

        private void CreateGamePlayPanel() {
            gamePlayPanel = new Control();
            gamePlayPanel.Name = "GamePlayPanel";
            gamePlayPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            gamePlayPanel.Visible = false;
            AddChild(gamePlayPanel);

            // Main container
            var mainVBox = new VBoxContainer();
            mainVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            gamePlayPanel.AddChild(mainVBox);

            // Game mode indicator
            gameModeLabel = new Label();
            gameModeLabel.Text = "MODE: CHARACTER BATTLE";
            gameModeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            gameModeLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            mainVBox.AddChild(gameModeLabel);

            // Main game container
            var mainHBox = new HBoxContainer();
            mainHBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            mainVBox.AddChild(mainHBox);

            // Player 1 Info Panel
            CreatePlayerInfoPanel(mainHBox, true);

            // Center Action Panel
            CreateActionPanel(mainHBox);

            // Player 2 Info Panel
            CreatePlayerInfoPanel(mainHBox, false);
        }

        private void CreatePlayerInfoPanel(HBoxContainer parent, bool isPlayer1) {
            var playerVBox = new VBoxContainer();
            playerVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            playerVBox.CustomMinimumSize = new Vector2(300, 0);
            parent.AddChild(playerVBox);

            // Player name (initially hidden)
            var nameLabel = new Label();
            nameLabel.Text = isPlayer1 ? "PLAYER 1" : "PLAYER 2";
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.Visible = false; // Hide until character is set
            playerVBox.AddChild(nameLabel);

            if (isPlayer1) {
                player1NameLabel = nameLabel;
            } else {
                player2NameLabel = nameLabel;
            }

            // Resource bars container
            var resourcesVBox = new VBoxContainer();
            resourcesVBox.Visible = false; // Hide until character is set
            playerVBox.AddChild(resourcesVBox);

            // LP (Health) Section
            var lpHBox = new HBoxContainer();
            resourcesVBox.AddChild(lpHBox);

            var lpLabel = new Label();
            lpLabel.Text = "LP: ";
            lpLabel.CustomMinimumSize = new Vector2(40, 0);
            lpHBox.AddChild(lpLabel);

            var lpBar = new ProgressBar();
            lpBar.ShowPercentage = false;
            lpBar.CustomMinimumSize = new Vector2(200, 25);
            lpBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            lpBar.AddThemeColorOverride("fill", Colors.Red);
            lpHBox.AddChild(lpBar);

            var lpValueLabel = new Label();
            lpValueLabel.Text = "0/0";
            lpValueLabel.CustomMinimumSize = new Vector2(80, 0);
            lpHBox.AddChild(lpValueLabel);

            // EP (Energy) Section
            var epHBox = new HBoxContainer();
            resourcesVBox.AddChild(epHBox);

            var epLabel = new Label();
            epLabel.Text = "EP: ";
            epLabel.CustomMinimumSize = new Vector2(40, 0);
            epHBox.AddChild(epLabel);

            var epBar = new ProgressBar();
            epBar.ShowPercentage = false;
            epBar.CustomMinimumSize = new Vector2(200, 20);
            epBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            epBar.AddThemeColorOverride("fill", Colors.Orange);
            epHBox.AddChild(epBar);

            var epValueLabel = new Label();
            epValueLabel.Text = "0/0";
            epValueLabel.CustomMinimumSize = new Vector2(80, 0);
            epHBox.AddChild(epValueLabel);

            // MP (Mana) Section
            var mpHBox = new HBoxContainer();
            resourcesVBox.AddChild(mpHBox);

            var mpLabel = new Label();
            mpLabel.Text = "MP: ";
            mpLabel.CustomMinimumSize = new Vector2(40, 0);
            mpHBox.AddChild(mpLabel);

            var mpBar = new ProgressBar();
            mpBar.ShowPercentage = false;
            mpBar.CustomMinimumSize = new Vector2(200, 20);
            mpBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            mpBar.AddThemeColorOverride("fill", Colors.Blue);
            mpHBox.AddChild(mpBar);

            var mpValueLabel = new Label();
            mpValueLabel.Text = "0/0";
            mpValueLabel.CustomMinimumSize = new Vector2(80, 0);
            mpHBox.AddChild(mpValueLabel);

            // UP (Ultimate) Section
            var upLabel = new Label();
            upLabel.Text = "UP: 0/0";
            upLabel.AddThemeColorOverride("font_color", Colors.Gold);
            resourcesVBox.AddChild(upLabel);

            // Store references
            if (isPlayer1) {
                player1LPBar = lpBar;
                player1EPBar = epBar;
                player1MPBar = mpBar;
                player1LPLabel = lpValueLabel;
                player1EPLabel = epValueLabel;
                player1MPLabel = mpValueLabel;
                player1UPLabel = upLabel;
            } else {
                player2LPBar = lpBar;
                player2EPBar = epBar;
                player2MPBar = mpBar;
                player2LPLabel = lpValueLabel;
                player2EPLabel = epValueLabel;
                player2MPLabel = mpValueLabel;
                player2UPLabel = upLabel;
            }

            // Equipment section (initially hidden)
            var equipmentSection = new VBoxContainer();
            equipmentSection.Visible = false;
            playerVBox.AddChild(equipmentSection);

            var equipmentLabel = new Label();
            equipmentLabel.Text = "EQUIPMENT:";
            equipmentLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            equipmentSection.AddChild(equipmentLabel);

            var equipmentScrollContainer = new ScrollContainer();
            equipmentScrollContainer.CustomMinimumSize = new Vector2(0, 150);
            equipmentScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            equipmentSection.AddChild(equipmentScrollContainer);

            var equipmentContainer = new VBoxContainer();
            equipmentScrollContainer.AddChild(equipmentContainer);

            // Charms section (initially hidden)
            var charmsLabel = new Label();
            charmsLabel.Text = "CHARMS:";
            charmsLabel.AddThemeColorOverride("font_color", Colors.Cyan);
            equipmentSection.AddChild(charmsLabel);

            var charmsScrollContainer = new ScrollContainer();
            charmsScrollContainer.CustomMinimumSize = new Vector2(0, 100);
            charmsScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            equipmentSection.AddChild(charmsScrollContainer);

            var charmsContainer = new VBoxContainer();
            charmsScrollContainer.AddChild(charmsContainer);

            if (isPlayer1) {
                player1EquipmentContainer = equipmentContainer;
                player1CharmsContainer = charmsContainer;
            } else {
                player2EquipmentContainer = equipmentContainer;
                player2CharmsContainer = charmsContainer;
            }

            // Show equipment section when character is set
            if (isPlayer1) {
                player1NameLabel.VisibilityChanged += () => {
                    if (player1NameLabel.Visible) {
                        resourcesVBox.Visible = true;
                        equipmentSection.Visible = true;
                    }
                };
            } else {
                player2NameLabel.VisibilityChanged += () => {
                    if (player2NameLabel.Visible) {
                        resourcesVBox.Visible = true;
                        equipmentSection.Visible = true;
                    }
                };
            }
        }

        private void CreateActionPanel(HBoxContainer parent) {
            var actionVBox = new VBoxContainer();
            actionVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            actionVBox.CustomMinimumSize = new Vector2(250, 0);
            parent.AddChild(actionVBox);

            // Current turn indicator
            currentTurnLabel = new Label();
            currentTurnLabel.Text = "WAITING FOR GAME TO START";
            currentTurnLabel.HorizontalAlignment = HorizontalAlignment.Center;
            actionVBox.AddChild(currentTurnLabel);

            // Add spacer
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 20);
            actionVBox.AddChild(spacer);

            // Action buttons
            normalAttackButton = new Button();
            normalAttackButton.Text = "NORMAL ATTACK";
            normalAttackButton.CustomMinimumSize = new Vector2(0, 40);
            normalAttackButton.Pressed += OnNormalAttackPressed;
            normalAttackButton.Disabled = true;
            actionVBox.AddChild(normalAttackButton);

            useCardButton = new Button();
            useCardButton.Text = "USE EQUIPPED CARDS";
            useCardButton.CustomMinimumSize = new Vector2(0, 40);
            useCardButton.Pressed += OnUseCardPressed;
            useCardButton.Disabled = true;
            actionVBox.AddChild(useCardButton);

            endTurnButton = new Button();
            endTurnButton.Text = "END TURN";
            endTurnButton.CustomMinimumSize = new Vector2(0, 40);
            endTurnButton.Pressed += OnEndTurnPressed;
            endTurnButton.Disabled = true;
            actionVBox.AddChild(endTurnButton);
        }

        private void CreateCardSelectionPanel() {
            cardSelectionPanel = new Control();
            cardSelectionPanel.Name = "CardSelectionPanel";
            cardSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
            cardSelectionPanel.OffsetTop = -200; // Take bottom 200px
            cardSelectionPanel.Visible = false;
            AddChild(cardSelectionPanel);

            var cardVBox = new VBoxContainer();
            cardVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            cardSelectionPanel.AddChild(cardVBox);

            var cardLabel = new Label();
            cardLabel.Text = "AVAILABLE CARDS";
            cardLabel.HorizontalAlignment = HorizontalAlignment.Center;
            cardLabel.AddThemeColorOverride("font_color", Colors.White);
            cardVBox.AddChild(cardLabel);

            var cardScrollContainer = new ScrollContainer();
            cardScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            cardVBox.AddChild(cardScrollContainer);

            availableCardsContainer = new GridContainer();
            availableCardsContainer.Columns = 5;
            cardScrollContainer.AddChild(availableCardsContainer);
        }

        private void CreateCharmSelectionPanel() {
            charmSelectionPanel = new Control();
            charmSelectionPanel.Name = "CharmSelectionPanel";
            charmSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
            charmSelectionPanel.OffsetLeft = -250; // Take right 250px
            charmSelectionPanel.Visible = false;
            AddChild(charmSelectionPanel);

            var charmVBox = new VBoxContainer();
            charmVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            charmSelectionPanel.AddChild(charmVBox);

            var charmLabel = new Label();
            charmLabel.Text = "CHARMS";
            charmLabel.HorizontalAlignment = HorizontalAlignment.Center;
            charmLabel.AddThemeColorOverride("font_color", Colors.White);
            charmVBox.AddChild(charmLabel);

            var charmScrollContainer = new ScrollContainer();
            charmScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            charmVBox.AddChild(charmScrollContainer);

            availableCharmsContainer = new GridContainer();
            availableCharmsContainer.Columns = 1;
            charmScrollContainer.AddChild(availableCharmsContainer);
        }

        private void SubscribeToEvents() {
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            GameEvents.OnPlayersSet += OnPlayersSet;
            GameEvents.OnTurnStarted += OnTurnStarted;
            GameEvents.OnResourceGained += OnResourceChanged;
            GameEvents.OnResourceLost += OnResourceChanged;
            GameEvents.OnCardEquipped += OnCardEquipped;
            GameEvents.OnCharmEquipped += OnCharmEquipped;
            GameEvents.OnDamageDealt += OnDamageDealt;
            GameEvents.OnActionsChanged += OnActionsChanged; // This should trigger the update
            GameEvents.OnPlayerVictory += OnPlayerVictory;
            GameEvents.OnGameEnded += OnGameEnded;
        }

        // Mode Selection
        private void ShowModeSelection() {
            modeSelectionPanel.Visible = true;
            characterSelectionPanel.Visible = false;
            cardSelectionPanel.Visible = false;
            gamePlayPanel.Visible = false;
            charmSelectionPanel.Visible = false;
        }

        private void OnModeSelected(GameMode mode) {
            GameManager.Instance.SetGameMode(mode);
        }

        // Character Selection
        private void ShowCharacterSelection() {
            modeSelectionPanel.Visible = false;
            characterSelectionPanel.Visible = true;
            cardSelectionPanel.Visible = false;
            gamePlayPanel.Visible = false;
            charmSelectionPanel.Visible = false;
            PopulateCharacterSelection();
        }

        private void PopulateCharacterSelection() {
            var characters = CreateAvailableCharacters();

            foreach (var characterData in characters) {
                // Player 1 character button
                var p1Button = CreateCharacterButton(characterData, 1);
                player1CharacterContainer.AddChild(p1Button);

                // Player 2 character button
                var p2Button = CreateCharacterButton(characterData, 2);
                player2CharacterContainer.AddChild(p2Button);
            }
        }

        private Button CreateCharacterButton(CharacterData data, int player) {
            var button = new Button();
            button.Text = $"{data.charName}\nLP: {data.maxLP}\nEP: {data.maxEP}\nMP: {data.maxMP}";
            button.CustomMinimumSize = new Vector2(150, 100);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.Pressed += () => OnCharacterSelected(data, player);
            return button;
        }

        private void OnCharacterSelected(CharacterData data, int player) {
            var character = CharacterCreator.InitCharacter(data);

            if (player == 1) {
                selectedPlayer1 = character;
                ConsoleLog.Info($"Player 1 selected: {character.CharName}");
                UpdateCharacterSelectionFeedback(player1CharacterContainer, data.charName);
            } else {
                selectedPlayer2 = character;
                ConsoleLog.Info($"Player 2 selected: {character.CharName}");
                UpdateCharacterSelectionFeedback(player2CharacterContainer, data.charName);
            }

            // Enable start battle button if both players selected
            if (selectedPlayer1 != null && selectedPlayer2 != null) {
                startBattleButton.Disabled = false;
                startBattleButton.Text = $"START BATTLE: {selectedPlayer1.CharName} vs {selectedPlayer2.CharName}";
            }
        }

        private void UpdateCharacterSelectionFeedback(VBoxContainer container, string selectedCharName) {
            foreach (Node child in container.GetChildren()) {
                if (child is Button button) {
                    if (button.Text.Contains(selectedCharName)) {
                        button.Modulate = Colors.Yellow;
                        button.Disabled = true;
                    } else {
                        button.Modulate = Colors.White;
                        button.Disabled = false;
                    }
                }
            }
        }

        private void OnStartBattlePressed() {
            if (selectedPlayer1 == null || selectedPlayer2 == null) return;

            EquipCharacterCards();
            GameManager.Instance.SetPlayers(selectedPlayer1, selectedPlayer2);
        }

        private void EquipCharacterCards() {
            // Always equip character cards first
            var rokCard = AllCards.GetCharacterCard("Rok");
            var yuCard = AllCards.GetCharacterCard("Yu");

            if (selectedPlayer1.CharName == "Rok") {
                CharacterLogic.EquipCardToSlot(selectedPlayer1, rokCard);
                if (GameManager.Instance.IsCharacterBattleMode()) {
                    EquipRokBasicCards(selectedPlayer1);
                }
            } else if (selectedPlayer1.CharName == "Yu") {
                CharacterLogic.EquipCardToSlot(selectedPlayer1, yuCard);
                if (GameManager.Instance.IsCharacterBattleMode()) {
                    EquipYuBasicCards(selectedPlayer1);
                }
            }

            if (selectedPlayer2.CharName == "Rok") {
                CharacterLogic.EquipCardToSlot(selectedPlayer2, rokCard);
                if (GameManager.Instance.IsCharacterBattleMode()) {
                    EquipRokBasicCards(selectedPlayer2);
                }
            } else if (selectedPlayer2.CharName == "Yu") {
                CharacterLogic.EquipCardToSlot(selectedPlayer2, yuCard);
                if (GameManager.Instance.IsCharacterBattleMode()) {
                    EquipYuBasicCards(selectedPlayer2);
                }
            }
        }

        private void EquipRokBasicCards(Character character) {
            var rokCards = AllCards.GetCharacterCardSet("Rok").Skip(1).ToList();
            foreach (var card in rokCards) {
                CharacterLogic.EquipCardToSlot(character, card);
            }
        }

        private void EquipYuBasicCards(Character character) {
            var yuCards = AllCards.GetCharacterCardSet("Yu").Skip(1).ToList();
            foreach (var card in yuCards) {
                CharacterLogic.EquipCardToSlot(character, card);
            }
        }

        // Game Play UI
        private void ShowGamePlay() {
            modeSelectionPanel.Visible = false;
            characterSelectionPanel.Visible = false;
            cardSelectionPanel.Visible = true;
            gamePlayPanel.Visible = true;
            charmSelectionPanel.Visible = true;

            SetupGamePlayUI();
            PopulateAvailableCards();
            PopulateAvailableCharms();
        }

        private void SetupGamePlayUI() {
            if (GameManager.Instance.Attacker != null && GameManager.Instance.Defender != null) {
                // Show character names and make UI visible
                player1NameLabel.Text = $"PLAYER 1: {GameManager.Instance.Attacker.CharName}";
                player1NameLabel.Visible = true;
                
                player2NameLabel.Text = $"PLAYER 2: {GameManager.Instance.Defender.CharName}";
                player2NameLabel.Visible = true;

                // Update game mode display
                string modeText = GameManager.Instance.IsCharacterBattleMode() ? 
                    "MODE: CHARACTER BATTLE" : "MODE: FLEXIBLE";
                gameModeLabel.Text = modeText;
                
                if (GameManager.Instance.IsFlexibleMode()) {
                    gameModeLabel.AddThemeColorOverride("font_color", Colors.Cyan);
                } else {
                    gameModeLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                }

                UpdatePlayerResources(GameManager.Instance.Attacker);
                UpdatePlayerResources(GameManager.Instance.Defender);
                
                // Force equipment display update after a short delay
                CallDeferred(nameof(UpdateEquipmentDisplay));
                
                UpdateCurrentTurnDisplay();
            }
        }

        private void PopulateAvailableCards() {
            foreach (Node child in availableCardsContainer.GetChildren()) {
                child.QueueFree();
            }

            var availableCards = GetAvailableCards();
            foreach (var card in availableCards) {
                var button = CreateCardButton(card);
                availableCardsContainer.AddChild(button);
            }
        }

        private void PopulateAvailableCharms() {
            foreach (Node child in availableCharmsContainer.GetChildren()) {
                child.QueueFree();
            }

            var availableCharms = GetAvailableCharms();
            foreach (var charm in availableCharms) {
                var button = CreateCharmButton(charm);
                availableCharmsContainer.AddChild(button);
            }
        }

        private Button CreateCardButton(Card card) {
            var button = new Button();
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            
            button.Text = $"{card.Name}\n{card.Type}\n{GetShortDescription(card.Description)}";
            button.CustomMinimumSize = new Vector2(160, 90);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            
            // Check if current player can equip this card (Q/U restrictions)
            bool canEquip = currentPlayer == null || GameManager.Instance.CanCharacterEquipCard(currentPlayer, card);
            
            if (!canEquip) {
                button.Modulate = Colors.Gray;
                button.Disabled = true;
                button.Text += "\n(RESTRICTED)";
            } else {
                button.Pressed += () => OnCardSelected(card);
            }
            
            // Show action cost in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode() && canEquip) {
                button.Text += "\n[Action Cost: 1]";
            }
            
            return button;
        }

        private Button CreateCharmButton(Charm charm) {
            var button = new Button();
            button.Text = $"{charm.Name}\n{charm.Slot}";
            button.CustomMinimumSize = new Vector2(200, 70);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.Pressed += () => OnCharmSelected(charm);
            
            // Show action cost in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                button.Text += "\n[Action Cost: 1]";
            }
            
            return button;
        }

        private string GetShortDescription(string description) {
            if (string.IsNullOrEmpty(description)) return "";
            return description.Length > 40 ? description.Substring(0, 37) + "..." : description;
        }

        private void OnCardSelected(Card card) {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                GameManager.Instance.EquipCard(currentPlayer, card);
                
                // Refresh available cards to update action costs and restrictions
                if (GameManager.Instance.IsFlexibleMode()) {
                    PopulateAvailableCards();
                }
            }
        }

        private void OnCharmSelected(Charm charm) {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                GameManager.Instance.EquipCharm(currentPlayer, charm);
                
                // Refresh available charms in Flexible Mode
                if (GameManager.Instance.IsFlexibleMode()) {
                    PopulateAvailableCharms();
                }
            }
        }

        private void OnEndTurnPressed() {
            GameManager.Instance.EndTurn();
        }

        private void OnNormalAttackPressed() {
            var attacker = GameManager.Instance.GetCurrentPlayer();
            var target = GameManager.Instance.GetOpponent(attacker);

            if (attacker != null && target != null) {
                GameManager.Instance.PerformNormalAttack(target);
            }
        }

        private void OnUseCardPressed() {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer == null) return;

            ShowEquippedCardsPopup(currentPlayer);
        }

        private void ShowEquippedCardsPopup(Character character) {
            var popup = new AcceptDialog();
            popup.Title = $"Use {character.CharName}'s Cards";
            popup.Size = new Vector2I(450, 350);

            var vbox = new VBoxContainer();
            popup.AddChild(vbox);

            bool hasUsableCards = false;

            foreach (var kvp in character.EquippedSlots) {
                if (kvp.Value != null && kvp.Key != Card.TYPE.C) {
                    hasUsableCards = true;
                    var button = new Button();
                    button.Text = $"Use {kvp.Value.Name} ({kvp.Key})";
                    
                    if (kvp.Value.IsFrozen) {
                        button.Text += " (FROZEN)";
                        button.Disabled = true;
                        button.Modulate = Colors.LightBlue;
                    } else {
                        var cardType = kvp.Key;
                        button.Pressed += () => {
                            var target = GameManager.Instance.GetOpponent(character);
                            GameManager.Instance.UseCard(cardType, target);
                            popup.QueueFree();
                        };
                    }
                    
                    vbox.AddChild(button);
                }
            }

            if (!hasUsableCards) {
                var noCardsLabel = new Label();
                noCardsLabel.Text = "No equipped cards available to use.";
                noCardsLabel.HorizontalAlignment = HorizontalAlignment.Center;
                vbox.AddChild(noCardsLabel);
            }

            GetViewport().AddChild(popup);
            popup.PopupCentered();
        }

        // Event Handlers
        private void OnGamePhaseChanged(string phase) {
            if (phase == "CHARACTER_SELECTION") {
                ShowCharacterSelection();
            } else if (phase == "BATTLE") {
                ShowGamePlay();
            }
        }

        private void OnPlayersSet(Character attacker, Character defender) {
            ConsoleLog.Info($"Players set: {attacker.CharName} vs {defender.CharName}");
        }

        private void OnTurnStarted(Character character) {
            UpdatePlayerResources(character);
            UpdateActionButtons();
            UpdateCurrentTurnDisplay(); // This will now show the correct actions
            
            // Refresh card/charm availability in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                PopulateAvailableCards();
                PopulateAvailableCharms();
            }
        }

        private void OnResourceChanged(Character character, int amount, string type) {
            UpdatePlayerResources(character);
        }

        private void OnCardEquipped(Character character, Card card) {
            UpdateEquipmentDisplay();
            
            // Refresh available cards in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                PopulateAvailableCards();
            }
        }

        private void OnCharmEquipped(Character character, Charm charm) {
            UpdateEquipmentDisplay();
            
            // Refresh available charms in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                PopulateAvailableCharms();
            }
        }

        private void OnDamageDealt(Character target, int damage, int remainingLP) {
            UpdatePlayerResources(target);
            ShowDamageIndicator(target, damage);
        }

        private void ShowDamageIndicator(Character target, int damage) {
            // Determine which health bar to align with
            ProgressBar targetLPBar = null;
            if (target == GameManager.Instance.Attacker) {
                targetLPBar = player1LPBar;
            } else if (target == GameManager.Instance.Defender) {
                targetLPBar = player2LPBar;
            }

            if (targetLPBar == null) return;

            var damageLabel = new Label();
            damageLabel.Text = $"-{damage}";
            damageLabel.Modulate = Colors.Red;
            damageLabel.AddThemeColorOverride("font_color", Colors.Red);
            damageLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            
            // Position near the health bar
            var barGlobalPos = targetLPBar.GlobalPosition;
            var barSize = targetLPBar.Size;
            
            damageLabel.Position = new Vector2(
                barGlobalPos.X + barSize.X / 2 - 25,
                barGlobalPos.Y - 30
            );
            
            AddChild(damageLabel);

            // Animate damage indicator
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(damageLabel, "modulate:a", 0.0f, 1.5f);
            tween.TweenProperty(damageLabel, "position:y", damageLabel.Position.Y - 60, 1.5f);
            tween.TweenCallback(Callable.From(() => damageLabel.QueueFree()));
        }

        private void OnActionsChanged(int remaining) {
            UpdateActionButtons();
            UpdateCurrentTurnDisplay(); // Force immediate display update
            
            // Refresh equipment options in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                PopulateAvailableCards();
                PopulateAvailableCharms();
            }
            
            ConsoleLog.Info($"Actions updated: {remaining} remaining"); // Debug log
        }

        private void OnPlayerVictory(Character winner) {
            ShowVictoryScreen(winner);
        }

        private void OnGameEnded() {
            // Additional cleanup if needed
        }

        private void ShowVictoryScreen(Character winner) {
            var victoryPopup = new AcceptDialog();
            victoryPopup.Title = "GAME OVER";
            victoryPopup.DialogText = $"{winner.CharName} WINS!";
            victoryPopup.Size = new Vector2I(300, 150);
            
            var restartButton = new Button();
            restartButton.Text = "RESTART GAME";
            restartButton.Pressed += () => {
                RestartGame();
                victoryPopup.QueueFree();
            };
            
            victoryPopup.AddChild(restartButton);
            GetViewport().AddChild(victoryPopup);
            victoryPopup.PopupCentered();
        }

        private void RestartGame() {
            // Clear selections
            selectedPlayer1 = null;
            selectedPlayer2 = null;
            
            // Hide character UI elements
            player1NameLabel.Visible = false;
            player2NameLabel.Visible = false;
            
            // Clear character containers
            foreach (Node child in player1CharacterContainer.GetChildren()) {
                child.QueueFree();
            }
            foreach (Node child in player2CharacterContainer.GetChildren()) {
                child.QueueFree();
            }
            
            // Reset button
            startBattleButton.Disabled = true;
            startBattleButton.Text = "START BATTLE";
            
            // Restart game manager
            GameManager.Instance.RestartGame();
        }

        // Update Methods
        private void UpdatePlayerResources(Character character) {
            if (character == GameManager.Instance.Attacker) {
                // Update LP
                player1LPBar.Value = (float)character.LP / character.MaxLP * 100;
                player1LPLabel.Text = $"{character.LP}/{character.MaxLP}";
                
                // Update EP
                player1EPBar.Value = (float)character.EP / character.MaxEP * 100;
                player1EPLabel.Text = $"{character.EP}/{character.MaxEP}";
                
                // Update MP
                player1MPBar.Value = (float)character.MP / character.MaxMP * 100;
                player1MPLabel.Text = $"{character.MP}/{character.MaxMP}";
                
                // Update UP
                player1UPLabel.Text = $"UP: {character.UP}/{character.MaxUP}";
                
            } else if (character == GameManager.Instance.Defender) {
                // Update LP
                player2LPBar.Value = (float)character.LP / character.MaxLP * 100;
                player2LPLabel.Text = $"{character.LP}/{character.MaxLP}";
                
                // Update EP
                player2EPBar.Value = (float)character.EP / character.MaxEP * 100;
                player2EPLabel.Text = $"{character.EP}/{character.MaxEP}";
                
                // Update MP
                player2MPBar.Value = (float)character.MP / character.MaxMP * 100;
                player2MPLabel.Text = $"{character.MP}/{character.MaxMP}";
                
                // Update UP
                player2UPLabel.Text = $"UP: {character.UP}/{character.MaxUP}";
            }
        }

        private void UpdateCurrentTurnDisplay() {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                string actionsText = "";
                
                // In Flexible Mode, show actions remaining
                if (GameManager.Instance.IsFlexibleMode()) {
                    int actionsRemaining = GameManager.Instance.StateManager?.ActionsRemaining ?? 0;
                    actionsText = $" (Actions: {actionsRemaining})";
                }
                
                currentTurnLabel.Text = $"CURRENT TURN: {currentPlayer.CharName}{actionsText}";
                currentTurnLabel.Modulate = currentPlayer == GameManager.Instance.Attacker ? Colors.Yellow : Colors.Cyan;
            }
        }

        private void UpdateActionButtons() {
            var canAct = GameManager.Instance.StateManager?.CanAct() ?? false;
            var gameInProgress = GameManager.Instance.CanPerformActions();
            
            endTurnButton.Disabled = !gameInProgress;
            normalAttackButton.Disabled = !canAct || !gameInProgress;
            useCardButton.Disabled = !canAct || !gameInProgress;
        }

        private void UpdateEquipmentDisplay() {
            UpdatePlayerEquipment(GameManager.Instance.Attacker, player1EquipmentContainer, player1CharmsContainer);
            UpdatePlayerEquipment(GameManager.Instance.Defender, player2EquipmentContainer, player2CharmsContainer);
        }

        private void UpdatePlayerEquipment(Character character, VBoxContainer equipmentContainer, VBoxContainer charmsContainer) {
            if (character == null) return;

            // Clear existing displays
            foreach (Node child in equipmentContainer.GetChildren()) {
                child.QueueFree();
            }
            foreach (Node child in charmsContainer.GetChildren()) {
                child.QueueFree();
            }

            // Show equipped cards with better formatting
            foreach (var kvp in character.EquippedSlots) {
                if (kvp.Value != null) {
                    var cardPanel = new Panel();
                    cardPanel.CustomMinimumSize = new Vector2(0, 40);
                    
                    var cardLabel = new Label();
                    cardLabel.Text = $"{kvp.Key}: {kvp.Value.Name}";
                    cardLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    cardLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                    cardLabel.OffsetLeft = 5;
                    cardLabel.OffsetRight = -5;
                    cardLabel.OffsetTop = 5;
                    cardLabel.OffsetBottom = -5;
                    
                    if (kvp.Value.IsFrozen) {
                        cardPanel.Modulate = Colors.LightBlue;
                        cardLabel.Text += " (FROZEN)";
                        cardLabel.AddThemeColorOverride("font_color", Colors.Blue);
                    } else {
                        cardPanel.Modulate = Colors.White;
                        cardLabel.AddThemeColorOverride("font_color", Colors.Black);
                    }
                    
                    cardPanel.AddChild(cardLabel);
                    equipmentContainer.AddChild(cardPanel);
                }
            }

            // Show equipped charms with better formatting
            foreach (var kvp in character.EquippedCharms) {
                if (kvp.Value != null) {
                    var charmPanel = new Panel();
                    charmPanel.CustomMinimumSize = new Vector2(0, 30);
                    
                    var charmLabel = new Label();
                    charmLabel.Text = $"{kvp.Key}: {kvp.Value.Name}";
                    charmLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    charmLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                    charmLabel.OffsetLeft = 5;
                    charmLabel.OffsetRight = -5;
                    charmLabel.OffsetTop = 5;
                    charmLabel.OffsetBottom = -5;
                    charmLabel.AddThemeColorOverride("font_color", Colors.Cyan);
                    
                    charmPanel.Modulate = Colors.DarkCyan;
                    charmPanel.AddChild(charmLabel);
                    charmsContainer.AddChild(charmPanel);
                }
            }

            // Show empty slots in Flexible Mode
            if (GameManager.Instance.IsFlexibleMode()) {
                // Show empty card slots
                var allSlotTypes = new[] { Card.TYPE.BW, Card.TYPE.SW, Card.TYPE.E, Card.TYPE.W, Card.TYPE.Q, Card.TYPE.P, Card.TYPE.U };
                foreach (var slotType in allSlotTypes) {
                    if (!character.EquippedSlots.ContainsKey(slotType) || character.EquippedSlots[slotType] == null) {
                        var emptyPanel = new Panel();
                        emptyPanel.CustomMinimumSize = new Vector2(0, 30);
                        emptyPanel.Modulate = Colors.Gray;
                        
                        var emptyLabel = new Label();
                        emptyLabel.Text = $"{slotType}: [EMPTY]";
                        emptyLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                        emptyLabel.OffsetLeft = 5;
                        emptyLabel.OffsetRight = -5;
                        emptyLabel.OffsetTop = 5;
                        emptyLabel.OffsetBottom = -5;
                        emptyLabel.AddThemeColorOverride("font_color", Colors.LightGray);
                        
                        emptyPanel.AddChild(emptyLabel);
                        equipmentContainer.AddChild(emptyPanel);
                    }
                }
            }
        }

        // Data Creation Methods
        private List<CharacterData> CreateAvailableCharacters() {
            var characters = new List<CharacterData>();

            // Rok
            var rokData = new CharacterData();
            rokData.charName = "Rok";
            rokData.star = Character.STAR.FIVE;
            rokData.essenceType = Character.ESSENCE_TYPE.FIRE;
            rokData.weaponType = Character.WEAPON_TYPE.MAGIC;
            rokData.maxLP = 8750;
            rokData.maxEP = 300;
            rokData.maxMP = 460;
            rokData.maxUP = 2;
            rokData.maxPotion = 1;
            characters.Add(rokData);

            // Yu
            var yuData = new CharacterData();
            yuData.charName = "Yu";
            yuData.star = Character.STAR.FIVE;
            yuData.essenceType = Character.ESSENCE_TYPE.ICE;
            yuData.weaponType = Character.WEAPON_TYPE.SWORD;
            yuData.maxLP = 14000;
            yuData.maxEP = 600;
            yuData.maxMP = 600;
            yuData.maxUP = 1;
            yuData.maxPotion = 1;
            characters.Add(yuData);

            return characters;
        }

        private List<Card> GetAvailableCards() {
            if (GameManager.Instance.IsFlexibleMode()) {
                // In flexible mode, show all character cards
                var allCards = new List<Card>();
                allCards.AddRange(AllCards.GetCharacterCardSet("Rok").Skip(1)); // Skip character card
                allCards.AddRange(AllCards.GetCharacterCardSet("Yu").Skip(1)); // Skip character card
                allCards.AddRange(AllCards.GetPotionCards());
                return allCards;
            } else {
                // In character battle mode, only show potion cards
                return AllCards.GetPotionCards();
            }
        }

        private List<Charm> GetAvailableCharms() {
            var charms = new List<Charm>();
            charms.AddRange(CharmLogic.PresetCharms.CreateRokCharms());
            charms.AddRange(CharmLogic.PresetCharms.CreateYuCharms());
            return charms;
        }
    }
}