using Godot;
using meph;

public partial class GameManager : Node {
    public static GameManager Instance { get; private set; }
    
    public StateManager StateManager { get; private set; }
    public FactorManager FactorManager { get; private set; }
    public Character Attacker { get; private set; }
    public Character Defender { get; private set; }

    // Node references for clean separation
    private Control boardRoot;
    private Control uiRoot;
    private RichTextLabel consoleLog;

    public override void _Ready() {
        Instance = this;
        
        // Initialize core systems
        StateManager = new StateManager();
        FactorManager = new FactorManager();
        
        // Provide player getter to StateManager
        StateManager.GetPlayer = (turn) => turn == TURN.ATTACKER ? Attacker : Defender;

        // Get node references
        InitializeNodeReferences();
        InitializeConsole();
        InitializeEvents();
        
        ConsoleLog.Game("GameManager ready.");
        GameEvents.TriggerGameStarted();
    }

    private void InitializeNodeReferences() {
        boardRoot = GetNode<Control>("%BoardRoot");
        uiRoot = GetNode<Control>("%UI");
        consoleLog = GetNode<RichTextLabel>("%ConsoleLog");
        
        if (boardRoot == null) ConsoleLog.Error("BoardRoot node not found!");
        if (uiRoot == null) ConsoleLog.Error("UI node not found!");
        if (consoleLog == null) ConsoleLog.Error("ConsoleLog node not found!");
    }

    private void InitializeConsole() {
        ConsoleLog.Init(consoleLog);
    }

    private void InitializeEvents() {
        // Factor events
        FactorManager.OnFactorApplied += (character, effect, instance) => {
            ConsoleLog.Factor($"Applied {effect} to {character} (dur {instance.Duration})");
            GameEvents.TriggerFactorApplied(character, effect, instance.Duration);
        };
        FactorManager.OnFactorRemoved += (character, effect, instance) =>
            ConsoleLog.Factor($"Removed {effect} from {character}");
        FactorManager.OnStatusCleared += (character, effect) => {
            ConsoleLog.Factor($"Status cleared: {effect} on {character}");
            GameEvents.TriggerFactorExpired(character, effect);
        };
        FactorManager.OnFactorUpdate += () =>
            ConsoleLog.Factor("Factors updated");

        // State events
        StateManager.OnTurnStarted += (turn, player) => {
            ResolveTurnStart(player, GetOpponent(player));
            GameEvents.TriggerTurnStarted(player);
        };
        StateManager.OnTurnEnded += (turn, player) => {
            ConsoleLog.Game($"{player?.CharName ?? turn.ToString()}'s turn ended");
            GameEvents.TriggerTurnEnded(player);
        };
        StateManager.OnActionLock += () => {
            ConsoleLog.Warn("No actions remaining");
            GameEvents.TriggerActionsLocked();
        };
        StateManager.OnActionsChanged += (remaining) => {
            ConsoleLog.Action($"Actions remaining: {remaining}");
            GameEvents.TriggerActionsChanged(remaining);
        };

        // Game events (console logging)
        GameEvents.OnDamageDealt += (target, damage, remaining) =>
            ConsoleLog.Combat($"{target} took {damage} damage ({remaining} LP remaining)");
        GameEvents.OnCardUsed += (user, card, target) =>
            ConsoleLog.Combat($"{user} used {card?.Name ?? "unknown card"} on {target}");
        GameEvents.OnCardEquipped += (character, card) =>
            ConsoleLog.Equip($"{character} equipped {card.Name}");
        GameEvents.OnHealingReceived += (character, amount) =>
            ConsoleLog.Combat($"{character} healed for {amount} LP");
        GameEvents.OnResourceStolen += (from, to, amount, type) =>
            ConsoleLog.Combat($"{to} stole {amount} {type} from {from}");
        GameEvents.OnResourceRegenerated += (character, ep, mp) =>
            ConsoleLog.Resource($"{character} regenerated {ep} EP and {mp} MP");
        GameEvents.OnCardFrozen += (card, duration) =>
            ConsoleLog.Factor($"{card.Name} was frozen for {duration} turns");
        GameEvents.OnCardUnfrozen += (card) =>
            ConsoleLog.Factor($"{card.Name} was unfrozen");
        GameEvents.OnAttackResolved += (attacker, target, damage, wasCrit) => {
            string critText = wasCrit ? " (CRITICAL HIT!)" : "";
            ConsoleLog.Combat($"{attacker} dealt {damage} damage to {target}{critText}");
        };
        GameEvents.OnFactorBlocked += (character, effect) =>
            ConsoleLog.Factor($"{effect} blocked by Storm on {character}");
        GameEvents.OnPlayerDefeated += (character) => {
            var winner = GetOpponent(character);
            ConsoleLog.Game($"{character} was defeated! {winner} wins!");
            // Fixed: Use trigger method consistently
            GameEvents.TriggerPlayerVictory(winner);
            GameEvents.TriggerGameEnded();
        };
        GameEvents.OnResourceGained += (character, amount, type) =>
            ConsoleLog.Resource($"{character} gained {amount} {type}");
        GameEvents.OnResourceLost += (character, amount, type) =>
            ConsoleLog.Resource($"{character} lost {amount} {type}");
    }

