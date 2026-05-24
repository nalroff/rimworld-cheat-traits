using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.Patches
{
    public class ChRequiredTraitExtension : DefModExtension
    {
        public string? traitDefName;
    }

    [HarmonyPatch(typeof(Bill), nameof(Bill.PawnAllowedToStartAnew))]
    internal static class Patch_Bill_PawnAllowedToStartAnew_TraitGate
    {
        public static void Postfix(Bill __instance, Pawn p, ref bool __result)
        {
            if (!__result)
                return;

            RecipeDef? recipe = __instance?.recipe;
            if (recipe == null)
                return;

            ChRequiredTraitExtension? ext = recipe.GetModExtension<ChRequiredTraitExtension>();
            if (ext == null || string.IsNullOrEmpty(ext.traitDefName))
                return;

            if (CheatTraitsUtils.HasTrait(p, ext.traitDefName!))
                return;

            JobFailReason.Is(
                "Requires the " + ext.traitDefName + " trait.",
                __instance!.Label
            );
            __result = false;
        }
    }
}
