using System.Collections.Generic;
using Verse;

namespace CheatTraits.Patches
{
    public class ChDiplomatBondsGameComponent : GameComponent
    {
        private HashSet<long> bonds = new HashSet<long>();

        public ChDiplomatBondsGameComponent(Game game) { }

        public static ChDiplomatBondsGameComponent? Instance =>
            Current.Game?.GetComponent<ChDiplomatBondsGameComponent>();

        public bool IsBonded(Pawn a, Pawn b)
        {
            if (a == null || b == null || a == b)
                return false;
            return bonds.Contains(MakeKey(a.thingIDNumber, b.thingIDNumber));
        }

        public bool AddBond(Pawn a, Pawn b)
        {
            if (a == null || b == null || a == b)
                return false;
            return bonds.Add(MakeKey(a.thingIDNumber, b.thingIDNumber));
        }

        public bool RemoveBond(Pawn a, Pawn b)
        {
            if (a == null || b == null || a == b)
                return false;
            return bonds.Remove(MakeKey(a.thingIDNumber, b.thingIDNumber));
        }

        // Pack two ints into one long, order-normalized so the lookup is symmetric.
        private static long MakeKey(int idA, int idB)
        {
            int low = idA < idB ? idA : idB;
            int high = idA < idB ? idB : idA;
            return ((long)low << 32) | (uint)high;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref bonds, "chDiplomat_bonds", LookMode.Value);
            if (bonds == null)
                bonds = new HashSet<long>();
        }
    }
}
