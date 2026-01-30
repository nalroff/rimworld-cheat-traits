using RimWorld;
using UnityEngine;
using Verse;

namespace ChTraits.Designators
{
    public class TeslaCoilDesignator : Designator_Build
    {
        private readonly Pawn pawn;

        public TeslaCoilDesignator(Pawn pawn, ThingDef entDef) : base(entDef)
        {
            this.pawn = pawn;
            defaultLabel = "Deploy Tesla Coil";
            defaultDesc = "Deploys a tesla coil that generates power and zaps nearby enemies.";
            icon = ContentFinder<Texture2D>.Get("Things/ChTesla_TeslaCoil", true);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(TutorTagDesignate, c)))
                return;

            var job = JobMaker.MakeJob(ChTeslaJobDefOf.ChDeployTeslaCoil, c);
            pawn.jobs.TryTakeOrderedJob(job);

            FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, PlacingDef.Size), Map);

            if (TutorSystem.TutorialMode)
                TutorSystem.Notify_Event(new EventPack(TutorTagDesignate, c));
        }
    }

    [DefOf]
    public static class ChTeslaJobDefOf
    {
        public static JobDef ChDeployTeslaCoil;

        static ChTeslaJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChTeslaJobDefOf));
        }
    }
}
