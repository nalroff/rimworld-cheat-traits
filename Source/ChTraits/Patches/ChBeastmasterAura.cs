using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    /// <summary>
    /// Beastmaster: ranching aura. Applies ChBeastmaster_HerdBlessing to nearby player-owned animals.
    /// While the hediff is present, wool/milk/chemfuel production progresses faster (patched via comps).
    /// </summary>
    internal static class ChBeastmasterAuraConfig
    {
        internal const int AuraRadius = 20;

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

        static ChBeastmasterDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChBeastmasterDefOf));
        }
    }

    internal static class ChBeastmasterAuraUtil
    {
        internal static bool IsBeastmaster(Pawn pawn)
            => pawn != null && ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait);

        internal static bool IsPlayerAnimal(Pawn pawn)
            => pawn != null
               && pawn.Spawned
               && pawn.RaceProps != null
               && pawn.RaceProps.Animal
               && pawn.Faction == Faction.OfPlayer;

        internal static bool SameMapAndSpawned(Pawn a, Pawn b)
            => a != null && b != null && a.Spawned && b.Spawned && a.Map == b.Map;

        internal static bool InRadius(IntVec3 a, IntVec3 b, int radius)
            => (a - b).LengthHorizontalSquared <= radius * radius;

        internal static void EnsureHerdBlessing(Pawn animal)
        {
            if (animal == null || !animal.Spawned) return;

            var hediffDef = ChBeastmasterDefOf.ChBeastmaster_HerdBlessing;
            if (hediffDef == null) return;

            Hediff existing = animal.health?.hediffSet?.GetFirstHediffOfDef(hediffDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(hediffDef, animal);
                animal.health.AddHediff(existing);
            }

            // Refresh linger duration each time the aura is applied.
            var disappears = existing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
                disappears.ticksToDisappear = ChBeastmasterAuraConfig.HerdBlessingRefreshTicks;
        }

        internal static bool HasHerdBlessing(Pawn pawn)
        {
            var hediffDef = ChBeastmasterDefOf.ChBeastmaster_HerdBlessing;
            if (pawn == null || hediffDef == null) return false;
            return pawn.health?.hediffSet?.HasHediff(hediffDef) ?? false;
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

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (ChBeastmasterAuraUtil.IsBeastmaster(p))
                    beastmasters.Add(p);
            }

            if (beastmasters.Count == 0) return;

            // Apply blessing to nearby player-owned animals.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn a = pawns[i];
                if (!ChBeastmasterAuraUtil.IsPlayerAnimal(a)) continue;

                bool inAura = false;
                for (int j = 0; j < beastmasters.Count; j++)
                {
                    Pawn bm = beastmasters[j];
                    if (!ChBeastmasterAuraUtil.SameMapAndSpawned(bm, a)) continue;

                    if (ChBeastmasterAuraUtil.InRadius(bm.Position, a.Position, ChBeastmasterAuraConfig.AuraRadius))
                    {
                        inAura = true;
                        break;
                    }
                }

                if (inAura)
                    ChBeastmasterAuraUtil.EnsureHerdBlessing(a);
            }
        }
    }

    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.BodyResourceGrowthSpeed))]
    public static class Patch_PawnUtility_BodyResourceGrowthSpeed_Beastmaster
    {
        static void Postfix(Pawn pawn, ref float __result)
        {
            if (pawn == null) return;
            if (pawn.Faction != Faction.OfPlayer) return;

            if (!ChBeastmasterAuraUtil.HasHerdBlessing(pawn)) return;
            __result *= ChBeastmasterAuraConfig.ProductionProgressMultiplier;
        }
    }
}
