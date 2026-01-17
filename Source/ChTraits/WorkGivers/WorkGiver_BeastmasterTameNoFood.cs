using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ChTraits.WorkGivers
{
    public class WorkGiver_BeastmasterTameNoFood : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn == null || !BeastmasterUtility.IsBeastmaster(pawn);
        }

        // IMPORTANT: Explicitly return only tame-designated animals.
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map?.designationManager == null)
                yield break;

            // DesignationManager keeps a list by def; this is the most direct & efficient scan.
            List<Designation> desigs = pawn.Map.designationManager.AllDesignations;
            for (int i = 0; i < desigs.Count; i++)
            {
                Designation d = desigs[i];
                if (d.def != DesignationDefOf.Tame) continue;

                if (d.target.Thing is Pawn p)
                    yield return p;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t is not Pawn animal) return false;
            if (!BeastmasterUtility.IsBeastmaster(pawn)) return false;

            if (animal.RaceProps?.Animal != true) return false;
            if (animal.Dead) return false;

            // Wild only
            if (animal.Faction != null) return false;

            if (!pawn.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Deadly))
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(ChJobDefOf.ChBeastmaster_TameNoFood, t);
        }
    }
}
