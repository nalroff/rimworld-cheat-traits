using HarmonyLib;
using Verse;

namespace CheatTraits.Patches
{
    // Mirrors the vanilla non-passive mutant pattern at Pawn.CombinedDisabledWorkTags:
    // strip WorkTags.Violent off the resolved tag set while the Super Soldier hediff
    // is active, so a Pacifist target can fight, draft, equip the conjured rifle, and
    // be ordered to attack for the duration of the buff.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags), MethodType.Getter)]
    internal static class Patch_Pawn_CombinedDisabledWorkTags_SuperSoldier
    {
        public static void Postfix(Pawn __instance, ref WorkTags __result)
        {
            if ((__result & WorkTags.Violent) == 0)
                return;
            if (__instance?.health?.hediffSet == null)
                return;
            if (__instance.health.hediffSet.GetFirstHediffOfDef(ChHediffDefOf.ChSuperSoldier) == null)
                return;

            __result &= ~WorkTags.Violent;
        }
    }
}
