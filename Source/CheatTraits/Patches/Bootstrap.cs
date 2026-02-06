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
            Log.Message("[Cheat Traits] Harmony patches applied.");
        }
    }
}