    // Game logic methods
    public Character GetOpponent(Character player) {
        if (player == null) return null;
        return player == Attacker ? Defender : Attacker;
    }

    public void SetAttacker(Character character) {
        Attacker = character;
        FactorManager.RegisterCharacter(character);
        ConsoleLog.Game($"Attacker set: {character?.CharName ?? "None"}");
    }

    public void SetDefender(Character character) {
        Defender = character;
        FactorManager.RegisterCharacter(character);
        ConsoleLog.Game($"Defender set: {character?.CharName ?? "None"}");
    }

    public void Reset() {
        if (Attacker != null) FactorManager.UnregisterCharacter(Attacker);
        if (Defender != null) FactorManager.UnregisterCharacter(Defender);
        Attacker = null;
        Defender = null;
        ConsoleLog.Game("Game reset");
    }

    private void ResolveTurnStart(Character current, Character other) {
        if (current == null) {
            FactorManager.UpdateFactors();
            return;
        }

        ConsoleLog.Game($"{current}'s turn started");
        
        // Characters can't be frozen - only cards can be frozen
        // if (FactorManager.GetFactors(current, Character.STATUS_EFFECT.FREEZE).Count > 0) {
        //     StateManager.LockAction();
        //     ConsoleLog.Warn($"{current} is frozen and cannot act this turn");
        // }
        
        // Resolve per-turn effects
        FactorLogic.ResolveHealing(FactorManager, current, other);
        if (other != null) {
            FactorLogic.ResolveRecharge(FactorManager, current, other);
            FactorLogic.ResolveGrowth(FactorManager, current, other);
        }
        FactorLogic.ResolveBurning(FactorManager, current);
        FactorLogic.ResolveStorm(FactorManager, current);

        // Age factors
        FactorManager.UpdateFactors();
    }

    public static void ApplyDamage(FactorManager factorManager, Character character, int damage) {
        if (damage <= 0 || character == null) return;
        
        int remaining = FactorLogic.ResolveToughness(factorManager, character, damage);
        
        if (remaining > 0) {
            int oldLP = character.LP;
            character.LP = Mathf.Max(character.LP - remaining, 0);
            
            // Trigger resource loss event
            if (oldLP > character.LP) {
                GameEvents.TriggerResourceLost(character, oldLP - character.LP, "LP");
            }
        }
        
        GameEvents.TriggerDamageDealt(character, damage, character.LP);
        
        // Check for defeat
        if (character.LP <= 0) {
            GameEvents.TriggerPlayerDefeated(character);
        }
    }

    // Public API for UI and Board interactions
    public void EndTurn() => StateManager.EndTurn();
    
    public void UseCard(Card.TYPE slotType, Character target) {
        var current = StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);
        if (current != null) {
            CharacterLogic.UseSlot(current, slotType, target);
        }
    }
    
    public void PerformNormalAttack(Character target) {
        var current = StateManager.GetPlayer?.Invoke(StateManager.CurrentTurn);
        if (current != null) {
            CharacterLogic.PerformNormalAttack(current, target);
        }
    }

    public void EquipCard(Character character, CardData cardData) {
        if (character == null || cardData == null) return;
        
        var card = CreateCardFromData(cardData);
        CharacterLogic.EquipCardToSlot(character, card);
    }

    // Helper method to create cards from data
    private Card CreateCardFromData(CardData data) {
        var card = new Card {
            Id = data.id,
            Name = data.name,
            Type = data.type,
            Description = data.description,
            IsSwift = data.isSwift,
            Requirements = new System.Collections.Generic.Dictionary<string, int>()
        };

        // Convert Godot dictionary to C# dictionary
        foreach (var kvp in data.requirements) {
            card.Requirements[kvp.Key] = kvp.Value;
        }

        // Card effects would be assigned here based on ID through a registry system
        // card.Effect = CardRegistry.GetEffect(data.id);

        return card;
    }

    // Node access helpers for other systems
    public Control GetBoardRoot() => boardRoot;
    public Control GetUIRoot() => uiRoot;
    public RichTextLabel GetConsole() => consoleLog;
}