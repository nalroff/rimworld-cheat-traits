using RimWorld;
using Verse;
using UnityEngine;

namespace ChTraits.Patches
{
    /// <summary>
    /// ChComfy:
    /// - Periodically stabilizes the pawn's current room temperature toward a target.
    /// - Optionally extinguishes nearby fires (gated by the pawn gizmo toggle stored in ChTraitsMapComponent).
    ///
    /// Notes:
    /// - Stabilizes the *room* the pawn is currently in (fast + predictable).
    /// - Uses GenTemperature.PushHeat (vanilla heat simulation) rather than setting temps directly.
    /// </summary>
    internal static class ChComfyAuraConfig
    {
        public const string TraitDefName = "ChComfy";

        // How often we update (ticks). 120 = ~2 seconds.
        public const int UpdateIntervalTicks = 120;

        // 70°F ~= 21.1°C
        public const float TargetTempC = 21.1f;

        // Fire suppression
        public const float FireSuppressRadius = 10f;
    }

    internal static class ChComfyAuraSystem
    {
        public static void TickMap(Map map)
        {
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null) return;

            ChTraitsMapComponent mapComp = map.GetComponent<ChTraitsMapComponent>();

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.story?.traits == null || !p.Spawned) continue;
                if (!ChTraitsUtils.HasTrait(p, ChComfyAuraConfig.TraitDefName)) continue;

                if (mapComp != null && mapComp.ChComfy_IsFireSuppressionEnabled(p))
                {
                    ExtinguishNearbyFires(map, p);
                }
            }
        }

        private static void ExtinguishNearbyFires(Map map, Pawn pawn)
        {
            foreach (IntVec3 c in GenRadial.RadialCellsAround(pawn.Position, ChComfyAuraConfig.FireSuppressRadius, true))
            {
                if (!c.InBounds(map)) continue;

                var things = c.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Fire fire = things[i] as Fire;
                    if (fire == null) continue;

                    fire.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
