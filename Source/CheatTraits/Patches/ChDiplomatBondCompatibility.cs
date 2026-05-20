using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    // Pawn_RelationsTracker.CompatibilityWith is computed every call (no cache), so a
    // postfix is sufficient — there's no stale-state risk.
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.CompatibilityWith))]
    internal static class ChDiplomatBondCompatibilityPatch
    {
        // Matches the upper inflection of InteractionWorker_DeepTalk's curve (×3 selection
        // weight) and pushes NegativeInteractionUtility's curve down to ~×0.5. Going higher
        // gains nothing on DeepTalk.
        private const float BondedCompatibility = 2.0f;

        public static void Postfix(Pawn ___pawn, Pawn otherPawn, ref float __result)
        {
            if (___pawn == null || otherPawn == null || ___pawn == otherPawn)
                return;

            ChDiplomatBondsGameComponent? bonds = ChDiplomatBondsGameComponent.Instance;
            if (bonds != null && bonds.IsBonded(___pawn, otherPawn))
                __result = BondedCompatibility;
        }
    }
}
