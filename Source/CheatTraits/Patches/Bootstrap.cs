using CheatTraits.Settings;
using HarmonyLib;
using Verse;

namespace CheatTraits.Patches
{
    [StaticConstructorOnStartup]
    public static class CheatTraitsBootstrap
    {
        static CheatTraitsBootstrap()
        {
            var harmony = new Harmony("nalroff.CheatTraits");
            harmony.PatchAll();
            // Stamp the configured commonality onto the trait defs now that they're loaded.
            CheatTraitsMod.ApplyCommonality();
            Log.Message("[Cheat Traits] Harmony patches applied.");
        }
    }
}
