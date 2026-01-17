using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    /// <summary>
    /// For Beastmasters, skip the vanilla Tame workgiver entirely.
    /// This prevents vanilla tame float menu options ("Prioritize taming", "Cannot tame ...") from appearing.
    /// Beastmasters will use the custom workgiver/job instead.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Tame), "ShouldSkip")]
    public static class ChBeastmasterDisableVanillaTameWorkgiver
    {
        static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result) return; // already skipping for vanilla reasons
            if (pawn == null) return;

            if (ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait))
                __result = true;
        }
    }
}
