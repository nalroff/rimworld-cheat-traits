using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;

namespace ChTraits.Patches
{
    internal static class ArtificerQualityUtil
    {
        internal static bool IsArtificerPawn(Pawn pawn)
            => ChTraitsUtils.HasTrait(pawn, ChTraitsNames.ArtificerTrait);

        internal static void ForceLegendaryIfPossible(Thing thing)
        {
            if (thing == null) return;

            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq == null) return;

            // Don't spam SetQuality if already legendary
            if (cq.Quality == QualityCategory.Legendary) return;

            cq.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
        }
    }

    // ---------------------------------------------------------------------
    // Crafting / Bills: force legendary on produced items (if they have CompQuality)
    // Target: GenRecipe.PostProcessProduct (worker pawn is provided here)
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_GenRecipe_PostProcessProduct_Artificer
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            // RimWorld 1.6 typically has GenRecipe.PostProcessProduct with a signature that includes:
            // (Thing product, RecipeDef recipeDef, Pawn worker, ...) - we only need the first three.
            // We'll find a method by name and then match the first parameters we care about.

            var methods = AccessTools.GetDeclaredMethods(typeof(GenRecipe))
                .Where(m => m.Name == "PostProcessProduct")
                .ToList();

            // Prefer the overload whose first 3 params are (Thing, RecipeDef, Pawn)
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length >= 3 &&
                    ps[0].ParameterType == typeof(Thing) &&
                    ps[1].ParameterType == typeof(RecipeDef) &&
                    ps[2].ParameterType == typeof(Pawn))
                    return m;
            }

            // Fallback: first method named PostProcessProduct (better than hard-failing)
            return methods.FirstOrDefault();
        }

        public static void Postfix(Thing product, RecipeDef recipeDef, Pawn worker)
        {
            if (product == null || worker == null) return;
            if (!ArtificerQualityUtil.IsArtificerPawn(worker)) return;

            ArtificerQualityUtil.ForceLegendaryIfPossible(product);
        }
    }

    [HarmonyPatch]
    internal static class Patch_QualityUtility_GenerateQualityCreatedByPawn_Artificer
    {
        static MethodBase TargetMethod()
        {
            // There are multiple overloads across versions/modpacks.
            // We want the one that returns QualityCategory and takes a Pawn as the first arg.
            var methods = AccessTools.GetDeclaredMethods(typeof(QualityUtility))
                .Where(m => m.Name == "GenerateQualityCreatedByPawn")
                .ToList();

            foreach (var m in methods)
            {
                if (m.ReturnType != typeof(QualityCategory)) continue;
                var ps = m.GetParameters();
                if (ps.Length >= 1 && ps[0].ParameterType == typeof(Pawn))
                    return m;
            }

            return methods.FirstOrDefault();
        }

        public static void Postfix(Pawn pawn, ref QualityCategory __result)
        {
            if (pawn == null) return;
            if (!ArtificerQualityUtil.IsArtificerPawn(pawn)) return;

            __result = QualityCategory.Legendary;
        }
    }
}
