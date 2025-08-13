using Godot;
using System.Collections.Generic;
using System.Linq;

namespace meph {
    [GlobalClass]
    public partial class GameUI : Control {
        // UI Containers
        private Control characterSelectionPanel;
        private Control cardSelectionPanel;
        private Control gamePlayPanel;
        private Control charmSelectionPanel;

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
        private ProgressBar player1LPBar;
        private ProgressBar player2LPBar;
        private Label player1ResourcesLabel;
        private Label player2ResourcesLabel;
        private Button endTurnButton;
        private Button normalAttackButton;
        private Button useCardButton;
        private Label currentTurnLabel;

        // Equipment Display
        private GridContainer player1EquipmentContainer;
        private GridContainer player2EquipmentContainer;
        private GridContainer player1CharmsContainer;
        private GridContainer player2CharmsContainer;

        // Current State
        private Character selectedPlayer1;
        private Character selectedPlayer2;
        private Character currentPlayerForEquipment;

        public override void _Ready() {
            CreateUIStructure();
            SubscribeToEvents();
            ShowCharacterSelection();
        }

        private void CreateUIStructure() {
            // Set main container properties
            SetAnchorsAndOffsetsToFullRect(this);
            
            // Create main panels
            CreateCharacterSelectionPanel();
            CreateGamePlayPanel();
            CreateCardSelectionPanel();
            CreateCharmSelectionPanel();
        }

        private void SetAnchorsAndOffsetsToFullRect(Control control) {
            control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }

        private void CreateCharacterSelectionPanel() {
            characterSelectionPanel = new Control();
            characterSelectionPanel.Name = "CharacterSelectionPanel";
            SetAnchorsAndOffsetsToFullRect(characterSelectionPanel);
            AddChild(characterSelectionPanel);

            // Main container for character selection
            var mainVBox = new VBoxContainer();
            SetAnchorsAndOffsetsToFullRect(mainVBox);
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
            SetAnchorsAndOffsetsToFullRect(gamePlayPanel);
            gamePlayPanel.Visible = false;
            AddChild(gamePlayPanel);

            // Main container
            var mainHBox = new HBoxContainer();
            SetAnchorsAndOffsetsToFullRect(mainHBox);
            gamePlayPanel.AddChild(mainHBox);

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
            playerVBox.CustomMinimumSize = new Vector2(250, 0);
            parent.AddChild(playerVBox);

            // Player name
            var nameLabel = new Label();
            nameLabel.Text = isPlayer1 ? "PLAYER 1" : "PLAYER 2";
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            playerVBox.AddChild(nameLabel);

            if (isPlayer1) {
                player1NameLabel = nameLabel;
            } else {
                player2NameLabel = nameLabel;
            }

            // LP Bar
            var lpBar = new ProgressBar();
            lpBar.ShowPercentage = false;
            lpBar.CustomMinimumSize = new Vector2(0, 30);
            playerVBox.AddChild(lpBar);

            if (isPlayer1) {
                player1LPBar = lpBar;
            } else {
                player2LPBar = lpBar;
            }

            // Resources label
            var resourcesLabel = new Label();
            resourcesLabel.Text = "LP: 0/0\nEP: 0/0\nMP: 0/0\nUP: 0/0";
            resourcesLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            playerVBox.AddChild(resourcesLabel);

            if (isPlayer1) {
                player1ResourcesLabel = resourcesLabel;
            } else {
                player2ResourcesLabel = resourcesLabel;
            }

            // Equipment section
            var equipmentLabel = new Label();
            equipmentLabel.Text = "EQUIPMENT:";
            playerVBox.AddChild(equipmentLabel);

            var equipmentContainer = new GridContainer();
            equipmentContainer.Columns = 2;
            equipmentContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            playerVBox.AddChild(equipmentContainer);

            if (isPlayer1) {
                player1EquipmentContainer = equipmentContainer;
            } else {
                player2EquipmentContainer = equipmentContainer;
            }

            // Charms section
            var charmsLabel = new Label();
            charmsLabel.Text = "CHARMS:";
            playerVBox.AddChild(charmsLabel);

            var charmsContainer = new GridContainer();
            charmsContainer.Columns = 2;
            charmsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            playerVBox.AddChild(charmsContainer);

            if (isPlayer1) {
                player1CharmsContainer = charmsContainer;
            } else {
                player2CharmsContainer = charmsContainer;
            }
        }

