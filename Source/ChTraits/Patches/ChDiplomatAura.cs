using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChDiplomatAuraConfig
    {
        internal const int AuraRadius = 16;
        internal const int AuraRadiusSquared = AuraRadius * AuraRadius;
        internal const int ScanIntervalTicks = 250;

        // 6 hours linger; you can bump to 8-12h if you want “always on” in base
        internal const int PresenceLingerTicks = 6 * 2500;
    }

    internal static class ChDiplomatAuraSystem
    {
        private static readonly List<Pawn> diplomats = new List<Pawn>(4);

        public static void TickMap(Map map)
        {
            if (map == null) return;

            ChTraitsUtils.CollectEmitters(map, ChTraitsNames.DiplomatTrait, diplomats);
            if (diplomats.Count == 0) return;

            ChTraitsUtils.ApplyAuraHediff(
                map,
                diplomats,
                targetPredicate: ChTraitsUtils.IsHediffEligible,
                hediffDef: ChDiplomatDefOf.ChDiplomat_Presence,
                radiusSquared: ChDiplomatAuraConfig.AuraRadiusSquared,
                refreshTicks: ChDiplomatAuraConfig.PresenceLingerTicks,
                humanlikesOnly: true);
        }
    }

    [DefOf]
    internal static class ChDiplomatDefOf
    {
#pragma warning disable 0649
        public static HediffDef ChDiplomat_Presence;
#pragma warning restore 0649

        static ChDiplomatDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChDiplomatDefOf));
    }
}
