using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
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
    }

    internal static class ChAscendantUtil
    {
        internal static bool IsAscendant(Pawn pawn) =>
            pawn != null && CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.AscendantTrait);
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
            if (map == null)
                return;

            HediffDef hediffDef = ChAscendantDefOf.ChAscendant_InspirationAura;
            if (hediffDef == null)
                return;

            CheatTraitsUtils.CollectEmitters(map, CheatTraitsNames.AscendantTrait, ascendants);
            if (ascendants.Count == 0)
                return;

            CheatTraitsUtils.ApplyAuraHediff(
                map,
                ascendants,
                targetPredicate: CheatTraitsUtils.IsHediffEligible,
                hediffDef: hediffDef,
                radiusSquared: ChAscendantAuraConfig.AuraRadiusSquared,
                refreshTicks: ChAscendantAuraConfig.InspirationRefreshTicks,
                humanlikesOnly: true
            );
        }
    }

    [DefOf]
    internal static class ChAscendantDefOf
    {
#pragma warning disable 0649
        public static HediffDef ChAscendant_InspirationAura = null!;
#pragma warning restore 0649

        static ChAscendantDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChAscendantDefOf));
    }
}