        private void CreateActionPanel(HBoxContainer parent) {
            var actionVBox = new VBoxContainer();
            actionVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            actionVBox.CustomMinimumSize = new Vector2(200, 0);
            parent.AddChild(actionVBox);

            // Current turn indicator
            currentTurnLabel = new Label();
            currentTurnLabel.Text = "CURRENT TURN: PLAYER 1";
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
            actionVBox.AddChild(normalAttackButton);

            useCardButton = new Button();
            useCardButton.Text = "USE EQUIPPED CARDS";
            useCardButton.CustomMinimumSize = new Vector2(0, 40);
            useCardButton.Pressed += OnUseCardPressed;
            actionVBox.AddChild(useCardButton);

            endTurnButton = new Button();
            endTurnButton.Text = "END TURN";
            endTurnButton.CustomMinimumSize = new Vector2(0, 40);
            endTurnButton.Pressed += OnEndTurnPressed;
            actionVBox.AddChild(endTurnButton);
        }

        private void CreateCardSelectionPanel() {
            cardSelectionPanel = new Control();
            cardSelectionPanel.Name = "CardSelectionPanel";
            cardSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
            // Remove manual position and size - let the preset handle it
            cardSelectionPanel.Visible = false;
            AddChild(cardSelectionPanel);

            var cardVBox = new VBoxContainer();
            SetAnchorsAndOffsetsToFullRect(cardVBox);
            cardSelectionPanel.AddChild(cardVBox);

            var cardLabel = new Label();
            cardLabel.Text = "AVAILABLE CARDS";
            cardLabel.HorizontalAlignment = HorizontalAlignment.Center;
            cardVBox.AddChild(cardLabel);

            var cardScrollContainer = new ScrollContainer();
            cardScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            cardVBox.AddChild(cardScrollContainer);

            availableCardsContainer = new GridContainer();
            availableCardsContainer.Columns = 3;
            cardScrollContainer.AddChild(availableCardsContainer);
        }

        private void CreateCharmSelectionPanel() {
            charmSelectionPanel = new Control();
            charmSelectionPanel.Name = "CharmSelectionPanel";
            charmSelectionPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
            // Remove manual position and size - let the preset handle it
            charmSelectionPanel.Visible = false;
            AddChild(charmSelectionPanel);

            var charmVBox = new VBoxContainer();
            SetAnchorsAndOffsetsToFullRect(charmVBox);
            charmSelectionPanel.AddChild(charmVBox);

            var charmLabel = new Label();
            charmLabel.Text = "CHARMS";
            charmLabel.HorizontalAlignment = HorizontalAlignment.Center;
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
            GameEvents.OnActionsChanged += OnActionsChanged;
            GameEvents.OnPlayerVictory += OnPlayerVictory;
            GameEvents.OnGameEnded += OnGameEnded;
        }

        // Character Selection
        private void ShowCharacterSelection() {
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
                // Update visual feedback
                UpdateCharacterSelectionFeedback(player1CharacterContainer, data.charName);
            } else {
                selectedPlayer2 = character;
                ConsoleLog.Info($"Player 2 selected: {character.CharName}");
                // Update visual feedback
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
            // Equip character cards and signature cards
            var rokCard = RokCards.CreateRokCharacterCard();
            var yuCard = YuCards.CreateYuCharacterCard();

            if (selectedPlayer1.CharName == "Rok") {
                CharacterLogic.EquipCardToSlot(selectedPlayer1, rokCard);
                EquipRokBasicCards(selectedPlayer1);
            } else if (selectedPlayer1.CharName == "Yu") {
                CharacterLogic.EquipCardToSlot(selectedPlayer1, yuCard);
                EquipYuBasicCards(selectedPlayer1);
            }

            if (selectedPlayer2.CharName == "Rok") {
                CharacterLogic.EquipCardToSlot(selectedPlayer2, rokCard);
                EquipRokBasicCards(selectedPlayer2);
            } else if (selectedPlayer2.CharName == "Yu") {
                CharacterLogic.EquipCardToSlot(selectedPlayer2, yuCard);
                EquipYuBasicCards(selectedPlayer2);
            }
        }

