using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_ChTeslaZap : CompProperties
    {
        public float radius = 6f;
        public int cooldownTicks = 180;
        public int stunTicks = 120;
        public bool requirePower = false; // coil is a generator; keep false unless you want it to stop if unpowered

        // Chain lightning: the pulse arcs from the coil to the nearest hostile, then
        // hops to the next-nearest hostile within chainRadius, up to chainCount targets.
        public int chainCount = 4;
        public float chainRadius = 5f;

        // Flesh targets (humanoids, animals) are tuned to DOWN rather than kill so
        // attackers can still be captured. A flesh pawn already below
        // fleshSpareHealthPercent takes no damage (stun only) so the coil won't land a
        // killing blow on someone about to collapse; downed pawns are never re-targeted.
        public float fleshDamage = 16f;
        public float fleshArmorPenetration = 0.15f;
        public float fleshSpareHealthPercent = 0.30f;

        // Non-flesh targets (mechanoids, drones) are tuned to DESTROY: armor-piercing so
        // heavy mech plating no longer soaks the whole hit.
        public float mechDamage = 55f;
        public float mechArmorPenetration = 1.2f;

        public CompProperties_ChTeslaZap()
        {
            compClass = typeof(CompChTeslaZap);
        }
    }

    public class CompChTeslaZap : ThingComp
    {
        private int nextZapTick = 0;

        private CompProperties_ChTeslaZap Props => (CompProperties_ChTeslaZap)props;

        private CompPowerTrader? cachedPowerTrader;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            cachedPowerTrader = parent.GetComp<CompPowerTrader>();
            if (!respawningAfterLoad)
            {
                nextZapTick = Find.TickManager.TicksGame + Rand.RangeInclusive(60, 120);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent.Map == null)
                return;

            // Light-weight: only scan on/after cooldown
            int now = Find.TickManager.TicksGame;
            if (now < nextZapTick)
                return;

            CompProperties_ChTeslaZap props = Props;

            if (props.requirePower && cachedPowerTrader != null && !cachedPowerTrader.PowerOn)
            {
                nextZapTick = now + props.cooldownTicks;
                return;
            }

            List<Pawn> chain = BuildChain(props);
            if (chain.Count == 0)
            {
                // try again soon, but don't spam
                nextZapTick = now + 30;
                return;
            }

            DoZap(chain, props);

            nextZapTick = now + props.cooldownTicks;
        }

        // Builds the chain of victims: nearest hostile to the coil, then each subsequent
        // hop is the nearest not-yet-hit hostile within chainRadius of the previous link.
        private List<Pawn> BuildChain(CompProperties_ChTeslaZap props)
        {
            var chain = new List<Pawn>();

            Map map = parent.Map;
            if (map == null)
                return chain;

            Faction myFaction = parent.Faction;
            if (myFaction == null)
                return chain; // no owner = no zapping (prevents weirdness)

            var hit = new HashSet<Pawn>();
            IntVec3 fromCell = parent.PositionHeld;
            float searchRadius = props.radius;

            for (int i = 0; i < props.chainCount; i++)
            {
                Pawn? next = FindNearestHostile(fromCell, searchRadius, map, myFaction, hit);
                if (next == null)
                    break;

                chain.Add(next);
                hit.Add(next);
                fromCell = next.PositionHeld;
                searchRadius = props.chainRadius; // hops after the first use the shorter arc range
            }

            return chain;
        }

        private Pawn? FindNearestHostile(
            IntVec3 center,
            float radius,
            Map map,
            Faction myFaction,
            HashSet<Pawn> exclude
        )
        {
            // RadialDistinctThingsAround walks outward, so the first match is ~nearest.
            foreach (
                Thing t in GenRadial.RadialDistinctThingsAround(
                    center,
                    map,
                    radius,
                    useCenter: true
                )
            )
            {
                Pawn? p = t as Pawn;
                if (
                    p == null
                    || !p.Spawned
                    || p.Dead
                    || p.Downed
                    || !p.HostileTo(myFaction)
                    || exclude.Contains(p)
                    || !GenSight.LineOfSight(center, p.PositionHeld, map)
                )
                {
                    continue;
                }

                return p;
            }

            return null;
        }

        private void DoZap(List<Pawn> chain, CompProperties_ChTeslaZap props)
        {
            Map map = parent.Map;
            DamageDef damageDef = ResolveTeslaDamageDef();

            try
            {
                PlayZapSound(parent.Position, map);
            }
            catch
            { /* ignore */
            }

            try
            {
                FleckMaker.ThrowMicroSparks(parent.DrawPos, map);
            }
            catch
            { /* ignore */
            }

            Vector3 prev = parent.DrawPos;
            foreach (Pawn target in chain)
            {
                try
                {
                    DrawZapBolt(prev, target.DrawPos, map);
                }
                catch
                { /* ignore */
                }

                try
                {
                    FleckMaker.ThrowLightningGlow(target.DrawPos, map, 1.2f);
                }
                catch
                { /* ignore if method signature differs */
                }

                ZapTarget(target, damageDef, props);
                prev = target.DrawPos;
            }
        }

        private void ZapTarget(Pawn target, DamageDef damageDef, CompProperties_ChTeslaZap props)
        {
            // Stun goes through StunHandler.StunFor, which bypasses EMP adaptation — so this
            // stays reliable on mechanoids no matter how many times they've been hit.
            try
            {
                target.stances?.stunner?.StunFor(props.stunTicks, parent);
            }
            catch
            { /* ignore */
            }

            float dmg;
            float ap;

            if (target.RaceProps != null && target.RaceProps.IsFlesh)
            {
                // Capture-friendly: don't finish off a flesh pawn that's already collapsing.
                // Stun still lands so it stays locked down for arrest.
                if (
                    target.health != null
                    && target.health.summaryHealth.SummaryHealthPercent
                        < props.fleshSpareHealthPercent
                )
                {
                    return;
                }

                dmg = props.fleshDamage;
                ap = props.fleshArmorPenetration;
            }
            else
            {
                dmg = props.mechDamage;
                ap = props.mechArmorPenetration;
            }

            try
            {
                DamageInfo dinfo = new DamageInfo(damageDef, dmg, ap, instigator: parent);
                target.TakeDamage(dinfo);
            }
            catch
            {
                // As a last resort, deal burn damage if the resolved def misbehaves.
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, dmg, ap, instigator: parent);
                target.TakeDamage(dinfo);
            }
        }

        private static void DrawZapBolt(Vector3 start, Vector3 end, Map map)
        {
            Vector3 delta = end - start;
            float horizontalLen = new Vector2(delta.x, delta.z).magnitude;
            if (horizontalLen < 0.01f)
                return;

            Vector3 perp = new Vector3(-delta.z, 0f, delta.x) / horizontalLen;

            int segments = Rand.RangeInclusive(3, 5);
            float maxOffset = Mathf.Min(0.35f, horizontalLen * 0.15f);

            Vector3 prev = start;
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next;
                if (i == segments)
                {
                    next = end;
                }
                else
                {
                    float t = (float)i / segments;
                    // Taper offset toward 0 at the endpoints, peak in the middle.
                    float taper = 1f - Mathf.Abs(t - 0.5f) * 2f;
                    float offset = Rand.Range(-maxOffset, maxOffset) * taper;
                    next = Vector3.Lerp(start, end, t) + perp * offset;
                    next.y = start.y;
                }

                FleckMaker.ConnectingLine(prev, next, FleckDefOf.LineEMP, map, 1.2f);
                prev = next;
            }
        }

        private static SoundDef? cachedZapSound;
        private static bool cachedZapSoundResolved;

        private static void PlayZapSound(IntVec3 cell, Map map)
        {
            if (!cachedZapSoundResolved)
            {
                string[] candidates =
                {
                    "EnergyShield_Broken",
                    "OrbitalBeam_Ongoing",
                    "Pawn_Melee_Punch_HitBuilding_Generic",
                };
                foreach (string name in candidates)
                {
                    SoundDef sd = DefDatabase<SoundDef>.GetNamedSilentFail(name);
                    if (sd != null)
                    {
                        cachedZapSound = sd;
                        break;
                    }
                }
                cachedZapSoundResolved = true;
            }

            if (cachedZapSound != null)
            {
                cachedZapSound.PlayOneShot(new TargetInfo(cell, map));
            }
        }

        private static DamageDef ResolveTeslaDamageDef()
        {
            // Always-safe fallback
            DamageDef def = DamageDefOf.Burn;

            // Try to use ElectricalBurn only if it's actually present in this modlist.
            // This avoids hard reliance on DefOf initialization and DLC presence.
            DamageDef maybe = DefDatabase<DamageDef>.GetNamedSilentFail("ElectricalBurn");
            if (maybe != null)
                def = maybe;

            return def;
        }
    }
}
