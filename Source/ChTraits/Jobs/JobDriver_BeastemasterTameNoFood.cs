using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ChTraits.Jobs
{
    public class JobDriver_BeastmasterTameNoFood : JobDriver
    {
        private const int TameWorkTicks = 300; // ~5 seconds at 60 TPS. Simple & fast.

        private Pawn Animal => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Animal, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Animal.Dead);
            this.FailOn(() => Animal.Faction != null); // If someone else tamed it first

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Simple "work" wait with progress bar
            Toil work = Toils_General.Wait(TameWorkTicks);
            work.WithProgressBarToilDelay(TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return work;

            // Apply tame
            Toil finish = new Toil();
            finish.initAction = () =>
            {
                // Still respect that it's wild and designated at completion time
                if (Animal != null &&
                    Animal.Spawned &&
                    Animal.Faction == null &&
                    pawn.Map?.designationManager?.DesignationOn(Animal, DesignationDefOf.Tame) != null)
                {
                    BeastmasterUtility.ForceTameToPlayer(Animal);

                    Messages.Message(
                        $"Successfully tamed {Animal.LabelShort}.",
                        Animal,
                        MessageTypeDefOf.PositiveEvent
                    );

                    // Clear the tame designation after success
                    pawn.Map.designationManager.RemoveAllDesignationsOn(Animal);
                }
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}
