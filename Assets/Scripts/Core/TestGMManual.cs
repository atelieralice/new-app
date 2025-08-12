// using Godot;
// using meph;

// public partial class GameManager : Node {
//     public static GameManager Instance { get; private set; }
    
//     public StateManager StateManager { get; private set; }
//     public FactorManager FactorManager { get; private set; }
//     public Character Attacker { get; private set; }
//     public Character Defender { get; private set; }

//     // Node references for clean separation
//     private Control boardRoot;
//     private Control uiRoot;
//     private RichTextLabel consoleLog;

//     // Testing variables
//     private bool testingMode = false;
//     private float testTimer = 0f;
//     private int currentTestIndex = 0;

//     public override void _Ready() {
//         Instance = this;
        
//         // Initialize core systems
//         StateManager = new StateManager();
//         FactorManager = new FactorManager();
        
//         // Provide player getter to StateManager
//         StateManager.GetPlayer = (turn) => turn == TURN.ATTACKER ? Attacker : Defender;

//         // Get node references
//         InitializeNodeReferences();
//         InitializeConsole();
//         InitializeEvents();
        
//         ConsoleLog.Game("GameManager ready.");
//         GameEvents.TriggerGameStarted();
        
//         // Start testing after a short delay
//         CallDeferred(nameof(StartTestingSuite));
//     }

//     public override void _Process(double delta) {
//         if (testingMode) {
//             testTimer += (float)delta;
//             if (testTimer >= 2f) { // Run next test every 2 seconds
//                 testTimer = 0f;
//                 RunNextTest();
//             }
//         }
//     }

//     private void StartTestingSuite() {
//         ConsoleLog.Game("=== STARTING AUTOMATED TEST SUITE ===");
//         SetupTestCharacters();
//         testingMode = true;
//         currentTestIndex = 0;
//     }

//     private void SetupTestCharacters() {
//         ConsoleLog.Game("Setting up test characters...");
        
//         // Create test attacker (Rok) - Fixed: Use FOUR instead of ONE
//         var rokData = new CharacterData {
//             charName = "Rok",
//             star = Character.STAR.FOUR,
//             essenceType = Character.ESSENCE_TYPE.FIRE,
//             weaponType = Character.WEAPON_TYPE.MAGIC,
//             maxLP = 1000,
//             maxEP = 300,
//             maxMP = 460,
//             maxUP = 2,
//             maxPotion = 3
//         };
//         // Fixed: Use CharacterCreator instead of CharacterFactory
//         var rok = CharacterCreator.InitCharacter(rokData);
//         SetAttacker(rok);
        
//         // Create test defender (Yu) - Fixed: Use FOUR instead of ONE
//         var yuData = new CharacterData {
//             charName = "Yu",
//             star = Character.STAR.FOUR,
//             essenceType = Character.ESSENCE_TYPE.ICE,
//             weaponType = Character.WEAPON_TYPE.SWORD,
//             maxLP = 950,
//             maxEP = 250,
//             maxMP = 380,
//             maxUP = 3,
//             maxPotion = 3
//         };
//         // Fixed: Use CharacterCreator instead of CharacterFactory
//         var yu = CharacterCreator.InitCharacter(yuData);
//         SetDefender(yu);
        
//         ConsoleLog.Game("Test characters created and set!");
//     }

//     private void RunNextTest() {
//         switch (currentTestIndex) {
//             case 0: TestBasicStats(); break;
//             case 1: TestResourceManagement(); break;
//             case 2: TestTurnSystem(); break;
//             case 3: TestCardEquipping(); break;
//             case 4: TestToughnessFactors(); break;
//             case 5: TestHealingFactors(); break;
//             case 6: TestBurningFactors(); break;
//             case 7: TestStormFactors(); break;
//             case 8: TestRechargeAndGrowth(); break;
//             case 9: TestCriticalHits(); break;
//             case 10: TestDamageSystem(); break;
//             case 11: TestResourceRegeneration(); break;
//             case 12: TestLowHealthScenario(); break;
//             case 13: TestDefeatCondition(); break;
//             case 14: TestFactorStacking(); break;
//             case 15: 
//                 TestEdgeCases(); 
//                 // Stop testing after edge cases complete
//                 testingMode = false;
//                 ConsoleLog.Game("=== ALL TESTS COMPLETED ===");
//                 return;
//             default: 
//                 ConsoleLog.Game("=== ALL TESTS COMPLETED ===");
//                 testingMode = false;
//                 return;
//         }
//         currentTestIndex++;
//     }

