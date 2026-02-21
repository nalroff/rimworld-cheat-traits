using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.GetInspectString))]
    internal static class Patch_Plant_GetInspectString_CheatTraitsAuras
    {
        public static void Postfix(Plant __instance, ref string __result)
        {
            if (__instance == null || !__instance.Spawned)
                return;

            // Avoid touching strings when nothing applies.
            bool greenThumb = ChAuraCache.IsAffected(__instance, ChAuraKeys.GreenThumb_Plants);
            bool floragen = ChAuraCache.IsAffected(__instance, ChAuraKeys.Floragen_Plants);

            if (!greenThumb && !floragen)
                return;

            // Build one extra line (keeps Inspect pane tidy).
            // Example:
            // Cheat Traits: Green Thumb, Floragen Core
            var sb = new StringBuilder();

            if (!__result.NullOrEmpty())
            {
                sb.Append(__result.TrimEndNewlines());
                sb.AppendLine();
            }

            if (greenThumb && floragen)
            {
                sb.Append("ChInspect_AffectedBy".Translate());
                sb.Append(": ");
                sb.Append("ChInspect_GreenThumb".Translate());
                sb.Append(", ");
                sb.Append("ChInspect_FloragenCore".Translate());
            }
            else if (greenThumb)
            {
                sb.Append("ChInspect_AffectedBy".Translate());
                sb.Append(": ");
                sb.Append("ChInspect_GreenThumb".Translate());
            }
            else // floragen
            {
                sb.Append("ChInspect_AffectedBy".Translate());
                sb.Append(": ");
                sb.Append("ChInspect_FloragenCore".Translate());
            }

            __result = sb.ToString();
        }
    }
}
