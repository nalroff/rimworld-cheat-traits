using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf))]
    internal static class ChDiplomatBondOpinionPatch
    {
        public static void Postfix(Pawn ___pawn, Pawn other, ref int __result)
        {
            if (___pawn == null || other == null || ___pawn == other)
                return;
            // Match vanilla's hard early-returns so we don't resurrect opinion for dead
            // pawns or non-humanlike targets (vanilla returns 0 for both).
            if (___pawn.Dead)
                return;
            if (!other.RaceProps.Humanlike)
                return;

            ChDiplomatBondsGameComponent? bonds = ChDiplomatBondsGameComponent.Instance;
            if (bonds != null && bonds.IsBonded(___pawn, other))
                __result = 100;
        }
    }
}
