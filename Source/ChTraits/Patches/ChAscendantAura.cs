using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChAscendantAuraConfig
    {
        internal const int AuraRadius = 20;

        // How often we scan & apply (ticks). 250 = ~4 seconds at 60 TPS.
        internal const int ScanIntervalTicks = 250;

        internal const string AscendantTraitDefName = "ChAscendant";
    }

    internal static class ChAscendantUtil
    {
        internal const string AscendantTrait = "ChAscendant";

        internal static bool IsAscendant(Pawn pawn)
            => pawn != null && ChTraitsUtils.HasTrait(pawn, AscendantTrait);

        internal static bool SameMapAndSpawned(Pawn a, Pawn b)
            => a != null && b != null && a.Spawned && b.Spawned && a.Map == b.Map;

        internal static bool InRadius(IntVec3 a, IntVec3 b, int radius)
            => (a - b).LengthHorizontalSquared <= radius * radius;
    }

    /// <summary>
    /// NEW: system entry point for your combined MapComponent.
    /// Call TickMap(map) every ScanIntervalTicks (or let the system self-gate).
    /// </summary>
    internal static class ChAscendantAuraSystem
    {
        private static readonly List<Pawn> ascendants = new List<Pawn>(8);

        public static void TickMap(Map map)
        {
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            ascendants.Clear();

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (ChAscendantUtil.IsAscendant(p))
                {
                    ascendants.Add(p);
                }
            }

            if (ascendants.Count == 0) return;

            // Apply growth hediff to nearby children.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!ChTraitsUtils.IsValidPlayerColonistTarget(p)) continue;

                bool inAura = false;
                for (int j = 0; j < ascendants.Count; j++)
                {
                    Pawn a = ascendants[j];
                    if (!ChAscendantUtil.SameMapAndSpawned(a, p)) continue;

                    if (ChAscendantUtil.InRadius(a.Position, p.Position, ChAscendantAuraConfig.AuraRadius))
                    {
                        inAura = true;
                        break;
                    }
                }

                if (inAura) {
                    EnsureSkillAuraHediff(p);
                }
            }
        }
        

        private static void EnsureSkillAuraHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;

            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(ChAscendantDefOf.ChAscendant_SkillAura);
            if (h == null)
            {
                h = HediffMaker.MakeHediff(ChAscendantDefOf.ChAscendant_SkillAura, pawn);
                pawn.health.AddHediff(h);
            }

            // Refresh linger time every time we "touch" them with the aura
            HediffComp_Disappears comp = h.TryGetComp<HediffComp_Disappears>();
            if (comp != null)
            {
                comp.ticksToDisappear = 7500; // 3h fixed; or randomize slightly if desired
            }
        }
    }

    [DefOf]
    internal static class ChAscendantDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChAscendant_SkillAura;
        #pragma warning restore 0649

        static ChAscendantDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChAscendantDefOf));
    }


}
