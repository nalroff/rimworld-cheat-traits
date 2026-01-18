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
    /// Beastmaster: ignore Animals skill minimum for interact-animal work (tame/train/etc).
    /// Patches WorkGiver_InteractAnimal.CanInteractWithAnimal(... ignoreSkillRequirements ...).
    /// </summary>
    [HarmonyPatch]
    public static class ChBeastmasterInteractAnimalIgnoreSkill
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
            // Patch all overloads named CanInteractWithAnimal that have:
            // (Pawn pawn, Pawn animal, out string jobFailReason, bool forced, ... bool ignoreSkillRequirements ...)
            var ms = AccessTools.GetDeclaredMethods(typeof(WorkGiver_InteractAnimal));
            var results = new List<MethodBase>();

            foreach (var m in ms)
            {
                if (m == null) continue;
                if (m.Name != "CanInteractWithAnimal") continue;

                var ps = m.GetParameters();
                if (ps.Length < 4) continue;

                if (ps[0].ParameterType != typeof(Pawn)) continue;
                if (ps[1].ParameterType != typeof(Pawn)) continue;

                // out string
                if (ps[2].ParameterType != typeof(string).MakeByRefType()) continue;

                if (ps[3].ParameterType != typeof(bool)) continue; // forced

                // Must contain a bool named ignoreSkillRequirements (or at least a bool in that position range).
                // We'll still patch and adjust by ref parameter if present.
                if (!ps.Any(p => p.ParameterType == typeof(bool) && p.Name == "ignoreSkillRequirements"))
                {
                    // If name metadata isn't preserved, still patch as long as there's >= 2 optional bools.
                    int boolCount = ps.Count(p => p.ParameterType == typeof(bool));
                    if (boolCount < 2) continue;
                }

                results.Add(m);
            }

            return results;
        }

        // Harmony will bind the by-ref parameter when it exists.
        public static void Prefix(Pawn pawn, Pawn animal, ref bool ignoreSkillRequirements)
        {
            if (pawn == null || animal == null) return;
            if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait)) return;

            // Beastmaster ignores skill gate for interact-animal actions.
            ignoreSkillRequirements = true;
        }
    }
}
