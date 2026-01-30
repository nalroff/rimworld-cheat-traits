using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ChTraits.Jobs
{
    public class JobDriver_DeployComfyNode : JobDriver
    {
        private const int PlaceDelayTicks = 0; // keep instant

        private IntVec3 TargetCell => job.targetA.Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the cell target so two pawns don’t try to deploy into the same spot.
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // If the cell becomes invalid/blocked before we get there, fail gracefully.
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

                    ThingDef nodeDef = DefDatabase<ThingDef>.GetNamedSilentFail("ChComfyClimateNode");
                    if (nodeDef == null)
                    {
                        Log.Error("[ChTraits] Missing ThingDef ChComfyClimateNode.");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // Spawn exactly at the chosen cell
                    Thing node = ThingMaker.MakeThing(nodeDef);
                    GenSpawn.Spawn(node, c, map, WipeMode.Vanish);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private static bool CanPlaceAt(Map map, IntVec3 c)
        {
            if (!c.InBounds(map)) return false;
            if (!c.Standable(map) && !c.Walkable(map)) return false;

            // For a 1x1 building, require the cell be empty of other buildings.
            // If you want to allow it on top of some things, loosen this.
            if (c.GetEdifice(map) != null) return false;

            // Also block if there is already a ComfyNode there
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i]?.def?.defName == "ChComfyClimateNode")
                    return false;
            }

            return true;
        }
    }
}
