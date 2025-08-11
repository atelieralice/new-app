using System;
using System.Collections.Generic;
using static meph.Character;

namespace meph {
    // FactorLogic contains stateless helpers to:
    // - Apply effects (build Param dictionaries and call FactorManager.ApplyFactor).
    // - Resolve effect ticks (healing, burning, storm) and shield math.
    // Storage lives in FactorManager; HP loss is finalized by GameManager.ApplyDamage.
    // Design notes:
    // - Durations are instance-based for all effects (each application is its own timer).
    // - FREEZE and STORM are overwrite-only (single instance), enforced by FactorManager.
    public static class FactorLogic {

        // Convenience check used to gate effect applications while target is under STORM.
        // STORM acts as a blocker; we don’t apply new buffs/debuffs to that target.
        private static bool IsStormed ( FactorManager fm, Character target ) =>
            fm.GetFactors ( target, STATUS_EFFECT.STORM ).Count > 0;

        // Safe param read with a default. Params are optional and sparse by design.
        private static int GetParamOrDefault ( FactorInstance fi, string key, int defVal = 0 ) =>
            fi.Params != null && fi.Params.TryGetValue ( key, out var v ) ? v : defVal;

        // Shield (TOUGHNESS). Instance-based: each shield has its own duration and DP pool.
        public static void AddToughness ( FactorManager factorManager, Character character, int duration = 2, int dp = 100 ) {
            if ( IsStormed ( factorManager, character ) ) return;
            var parameters = new Dictionary<string, int> { { ParamKeys.DP, dp } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.TOUGHNESS, duration, parameters );
        }

        // HEALING over time. Instance-based; each tick adds HA up to MaxLP.
        public static void AddHealing ( FactorManager factorManager, Character character, int duration = 2, int ha = 100 ) {
            if ( IsStormed ( factorManager, character ) ) return;
            var parameters = new Dictionary<string, int> { { ParamKeys.HA, ha } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.HEALING, duration, parameters );
        }

        // RECHARGE over time. Instance-based; steals EP from target each tick.
        public static void AddRecharge ( FactorManager factorManager, Character character, int duration = 2, int recharge = 150 ) {
            if ( IsStormed ( factorManager, character ) ) return;
            var parameters = new Dictionary<string, int> { { ParamKeys.RC, recharge } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.RECHARGE, duration, parameters );
        }

        // GROWTH over time. Instance-based; steals MP from target each tick.
        public static void AddGrowth ( FactorManager factorManager, Character character, int duration = 2, int growthMp = 100 ) {
            if ( IsStormed ( factorManager, character ) ) return;
            var parameters = new Dictionary<string, int> { { ParamKeys.MP, growthMp } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.GROWTH, duration, parameters );
        }

        // STORM is overwrite-only: re-applying refreshes the single instance and its SD.
        public static void AddStorm ( FactorManager factorManager, Character character, int duration = 2, int stormDamage = 50 ) {
            var parameters = new Dictionary<string, int> { { ParamKeys.SD, stormDamage } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.STORM, duration, parameters );
        }