//     private void TestBasicStats() {
//         ConsoleLog.Game("=== TEST 1: Basic Character Stats ===");
//         ConsoleLog.Info($"Attacker: {Attacker.CharName} - LP:{Attacker.LP}/{Attacker.MaxLP}, EP:{Attacker.EP}/{Attacker.MaxEP}, MP:{Attacker.MP}/{Attacker.MaxMP}");
//         ConsoleLog.Info($"Defender: {Defender.CharName} - LP:{Defender.LP}/{Defender.MaxLP}, EP:{Defender.EP}/{Defender.MaxEP}, MP:{Defender.MP}/{Defender.MaxMP}");
//         ConsoleLog.Info($"Current Turn: {StateManager.CurrentTurn}, Actions: {StateManager.ActionsRemaining}");
//     }

//     private void TestResourceManagement() {
//         ConsoleLog.Game("=== TEST 2: Resource Management ===");
        
//         // Test spending resources
//         CharacterLogic.SpendResource(Attacker, "EP", 50);
//         CharacterLogic.SpendResource(Attacker, "MP", 100);
        
//         // Test gaining resources
//         CharacterLogic.GainResource(Attacker, "EP", 25);
//         CharacterLogic.GainResource(Defender, "LP", 50);
        
//         // Test edge case: spending more than available
//         CharacterLogic.SpendResource(Defender, "EP", 999);
        
//         // Test edge case: gaining beyond max
//         CharacterLogic.GainResource(Attacker, "LP", 500);
//     }

//     private void TestTurnSystem() {
//         ConsoleLog.Game("=== TEST 3: Turn System ===");
//         ConsoleLog.Info($"Before turn change: {StateManager.CurrentTurn}");
//         StateManager.NextTurn();
//         ConsoleLog.Info($"After turn change: {StateManager.CurrentTurn}");
        
//         // Test action system
//         StateManager.TryAction(() => {
//             ConsoleLog.Action("Performing test action");
//         });
        
//         // Test swift action
//         StateManager.TryAction(() => {
//             ConsoleLog.Action("Performing swift test action");
//         }, true);
        
//         // Try action when locked
//         StateManager.TryAction(() => {
//             ConsoleLog.Action("This should not execute");
//         });
//     }

//     private void TestCardEquipping() {
//         ConsoleLog.Game("=== TEST 4: Card Equipping ===");
        
//         // Create test cards
//         var testBW = new Card {
//             Id = "test_bw",
//             Name = "Test Base Weapon",
//             Type = Card.TYPE.BW,
//             Description = "Test weapon",
//             Effect = (user, target) => {
//                 ConsoleLog.Combat($"{user.CharName} uses Test Base Weapon on {target.CharName}");
//                 CharacterLogic.ResolveAttackDamage(user, target, 100);
//             }
//         };
        
//         var testSW = new Card {
//             Id = "test_sw",
//             Name = "Test Secondary Weapon",
//             Type = Card.TYPE.SW,
//             Description = "Test secondary weapon",
//             Effect = (user, target) => {
//                 ConsoleLog.Combat($"{user.CharName} uses Test Secondary Weapon on {target.CharName}");
//                 CharacterLogic.ResolveAttackDamage(user, target, 80);
//             }
//         };
        
//         // Equip cards
//         CharacterLogic.EquipCardToSlot(Attacker, testBW);
//         CharacterLogic.EquipCardToSlot(Attacker, testSW);
        
//         // Test duplicate slot equipping
//         var testBW2 = new Card {
//             Id = "test_bw2",
//             Name = "Another Base Weapon",
//             Type = Card.TYPE.BW,
//             Description = "Should not equip"
//         };
//         CharacterLogic.EquipCardToSlot(Attacker, testBW2);
//     }

//     private void TestToughnessFactors() {
//         ConsoleLog.Game("=== TEST 5: Toughness Factors ===");
        
