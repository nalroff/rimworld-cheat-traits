using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ChTraits.Patches
{
    /// <summary>
    /// Beastmaster: prevent the special "take food for animal interaction" job from being created.
    /// This is a safety net in case vanilla still tries to enqueue a food-hauling prep job
    /// after we force HasFoodToInteractAnimal to succeed.
    /// </summary>
    [HarmonyPatch]
    public static class ChBeastmasterInteractAnimalNoFoodJob
    {
        public static bool Prepare()
        {
            return FindTargets().Count > 0;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return FindTargets();
        }

        private static List<MethodBase> FindTargets()
        {
            var results = new List<MethodBase>();

            foreach (var m in AccessTools.GetDeclaredMethods(typeof(WorkGiver_InteractAnimal)))
            {
                if (m == null) continue;
                if (m.Name != "TakeFoodForAnimalInteractJob") continue;

                // Return type should be Job (or possibly nullable Job).
                if (!typeof(Job).IsAssignableFrom(m.ReturnType)) continue;

                var ps = m.GetParameters();
                if (ps.Length != 2) continue;
                if (ps[0].ParameterType != typeof(Pawn)) continue;
                if (ps[1].ParameterType != typeof(Pawn)) continue;

                results.Add(m);
            }

            return results;
        }

        public static void Postfix(Pawn pawn, Pawn tamee, ref Job __result)
        {
            if (__result == null) return;

            if (pawn == null || tamee == null) return;
            if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait)) return;
            if (tamee.RaceProps?.Animal != true) return;

            // Beastmaster never needs a food-hauling prep job.
            __result = null;
        }
    }
}
