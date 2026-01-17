using RimWorld;
using Verse;

namespace ChTraits
{
    public static class BeastmasterUtility
    {
        public static bool IsBeastmaster(Pawn pawn)
            => ChTraits.Patches.ChTraitsUtils.HasTrait(pawn, ChTraits.Patches.ChTraitsNames.BeastmasterTrait);

        public static void ForceTameToPlayer(Pawn animal)
        {
            if (animal == null) return;

            animal.SetFaction(Faction.OfPlayer);
            animal.mindState?.mentalStateHandler?.Reset();
        }
    }
}
