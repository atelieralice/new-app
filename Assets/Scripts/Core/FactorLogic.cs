using System;
using System.Collections.Generic;
using static meph.Character;

// Gets a specific instance's parameter's value. This value is then used when "marching" factors
namespace meph {
    public static class FactorLogic {

        private static int GetParamOrDefault ( FactorInstance factor, string key, int defVal = 0 ) =>
            factor.Params != null && factor.Params.TryGetValue ( key, out var v ) ? v : defVal;

        // Calculate effective factor value from equipment bonuses
        private static int GetEffectiveFactorValue ( Character user, string paramKey, int baseValue ) {
            int bonus = user.GetEffectiveStat ( paramKey );
            return baseValue + bonus;
        }

        private static bool IsStormed ( FactorManager fm, Character target ) =>
            fm.GetFactors ( target, STATUS_EFFECT.STORM ).Count > 0;

        public static void AddToughness ( FactorManager fm, Character character, int duration = 2, int DP = 100 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveDP = GetEffectiveFactorValue ( character, ParamKeys.DP, DP );
            var parameters = new Dictionary<string, int> { { ParamKeys.DP, effectiveDP } };
            fm.ApplyFactor ( character, STATUS_EFFECT.TOUGHNESS, duration, parameters );
        }

        public static void AddHealing ( FactorManager fm, Character character, int duration = 2, int HA = 100 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveHA = GetEffectiveFactorValue ( character, ParamKeys.HA, HA );
            var parameters = new Dictionary<string, int> { { ParamKeys.HA, effectiveHA } };
            fm.ApplyFactor ( character, STATUS_EFFECT.HEALING, duration, parameters );
        }

        public static void AddRecharge ( FactorManager fm, Character character, int duration = 2, int RA = 150 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveRA = GetEffectiveFactorValue ( character, ParamKeys.RA, RA );
            var parameters = new Dictionary<string, int> { { ParamKeys.RA, effectiveRA } };
            fm.ApplyFactor ( character, STATUS_EFFECT.RECHARGE, duration, parameters );
        }

        public static void AddGrowth ( FactorManager fm, Character character, int duration = 2, int GA = 100 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveGA = GetEffectiveFactorValue ( character, ParamKeys.GA, GA );
            var parameters = new Dictionary<string, int> { { ParamKeys.GA, effectiveGA } };
            fm.ApplyFactor ( character, STATUS_EFFECT.GROWTH, duration, parameters );
        }

        public static void AddStorm ( FactorManager fm, Character character, int duration = 2, int SD = 50 ) {
            int effectiveSD = GetEffectiveFactorValue ( character, ParamKeys.SD, SD );
            var parameters = new Dictionary<string, int> { { ParamKeys.SD, effectiveSD } };
            fm.ApplyFactor ( character, STATUS_EFFECT.STORM, duration, parameters );
        }

        public static void AddBurning ( FactorManager fm, Character character, int duration = 2, int BD = 2 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveBD = GetEffectiveFactorValue ( character, ParamKeys.BD, BD );
            var parameters = new Dictionary<string, int> { { ParamKeys.BD, effectiveBD } };
            fm.ApplyFactor ( character, STATUS_EFFECT.BURNING, duration, parameters );
        }

        public static void AddFreeze ( FactorManager fm, Character character, int FT = 2 ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveFT = GetEffectiveFactorValue ( character, ParamKeys.FT, FT );
            var parameters = new Dictionary<string, int> { { ParamKeys.FT, effectiveFT } };
            fm.ApplyFactor ( character, STATUS_EFFECT.FREEZE, effectiveFT, parameters );
        }

        public static void FreezeCard ( FactorManager fm, Character character, Card.TYPE slotName, int FT ) {
            if ( IsStormed ( fm, character ) ) return;
            int effectiveFT = GetEffectiveFactorValue ( character, ParamKeys.FT, FT );
            if ( character.EquippedSlots.TryGetValue ( slotName, out var card ) ) {
                card.Freeze ( effectiveFT );
            }
        }

        // Shouldn't be used as there are no characters that directly unfreeze a card
        public static void UnfreezeCard ( Character character, Card.TYPE slotName ) {
            if ( character.EquippedSlots.TryGetValue ( slotName, out var card ) ) {
                card.Unfreeze ( );
            }
        }

        public static int ResolveToughness ( FactorManager fm, Character character, int damage ) {
            if ( damage <= 0 ) return 0;

            var shields = fm.GetFactors ( character, STATUS_EFFECT.TOUGHNESS );
            if ( shields.Count == 0 ) return damage;

            int totalDP = 0;
            for ( int i = 0; i < shields.Count; i++ )
                totalDP += GetParamOrDefault ( shields[i], ParamKeys.DP );

            if ( totalDP == 0 ) return damage;

            if ( damage >= totalDP ) {
                int remaining = damage - totalDP;
                for ( int i = 0; i < shields.Count; i++ ) {
                    shields[i].Duration = 0;
                    if ( shields[i].Params != null && shields[i].Params.ContainsKey ( ParamKeys.DP ) )
                        shields[i].Params[ParamKeys.DP] = 0;
                }
                return remaining;
            }

            int toConsume = damage;
            for ( int i = 0; i < shields.Count && toConsume > 0; i++ ) {
                var s = shields[i];
                int dp = GetParamOrDefault ( s, ParamKeys.DP );
                if ( dp <= 0 ) continue;

                if ( toConsume >= dp ) {
                    toConsume -= dp;
                    s.Duration = 0;
                    if ( s.Params != null ) s.Params[ParamKeys.DP] = 0;
                } else {
                    s.Params[ParamKeys.DP] = dp - toConsume;
                    toConsume = 0;
                }
            }
            return 0;
        }