//         // Add toughness to defender
//         FactorLogic.AddToughness(FactorManager, Defender, 3, 200);
        
//         // Test shield absorption
//         ConsoleLog.Combat("Testing damage against toughness shield...");
//         ApplyDamage(FactorManager, Defender, 150); // Should be absorbed
//         ApplyDamage(FactorManager, Defender, 100); // Should break shield and deal remaining
        
//         // Test multiple toughness stacking
//         FactorLogic.AddToughness(FactorManager, Defender, 2, 100);
//         FactorLogic.AddToughness(FactorManager, Defender, 2, 150);
//         ApplyDamage(FactorManager, Defender, 200); // Test stacked shields
//     }

//     private void TestHealingFactors() {
//         ConsoleLog.Game("=== TEST 6: Healing Factors ===");
        
//         // Damage defender first so healing is visible
//         ApplyDamage(FactorManager, Defender, 200);
        
//         // Add healing factor
//         FactorLogic.AddHealing(FactorManager, Defender, 2, 150);
        
//         // Resolve healing
//         FactorLogic.ResolveHealing(FactorManager, Defender, Attacker);
//     }

//     private void TestBurningFactors() {
//         ConsoleLog.Game("=== TEST 7: Burning Factors ===");
        
//         // Add burning to attacker
//         FactorLogic.AddBurning(FactorManager, Attacker, 3, 5); // 5% burning
        
//         // Resolve burning damage
//         FactorLogic.ResolveBurning(FactorManager, Attacker);
        
//         // Test stacking burning
//         FactorLogic.AddBurning(FactorManager, Attacker, 2, 3); // Additional 3%
//         FactorLogic.ResolveBurning(FactorManager, Attacker);
//     }

//     private void TestStormFactors() {
//         ConsoleLog.Game("=== TEST 8: Storm Factors ===");
        
//         // Add storm to defender
//         FactorLogic.AddStorm(FactorManager, Defender, 2, 75);
        
//         // Try to add other factors while stormed (should be blocked)
//         FactorLogic.AddToughness(FactorManager, Defender, 2, 100);
//         FactorLogic.AddHealing(FactorManager, Defender, 2, 100);
        
//         // Resolve storm damage
//         FactorLogic.ResolveStorm(FactorManager, Defender);
//     }

//     private void TestRechargeAndGrowth() {
//         ConsoleLog.Game("=== TEST 9: Recharge and Growth ===");
        
//         // Add recharge to attacker (steals EP)
//         FactorLogic.AddRecharge(FactorManager, Attacker, 2, 100);
//         FactorLogic.ResolveRecharge(FactorManager, Attacker, Defender);
        
//         // Add growth to attacker (steals MP)
//         FactorLogic.AddGrowth(FactorManager, Attacker, 2, 80);
//         FactorLogic.ResolveGrowth(FactorManager, Attacker, Defender);
//     }

//     private void TestCriticalHits() {
//         ConsoleLog.Game("=== TEST 10: Critical Hit System ===");
        
//         // Test multiple crit rolls
//         for (int i = 0; i < 10; i++) {
//             bool crit = Attacker.RollCritical();
//             ConsoleLog.Combat($"Crit roll {i + 1}: {(crit ? "CRITICAL!" : "Normal")}");
//         }
        
//         // Test attack with crit calculation
//         CharacterLogic.ResolveAttackDamage(Attacker, Defender, 200);
//     }

//     private void TestDamageSystem() {
//         ConsoleLog.Game("=== TEST 11: Damage System ===");
        
//         // Test normal attack
//         if (Attacker.EquippedSlots.ContainsKey(Card.TYPE.BW) && 
//             Attacker.EquippedSlots.ContainsKey(Card.TYPE.SW)) {
//             CharacterLogic.PerformNormalAttack(Attacker, Defender);
//         }
        
//         // Test direct damage
//         ApplyDamage(FactorManager, Defender, 150);
        
//         // Test zero/negative damage
//         ApplyDamage(FactorManager, Defender, 0);
//         ApplyDamage(FactorManager, Defender, -50);
//     }

//     private void TestResourceRegeneration() {
//         ConsoleLog.Game("=== TEST 12: Resource Regeneration ===");
        
