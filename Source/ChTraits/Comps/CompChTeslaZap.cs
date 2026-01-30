using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace ChTraits.Comps
{
    public class CompProperties_ChTeslaZap : CompProperties
    {
        public float radius = 6f;
        public int cooldownTicks = 180;
        public int stunTicks = 120;
        public float damageAmount = -1f; // if <= 0, we'll try to match wooden spike trap damage
        public float armorPenetration = 0.15f;
        public bool requirePower = false; // coil is a generator; keep false unless you want it to stop if unpowered

        public CompProperties_ChTeslaZap()
        {
            compClass = typeof(CompChTeslaZap);
        }
    }

    public class CompChTeslaZap : ThingComp
    {
        private int nextZapTick = 0;

        private CompProperties_ChTeslaZap Props => (CompProperties_ChTeslaZap)props;

        private CompPowerTrader cachedPowerTrader;

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

            if (parent.Map == null) return;

            // Light-weight: only scan on/after cooldown
            int now = Find.TickManager.TicksGame;
            if (now < nextZapTick) return;

            CompProperties_ChTeslaZap props = Props;

            if (props.requirePower && cachedPowerTrader != null && !cachedPowerTrader.PowerOn)
            {
                nextZapTick = now + props.cooldownTicks;
                return;
            }

            Pawn target = FindHostilePawnInRange(props.radius);
            if (target == null)
            {
                // try again soon, but don't spam
                nextZapTick = now + 30;
                return;
            }

            DoZap(target, props);

            nextZapTick = now + props.cooldownTicks;
        }

        private Pawn FindHostilePawnInRange(float radius)
        {
            Map map = parent.Map;
            if (map == null) return null;

            Faction myFaction = parent.Faction;
            if (myFaction == null) return null; // no owner = no zapping (prevents weirdness)

            IntVec3 center = parent.PositionHeld;

            foreach (Thing t in GenRadial.RadialDistinctThingsAround(center, map, radius, useCenter: true))
            {
                Pawn p = t as Pawn;
                if (p == null) continue;
                if (!p.Spawned || p.Dead) continue;

                // “Hostile to owner faction” is the key versatility upgrade
                if (!p.HostileTo(myFaction)) continue;

                // Optional: ignore downed targets so it doesn’t farm/execute
                // if (p.Downed) continue;

                // LOS check so it doesn’t zap through walls
                if (!GenSight.LineOfSight(center, p.PositionHeld, map)) continue;

                return p;
            }

            return null;
        }

        private void DoZap(Pawn target, CompProperties_ChTeslaZap props)
        {
            Map map = parent.Map;

            // Visual
            try
            {
                FleckMaker.ThrowLightningGlow(target.DrawPos, map, 1.2f);
            }
            catch { /* ignore if method signature differs */ }

            // Stun
            try
            {
                target.stances?.stunner?.StunFor(props.stunTicks, parent);
            }
            catch { /* ignore */ }

            float dmg = props.damageAmount;
            if (dmg <= 0f)
                dmg = TryGetWoodenSpikeTrapDamageFallback(40f);

            try
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, dmg, props.armorPenetration, instigator: parent);
                target.TakeDamage(dinfo);
            }
            catch
            {
                // As a last resort, deal burn damage if Stab isn't available for some reason
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, dmg, props.armorPenetration, instigator: parent);
                target.TakeDamage(dinfo);
            }
        }

        private static float cachedSpikeTrapDamage = float.NaN;

        private static float TryGetWoodenSpikeTrapDamageFallback(float fallback)
        {
            if (!float.IsNaN(cachedSpikeTrapDamage)) return cachedSpikeTrapDamage;

            try
            {
                ThingDef trap = DefDatabase<ThingDef>.GetNamedSilentFail("TrapSpike");
                if (trap?.building == null)
                {
                    cachedSpikeTrapDamage = fallback;
                    return cachedSpikeTrapDamage;
                }

                // Use reflection so we don't hard-depend on internal field names across versions.
                object building = trap.building;
                Type bt = building.GetType();

                string[] fieldNames =
                {
                    "trapDamage",
                    "trapDamageBase",
                    "trapDamageDefault",
                    "trapDamageAmount",
                    "TrapDamage"
                };

                foreach (string name in fieldNames)
                {
                    FieldInfo fi = bt.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(int)))
                    {
                        object val = fi.GetValue(building);
                        cachedSpikeTrapDamage = Convert.ToSingle(val);
                        return cachedSpikeTrapDamage;
                    }

                    PropertyInfo pi = bt.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && (pi.PropertyType == typeof(float) || pi.PropertyType == typeof(int)))
                    {
                        object val = pi.GetValue(building, null);
                        cachedSpikeTrapDamage = Convert.ToSingle(val);
                        return cachedSpikeTrapDamage;
                    }
                }
            }
            catch { }

            cachedSpikeTrapDamage = fallback;
            return cachedSpikeTrapDamage;
        }

        private static int DistanceSquaredToRect(IntVec3 c, CellRect r)
        {
            // CellRect in RimWorld is inclusive (min..max).
            int dx = 0;
            if (c.x < r.minX) dx = r.minX - c.x;
            else if (c.x > r.maxX) dx = c.x - r.maxX;

            int dz = 0;
            if (c.z < r.minZ) dz = r.minZ - c.z;
            else if (c.z > r.maxZ) dz = c.z - r.maxZ;

            return dx * dx + dz * dz;
        }
    }
}
