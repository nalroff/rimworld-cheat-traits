using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChTunnel : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChTunnel()
        {
            compClass = typeof(CompAbilityEffect_ChTunnel);
        }
    }

    public class CompAbilityEffect_ChTunnel : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (caster == null || map == null)
                return;

            IntVec3 from = caster.Position;
            IntVec3 to = target.Cell;
            if (!to.IsValid || !to.InBounds(map) || from == to)
                return;

            // Walk the line caster → target with Bresenham, then widen each step
            // by the perpendicular axis (-1, 0, +1) to get the 3-wide passage.
            Vector2 axis = new Vector2(to.x - from.x, to.z - from.z);
            if (axis.sqrMagnitude < 0.0001f)
                return;
            axis.Normalize();
            // 90° perpendicular in cell-space — round to nearest cell offset.
            IntVec3 perp = new IntVec3(Mathf.RoundToInt(-axis.y), 0, Mathf.RoundToInt(axis.x));
            if (perp == IntVec3.Zero)
                perp = new IntVec3(0, 0, 1);

            HashSet<IntVec3> visited = new HashSet<IntVec3>();
            foreach (IntVec3 lineCell in GenSight.PointsOnLineOfSight(from, to))
            {
                for (int offset = -1; offset <= 1; offset++)
                {
                    IntVec3 cell = lineCell + perp * offset;
                    if (!visited.Add(cell))
                        continue;
                    TryExcavate(cell, map, caster);
                }
            }
            // PointsOnLineOfSight skips the endpoint; include the target row too.
            for (int offset = -1; offset <= 1; offset++)
            {
                IntVec3 cell = to + perp * offset;
                if (visited.Add(cell))
                    TryExcavate(cell, map, caster);
            }

            SoundDefOf.Building_Deconstructed.PlayOneShot(new TargetInfo(to, map, false));
            FleckMaker.Static(to.ToVector3Shifted(), map, FleckDefOf.DustPuffThick, 2f);
        }

        private static void TryExcavate(IntVec3 cell, Map map, Pawn caster)
        {
            if (!cell.InBounds(map))
                return;

            Mineable? mineable = cell.GetFirstMineable(map);
            if (mineable == null)
                return;

            // Pre-charge yieldPct using the caster's MiningYield stat so the
            // vanilla DestroyMined → TrySpawnYield path drops at the Digger's
            // multiplier. Notify_TookMiningDamage caps the amount at HitPoints,
            // so passing full HP fills yieldPct to exactly the stat value.
            mineable.Notify_TookMiningDamage(mineable.HitPoints, caster);
            mineable.DestroyMined(caster);
        }
    }
}