//         // Spend some resources first
//         CharacterLogic.SpendResource(Attacker, "EP", 100);
//         CharacterLogic.SpendResource(Attacker, "MP", 200);
        
//         ConsoleLog.Resource($"Before regen - EP: {Attacker.EP}, MP: {Attacker.MP}");
        
//         // Trigger turn to test regeneration
//         StateManager.NextTurn();
        
//         ConsoleLog.Resource($"After regen - EP: {Attacker.EP}, MP: {Attacker.MP}");
//     }

//     private void TestLowHealthScenario() {
//         ConsoleLog.Game("=== TEST 13: Low Health Scenario ===");
        
//         // Reduce attacker to low health
//         int targetLP = (int)(Attacker.MaxLP * 0.2f); // 20% health
//         int damage = Attacker.LP - targetLP;
//         ApplyDamage(FactorManager, Attacker, damage);
        
//         ConsoleLog.Combat($"{Attacker.CharName} is now at {Attacker.LP} LP ({(float)Attacker.LP / Attacker.MaxLP * 100:F1}%)");
//         // Fixed: Create helper method instead of calling non-existent IsLowHealth
//         bool isLowHealth = Attacker.LP <= (Attacker.MaxLP * 0.25f);
//         ConsoleLog.Combat($"Is low health: {isLowHealth}");
//     }

//     private void TestDefeatCondition() {
//         ConsoleLog.Game("=== TEST 14: Defeat Condition ===");
        
//         // Create a temporary character for defeat testing
//         var tempData = new CharacterData {
//             charName = "TestDummy",
//             maxLP = 100,
//             maxEP = 100,
//             maxMP = 100
//         };
//         // Fixed: Use CharacterCreator instead of CharacterFactory
//         var tempChar = CharacterCreator.InitCharacter(tempData);
        
//         ConsoleLog.Combat("Testing defeat condition on temporary character...");
//         ApplyDamage(FactorManager, tempChar, 150); // Should defeat the character
//     }

//     private void TestFactorStacking() {
//         ConsoleLog.Game("=== TEST 15: Factor Stacking ===");
        
//         // Test multiple instances of same factor
//         FactorLogic.AddBurning(FactorManager, Defender, 2, 2);
//         FactorLogic.AddBurning(FactorManager, Defender, 3, 3);
//         FactorLogic.AddBurning(FactorManager, Defender, 1, 1);
        
//         ConsoleLog.Factor($"Burning factors on {Defender.CharName}: {FactorManager.GetFactors(Defender, Character.STATUS_EFFECT.BURNING).Count}");
        
//         // Resolve stacked burning
//         FactorLogic.ResolveBurning(FactorManager, Defender);
        
//         // Test factor aging
//         FactorManager.UpdateFactors();
//         ConsoleLog.Factor($"After aging - Burning factors: {FactorManager.GetFactors(Defender, Character.STATUS_EFFECT.BURNING).Count}");
//     }

//     private void TestEdgeCases() {
//         ConsoleLog.Game("=== TEST 16: Edge Cases ===");
        
//         // Test null character operations
//         CharacterLogic.SpendResource(null, "EP", 50);
//         ApplyDamage(FactorManager, null, 100);
        
//         // Test invalid resource types
//         CharacterLogic.SpendResource(Attacker, "INVALID", 50);
        
//         // Test zero duration factors
//         FactorLogic.AddBurning(FactorManager, Defender, 0, 5);
        
//         // Test card operations with null
//         CharacterLogic.EquipCardToSlot(Attacker, null);
//         CharacterLogic.EquipCardToSlot(null, new Card());
        
//         // Test using non-existent card slot
//         CharacterLogic.UseSlot(Attacker, Card.TYPE.E, Defender);
        
//         ConsoleLog.Game("Edge case testing completed!");
//     }

//     // Existing methods...
//     private void InitializeNodeReferences() {
//         boardRoot = GetNode<Control>("%BoardRoot");
//         uiRoot = GetNode<Control>("%UI");
//         consoleLog = GetNode<RichTextLabel>("%ConsoleLog");
        
