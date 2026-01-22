using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChAscendantAuraConfig
    {
        internal const int AuraRadius = 20;
        internal const int AuraRadiusSquared = AuraRadius * AuraRadius;

        // How often we scan & apply (ticks). 250 = ~4 seconds at 60 TPS.
        internal const int ScanIntervalTicks = 250;

        // Refresh this exact remaining duration every time we apply the aura.
        // Should match the HediffDef's disappearsAfterTicks (3 hours = 7500 ticks).
        internal const int InspirationRefreshTicks = 7500;

        internal const string AscendantTraitDefName = "ChAscendant";
    }

    internal static class ChAscendantUtil
    {
        internal const string AscendantTrait = "ChAscendant";

        internal static bool IsAscendant(Pawn pawn)
            => pawn != null && ChTraitsUtils.HasTrait(pawn, AscendantTrait);
    }

    /// <summary>
    /// System entry point for your combined MapComponent.
    /// Call TickMap(map) every ScanIntervalTicks (or let the system self-gate).
    ///
    /// Conventions:
    /// - Downed pawns are allowed to emit and receive auras.
    /// - Ascendant aura affects same-faction humanlikes only (via IsAuraAlly default).
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

            // Collect ascendants (emitters). Downed allowed.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.story?.traits == null) continue;
                if (ChAscendantUtil.IsAscendant(p))
                {
                    ascendants.Add(p);
                }
            }

            if (ascendants.Count == 0) return;

            HediffDef hediffDef = ChAscendantDefOf.ChAscendant_InspirationAura;
            if (hediffDef == null) return;

            // Apply inspiration hediff to nearby same-faction humanlikes.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];

                // Eligibility for hediff application (do this once per target).
                if (!ChTraitsUtils.IsHediffEligible(target)) continue;

                IntVec3 tPos = target.Position;

                for (int j = 0; j < ascendants.Count; j++)
                {
                    Pawn source = ascendants[j];
                    if (source == null || source.Dead || !source.Spawned) continue;
                    if (source.Map != map) continue;

                    // Range check first (cheap).
                    if ((source.Position - tPos).LengthHorizontalSquared > ChAscendantAuraConfig.AuraRadiusSquared) continue;

                    // Same-faction "ally" check (blocks self-aura; humanlikesOnly defaults true).
                    if (!ChTraitsUtils.IsAuraAlly(source, target)) continue;

                    EnsureInspirationAuraHediff(target, hediffDef);
                    break;
                }
            }
        }

        private static void EnsureInspirationAuraHediff(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn?.health?.hediffSet == null) return;
            if (hediffDef == null) return;

            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (h == null)
            {
                h = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(h);
            }

            // Refresh linger time every time we "touch" them with the aura.
            HediffComp_Disappears comp = h.TryGetComp<HediffComp_Disappears>();
            if (comp != null)
            {
                comp.ticksToDisappear = ChAscendantAuraConfig.InspirationRefreshTicks;
            }
        }
    }

    [DefOf]
    internal static class ChAscendantDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChAscendant_InspirationAura;
        #pragma warning restore 0649

        static ChAscendantDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChAscendantDefOf));
    }
}
