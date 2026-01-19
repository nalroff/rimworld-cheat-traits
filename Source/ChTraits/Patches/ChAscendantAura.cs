using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    /// <summary>
    /// Ascendant: Child-focused aura. Applies a lingering growth hediff to nearby children.
    /// Growth hediff makes child growth-point gain faster (patched via Pawn_GrowthTracker).
    /// </summary>
    internal static class ChAscendantAuraConfig
    {
        internal const int AuraRadius = 20;

        // How often we scan & apply (ticks). 250 = ~4 seconds at 60 TPS.
        internal const int ScanIntervalTicks = 250;

        // Growth boost linger duration: 6-12 hours in ticks.
        // RimWorld uses 60k ticks per day; 1 hour = 2500 ticks.
        internal const int GrowthHediffMinTicks = 6 * 2500;   // 15000
        internal const int GrowthHediffMaxTicks = 12 * 2500;  // 30000

        // Growth point gain multiplier while the hediff is present.
        internal const float GrowthPointsMultiplier = 3.0f;

        internal const string AscendantTraitDefName = "ChAscendant";
    }

    [DefOf]
    internal static class ChAscendantDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChAscendant_GrowthAura;
        #pragma warning restore 0649

        static ChAscendantDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChAscendantDefOf));
        }
    }

    internal static class ChAscendantUtil
    {
        internal const string AscendantTrait = "ChAscendant";

        internal static bool IsAscendant(Pawn pawn)
            => pawn != null && ChTraitsUtils.HasTrait(pawn, AscendantTrait);

        internal static bool IsChild(Pawn pawn)
        {
            if (pawn == null) return false;
            return pawn.DevelopmentalStage == DevelopmentalStage.Child;
        }

        internal static bool SameMapAndSpawned(Pawn a, Pawn b)
            => a != null && b != null && a.Spawned && b.Spawned && a.Map == b.Map;

        internal static bool InRadius(IntVec3 a, IntVec3 b, int radius)
            => (a - b).LengthHorizontalSquared <= radius * radius;

        internal static void EnsureGrowthHediff(Pawn child)
        {
            if (child == null || !child.Spawned) return;

            var hediffDef = ChAscendantDefOf.ChAscendant_GrowthAura;
            if (hediffDef == null) return;

            Hediff existing = child.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(hediffDef, child);
                child.health.AddHediff(existing);
            }

            // Refresh / randomize linger duration each time the aura is applied.
            var disappears = existing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Rand.RangeInclusive(
                    ChAscendantAuraConfig.GrowthHediffMinTicks,
                    ChAscendantAuraConfig.GrowthHediffMaxTicks
                );
            }
        }

        internal static bool HasGrowthAuraHediff(Pawn pawn)
        {
            if (pawn == null) return false;
            var hediffDef = ChAscendantDefOf.ChAscendant_GrowthAura;
            if (hediffDef == null) return false;
            return pawn.health?.hediffSet?.HasHediff(hediffDef) ?? false;
        }
    }

    /// <summary>
    /// NEW: system entry point for your combined MapComponent.
    /// Call TickMap(map) every ScanIntervalTicks (or let the system self-gate).
    /// </summary>
    internal static class ChAscendantAuraSystem
    {
        private static readonly List<Pawn> ascendants = new List<Pawn>(8);

        public static void TickMap(Map map)
        {
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            ascendants.Clear();

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (ChAscendantUtil.IsAscendant(p))
                {
                    ascendants.Add(p);
                }
            }

            if (ascendants.Count == 0) return;

            // Apply growth hediff to nearby children.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!ChAscendantUtil.IsChild(p)) continue;

                bool inAura = false;
                for (int j = 0; j < ascendants.Count; j++)
                {
                    Pawn a = ascendants[j];
                    if (!ChAscendantUtil.SameMapAndSpawned(a, p)) continue;

                    if (ChAscendantUtil.InRadius(a.Position, p.Position, ChAscendantAuraConfig.AuraRadius))
                    {
                        inAura = true;
                        break;
                    }
                }

                if (inAura)
                    ChAscendantUtil.EnsureGrowthHediff(p);
            }
        }
    }

    // ---------------------------------------------------------------------
    // Growth hook: multiply growth-point gain while ChAscendant_GrowthAura hediff is present.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_PawnGrowthTracker_GrowthPointsPerDay_Ascendant
    {
        static Type GrowthTrackerType => AccessTools.TypeByName("RimWorld.Pawn_GrowthTracker");

        static bool Prepare() => GrowthTrackerType != null;

        static MethodBase TargetMethod()
        {
            var getter = AccessTools.PropertyGetter(GrowthTrackerType, "GrowthPointsPerDay");
            if (getter != null) return getter;

            var methods = AccessTools.GetDeclaredMethods(GrowthTrackerType)
                .Where(m => m.ReturnType == typeof(float) && m.Name.Contains("GrowthPointsPerDay"))
                .ToList();

            return methods.FirstOrDefault();
        }

        public static void Postfix(object __instance, ref float __result)
        {
            try
            {
                if (__instance == null) return;

                Pawn pawn = AccessTools.Field(GrowthTrackerType, "pawn")?.GetValue(__instance) as Pawn;
                if (pawn == null) return;

                if (!ChAscendantUtil.IsChild(pawn)) return;
                if (!ChAscendantUtil.HasGrowthAuraHediff(pawn)) return;

                __result *= ChAscendantAuraConfig.GrowthPointsMultiplier;
            }
            catch (Exception ex)
            {
                Log.Error($"[ChTraits] Ascendant GrowthPointsPerDay patch error: {ex}");
            }
        }
    }
}
