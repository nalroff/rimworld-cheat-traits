using RimWorld;
using Verse;
using CheatTraits.Patches;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChReforge : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChReforge()
        {
            compClass = typeof(CompAbilityEffect_ChReforge);
        }
    }

    /// <summary>
    /// Rerolls the quality of the targeted building or item using the
    /// Artificer's 60/30/10 Excellent/Masterwork/Legendary weights. Always
    /// replaces the current quality — even a downgrade is honored per spec.
    /// </summary>
    public class CompAbilityEffect_ChReforge : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing? thing = ResolveQualityThing(target);
            if (thing == null)
                return;

            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq == null)
                return;

            QualityCategory newQuality = ArtificerQualityUtil.GetArtificerQualityLevel();
            cq.SetQuality(newQuality, ArtGenerationContext.Colony);

            // Force a redraw so any quality-tinted graphics (e.g. art) refresh.
            if (thing.Spawned && thing.Map != null)
            {
                thing.DirtyMapMesh(thing.Map);
                FleckMaker.Static(
                    thing.DrawPos,
                    thing.Map,
                    FleckDefOf.PsycastSkipFlashEntry,
                    1.5f
                );
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Thing? thing = ResolveQualityThing(target);
            if (thing == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Reforge requires a building or item.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            if (thing.TryGetComp<CompQuality>() == null)
            {
                if (throwMessages)
                    Messages.Message(
                        $"{thing.LabelShortCap} has no quality to reforge.",
                        thing,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        public override string? ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            Thing? thing = ResolveQualityThing(target);
            if (thing == null)
                return null;
            if (thing.TryGetComp<CompQuality>() == null)
                return "No quality";
            return null;
        }

        private static Thing? ResolveQualityThing(LocalTargetInfo target)
        {
            if (!target.HasThing)
                return null;
            Thing t = target.Thing;
            if (t == null || t.Destroyed)
                return null;
            // Targeting items in stacks: TryGetComp on the stack thing works
            // directly; quality items don't stack with differing quality (see
            // CompQuality.AllowStackWith), so the picked Thing is the one to
            // reforge.
            return t;
        }
    }
}
