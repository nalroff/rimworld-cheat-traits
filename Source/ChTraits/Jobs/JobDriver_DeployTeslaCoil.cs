using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ChTraits.Jobs
{
    public class JobDriver_DeployTeslaCoil : JobDriver
    {
        private IntVec3 TargetCell => job.targetA.Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch)
                .FailOn(() => !CanPlaceAt(pawn.Map, TargetCell));

            yield return new Toil
            {
                initAction = () =>
                {
                    Map map = pawn.Map;
                    IntVec3 c = TargetCell;

                    if (!CanPlaceAt(map, c))
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("ChTeslaCoil");
                    if (def == null)
                    {
                        Log.Error("[ChTraits] Missing ThingDef ChTeslaCoil.");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    Thing coil = ThingMaker.MakeThing(def);
                    // Be explicit to avoid any ambiguity with namespaces.
                    if (pawn.Faction != null)
                        coil.SetFaction(pawn.Faction);
                    GenSpawn.Spawn(coil, c, map, WipeMode.Vanish);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private static bool CanPlaceAt(Map map, IntVec3 c)
        {
            if (map == null) return false;
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map) && !c.Walkable(map)) return false;

            if (c.GetEdifice(map) != null) return false;

            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i]?.def?.defName == "ChTeslaCoil")
                    return false;
            }

            return true;
        }
    }
}
