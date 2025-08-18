using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static meph.Character;

namespace meph {
    
    /// <summary>
    /// Static utility class providing comprehensive factor system management and application
    /// Implements all status effects, factor interactions, and character-specific enhancements
    /// Serves as the primary interface for factor-based combat mechanics and turn resolution
    /// 
    /// Core Factor Types:
    /// - Toughness: Earth damage bonus and damage absorption shield system
    /// - Healing: LP recovery with opponent LP loss mechanics (not damage)
    /// - Recharge/Growth: Resource stealing mechanics for EP and MP
    /// - Storm: Factor prevention and defense-bypassing damage over time
    /// - Burning: Percentage-based Fire damage with character multiplier support
    /// - Freeze: Card lockdown mechanics with duration bonuses and special effects
    /// - Specialized: Essence shields, damage boosts, MP regeneration, and immunity
    /// 
    /// Character Integration:
    /// - Rok: Low health passive enhances burning duration and damage conversions
    /// - Yu: Freeze applications charge Ultimate Points and trigger Glacial Trap
    /// - Factor blocking through Storm effects and immunity systems
    /// - Charm-based factor enhancement through duration and damage bonuses
    /// </summary>
    public static class FactorLogic {
        
        #region Utility Methods
        
        /// <summary>
        /// Safely retrieves parameter values from factor instances with default fallback
        /// Prevents null reference exceptions and provides consistent parameter access
        /// Used throughout factor calculations for damage, duration, and effect values
        /// 
        /// Parameter Access Pattern:
        /// - Check if factor has parameters dictionary
        /// - Attempt to retrieve specific parameter key
        /// - Return parameter value if found, default value if not found
        /// - Ensures all factor calculations have safe fallback values
        /// </summary>
        /// <param name="factor">FactorInstance containing parameter data</param>
        /// <param name="key">Parameter key to retrieve (e.g., ParamKeys.DP, ParamKeys.BD)</param>
        /// <param name="defVal">Default value to return if parameter not found</param>
        /// <returns>Parameter value if found, default value otherwise</returns>
        private static int GetParamOrDefault(FactorInstance factor, string key, int defVal = 0) =>
            factor.Params != null && factor.Params.TryGetValue(key, out var v) ? v : defVal;

        /// <summary>
        /// Checks if a character is affected by Storm status, preventing factor activation
        /// Storm blocks all positive factor applications except Storm itself
        /// Used as validation gate before applying beneficial factors
        /// 
        /// Storm Prevention Rules:
        /// - Characters with Storm cannot receive Toughness, Healing, Recharge, Growth
        /// - Storm does not prevent negative factors like Burning or Freeze
        /// - Multiple Storm instances can stack for increased damage
        /// - Storm immunity bypasses this check completely
        /// </summary>
        /// <param name="fm">FactorManager instance for factor queries</param>
        /// <param name="target">Character to check for Storm effects</param>
        /// <returns>True if character has Storm and cannot receive beneficial factors</returns>
        private static bool IsStormed(FactorManager fm, Character target) =>
            fm.GetFactors(target, STATUS_EFFECT.STORM).Count > 0;
        
        #endregion
        
        #region Beneficial Factor Application
        
        /// <summary>
        /// Applies Toughness factor providing Earth damage bonus and damage absorption shield
        /// Enhanced implementation provides dual benefits: offensive Earth scaling and defensive protection
        /// Blocked by Storm effects but provides significant combat advantage when active
        /// 
        /// Toughness Mechanics:
        /// - Earth Damage Bonus: +150 damage per Toughness instance for Earth essence attacks
        /// - Damage Shield: Absorbs damage equal to DP parameter before affecting LP
        /// - Shield Stacking: Multiple Toughness instances provide separate shields
        /// - Shield Depletion: Shields break when damage exceeds DP, remaining damage carries over
        /// 
        /// Strategic Usage:
        /// - Earth essence builds gain significant damage scaling
        /// - Defensive builds use for damage mitigation
        /// - Combines well with Earth-heavy card strategies
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving Toughness benefits</param>
        /// <param name="duration">Number of turns Toughness remains active</param>
        /// <param name="dp">Damage Points absorbed by the shield component</param>
        public static void AddToughness(FactorManager fm, Character character, int duration = 2, int dp = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.TOUGHNESS);
                return;
            }
            