        // BURNING is instance-based: each application adds its own BD and timer.
        // Per tick we sum all active instances' BD% against target.MaxLP.
        public static void AddBurning ( FactorManager factorManager, Character character, int duration = 2, int bdPercent = 2 ) {
            if ( IsStormed ( factorManager, character ) ) return;
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, bdPercent } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.BURNING, duration, parameters );
        }

        // FREEZE is overwrite-only: a single instance with FD is kept and refreshed.
        public static void AddFreeze ( FactorManager factorManager, Character character, int duration = 2 ) {
            var parameters = new Dictionary<string, int> { { ParamKeys.FD, duration } };
            factorManager.ApplyFactor ( character, STATUS_EFFECT.FREEZE, duration, parameters );
        }

        // Card-local freeze toggles. Keep these on Card for encapsulation; expose wrappers here for orchestration.
        public static void FreezeCard ( Card card, int duration ) => card.Freeze ( duration );
        public static void UnfreezeCard ( Card card ) => card.Unfreeze ( );

        // Shield resolution:
        // - Consume DP across shield instances until damage is fully absorbed or shields depleted.
        // - When fully consuming a shield, we zero its DP and set Duration=0 to avoid double-counting before cleanup.
        // - Return remaining damage for GameManager.ApplyDamage to subtract from LP.
        public static int ResolveToughness ( FactorManager factorManager, Character character, int damage ) {
            if ( damage <= 0 ) return 0;

            var shields = factorManager.GetFactors ( character, STATUS_EFFECT.TOUGHNESS );
            if ( shields.Count == 0 ) return damage;

            int totalDP = 0;
            for ( int i = 0; i < shields.Count; i++ )
                totalDP += GetParamOrDefault ( shields[i], ParamKeys.DP, 0 );

            if ( totalDP == 0 ) return damage;

            if ( damage >= totalDP ) {
                int remaining = damage - totalDP;
                for ( int i = 0; i < shields.Count; i++ ) {
                    // fully consumed this turn; mark for cleanup and prevent re-count
                    shields[i].Duration = 0;
                    if ( shields[i].Params != null && shields[i].Params.ContainsKey ( ParamKeys.DP ) )
                        shields[i].Params[ParamKeys.DP] = 0;
                }
                return remaining;
            }

            int toConsume = damage;
            for ( int i = 0; i < shields.Count && toConsume > 0; i++ ) {
                var s = shields[i];
                int dp = GetParamOrDefault ( s, ParamKeys.DP, 0 );
                if ( dp <= 0 ) continue;

                if ( toConsume >= dp ) {
                    toConsume -= dp;
                    s.Duration = 0;           // fully consumed
                    if ( s.Params != null ) s.Params[ParamKeys.DP] = 0; // avoid double-count before cleanup tick
                } else {
                    s.Params[ParamKeys.DP] = dp - toConsume;
                    toConsume = 0;
                }
            }
            return 0; // all damage absorbed by shields
        }

        // Example of a derived bonus: presence of any shield grants an Earth damage bonus.
        public static int GetToughnessEarthBonus ( FactorManager factorManager, Character character ) =>
            factorManager.GetFactors ( character, STATUS_EFFECT.TOUGHNESS ).Count > 0 ? 150 : 0;

        // Tick resolvers. These read instances from FactorManager and apply per-turn outcomes.

        public static void ResolveHealing ( FactorManager factorManager, Character target ) {
            var heals = factorManager.GetFactors ( target, STATUS_EFFECT.HEALING );
            for ( int i = 0; i < heals.Count; i++ ) {
                int ha = GetParamOrDefault ( heals[i], ParamKeys.HA, 100 );
                target.LP = Math.Min ( target.LP + ha, target.MaxLP );
            }
        }

        public static void ResolveRecharge ( FactorManager factorManager, Character character, Character target ) {
            var recharges = factorManager.GetFactors ( character, STATUS_EFFECT.RECHARGE );
            for ( int i = 0; i < recharges.Count; i++ ) {
                int amount = GetParamOrDefault ( recharges[i], ParamKeys.RC, 150 );
                int steal = Math.Min ( amount, target.EP );
                character.EP = Math.Min ( character.EP + steal, character.MaxEP );
                target.EP -= steal;
            }
        }

        public static void ResolveGrowth ( FactorManager factorManager, Character character, Character target ) {
            var growths = factorManager.GetFactors ( character, STATUS_EFFECT.GROWTH );
            for ( int i = 0; i < growths.Count; i++ ) {
                int amount = GetParamOrDefault ( growths[i], ParamKeys.MP, 100 );
                int steal = Math.Min ( amount, target.MP );
                character.MP = Math.Min ( character.MP + steal, character.MaxMP );
                target.MP -= steal;
            }
        }

        // STORM ticks deal summed SD to the target and also block new effect applications via IsStormed gate.
        public static void ResolveStorm ( FactorManager factorManager, Character target ) {
            var storms = factorManager.GetFactors ( target, STATUS_EFFECT.STORM );
            if ( storms.Count == 0 ) return;

            int totalDmg = 0;
            for ( int i = 0; i < storms.Count; i++ )
                totalDmg += GetParamOrDefault ( storms[i], ParamKeys.SD, 50 );

            if ( totalDmg > 0 )
                GameManager.ApplyDamage ( factorManager, target, totalDmg );
        }

        // BURNING ticks: sum BD across active instances (each with its own duration) and hit MaxLP * total%.
        public static void ResolveBurning ( FactorManager factorManager, Character target ) {
            var burns = factorManager.GetFactors ( target, STATUS_EFFECT.BURNING );
            if ( burns.Count == 0 ) return;

            int totalPercent = 0;
            for ( int i = 0; i < burns.Count; i++ )
                totalPercent += GetParamOrDefault ( burns[i], ParamKeys.BD, 2 );

            if ( totalPercent <= 0 ) return;

            int dmg = target.MaxLP * totalPercent / 100;
            GameManager.ApplyDamage ( factorManager, target, dmg );
        }
    }
}