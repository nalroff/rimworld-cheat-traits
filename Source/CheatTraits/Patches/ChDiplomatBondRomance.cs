using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    // Boost romance chance only when the engine would already permit it. Vanilla
    // SecondaryRomanceChanceFactor returns 0 for hard blocks:
    //   - orientation (Asexual / wrong-gender pairing) and age < 16 — from SecondaryLovinChanceFactor
    //   - different species (def mismatch) — same
    //   - incest — family PawnRelationDefs carry romanceChanceFactor = 0, zeroing the product
    //   - missing-gene block — same multiplicative chain
    // So `__result <= 0` means the engine has said "no" and we respect that. Any positive
    // result means it's allowed; we push to 2.0 so SuccessChance clamps to 100%.
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
    internal static class ChDiplomatBondRomancePatch
    {
        private const float BondedRomanceFactor = 2.0f;

        public static void Postfix(Pawn ___pawn, Pawn otherPawn, ref float __result)
        {
            if (___pawn == null || otherPawn == null || ___pawn == otherPawn)
                return;
            if (__result <= 0f)
                return;

            ChDiplomatBondsGameComponent? bonds = ChDiplomatBondsGameComponent.Instance;
            if (bonds != null && bonds.IsBonded(___pawn, otherPawn))
                __result = BondedRomanceFactor;
        }
    }
}