//         if (boardRoot == null) ConsoleLog.Error("BoardRoot node not found!");
//         if (uiRoot == null) ConsoleLog.Error("UI node not found!");
//         if (consoleLog == null) ConsoleLog.Error("ConsoleLog node not found!");
//     }

//     private void InitializeConsole() {
//         ConsoleLog.Init(consoleLog);
//     }

//     private void InitializeEvents() {
//         // Factor events
//         FactorManager.OnFactorApplied += (character, effect, instance) => {
//             ConsoleLog.Factor($"Applied {effect} to {character} (dur {instance.Duration})");
//             GameEvents.TriggerFactorApplied(character, effect, instance.Duration);
//         };
//         FactorManager.OnFactorRemoved += (character, effect, instance) =>
//             ConsoleLog.Factor($"Removed {effect} from {character}");
//         FactorManager.OnStatusCleared += (character, effect) => {
//             ConsoleLog.Factor($"Status cleared: {effect} on {character}");
//             GameEvents.TriggerFactorExpired(character, effect);
//         };
//         FactorManager.OnFactorUpdate += () =>
//             ConsoleLog.Factor("Factors updated");

//         // State events
//         StateManager.OnTurnStarted += (turn, player) => {
//             ResolveTurnStart(player, GetOpponent(player));
//             GameEvents.TriggerTurnStarted(player);
//         };
//         StateManager.OnTurnEnded += (turn, player) => {
//             ConsoleLog.Game($"{player?.CharName ?? turn.ToString()}'s turn ended");
//             GameEvents.TriggerTurnEnded(player);
//         };
//         StateManager.OnActionLock += () => {
//             ConsoleLog.Warn("No actions remaining");
//             GameEvents.TriggerActionsLocked();
//         };
//         StateManager.OnActionsChanged += (remaining) => {
//             ConsoleLog.Action($"Actions remaining: {remaining}");
//             GameEvents.TriggerActionsChanged(remaining);
//         };

//         // Game events (console logging)
//         GameEvents.OnDamageDealt += (target, damage, remaining) =>
//             ConsoleLog.Combat($"{target} took {damage} damage ({remaining} LP remaining)");
//         GameEvents.OnCardUsed += (user, card, target) =>
//             ConsoleLog.Combat($"{user} used {card?.Name ?? "unknown card"} on {target}");
//         GameEvents.OnCardEquipped += (character, card) =>
//             ConsoleLog.Equip($"{character} equipped {card.Name}");
//         GameEvents.OnHealingReceived += (character, amount) =>
//             ConsoleLog.Combat($"{character} healed for {amount} LP");
//         GameEvents.OnResourceStolen += (from, to, amount, type) =>
//             ConsoleLog.Combat($"{to} stole {amount} {type} from {from}");
//         GameEvents.OnResourceRegenerated += (character, ep, mp) =>
//             ConsoleLog.Resource($"{character} regenerated {ep} EP and {mp} MP");
//         GameEvents.OnCardFrozen += (card, duration) =>
//             ConsoleLog.Factor($"{card.Name} was frozen for {duration} turns");
//         GameEvents.OnCardUnfrozen += (card) =>
//             ConsoleLog.Factor($"{card.Name} was unfrozen");
//         GameEvents.OnAttackResolved += (attacker, target, damage, wasCrit) => {
//             string critText = wasCrit ? " (CRITICAL HIT!)" : "";
//             ConsoleLog.Combat($"{attacker} dealt {damage} damage to {target}{critText}");
//         };
//         GameEvents.OnFactorBlocked += (character, effect) =>
//             ConsoleLog.Factor($"{effect} blocked by Storm on {character}");
//         GameEvents.OnPlayerDefeated += (character) => {
//             var winner = GetOpponent(character);
//             ConsoleLog.Game($"{character} was defeated! {winner} wins!");
//             GameEvents.TriggerPlayerVictory(winner);
//             GameEvents.TriggerGameEnded();
//         };
//         GameEvents.OnResourceGained += (character, amount, type) =>
//             ConsoleLog.Resource($"{character} gained {amount} {type}");
//         GameEvents.OnResourceLost += (character, amount, type) =>
//             ConsoleLog.Resource($"{character} lost {amount} {type}");
//     }

