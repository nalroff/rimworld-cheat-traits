using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    internal static class ArtificerQualityUtil
    {
        internal static bool IsArtificerPawn(Pawn pawn) =>
            CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.ArtificerTrait);

        internal static void ForceArtificerQuality(Thing thing)
        {
            if (thing == null)
                return;

            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq == null)
                return;

            QualityCategory rolledQuality = GetArtificerQualityLevel();
            if (rolledQuality <= cq.Quality)
                return;

            cq.SetQuality(rolledQuality, ArtGenerationContext.Colony);
        }

        internal static QualityCategory GetArtificerQualityLevel()
        {
            // Odds: Legendary 10%, Masterwork 30%, Excellent 60%
            float roll = Rand.Value;
            if (roll < 0.10f)
                return QualityCategory.Legendary;
            if (roll < 0.40f)
                return QualityCategory.Masterwork;
            return QualityCategory.Excellent;
        }
    }

    internal static class EngineerQualityUtil
    {
        internal static bool IsEngineerPawn(Pawn pawn) =>
            CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.EngineerTrait);

        // The Engineer's building quality uses the same 60/30/10 weights as the
        // Artificer's item quality — the split is about *what* each forces, not the odds.
        internal static void ForceEngineerQuality(Thing thing) =>
            ArtificerQualityUtil.ForceArtificerQuality(thing);
    }

    // ---------------------------------------------------------------------
    // Crafting / Bills: force artificer quality roll on produced items (if they have CompQuality)
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

            var methods = AccessTools
                .GetDeclaredMethods(typeof(GenRecipe))
                .Where(m => m.Name == "PostProcessProduct")
                .ToList();

            // Prefer the overload whose first 3 params are (Thing, RecipeDef, Pawn)
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (
                    ps.Length >= 3
                    && ps[0].ParameterType == typeof(Thing)
                    && ps[1].ParameterType == typeof(RecipeDef)
                    && ps[2].ParameterType == typeof(Pawn)
                )
                    return m;
            }

            // Fallback: first method named PostProcessProduct (better than hard-failing)
            return methods.FirstOrDefault();
        }

        public static void Postfix(Thing product, RecipeDef recipeDef, Pawn worker)
        {
            if (product == null || worker == null)
                return;
            if (!ArtificerQualityUtil.IsArtificerPawn(worker))
                return;

            ArtificerQualityUtil.ForceArtificerQuality(product);
        }
    }

    [HarmonyPatch]
    internal static class Patch_QualityUtility_GenerateQualityCreatedByPawn_Artificer
    {
        static MethodBase TargetMethod()
        {
            // There are multiple overloads across versions/modpacks.
            // We want the one that returns QualityCategory and takes a Pawn as the first arg.
            var methods = AccessTools
                .GetDeclaredMethods(typeof(QualityUtility))
                .Where(m => m.Name == "GenerateQualityCreatedByPawn")
                .ToList();

            foreach (var m in methods)
            {
                if (m.ReturnType != typeof(QualityCategory))
                    continue;
                var ps = m.GetParameters();
                if (ps.Length >= 1 && ps[0].ParameterType == typeof(Pawn))
                    return m;
            }

            return methods.FirstOrDefault();
        }

        public static void Postfix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result)
        {
            if (pawn == null)
                return;

            // Buildings roll quality off Construction (Frame.CompleteConstruction).
            // We let the Frame patch handle those so we can tell art (-> Artificer)
            // from non-art (-> Engineer). Here we only force item/recipe quality.
            if (relevantSkill == SkillDefOf.Construction)
                return;
            if (!ArtificerQualityUtil.IsArtificerPawn(pawn))
                return;

            __result = ArtificerQualityUtil.GetArtificerQualityLevel();
        }
    }

    // ---------------------------------------------------------------------
    // Construction: split building quality forcing between the two traits.
    //   - Sculptures / art buildings (have CompArt)  -> Artificer
    //   - Everything else (furniture, benches, etc.) -> Engineer
    // Frame.CompleteConstruction rolls quality off SkillDefOf.Construction for
    // both, so we re-roll here where the finished Thing is available.
    // ---------------------------------------------------------------------
    [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
    internal static class Patch_Frame_CompleteConstruction_BuildingQuality
    {
        // CompleteConstruction destroys the frame mid-method, so capture the map
        // and cell in a prefix and read back the spawned building in the postfix.
        [System.ThreadStatic]
        private static Map? capturedMap;

        [System.ThreadStatic]
        private static IntVec3 capturedPos;

        static void Prefix(Frame __instance)
        {
            capturedMap = __instance.Map;
            capturedPos = __instance.Position;
        }

        static void Postfix(Pawn worker)
        {
            Map? map = capturedMap;
            capturedMap = null;
            if (map == null || worker == null)
                return;

            bool artificer = ArtificerQualityUtil.IsArtificerPawn(worker);
            bool engineer = EngineerQualityUtil.IsEngineerPawn(worker);
            if (!artificer && !engineer)
                return;

            List<Thing> things = map.thingGrid.ThingsListAtFast(capturedPos);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is not Building building)
                    continue;
                if (building.TryGetComp<CompQuality>() == null)
                    continue;

                if (building.TryGetComp<CompArt>() != null)
                {
                    if (artificer)
                        ArtificerQualityUtil.ForceArtificerQuality(building);
                }
                else if (engineer)
                {
                    EngineerQualityUtil.ForceEngineerQuality(building);
                }
                break;
            }
        }
    }
}
