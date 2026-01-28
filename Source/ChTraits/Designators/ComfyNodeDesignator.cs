using RimWorld;
using Verse;
using UnityEngine;

namespace ChTraits.Designators
{
    public class ComfyNodeDesignator : Designator_Build
    {
        private Pawn pawn;

        public ComfyNodeDesignator(Pawn pawn, ThingDef entDef) : base(entDef)
        {
            this.pawn = pawn;
            defaultLabel = "Deploy Comfy Node";
            defaultDesc = "Deploys a climate control node at the target location.";
            icon = ContentFinder<Texture2D>.Get("Things/ChComfy_ComfyNode", true);
            // soundDragSustain = SoundDefOf.Designate_DragStandard;
            // soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
            // useMouseIcon = true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            // IMPORTANT: keep the tutorial guard if you care
            // (optional, but matches vanilla behavior)
            if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(TutorTagDesignate, c)))
                return;

            // You can still use vanilla placement legality checks
            // base.CanDesignateCell already uses GenConstruct.CanPlaceBlueprintAt(...)
            // so if we got here, it’s legal.

            // Instead of placing a blueprint/spawning,
            // enqueue your job that makes pawn walk to c and deploy instantly.
            // Example call (whatever your existing system is):
            var job = JobMaker.MakeJob(ChJobDefOf.ChDeployComfyNode, c);
            pawn.jobs.TryTakeOrderedJob(job);

            // do placement puffs like vanilla if you want
            FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, PlacingDef.Size), Map);

            if (TutorSystem.TutorialMode)
                TutorSystem.Notify_Event(new EventPack(TutorTagDesignate, c));
        }

        // public override AcceptanceReport CanDesignateCell(IntVec3 c)
        // {
        //     if (!c.InBounds(pawn.Map)) return false;
        //     if (!c.Standable(pawn.Map)) return "Must place on standable ground.";
        //     if (c.GetEdifice(pawn.Map) != null) return "Blocked.";

        //     return true;
        // }

        // public override void DesignateSingleCell(IntVec3 c)
        // {
        //     var job = JobMaker.MakeJob(ChJobDefOf.ChDeployComfyNode, c);
        //     pawn.jobs.TryTakeOrderedJob(job);
        // }
    }

    [DefOf]
    public static class ChJobDefOf
    {
        public static JobDef ChDeployComfyNode;

        static ChJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChJobDefOf));
        }
    }
}
