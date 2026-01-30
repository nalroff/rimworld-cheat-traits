using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
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
        static MethodBase TargetMethod()
          => AccessTools.Method(
            typeof(WorkGiver_InteractAnimal),
            "CanInteractWithAnimal",
            new Type[]
            {
          typeof(Pawn),
          typeof(Pawn),
          typeof(string).MakeByRefType(), // out string
          typeof(bool),
          typeof(bool),
          typeof(bool),
          typeof(bool)
            });

        // Harmony will bind the by-ref parameter when it exists.
        static void Prefix(Pawn pawn, Pawn animal, ref bool ignoreSkillRequirements)
        {
            if (ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait))
                ignoreSkillRequirements = true;
        }
    }
}
