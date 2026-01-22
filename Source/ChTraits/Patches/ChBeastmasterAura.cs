using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    /// <summary>
    /// Beastmaster: ranching aura. Applies ChBeastmaster_HerdBlessing to nearby same-faction animals.
    /// While the hediff is present, wool/milk/chemfuel production progresses faster (patched via BodyResourceGrowthSpeed).
    ///
    /// Conventions:
    /// - Downed pawns are allowed to emit and receive auras.
    /// - Targets are animals (not humanlikes) and must be same faction as the emitting beastmaster.
    /// - No hardcoded player faction checks (future-proof for non-player factions).
    /// </summary>
    internal static class ChBeastmasterAuraConfig
    {
        internal const int AuraRadius = 20;
        internal const int AuraRadiusSquared = AuraRadius * AuraRadius;

        // How often we scan & apply (ticks). 250 = ~4 seconds at 60 TPS.
        internal const int ScanIntervalTicks = 250;

        // Refresh this exact remaining duration every time we apply the aura.
        // Should match the HediffDef's disappearsAfterTicks (2 hours = 5000 ticks).
        internal const int HerdBlessingRefreshTicks = 5000;

        internal const float ProductionProgressMultiplier = 7.0f;
    }

    [DefOf]
    internal static class ChBeastmasterDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChBeastmaster_HerdBlessing;
        #pragma warning restore 0649

        static ChBeastmasterDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChBeastmasterDefOf));
    }

    internal static class ChBeastmasterAuraUtil
    {
        internal static bool IsBeastmaster(Pawn pawn)
            => pawn != null && ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait);

        internal static bool HasHerdBlessing(Pawn pawn)
        {
            if (pawn == null || ChBeastmasterDefOf.ChBeastmaster_HerdBlessing == null) return false;
            return pawn.health?.hediffSet?.HasHediff(ChBeastmasterDefOf.ChBeastmaster_HerdBlessing) ?? false;
        }
    }

    /// <summary>
    /// Call TickMap(map) every ScanIntervalTicks (or let the system self-gate).
    /// </summary>
    internal static class ChBeastmasterAuraSystem
    {
        private static readonly List<Pawn> beastmasters = new List<Pawn>(8);

        public static void TickMap(Map map)
        {
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            beastmasters.Clear();

            // Collect beastmasters (emitters). Downed allowed.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.story?.traits == null) continue;
                if (ChBeastmasterAuraUtil.IsBeastmaster(p))
                {
                    beastmasters.Add(p);
                }
            }

            if (beastmasters.Count == 0) return;

            // Apply blessing to nearby same-faction animals.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];

                if (!ChTraitsUtils.IsAnimal(target)) continue;
                if (!ChTraitsUtils.IsHediffEligible(target)) continue;

                IntVec3 tPos = target.Position;

                for (int j = 0; j < beastmasters.Count; j++)
                {
                    Pawn source = beastmasters[j];
                    if (source == null || source.Dead || !source.Spawned) continue;
                    if (source.Map != map) continue;

                    // Must be in range first.
                    if ((source.Position - tPos).LengthHorizontalSquared > ChBeastmasterAuraConfig.AuraRadiusSquared) continue;

                    // Same-faction ally check (and blocks self-targeting).
                    // humanlikesOnly:false so it works on animals.
                    if (!ChTraitsUtils.IsAuraAlly(source, target, humanlikesOnly: false)) continue;

                    EnsureHerdBlessing(target);
                    break;
                }
            }
        }

        private static void EnsureHerdBlessing(Pawn animal)
        {
            if (!ChTraitsUtils.IsHediffEligible(animal)) return;
            if (ChBeastmasterDefOf.ChBeastmaster_HerdBlessing == null) return;

            Hediff existing = animal.health.hediffSet.GetFirstHediffOfDef(ChBeastmasterDefOf.ChBeastmaster_HerdBlessing);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(ChBeastmasterDefOf.ChBeastmaster_HerdBlessing, animal);
                animal.health.AddHediff(existing);
            }

            HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = ChBeastmasterAuraConfig.HerdBlessingRefreshTicks;
            }
        }
    }

    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.BodyResourceGrowthSpeed))]
    public static class Patch_PawnUtility_BodyResourceGrowthSpeed_Beastmaster
    {
        static void Postfix(Pawn pawn, ref float __result)
        {
            if (pawn == null) return;
            if (pawn.RaceProps == null || !pawn.RaceProps.Animal) return;

            // No faction check: any animal with the blessing benefits (future-proof).
            if (!ChBeastmasterAuraUtil.HasHerdBlessing(pawn)) return;

            __result *= ChBeastmasterAuraConfig.ProductionProgressMultiplier;
        }
    }
}