        private void EquipRokBasicCards(Character character) {
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateAuraOfMagic());
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateTalismanOfCalamity());
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateSparkingPunch());
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateWoundingEmber());
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateAlteringPyre());
            CharacterLogic.EquipCardToSlot(character, RokCards.CreateBlazingDash());
        }

        private void EquipYuBasicCards(Character character) {
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateKatanaOfBlizzard());
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateColdBringer());
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateGlacialTrap());
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateJudgementOfHailstones());
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateFreezingStrike());
            CharacterLogic.EquipCardToSlot(character, YuCards.CreateForbiddenTechniqueOfFrostbite());
        }

        // Game Play UI
        private void ShowGamePlay() {
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
                player1NameLabel.Text = $"PLAYER 1: {GameManager.Instance.Attacker.CharName}";
                player2NameLabel.Text = $"PLAYER 2: {GameManager.Instance.Defender.CharName}";

                UpdatePlayerResources(GameManager.Instance.Attacker);
                UpdatePlayerResources(GameManager.Instance.Defender);
                UpdateEquipmentDisplay();
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
            button.Text = $"{card.Name}\n{card.Type}\n{GetShortDescription(card.Description)}";
            button.CustomMinimumSize = new Vector2(180, 100);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.Pressed += () => OnCardSelected(card);
            return button;
        }

        private Button CreateCharmButton(Charm charm) {
            var button = new Button();
            button.Text = $"{charm.Name}\n{charm.Slot}";
            button.CustomMinimumSize = new Vector2(180, 80);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.Pressed += () => OnCharmSelected(charm);
            return button;
        }

        private string GetShortDescription(string description) {
            if (string.IsNullOrEmpty(description)) return "";
            return description.Length > 50 ? description.Substring(0, 47) + "..." : description;
        }

        private void OnCardSelected(Card card) {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                GameManager.Instance.EquipCard(currentPlayer, card);
                ConsoleLog.Info($"{currentPlayer.CharName} equipped {card.Name}");
            }
        }

        private void OnCharmSelected(Charm charm) {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                GameManager.Instance.EquipCharm(currentPlayer, charm);
                ConsoleLog.Info($"{currentPlayer.CharName} equipped charm {charm.Name}");
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

            // Create a popup with equipped cards
            ShowEquippedCardsPopup(currentPlayer);
        }

        private void ShowEquippedCardsPopup(Character character) {
            var popup = new AcceptDialog();
            popup.Title = $"Use {character.CharName}'s Cards";
            popup.Size = new Vector2I(400, 300);

            var vbox = new VBoxContainer();
            popup.AddChild(vbox);

            foreach (var kvp in character.EquippedSlots) {
                if (kvp.Value != null && kvp.Key != Card.TYPE.C) { // Don't show character cards
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

            GetViewport().AddChild(popup);
            popup.PopupCentered();
        }

        // Event Handlers
        private void OnGamePhaseChanged(string phase) {
            if (phase == "BATTLE") {
                ShowGamePlay();
            }
        }

        private void OnPlayersSet(Character attacker, Character defender) {
            ConsoleLog.Info($"Players set: {attacker.CharName} vs {defender.CharName}");
        }

        private void OnTurnStarted(Character character) {
            UpdatePlayerResources(character);
            UpdateActionButtons();
            UpdateCurrentTurnDisplay();
        }

        private void OnResourceChanged(Character character, int amount, string type) {
            UpdatePlayerResources(character);
        }

        private void OnCardEquipped(Character character, Card card) {
            UpdateEquipmentDisplay();
            ConsoleLog.Info($"{character.CharName} equipped {card.Name}");
        }

        private void OnCharmEquipped(Character character, Charm charm) {
            UpdateEquipmentDisplay();
            ConsoleLog.Info($"{character.CharName} equipped charm {charm.Name}");
        }

        private void OnDamageDealt(Character target, int damage, int remainingLP) {
            UpdatePlayerResources(target);
            
            // Create damage indicator
            ShowDamageIndicator(target, damage);
        }

        private void ShowDamageIndicator(Character target, int damage) {
            var damageLabel = new Label();
            damageLabel.Text = $"-{damage}";
            damageLabel.Modulate = Colors.Red;
            damageLabel.Position = new Vector2(GD.RandRange(100, 300), GD.RandRange(100, 200));
            AddChild(damageLabel);

            // Animate and remove
            var tween = CreateTween();
            tween.TweenProperty(damageLabel, "modulate:a", 0.0f, 1.0f);
            tween.TweenProperty(damageLabel, "position:y", damageLabel.Position.Y - 50, 1.0f);
            tween.TweenCallback(Callable.From(() => damageLabel.QueueFree()));
        }

        private void OnActionsChanged(int remaining) {
            UpdateActionButtons();
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
                player1LPBar.Value = (float)character.LP / character.MaxLP * 100;
                player1ResourcesLabel.Text = $"LP: {character.LP}/{character.MaxLP}\nEP: {character.EP}/{character.MaxEP}\nMP: {character.MP}/{character.MaxMP}\nUP: {character.UP}/{character.MaxUP}";
            } else if (character == GameManager.Instance.Defender) {
                player2LPBar.Value = (float)character.LP / character.MaxLP * 100;
                player2ResourcesLabel.Text = $"LP: {character.LP}/{character.MaxLP}\nEP: {character.EP}/{character.MaxEP}\nMP: {character.MP}/{character.MaxMP}\nUP: {character.UP}/{character.MaxUP}";
            }
        }

        private void UpdateCurrentTurnDisplay() {
            var currentPlayer = GameManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null) {
                currentTurnLabel.Text = $"CURRENT TURN: {currentPlayer.CharName}";
                currentTurnLabel.Modulate = currentPlayer == GameManager.Instance.Attacker ? Colors.Yellow : Colors.Cyan;
            }
        }

        private void UpdateActionButtons() {
            var canAct = GameManager.Instance.StateManager?.CanAct() ?? false;
            var gameInProgress = GameManager.Instance.CanPerformActions();
            
            endTurnButton.Disabled = !gameInProgress;
            normalAttackButton.Disabled = !canAct;
            useCardButton.Disabled = !canAct;
        }

        private void UpdateEquipmentDisplay() {
            UpdatePlayerEquipment(GameManager.Instance.Attacker, player1EquipmentContainer, player1CharmsContainer);
            UpdatePlayerEquipment(GameManager.Instance.Defender, player2EquipmentContainer, player2CharmsContainer);
        }

        private void UpdatePlayerEquipment(Character character, GridContainer equipmentContainer, GridContainer charmsContainer) {
            if (character == null) return;

            // Clear existing displays
            foreach (Node child in equipmentContainer.GetChildren()) {
                child.QueueFree();
            }
            foreach (Node child in charmsContainer.GetChildren()) {
                child.QueueFree();
            }

            // Show equipped cards
            foreach (var kvp in character.EquippedSlots) {
                if (kvp.Value != null) {
                    var label = new Label();
                    label.Text = $"{kvp.Key}: {kvp.Value.Name}";
                    label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    
                    if (kvp.Value.IsFrozen) {
                        label.Modulate = Colors.LightBlue;
                        label.Text += " (FROZEN)";
                    }
                    equipmentContainer.AddChild(label);
                }
            }

            // Show equipped charms
            foreach (var kvp in character.EquippedCharms) {
                if (kvp.Value != null) {
                    var label = new Label();
                    label.Text = $"{kvp.Key}: {kvp.Value.Name}";
                    label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    charmsContainer.AddChild(label);
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
            var cards = new List<Card>();

            // Add potion cards
            cards.Add(RokCards.CreateFiercefulRecover());
            cards.Add(YuCards.CreateUltimateHeadStart());

            return cards;
        }

        private List<Charm> GetAvailableCharms() {
            var charms = new List<Charm>();

            // Add Rok charms
            charms.AddRange(CharmLogic.PresetCharms.CreateRokCharms());

            // Add Yu charms  
            charms.AddRange(CharmLogic.PresetCharms.CreateYuCharms());

            return charms;
        }
    }
}