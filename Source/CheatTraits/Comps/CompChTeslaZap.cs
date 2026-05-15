using System;
using System.Reflection;
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
        public float damageAmount = 45f; // if <= 0, we'll try to match wooden spike trap damage
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

            Pawn? target = FindHostilePawnInRange(props.radius);
            if (target == null)
            {
                // try again soon, but don't spam
                nextZapTick = now + 30;
                return;
            }

            DoZap(target, props);

            nextZapTick = now + props.cooldownTicks;
        }

        private Pawn? FindHostilePawnInRange(float radius)
        {
            Map map = parent.Map;
            if (map == null)
                return null;

            Faction myFaction = parent.Faction;
            if (myFaction == null)
                return null; // no owner = no zapping (prevents weirdness)

            IntVec3 center = parent.PositionHeld;

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
                    || !p.HostileTo(myFaction)
                    || p.Downed
                    || !GenSight.LineOfSight(center, p.PositionHeld, map)
                )
                {
                    continue;
                }

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
            catch
            { /* ignore if method signature differs */
            }

            try
            {
                FleckMaker.ThrowMicroSparks(parent.DrawPos, map);
            }
            catch
            { /* ignore */
            }

            try
            {
                DrawZapBolt(parent.DrawPos, target.DrawPos, map);
            }
            catch
            { /* ignore */
            }

            try
            {
                PlayZapSound(parent.Position, map);
            }
            catch
            { /* ignore */
            }

            // Stun
            try
            {
                target.stances?.stunner?.StunFor(props.stunTicks, parent);
            }
            catch
            { /* ignore */
            }

            float dmg = props.damageAmount;
            try
            {
                var damageDef = ResolveTeslaDamageDef();

                DamageInfo dinfo = new DamageInfo(
                    damageDef,
                    dmg,
                    props.armorPenetration,
                    instigator: parent
                );
                target.TakeDamage(dinfo);
            }
            catch
            {
                // As a last resort, deal burn damage if Stab isn't available for some reason
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Burn,
                    dmg,
                    props.armorPenetration,
                    instigator: parent
                );
                target.TakeDamage(dinfo);
            }
        }

        private static float cachedSpikeTrapDamage = float.NaN;

        private static float TryGetWoodenSpikeTrapDamageFallback(float fallback)
        {
            if (!float.IsNaN(cachedSpikeTrapDamage))
                return cachedSpikeTrapDamage;

            try
            {
                ThingDef? trap = DefDatabase<ThingDef>.GetNamedSilentFail("TrapSpike");
                if (trap?.building == null)
                {
                    cachedSpikeTrapDamage = fallback;
                    return cachedSpikeTrapDamage;
                }

                // Use reflection so we don't hard-depend on internal field names across versions.
                object building = trap.building!;
                Type bt = building.GetType();

                string[] fieldNames =
                {
                    "trapDamage",
                    "trapDamageBase",
                    "trapDamageDefault",
                    "trapDamageAmount",
                    "TrapDamage",
                };

                foreach (string name in fieldNames)
                {
                    FieldInfo fi = bt.GetField(
                        name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    if (
                        fi != null
                        && (fi.FieldType == typeof(float) || fi.FieldType == typeof(int))
                    )
                    {
                        object val = fi.GetValue(building);
                        if (val == null)
                            continue;
                        cachedSpikeTrapDamage = Convert.ToSingle(val);
                        return cachedSpikeTrapDamage;
                    }

                    PropertyInfo pi = bt.GetProperty(
                        name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    if (
                        pi != null
                        && (pi.PropertyType == typeof(float) || pi.PropertyType == typeof(int))
                    )
                    {
                        object val = pi.GetValue(building, null);
                        if (val == null)
                            continue;
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
            if (c.x < r.minX)
                dx = r.minX - c.x;
            else if (c.x > r.maxX)
                dx = c.x - r.maxX;

            int dz = 0;
            if (c.z < r.minZ)
                dz = r.minZ - c.z;
            else if (c.z > r.maxZ)
                dz = c.z - r.maxZ;

            return dx * dx + dz * dz;
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