//     // Game logic methods
//     public Character GetOpponent(Character player) {
//         if (player == null) return null;
//         return player == Attacker ? Defender : Attacker;
//     }

//     public void SetAttacker(Character character) {
//         Attacker = character;
//         FactorManager.RegisterCharacter(character);
//         ConsoleLog.Game($"Attacker set: {character?.CharName ?? "None"}");
//     }

//     public void SetDefender(Character character) {
//         Defender = character;
//         FactorManager.RegisterCharacter(character);
//         ConsoleLog.Game($"Defender set: {character?.CharName ?? "None"}");
//     }

//     public void Reset() {
//         if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
//         if (Defender != null) FactorManager.UnregisterCharacter(Defender);
//         Attacker = null;
//         Defender = null;
//         ConsoleLog.Game("Game reset");
//     }

//     private void ResolveTurnStart(Character current, Character other) {
//         if (current == null) {
//             FactorManager.UpdateFactors();
//             return;
//         }

//         ConsoleLog.Game($"{current}'s turn started");
        
//         // Resolve per-turn effects
//         FactorLogic.ResolveHealing(FactorManager, current, other);
//         if (other != null) {
//             FactorLogic.ResolveRecharge(FactorManager, current, other);
//             FactorLogic.ResolveGrowth(FactorManager, current, other);
//         }
//         FactorLogic.ResolveBurning(FactorManager, current);
//         FactorLogic.ResolveStorm(FactorManager, current);

//         // Age factors
//         FactorManager.UpdateFactors();
//     }

//     public static void ApplyDamage(FactorManager factorManager, Character character, int damage) {
//         if (damage <= 0 || character == null) return;
        
//         int remaining = FactorLogic.ResolveToughness(factorManager, character, damage);
        
//         if (remaining > 0) {
//             int oldLP = character.LP;
//             character.LP = Mathf.Max(character.LP - remaining, 0);
            
//             // Trigger resource loss event
//             if (oldLP > character.LP) {
//                 GameEvents.TriggerResourceLost(character, oldLP - character.LP, "LP");
//             }
//         }
        
//         GameEvents.TriggerDamageDealt(character, damage, character.LP);
        
//         // Check for defeat
//         if (character.LP <= 0) {
//             GameEvents.TriggerPlayerDefeated(character);
//         }
//     }

//     // Public API for UI and Board interactions
//     public void EndTurn() => StateManager.EndTurn();
    
//     public void UseCard(Card.TYPE slotType, Character target) {
//         var current = StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);
//         if (current != null) {
//             CharacterLogic.UseSlot(current, slotType, target);
//         }
//     }
    
//     public void PerformNormalAttack(Character target) {
//         var current = StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);
//         if (current != null) {
//             CharacterLogic.PerformNormalAttack(current, target);
//         }
//     }

//     public void EquipCard(Character character, CardData cardData) {
//         if (character == null || cardData == null) return;
        
//         var card = CreateCardFromData(cardData);
//         CharacterLogic.EquipCardToSlot(character, card);
//     }

//     // Simple card creation from data - card effects will be handled by UI/scripting layer
//     private Card CreateCardFromData(CardData data) {
//         var card = new Card {
//             Id = data.id,
//             Name = data.name,
//             Type = data.type,
//             Description = data.description,
//             IsSwift = data.isSwift,
//             Requirements = new System.Collections.Generic.Dictionary<string, int>()
//         };

//         // Convert Godot dictionary to C# dictionary
//         foreach (var kvp in data.requirements) {
//             card.Requirements[kvp.Key] = kvp.Value;
//         }

//         // Card effects will be assigned by the UI layer or through scripting
//         // This keeps the core logic clean and flexible
//         return card;
//     }

//     // Node access helpers for other systems
//     public Control GetBoardRoot() => boardRoot;
//     public Control GetUIRoot() => uiRoot;
//     public RichTextLabel GetConsole() => consoleLog;

//     // Manual test triggers for debugging
//     public void RunSingleTest(int testIndex) {
//         currentTestIndex = testIndex;
//         RunNextTest();
//     }

//     public void StopTesting() {
//         testingMode = false;
//         ConsoleLog.Game("Testing stopped manually");
//     }
// }