        // Broken shields still count as instances. Currently it is fine, but may need to change 
        // along with other systems that rely on the number of shields in the future
        // A simple lambda would do later
        public static int GetToughnessEarthBonus ( FactorManager fm, Character character ) {
            var shields = fm.GetFactors ( character, STATUS_EFFECT.TOUGHNESS );
            return shields.Count * 150;
        }

        // Healing: healer gains HA (capped), target loses HA/2 (not damage; bypasses shields).
        public static void ResolveHealing ( FactorManager fm, Character character, Character target ) {
            var heals = fm.GetFactors ( character, STATUS_EFFECT.HEALING );
            for ( int i = 0; i < heals.Count; i++ ) {
                int HA = GetParamOrDefault ( heals[i], ParamKeys.HA, 100 );
                character.LP = Math.Min ( character.LP + HA, character.MaxLP );
                if ( target != null )
                    target.LP = Math.Max ( target.LP - ( HA / 2 ), 0 );
            }
        }

        public static void ResolveHealingInstant ( Character character, Character target, int HA = 100 ) {
            int effectiveHA = GetEffectiveFactorValue ( character, ParamKeys.HA, HA );
            character.LP = Math.Min ( character.LP + effectiveHA, character.MaxLP );
            if ( target != null )
                target.LP = Math.Max ( target.LP - ( effectiveHA / 2 ), 0 );
        }

        public static void ResolveRecharge ( FactorManager fm, Character character, Character target ) {
            var recharges = fm.GetFactors ( character, STATUS_EFFECT.RECHARGE );
            for ( int i = 0; i < recharges.Count; i++ ) {
                int RA = GetParamOrDefault ( recharges[i], ParamKeys.RA, 150 );
                int steal = Math.Min ( RA, target.EP );
                character.EP = Math.Min ( character.EP + steal, character.MaxEP );
                target.EP -= steal;
            }
        }

        public static void ResolveRechargeInstant ( Character character, Character target, int RA = 150 ) {
            int effectiveRA = GetEffectiveFactorValue ( character, ParamKeys.RA, RA );
            int steal = Math.Min ( effectiveRA, target.EP );
            character.EP = Math.Min ( character.EP + steal, character.MaxEP );
            target.EP -= steal;
        }

        public static void ResolveGrowth ( FactorManager fm, Character character, Character target ) {
            var growths = fm.GetFactors ( character, STATUS_EFFECT.GROWTH );
            for ( int i = 0; i < growths.Count; i++ ) {
                int GA = GetParamOrDefault ( growths[i], ParamKeys.GA, 100 );
                int steal = Math.Min ( GA, target.MP );
                character.MP = Math.Min ( character.MP + steal, character.MaxMP );
                target.MP -= steal;
            }
        }

        public static void ResolveGrowthInstant ( Character character, Character target, int GA = 100 ) {
            int effectiveGA = GetEffectiveFactorValue ( character, ParamKeys.GA, GA );
            int steal = Math.Min ( effectiveGA, target.MP );
            character.MP = Math.Min ( character.MP + steal, character.MaxMP );
            target.MP -= steal;
        }

        public static void ResolveStorm ( FactorManager fm, Character target ) {
            var storms = fm.GetFactors ( target, STATUS_EFFECT.STORM );
            if ( storms.Count == 0 ) return;

            int totalSD = 0;
            for ( int i = 0; i < storms.Count; i++ )
                totalSD += GetParamOrDefault ( storms[i], ParamKeys.SD, 50 );

            if ( totalSD > 0 )
                DamageLogic.ApplyDamage ( fm, target, totalSD, DAMAGE_TYPE.AIR );
        }

        public static void ResolveBurning ( FactorManager fm, Character target ) {
            var burns = fm.GetFactors ( target, STATUS_EFFECT.BURNING );
            if ( burns.Count == 0 ) return;

            int totalBD = 0;
            for ( int i = 0; i < burns.Count; i++ )
                totalBD += GetParamOrDefault ( burns[i], ParamKeys.BD, 2 );

            if ( totalBD <= 0 ) return;

            int dmg = target.MaxLP * totalBD / 100;
            DamageLogic.ApplyDamage ( fm, target, dmg, DAMAGE_TYPE.FIRE );
        }

        public static void ResolveCardFreeze ( Character character ) {
            foreach ( var card in character.EquippedSlots.Values ) {
                card.TickFreeze ( );
            }
        }
    }
}