using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    internal enum ChAlchemistMealTier
    {
        None,
        Simple,
        Fine,
        Lavish,
    }

    internal sealed class ChAlchemistMealInfo
    {
        public ChAlchemistMealTier Tier;
        public bool Perfect;
    }

    internal static class ChAlchemistMealTracker
    {
        private static readonly ConditionalWeakTable<Thing, ChAlchemistMealInfo> infoByMeal =
            new ConditionalWeakTable<Thing, ChAlchemistMealInfo>();

        internal static void MarkMeal(Thing meal, ChAlchemistMealTier tier, bool perfect)
        {
            if (meal == null || tier == ChAlchemistMealTier.None)
                return;

            infoByMeal.Remove(meal);
            infoByMeal.Add(meal, new ChAlchemistMealInfo { Tier = tier, Perfect = perfect });
        }

        internal static bool TryGetMealInfo(Thing meal, out ChAlchemistMealInfo info)
        {
            if (meal == null)
            {
                Log.Message("[CheatTraits] Alchemist meal lookup: meal is null");
                info = null!;
                return false;
            }

            return infoByMeal.TryGetValue(meal, out info);
        }

        internal static bool IsAlchemistMeal(Thing meal) => TryGetMealInfo(meal, out _);
    }

    internal static class ChAlchemistMealUtility
    {
        internal const float PerfectMealChanceSimple = 0.15f;
        internal const float PerfectMealChanceFine = 0.25f;
        internal const float PerfectMealChanceLavish = 0.35f;

        internal static bool IsAlchemist(Pawn pawn) =>
            pawn != null && CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.AlchemistTrait);

        internal static bool IsMeal(Thing thing) =>
            thing?.def != null
            && thing.def.ingestible != null
            && IsSimpleOrBetterMeal(thing.def.ingestible.preferability);

        private static bool IsSimpleOrBetterMeal(FoodPreferability preferability) =>
            preferability == FoodPreferability.MealSimple
            || preferability == FoodPreferability.MealFine
            || preferability == FoodPreferability.MealLavish;

        internal static ChAlchemistMealTier GetMealTier(ThingDef def)
        {
            if (def?.ingestible == null)
                return ChAlchemistMealTier.None;

            switch (def.ingestible.preferability)
            {
                case FoodPreferability.MealSimple:
                    return ChAlchemistMealTier.Simple;
                case FoodPreferability.MealFine:
                    return ChAlchemistMealTier.Fine;
                case FoodPreferability.MealLavish:
                    return ChAlchemistMealTier.Lavish;
                default:
                    return ChAlchemistMealTier.None;
            }
        }

        internal static HediffDef? GetHediffFor(ChAlchemistMealTier tier, bool perfect)
        {
            if (tier == ChAlchemistMealTier.None)
                return null;

            if (perfect)
            {
                return tier switch
                {
                    ChAlchemistMealTier.Simple => ChAlchemistDefOf.ChAlchemist_SimpleMealPerfect,
                    ChAlchemistMealTier.Fine => ChAlchemistDefOf.ChAlchemist_FineMealPerfect,
                    ChAlchemistMealTier.Lavish => ChAlchemistDefOf.ChAlchemist_LavishMealPerfect,
                    _ => null,
                };
            }

            return tier switch
            {
                ChAlchemistMealTier.Simple => ChAlchemistDefOf.ChAlchemist_SimpleMealBoost,
                ChAlchemistMealTier.Fine => ChAlchemistDefOf.ChAlchemist_FineMealBoost,
                ChAlchemistMealTier.Lavish => ChAlchemistDefOf.ChAlchemist_LavishMealBoost,
                _ => null,
            };
        }

        internal static float GetPerfectMealChance(ChAlchemistMealTier tier)
        {
            return tier switch
            {
                ChAlchemistMealTier.Simple => PerfectMealChanceSimple,
                ChAlchemistMealTier.Fine => PerfectMealChanceFine,
                ChAlchemistMealTier.Lavish => PerfectMealChanceLavish,
                _ => 0f,
            };
        }

        internal static void ApplyOrRefreshHediff(Pawn target, HediffDef hediffDef)
        {
            if (target?.health?.hediffSet == null || hediffDef == null)
                return;

            Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, target);
                target.health.AddHediff(hediff);
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = disappears.Props.disappearsAfterTicks.RandomInRange;
            }
        }
    }

    // ---------------------------------------------------------------------
    // On bill completion: mark meals cooked by an alchemist and roll for perfect meals.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_GenRecipe_PostProcessProduct_Alchemist
    {
        static MethodBase TargetMethod()
        {
            var methods = AccessTools
                .GetDeclaredMethods(typeof(GenRecipe))
                .Where(m => m.Name == "PostProcessProduct")
                .ToList();

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

            return methods.FirstOrDefault();
        }

        public static void Postfix(Thing product, RecipeDef recipeDef, Pawn worker)
        {
            if (product == null || worker == null)
                return;
            if (!ChAlchemistMealUtility.IsAlchemist(worker))
                return;
            if (!ChAlchemistMealUtility.IsMeal(product))
                return;

            ChAlchemistMealTier tier = ChAlchemistMealUtility.GetMealTier(product.def);
            if (tier == ChAlchemistMealTier.None)
                return;

            bool perfect = Rand.Value < ChAlchemistMealUtility.GetPerfectMealChance(tier);
            ChAlchemistMealTracker.MarkMeal(product, tier, perfect);
        }
    }

    // ---------------------------------------------------------------------
    // On ingest: apply alchemical meal hediff to the eater.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_Thing_Ingested_Alchemist
    {
        static MethodBase TargetMethod()
        {
            var method = AccessTools.Method(
                typeof(Thing),
                "Ingested",
                new[] { typeof(Pawn), typeof(float) }
            );
            if (method != null)
                return method;

            var methods = AccessTools
                .GetDeclaredMethods(typeof(Thing))
                .Where(m => m.Name == "Ingested")
                .ToList();

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length >= 1 && ps[0].ParameterType == typeof(Pawn))
                    return m;
            }

            return methods.FirstOrDefault();
        }

        public static void Postfix(Thing __instance, Pawn ingester)
        {
            if (__instance == null || ingester == null)
                return;

            if (!ChAlchemistMealTracker.TryGetMealInfo(__instance, out ChAlchemistMealInfo info))
                return;

            HediffDef? hediffDef = ChAlchemistMealUtility.GetHediffFor(info.Tier, info.Perfect);
            if (hediffDef == null)
                return;

            ChAlchemistMealUtility.ApplyOrRefreshHediff(ingester, hediffDef);
        }
    }

    // ---------------------------------------------------------------------
    // Food poisoning: skip for alchemist-cooked meals.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_FoodUtility_TryAddFoodPoisoningHediff_Alchemist
    {
        static MethodBase TargetMethod()
        {
            var methods = AccessTools
                .GetDeclaredMethods(typeof(FoodUtility))
                .Where(m => m.Name == "AddFoodPoisoningHediff")
                .ToList();

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (
                    ps.Length >= 3
                    && ps[0].ParameterType == typeof(Pawn)
                    && ps[1].ParameterType == typeof(Thing)
                    && ps[2].ParameterType == typeof(FoodPoisonCause)
                )
                    return m;
            }

            return methods.FirstOrDefault();
        }

        public static bool Prefix(Pawn pawn, Thing ingestible, FoodPoisonCause cause)
        {
            if (ingestible == null)
                return true;

            if (ChAlchemistMealTracker.IsAlchemistMeal(ingestible))
                return false;

            return true;
        }
    }

    [DefOf]
    internal static class ChAlchemistDefOf
    {
#pragma warning disable 0649
        public static HediffDef ChAlchemist_SimpleMealBoost = null!;
        public static HediffDef ChAlchemist_FineMealBoost = null!;
        public static HediffDef ChAlchemist_LavishMealBoost = null!;
        public static HediffDef ChAlchemist_SimpleMealPerfect = null!;
        public static HediffDef ChAlchemist_FineMealPerfect = null!;
        public static HediffDef ChAlchemist_LavishMealPerfect = null!;
#pragma warning restore 0649

        static ChAlchemistDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChAlchemistDefOf));
    }
}
