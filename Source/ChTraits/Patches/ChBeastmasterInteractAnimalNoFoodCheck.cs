using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    /// <summary>
    /// Beastmaster: treat "has food to interact with animal" as always true.
    /// Patches WorkGiver_InteractAnimal.HasFoodToInteractAnimal(Pawn pawn, Pawn tamee).
    /// </summary>
    [HarmonyPatch]
    public static class ChBeastmasterInteractAnimalNoFoodCheck
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
                if (m.Name != "HasFoodToInteractAnimal") continue;
                if (m.ReturnType != typeof(bool)) continue;

                var ps = m.GetParameters();
                if (ps.Length != 2) continue;
                if (ps[0].ParameterType != typeof(Pawn)) continue;
                if (ps[1].ParameterType != typeof(Pawn)) continue;

                results.Add(m);
            }

            return results;
        }

        public static void Postfix(Pawn pawn, Pawn tamee, ref bool __result)
        {
            if (!__result)
            {
                // Only override failures (less invasive).
                if (pawn == null || tamee == null) return;
                if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait)) return;
                if (tamee.RaceProps?.Animal != true) return;

                __result = true;
            }
        }
    }
}
