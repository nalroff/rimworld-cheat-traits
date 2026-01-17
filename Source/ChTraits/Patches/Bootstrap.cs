using HarmonyLib;
using Verse;

namespace ChTraits.Patches
{
    [StaticConstructorOnStartup]
    public static class ChTraitsBootstrap
    {
        static ChTraitsBootstrap()
        {
            var harmony = new Harmony("nalroff.chtraits");
            harmony.PatchAll();
            Log.Message("[Cheat Traits] Harmony patches applied.");
        }
    }
}