            // Check for existing Toughness to stack DP
            var existingToughness = fm.GetFactors(character, STATUS_EFFECT.TOUGHNESS);
            if (existingToughness.Count > 0) {
                // Stack DP onto existing Toughness (don't extend duration)
                var existing = existingToughness[0];
                int currentDP = GetParamOrDefault(existing, ParamKeys.DP, 100);
                existing.Params[ParamKeys.DP] = currentDP + dp;
                ConsoleLog.Factor($"{character.CharName}'s Toughness DP increased by {dp} (total: {currentDP + dp})");
            } else {
                // Apply new Toughness
                var parameters = new Dictionary<string, int> { { ParamKeys.DP, dp } };
                fm.ApplyFactor(character, STATUS_EFFECT.TOUGHNESS, duration, parameters);
                ConsoleLog.Factor($"{character.CharName} gained Toughness: +150 Earth Damage and {dp} DP shield for {duration} turns");
            }
        }

        /// <summary>
        /// Applies Healing factor providing LP recovery with opponent LP loss side effect
        /// Unique dual-effect factor: benefits caster while harming opponent through LP loss (not damage)
        /// Blocked by Storm but provides powerful resource swing when successfully applied
        /// 
        /// Healing Mechanics:
        /// - Self Healing: Character regains LP equal to HA parameter (capped at MaxLP)
        /// - Opponent LP Loss: Opponent loses LP equal to half the heal amount (not damage)
        /// - Defense Bypass: LP loss bypasses all defense and shield systems
        /// - Defeat Checking: Opponent defeat triggers if LP reaches zero from LP loss
        /// 
        /// Strategic Considerations:
        /// - Provides resource sustainability for extended matches
        /// - Applies pressure to opponent even while healing
        /// - LP loss cannot be mitigated by shields or damage reduction
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving healing benefits</param>
        /// <param name="duration">Number of turns healing effect remains active</param>
        /// <param name="ha">Heal Amount per turn and basis for opponent LP loss calculation</param>
        public static void AddHealing(FactorManager fm, Character character, int duration = 2, int ha = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.HEALING);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.HA, ha } };
            fm.ApplyFactor(character, STATUS_EFFECT.HEALING, duration, parameters);
        }

        /// <summary>
        /// Applies Recharge factor providing EP stealing mechanics from opponent
        /// Resource stealing factor that transfers opponent's EP to caster with maximum cap protection
        /// Blocked by Storm but provides significant resource advantage for EP-dependent strategies
        /// 
        /// Recharge Mechanics:
        /// - EP Stealing: Steals up to RC parameter amount from opponent's current EP
        /// - Transfer Cap: Gained EP cannot exceed caster's MaxEP (excess is lost)
        /// - Resource Protection: Cannot steal more EP than opponent currently has
        /// - Turn-by-Turn: Multiple Recharge instances steal independently each turn
        /// 
        /// Strategic Applications:
        /// - Disrupts opponent's physical ability usage
        /// - Sustains caster's EP-intensive strategies
        /// - Combines well with high EP cost abilities
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving EP stealing benefits</param>
        /// <param name="duration">Number of turns EP stealing remains active</param>
        /// <param name="recharge">Maximum EP amount stolen per turn</param>
        public static void AddRecharge(FactorManager fm, Character character, int duration = 2, int recharge = 150) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.RECHARGE);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.RC, recharge } };
            fm.ApplyFactor(character, STATUS_EFFECT.RECHARGE, duration, parameters);
        }

        /// <summary>
        /// Applies Growth factor providing MP stealing mechanics from opponent
        /// Resource stealing factor that transfers opponent's MP to caster with maximum cap protection
        /// Blocked by Storm but provides significant resource advantage for MP-dependent strategies
        /// 
        /// Growth Mechanics:
        /// - MP Stealing: Steals up to MP parameter amount from opponent's current MP
        /// - Transfer Cap: Gained MP cannot exceed caster's MaxMP (excess is lost)
        /// - Resource Protection: Cannot steal more MP than opponent currently has
        /// - Turn-by-Turn: Multiple Growth instances steal independently each turn
        /// 
        /// Strategic Applications:
        /// - Disrupts opponent's magical ability usage
        /// - Sustains caster's MP-intensive strategies
        /// - Combines well with high MP cost abilities and essence builds
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving MP stealing benefits</param>
        /// <param name="duration">Number of turns MP stealing remains active</param>
        /// <param name="growthMp">Maximum MP amount stolen per turn</param>
        public static void AddGrowth(FactorManager fm, Character character, int duration = 2, int growthMp = 100) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.GROWTH);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.MP, growthMp } };
            fm.ApplyFactor(character, STATUS_EFFECT.GROWTH, duration, parameters);
        }
        
        #endregion
        
        #region Detrimental Factor Application
        
        /// <summary>
        /// Applies Storm factor preventing factor activation and dealing defense-bypassing damage
        /// Powerful control factor that blocks beneficial factors while applying consistent damage pressure
        /// Cannot be blocked by existing Storm (Storm can stack) and bypasses most defensive measures
        /// 
        /// Storm Mechanics:
        /// - Factor Prevention: Prevents target from receiving Toughness, Healing, Recharge, Growth
        /// - Defense Bypass: Storm damage ignores DEF but respects shield systems
        /// - Damage Stacking: Multiple Storm instances deal cumulative damage per turn
        /// - Turn Resolution: Damage applies during opponent's turn start before other effects
        /// 
        /// Strategic Impact:
        /// - Shuts down opponent's factor-based strategies
        /// - Provides consistent damage pressure that builds over time
        /// - Forces opponent to focus on Storm removal or face escalating damage
        /// - Cannot be mitigated by normal defensive measures
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving Storm effects (opponent)</param>
        /// <param name="duration">Number of turns Storm prevents factors and deals damage</param>
        /// <param name="stormDamage">Damage dealt per turn that bypasses DEF</param>
        public static void AddStorm(FactorManager fm, Character character, int duration = 2, int stormDamage = 50) {
            var parameters = new Dictionary<string, int> { { ParamKeys.SD, stormDamage } };
            fm.ApplyFactor(character, STATUS_EFFECT.STORM, duration, parameters);

            ConsoleLog.Factor($"{character.CharName} is affected by Storm - cannot activate factors and takes {stormDamage} damage per turn for {duration} turns");
        }

        /// <summary>
        /// Applies Burning factor dealing percentage-based Fire damage over time
        /// Enhanced burning system with character modifier support and passive ability integration
        /// Blocked by Storm but provides significant damage over time with scaling potential
        /// 
        /// Enhanced Burning Mechanics:
        /// - Percentage Damage: Deals BD% of target's MaxLP as Fire damage per turn
        /// - Defense Bypass: Burning damage ignores DEF and EssenceDEF completely
        /// - Character Multipliers: Source character's burning damage bonuses enhance effectiveness
        /// - Passive Integration: Rok's low health mode increases burning duration by 1 turn
        /// - Set Bonus Scaling: Charm set bonuses can double burning damage effectiveness
        /// 
        /// Damage Calculation:
        /// 1. Base percentage damage = target's MaxLP × BD% ÷ 100
        /// 2. Apply source character's burning damage multiplier
        /// 3. Bypass all defense calculations
        /// 4. Apply through shield systems only
        /// 
        /// Strategic Applications:
        /// - Scales with opponent's health pool for consistent threat
        /// - Enhanced by Fire essence builds and burning damage charms
        /// - Pressure tool that forces opponent action or faces escalating damage
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving burning damage</param>
        /// <param name="duration">Number of turns burning damage persists</param>
        /// <param name="bdPercent">Percentage of MaxLP dealt as damage per turn</param>
        /// <param name="source">Optional source character for burning damage modifiers</param>
        public static void AddBurning(FactorManager fm, Character character, int duration = 2, int bdPercent = 2, Character source = null) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING);
                return;
            }

            // Apply Rok's low health passive - increase BT by 1
            if (source != null && source.PassiveState.IsLowHealthModeActive) {
                duration += 1;
                ConsoleLog.Combat($"Burning duration increased by 1 due to {source.CharName}'s low health state");
            }

            var parameters = new Dictionary<string, int> { { ParamKeys.BD, bdPercent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING, duration, parameters);
        }

        /// <summary>
        /// Applies Freeze factor to character for movement and ability restriction
        /// Basic character-level freeze that can be enhanced by card-specific freeze mechanics
        /// Blocked by Storm but provides control utility for strategic positioning
        /// 
        /// Character Freeze Mechanics:
        /// - Action Limitation: Reduces available actions per turn
        /// - Duration Tracking: FD parameter tracks remaining freeze turns
        /// - Enhanced Duration: Source character's freeze duration bonuses apply
        /// - Stack Prevention: Multiple freezes extend duration rather than stack separately
        /// 
        /// Note: Card-level freezing is handled separately through FreezeCard methods
        /// Character freeze affects turn structure while card freeze affects card availability
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving freeze effects</param>
        /// <param name="duration">Number of turns character remains frozen</param>
        public static void AddFreeze(FactorManager fm, Character character, int duration = 2) {
            var parameters = new Dictionary<string, int> { { ParamKeys.FD, duration } };
            fm.ApplyFactor(character, STATUS_EFFECT.FREEZE, duration, parameters);
        }
        
        #endregion
        
        #region Card Freeze System
        
        /// <summary>
        /// Freezes individual cards with duration bonuses and Ultimate Point tracking
        /// Enhanced card freeze system with character-specific bonuses and Yu's passive integration
        /// Provides tactical card lockdown with scaling potential through charm and passive effects
        /// 
        /// Enhanced Card Freeze Mechanics:
        /// - Duration Enhancement: Source character's GetFreezeDurationBonus() adds turns
        /// - Yu's UP Charging: Yu gains 1 UP per 10 freeze applications (passive tracking)
        /// - Card Lockdown: Frozen cards cannot be activated until duration expires
        /// - Event Integration: Triggers card freeze events for UI and game state updates
        /// 
        /// Targeting Rules:
        /// - Cannot freeze already frozen cards (prevents redundant applications)
        /// - Cannot freeze C-type cards (Charm cards immune to freeze)
        /// - Duration bonuses from charms and set effects apply automatically
        /// 
        /// Strategic Applications:
        /// - Disrupts opponent's card strategies and combos
        /// - Builds towards Yu's Ultimate Point accumulation
        /// - Scales with freeze duration charm bonuses for extended lockdown
        /// </summary>
        /// <param name="card">Card to freeze (must not be null or already frozen)</param>
        /// <param name="duration">Base freeze duration before character bonuses</param>
        /// <param name="source">Optional source character for duration bonuses and UP tracking</param>
        public static void FreezeCard(Card card, int duration, Character source = null) {
            if (card == null || card.IsFrozen) return;

            if (source != null) {
                duration += source.GetFreezeDurationBonus();
            }

            card.Freeze(duration);
            GameEvents.TriggerCardFrozen(card, duration);

            // Track freeze applications for Yu's UP charging
            if (source != null && source.CharName == "Yu") {
                source.PassiveState.FreezeApplicationCount++;

                if (source.PassiveState.FreezeApplicationCount >= 10) {
                    source.UP = Math.Min(source.UP + 1, source.MaxUP);
                    source.PassiveState.FreezeApplicationCount = 0;
                    ConsoleLog.Resource($"{source.CharName} gained 1 UP from freeze applications");
                    GameEvents.TriggerResourceGained(source, 1, "UP");
                }
            }
        }

        /// <summary>
        /// Unfreezes individual cards and triggers appropriate game events
        /// Simple card unfreeze utility for ability effects and duration expiration
        /// Ensures proper event triggering for UI synchronization and game state consistency
        /// 
        /// Usage Examples:
        /// - Card abilities that remove freeze effects
        /// - Turn-based freeze duration expiration
        /// - Special abilities that provide freeze immunity or removal
        /// </summary>
        /// <param name="card">Card to unfreeze (null checking performed by Card.Unfreeze)</param>
        public static void UnfreezeCard(Card card) {
            if (card == null) return;
            
            card.Unfreeze();
            GameEvents.TriggerCardUnfrozen(card);
        }

        /// <summary>
        /// Freezes multiple cards simultaneously with randomized targeting
        /// Mass freeze utility for abilities that affect multiple opponent cards
        /// Implements intelligent targeting to avoid frozen and immune cards
        /// 
        /// Multi-Freeze Mechanics:
        /// - Random Selection: Chooses random cards from available pool
        /// - Smart Filtering: Excludes frozen cards and C-type (Charm) cards
        /// - Capacity Limiting: Cannot freeze more cards than available targets
        /// - Individual Processing: Each card receives full freeze duration and bonuses
        /// 
        /// Targeting Priority:
        /// 1. Non-frozen cards only (prevents redundant applications)
        /// 2. Non-C type cards only (Charm cards cannot be frozen)
        /// 3. Random selection from eligible pool for unpredictability
        /// 
        /// Strategic Applications:
        /// - Mass control abilities for board lockdown
        /// - Yu's advanced freeze strategies with Ultimate abilities
        /// - Disruption tools against deck-based strategies
        /// </summary>
        /// <param name="target">Character whose cards will be frozen</param>
        /// <param name="cardCount">Maximum number of cards to freeze</param>
        /// <param name="duration">Freeze duration applied to each card</param>
        /// <param name="source">Optional source character for duration bonuses and tracking</param>
        public static void FreezeMultipleCards(Character target, int cardCount, int duration, Character source = null) {
            var availableCards = target.EquippedSlots.Values
                .Where(c => c != null && !c.IsFrozen && c.Type != Card.TYPE.C)
                .ToList();

            int cardsToFreeze = Math.Min(cardCount, availableCards.Count);

            for (int i = 0; i < cardsToFreeze; i++) {
                var randomIndex = (int)(GD.Randi() % availableCards.Count);
                var cardToFreeze = availableCards[randomIndex];
                availableCards.RemoveAt(randomIndex);

                FreezeCard(cardToFreeze, duration, source);
            }
        }

        /// <summary>
        /// Extends freeze duration on all currently frozen cards
        /// Utility for abilities that enhance existing freeze effects rather than applying new ones
        /// Affects only cards that are already frozen, leaving unfrozen cards unaffected
        /// 
        /// Duration Extension Mechanics:
        /// - Affects only currently frozen cards
        /// - Adds additional duration to existing freeze timers
        /// - Logs each extension for combat feedback
        /// - Does not apply character duration bonuses (pure extension)
        /// 
        /// Strategic Usage:
        /// - Extends control over locked opponent cards
        /// - Prolongs advantage gained from previous freeze applications
        /// - Prevents opponent recovery from freeze lockdown
        /// </summary>
        /// <param name="target">Character whose frozen cards will have extended duration</param>
        /// <param name="additionalDuration">Additional turns added to each frozen card</param>
        public static void ExtendFreezeOnAllCards(Character target, int additionalDuration) {
            foreach (var card in target.EquippedSlots.Values) {
                if (card != null && card.IsFrozen) {
                    card.ExtendFreezeDuration(additionalDuration);
                    ConsoleLog.Combat($"{card.Name}'s freeze duration extended by {additionalDuration} turns");
                }
            }
        }
        
        #endregion
        
        #region Specialized Factor Application
        
        /// <summary>
        /// Applies specialized essence-only shield for specific character abilities
        /// Advanced shield system that protects only against elemental damage types
        /// Blocked by Storm but provides targeted defense against essence-heavy opponents
        /// 
        /// Essence Shield Mechanics:
        /// - Essence-Only Protection: Blocks elemental damage but allows normal damage through
        /// - Damage Absorption: Functions like Toughness shields with DP parameter
        /// - Shield Breaking: Depletes when absorbed damage exceeds DP value
        /// - Selective Defense: Strategic protection against essence-focused builds
        /// 
        /// Usage Examples:
        /// - Rok's Talisman of Calamity: 80 DP essence shield
        /// - Counters opponent essence damage strategies
        /// - Allows normal damage for balanced defensive profile
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving essence shield protection</param>
        /// <param name="duration">Number of turns essence shield remains active</param>
        /// <param name="dp">Damage Points absorbed specifically from essence attacks</param>
        public static void AddEssenceShield(FactorManager fm, Character character, int duration = 2, int dp = 80) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.ESSENCE_SHIELD);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.DP, dp } };
            fm.ApplyFactor(character, STATUS_EFFECT.ESSENCE_SHIELD, duration, parameters);
        }

        /// <summary>
        /// Applies burning damage boost effect for enhanced Fire damage output
        /// Temporary damage enhancement that scales all burning effects for the character
        /// Blocked by Storm but provides significant damage scaling for Fire essence builds
        /// 
        /// Burning Damage Boost Mechanics:
        /// - Damage Scaling: Increases all burning damage dealt by BD percentage
        /// - Extended Duration: Typically longer than base factors (6 turns default)
        /// - Stack Compatibility: Multiple boosts can stack for compounding effects
        /// - Source Enhancement: Affects all burning damage sources from the character
        /// 
        /// Strategic Applications:
        /// - Rok's Sparking Punch: 2% boost for 6 turns
        /// - Synergizes with burning damage charm bonuses
        /// - Scales both direct burning applications and ongoing burning effects
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving burning damage enhancement</param>
        /// <param name="duration">Number of turns damage boost remains active</param>
        /// <param name="percent">Percentage increase to all burning damage dealt</param>
        public static void AddBurningDamageBoost(FactorManager fm, Character character, int duration = 6, int percent = 2) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING_DAMAGE_BOOST);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, percent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING_DAMAGE_BOOST, duration, parameters);
        }

        /// <summary>
        /// Applies MP regeneration effect for sustained magical resource recovery
        /// Turn-based MP restoration that supports MP-intensive strategies and builds
        /// Blocked by Storm but provides excellent resource sustainability for magic users
        /// 
        /// MP Regeneration Mechanics:
        /// - Turn-Based Recovery: Grants MP amount each turn during resolution
        /// - Maximum Cap: Cannot exceed character's MaxMP through regeneration
        /// - Stack Compatibility: Multiple regeneration effects provide cumulative MP
        /// - Resource Events: Triggers appropriate resource gain events for tracking
        /// 
        /// Strategic Applications:
        /// - Rok's Fierceful Recover: 40 MP per turn for 4 turns
        /// - Sustains high MP cost ability strategies
        /// - Supports essence-heavy builds requiring consistent MP availability
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving MP regeneration benefits</param>
        /// <param name="duration">Number of turns MP regeneration remains active</param>
        /// <param name="amount">MP amount regenerated per turn</param>
        public static void AddMpRegeneration(FactorManager fm, Character character, int duration = 4, int amount = 40) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.MP_REGEN);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.MP, amount } };
            fm.ApplyFactor(character, STATUS_EFFECT.MP_REGEN, duration, parameters);
        }

        /// <summary>
        /// Applies complete immunity effect for temporary damage and factor protection
        /// Ultimate protection status that blocks all negative effects and damage
        /// Cannot be blocked by Storm and provides absolute safety for limited duration
        /// 
        /// Immunity Mechanics:
        /// - Complete Protection: Blocks all damage, factors, and negative effects
        /// - Extended Duration: Default 999 turns for persistent protection
        /// - Absolute Priority: Cannot be blocked, removed, or bypassed by most effects
        /// - Strategic Timing: Requires careful timing due to limited availability
        /// 
        /// Usage Examples:
        /// - Rok's Blazing Dash: Complete immunity for escape or positioning
        /// - Ultimate ability protection during vulnerable states
        /// - Emergency defensive measure against overwhelming damage
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving complete immunity protection</param>
        /// <param name="duration">Number of turns immunity remains active (default 999)</param>
        public static void AddImmunity(FactorManager fm, Character character, int duration = 999) {
            var parameters = new Dictionary<string, int>();
            fm.ApplyFactor(character, STATUS_EFFECT.IMMUNE, duration, parameters);
        }
        
        #endregion
        
        #region Factor Resolution System
        
        /// <summary>
        /// Resolves Toughness shield protection during damage application
        /// Implements shield depletion mechanics with damage carry-over and shield breaking
        /// Handles multiple shield instances independently for proper damage absorption
        /// 
        /// Shield Resolution Process:
        /// 1. Iterate through all Toughness shields on character
        /// 2. For each shield, check if incoming damage exceeds DP value
        /// 3. If damage >= DP: Shield breaks, damage reduced by DP, continue to next shield
        /// 4. If damage < DP: Shield absorbs all damage, reduce shield DP, stop processing
        /// 5. Return remaining damage after all shields processed
        /// 
        /// Shield Management:
        /// - Independent Processing: Each shield handles damage separately
        /// - Breaking Logic: Shields remove themselves when depleted
        /// - Damage Tracking: Logs damage absorbed and shield status changes
        /// - Remainder Calculation: Accurate damage carry-over for LP application
        /// </summary>
        /// <param name="fm">FactorManager for shield instance management</param>
        /// <param name="character">Character whose shields will absorb damage</param>
        /// <param name="incomingDamage">Total damage before shield absorption</param>
        /// <returns>Remaining damage after all shields have been processed</returns>
        public static int ResolveToughness(FactorManager factorManager, Character character, int damage) {
            if (factorManager == null || character == null || damage <= 0) return damage;
            
            // FIXED: Get ALL shield instances, not just first
            var shields = factorManager.GetFactors(character, Character.STATUS_EFFECT.TOUGHNESS);
            if (shields.Count == 0) return damage;
            
            int remainingDamage = damage;
            
            // FIXED: Process shields sequentially until damage is absorbed or shields depleted
            for (int i = 0; i < shields.Count && remainingDamage > 0; i++) {
                var shield = shields[i];
                int shieldDP = shield.Params.GetValueOrDefault(ParamKeys.DP, 0);
                
                if (shieldDP > 0) {
                    int damageToShield = Math.Min(remainingDamage, shieldDP);
                    remainingDamage -= damageToShield;
                    shield.Params[ParamKeys.DP] = shieldDP - damageToShield;
                    
                    ConsoleLog.Combat($"Shield {i+1} absorbed {damageToShield} damage ({shieldDP - damageToShield} DP remaining)");
                    
                    // Remove shield if depleted
                    if (shield.Params[ParamKeys.DP] <= 0) {
                        factorManager.RemoveFactorInstance(character, Character.STATUS_EFFECT.TOUGHNESS, i);
                        ConsoleLog.Combat($"Shield {i+1} destroyed");
                        i--; // Adjust index after removal
                    }
                }
            }
            
            return remainingDamage;
        }

        /// <summary>
        /// Calculates Earth damage bonus from active Toughness factors
        /// Provides scaling Earth damage based on number of Toughness instances
        /// Used by damage calculation system for Earth essence attack enhancement
        /// 
        /// Earth Damage Calculation:
        /// - Base Bonus: +150 damage per Toughness factor instance
        /// - Stack Accumulation: Multiple Toughness effects provide cumulative bonuses
        /// - Dynamic Scaling: Bonus decreases as shields are consumed/broken
        /// - Essence Integration: Applied specifically to Earth type attacks
        /// 
        /// Strategic Impact:
        /// - Encourages Earth essence builds to maintain Toughness factors
        /// - Provides offensive scaling that diminishes as defensive protection is used
        /// - Creates risk/reward balance between offensive damage and defensive survival
        /// </summary>
        /// <param name="fm">FactorManager for Toughness factor queries</param>
        /// <param name="character">Character whose Toughness factors provide Earth damage bonus</param>
        /// <returns>Total Earth damage bonus from all active Toughness factors</returns>
        public static int GetToughnessEarthBonus(FactorManager fm, Character character) {
            var toughnessFactors = fm.GetFactors(character, Character.STATUS_EFFECT.TOUGHNESS);
            return toughnessFactors.Count * 150; // +150 Earth damage per Toughness
        }

        /// <summary>
        /// Resolves Healing factor effects with dual LP mechanics
        /// Enhanced healing system affecting both caster and opponent through different mechanisms
        /// Implements healing cap limits and opponent LP loss with defeat checking
        /// 
        /// Dual Healing Resolution:
        /// 1. Character Healing: Restore LP equal to HA parameter (capped at MaxLP)
        /// 2. Opponent LP Loss: Reduce opponent LP by half the heal amount (not damage)
        /// 3. Resource Events: Trigger appropriate healing and LP loss events
        /// 4. Defeat Checking: Check opponent defeat if LP loss reduces to zero
        /// 
        /// Important Distinction:
        /// - Character healing is normal LP restoration with maximum caps
        /// - Opponent effect is LP LOSS, not damage (bypasses shields and defense)
        /// - LP loss cannot be mitigated by any damage reduction effects
        /// - Defeat from LP loss still triggers normal defeat handling
        /// </summary>
        /// <param name="fm">FactorManager for Healing factor queries</param>
        /// <param name="character">Character receiving healing benefits</param>
        /// <param name="target">Opponent character suffering LP loss effects</param>
        public static void ResolveHealing(FactorManager fm, Character character, Character target) {
            var heals = fm.GetFactors(character, STATUS_EFFECT.HEALING);
            for (int i = 0; i < heals.Count; i++) {
                int ha = GetParamOrDefault(heals[i], ParamKeys.HA, 100);

                // Character heals (capped at max)
                int oldLP = character.LP;
                character.LP = Math.Min(character.LP + ha, character.MaxLP);
                int actualHealing = character.LP - oldLP;

                if (actualHealing > 0) {
                    GameEvents.TriggerHealingReceived(character, actualHealing);
                }

                // FIXED: Opponent loses LP (half the heal amount) - this is NOT damage according to design
                if (target != null) {
                    int lpLoss = ha / 2;
                    int oldTargetLP = target.LP;
                    target.LP = Math.Max(target.LP - lpLoss, 0);
                    int actualLoss = oldTargetLP - target.LP;
                    
                    if (actualLoss > 0) {
                        GameEvents.TriggerResourceLost(target, actualLoss, "LP");
                        ConsoleLog.Combat($"{target.CharName} lost {actualLoss} LP from {character.CharName}'s healing (not damage)");
                    }

                    // Check for defeat from LP loss
                    if (target.LP <= 0) {
                        GameEvents.TriggerPlayerDefeated(target);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves Recharge factor effects with EP stealing mechanics
        /// Resource transfer system that moves EP from opponent to caster with capacity limits
        /// Implements safe resource stealing with maximum cap protection and theft tracking
        /// 
        /// EP Stealing Resolution:
        /// 1. Calculate steal amount limited by opponent's current EP and RC parameter
        /// 2. Transfer stolen EP to caster (capped at caster's MaxEP)
        /// 3. Reduce opponent's EP by stolen amount
        /// 4. Trigger resource stealing events for both characters
        /// 
        /// Resource Protection:
        /// - Cannot steal more EP than opponent currently has
        /// - Cannot gain more EP than caster's maximum capacity
        /// - Excess stolen EP is lost if caster is at MaxEP
        /// - Resource events track both the theft and gain for UI updates
        /// </summary>
        /// <param name="fm">FactorManager for Recharge factor queries</param>
        /// <param name="character">Character gaining stolen EP</param>
        /// <param name="target">Opponent character losing EP</param>
        public static void ResolveRecharge(FactorManager fm, Character character, Character target) {
            var recharges = fm.GetFactors(character, STATUS_EFFECT.RECHARGE);
            for (int i = 0; i < recharges.Count; i++) {
                int amount = GetParamOrDefault(recharges[i], ParamKeys.RC, 150);
                int steal = Math.Min(amount, target.EP);
                character.EP = Math.Min(character.EP + steal, character.MaxEP);
                target.EP -= steal;

                if (steal > 0) {
                    GameEvents.TriggerResourceStolen(target, character, steal, "EP");
                }
            }
        }

        /// <summary>
        /// Resolves Growth factor effects with MP stealing mechanics
        /// Resource transfer system that moves MP from opponent to caster with capacity limits
        /// Implements safe resource stealing with maximum cap protection and theft tracking
        /// 
        /// MP Stealing Resolution:
        /// 1. Calculate steal amount limited by opponent's current MP and MP parameter
        /// 2. Transfer stolen MP to caster (capped at caster's MaxMP)
        /// 3. Reduce opponent's MP by stolen amount
        /// 4. Trigger resource stealing events for both characters
        /// 
        /// Resource Protection:
        /// - Cannot steal more MP than opponent currently has
        /// - Cannot gain more MP than caster's maximum capacity
        /// - Excess stolen MP is lost if caster is at MaxMP
        /// - Resource events track both the theft and gain for UI updates
        /// </summary>
        /// <param name="fm">FactorManager for Growth factor queries</param>
        /// <param name="character">Character gaining stolen MP</param>
        /// <param name="target">Opponent character losing MP</param>
        public static void ResolveGrowth(FactorManager fm, Character character, Character target) {
            var growths = fm.GetFactors(character, STATUS_EFFECT.GROWTH);
            for (int i = 0; i < growths.Count; i++) {
                int amount = GetParamOrDefault(growths[i], ParamKeys.MP, 100);
                int steal = Math.Min(amount, target.MP);
                character.MP = Math.Min(character.MP + steal, character.MaxMP);
                target.MP -= steal;

                if (steal > 0) {
                    GameEvents.TriggerResourceStolen(target, character, steal, "MP");
                }
            }
        }

        /// <summary>
        /// Resolves Storm damage effects with defense bypass mechanics
        /// Defense-bypassing damage system that applies storm damage through shield systems only
        /// Implements cumulative storm damage with proper defeat checking and resource tracking
        /// 
        /// Storm Damage Resolution:
        /// 1. Accumulate total storm damage from all active Storm instances
        /// 2. Apply damage through shield resolution (bypasses DEF/EssenceDEF)
        /// 3. Apply remaining damage directly to LP
        /// 4. Trigger damage events and defeat checking
        /// 
        /// Defense Bypass Mechanics:
        /// - Storm damage ignores normal DEF and EssenceDEF completely
        /// - Shields (Toughness) still provide protection through ResolveToughness
        /// - Remaining damage after shields directly reduces LP
        /// - Maintains shield interaction while bypassing armor-based defense
        /// </summary>
        /// <param name="fm">FactorManager for Storm factor queries</param>
        /// <param name="target">Character receiving storm damage</param>
        public static void ResolveStorm(FactorManager fm, Character target) {
            var storms = fm.GetFactors(target, STATUS_EFFECT.STORM);
            if (storms.Count == 0) return;

            int totalDmg = 0;
            for (int i = 0; i < storms.Count; i++)
                totalDmg += GetParamOrDefault(storms[i], ParamKeys.SD, 50);

            if (totalDmg > 0) {
                ConsoleLog.Combat($"{target.CharName} takes {totalDmg} Storm damage (bypasses DEF)");
                
                // FIXED: Storm damage bypasses DEF but goes through shield system
                // Apply damage directly through shield resolution only
                int remaining = ResolveToughness(fm, target, totalDmg);
                
                if (remaining > 0) {
                    int oldLP = target.LP;
                    target.LP = Math.Max(target.LP - remaining, 0);
                    
                    if (oldLP > target.LP) {
                        GameEvents.TriggerResourceLost(target, oldLP - target.LP, "LP");
                    }
                    
                    GameEvents.TriggerDamageDealt(target, totalDmg, target.LP);
                    
                    if (target.LP <= 0) {
                        GameEvents.TriggerPlayerDefeated(target);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves Burning damage effects with percentage calculation and character modifiers
        /// Enhanced burning system using DamageCalculator for proper character modifier application
        /// Implements defense-bypassing percentage damage with Fire essence scaling
        /// 
        /// Enhanced Burning Resolution:
        /// 1. Accumulate total burning percentage from all active Burning instances
        /// 2. Identify source character for burning damage modifier application
        /// 3. Apply percentage damage through DamageCalculator with Fire essence type
        /// 4. Benefit from character burning damage multipliers and charm bonuses
        /// 
        /// Character Modifier Integration:
        /// - Source character's GetBurningDamageMultiplier() enhances effectiveness
        /// - Charm bonuses and set effects apply through character calculation
        /// - Percentage damage bypasses defense but respects character scaling
        /// - Fire essence type ensures proper damage categorization and logging
        /// </summary>
        /// <param name="fm">FactorManager for Burning factor queries</param>
        /// <param name="target">Character receiving burning damage</param>
        public static void ResolveBurning(FactorManager fm, Character target) {
            var burns = fm.GetFactors(target, STATUS_EFFECT.BURNING);
            if (burns.Count == 0) return;

            int totalPercent = 0;
            for (int i = 0; i < burns.Count; i++)
                totalPercent += GetParamOrDefault(burns[i], ParamKeys.BD, 2);

            if (totalPercent <= 0) return;

            // Find the source character who applied burning (for modifiers)
            var gameManager = GameManager.Instance;
            Character source = null;
            if (gameManager != null) {
                source = gameManager.GetOpponent(target);
            }

            // Apply burning damage with character modifiers
            DamageCalculator.ApplyPercentageDamage(target, totalPercent, Character.ESSENCE_TYPE.FIRE, source);
        }

        /// <summary>
        /// Resolves MP regeneration effects for sustained magical resource recovery
        /// Turn-based MP restoration using CharacterLogic for proper resource gain handling
        /// Implements maximum cap protection and resource event triggering
        /// 
        /// MP Regeneration Resolution:
        /// 1. Process each MP regeneration instance independently
        /// 2. Grant MP amount through CharacterLogic.GainResource for proper capping
        /// 3. Trigger resource gain events for UI updates and tracking
        /// 4. Log regeneration for combat feedback and strategy analysis
        /// 
        /// Resource Management:
        /// - Uses CharacterLogic.GainResource for consistent resource handling
        /// - Automatic MaxMP cap enforcement through character logic
        /// - Resource events ensure UI synchronization and state tracking
        /// - Supports multiple regeneration sources with cumulative effects
        /// </summary>
        /// <param name="fm">FactorManager for MP regeneration factor queries</param>
        /// <param name="character">Character receiving MP regeneration benefits</param>
        public static void ResolveMpRegeneration(FactorManager fm, Character character) {
            var regens = fm.GetFactors(character, STATUS_EFFECT.MP_REGEN);
            for (int i = 0; i < regens.Count; i++) {
                int amount = GetParamOrDefault(regens[i], ParamKeys.MP, 40);
                CharacterLogic.GainResource(character, "MP", amount);
                ConsoleLog.Resource($"{character.CharName} regenerated {amount} MP from effect");
            }
        }
        
        #endregion
        
        #region Legacy Factor Application Methods
        
        /// <summary>
        /// Legacy AddBurning overload for backward compatibility
        /// Simplified burning application without source character integration
        /// Maintained for existing code that doesn't require character modifier support
        /// 
        /// Note: For new implementations, use the enhanced AddBurning with source parameter
        /// Enhanced version provides character passive integration and damage scaling
        /// </summary>
        /// <param name="fm">FactorManager for factor application</param>
        /// <param name="character">Character receiving burning damage</param>
        /// <param name="duration">Number of turns burning damage persists</param>
        /// <param name="bdPercent">Percentage of MaxLP dealt as damage per turn</param>
        public static void AddBurning(FactorManager fm, Character character, int duration = 2, int bdPercent = 2) {
            if (IsStormed(fm, character)) {
                GameEvents.TriggerFactorBlocked(character, STATUS_EFFECT.BURNING);
                return;
            }
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, bdPercent } };
            fm.ApplyFactor(character, STATUS_EFFECT.BURNING, duration, parameters);
        }

        /// <summary>
        /// Legacy FreezeCard overload for backward compatibility
        /// Simplified card freeze without source character integration
        /// Maintained for existing code that doesn't require character modifier support
        /// 
        /// Note: For new implementations, use the enhanced FreezeCard with source parameter
        /// Enhanced version provides duration bonuses and Yu's UP charging integration
        /// </summary>
        /// <param name="card">Card to freeze (must not be null or already frozen)</param>
        /// <param name="duration">Base freeze duration without character bonuses</param>
        public static void FreezeCard(Card card, int duration) {
            if (card != null && !card.IsFrozen) {
                card.Freeze(duration);
                GameEvents.TriggerCardFrozen(card, duration);
            }
        }
        
        #endregion
    }
}