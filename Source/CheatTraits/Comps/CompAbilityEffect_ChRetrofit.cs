using RimWorld;
using Verse;
using CheatTraits.Patches;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChRetrofit : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChRetrofit()
        {
            compClass = typeof(CompAbilityEffect_ChRetrofit);
        }
    }

    /// <summary>
    /// The Engineer's counterpart to the Artificer's Reforge: rerolls the
    /// quality of an installed, non-art building (beds, chairs, tables,
    /// benches, etc.) using the same 60/30/10 Excellent/Masterwork/Legendary
    /// weights. Sculptures and carried items are excluded — those stay with
    /// the Artificer's Reforge.
    /// </summary>
    public class CompAbilityEffect_ChRetrofit : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing? thing = ResolveQualityThing(target);
            if (thing == null || !IsRetrofitTarget(thing))
                return;

            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq == null)
                return;

            QualityCategory newQuality = ArtificerQualityUtil.GetArtificerQualityLevel();
            cq.SetQuality(newQuality, ArtGenerationContext.Colony);

            // Force a redraw so any quality-tinted graphics refresh.
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
                        "Retrofit requires an installed building.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            if (!IsRetrofitTarget(thing))
            {
                if (throwMessages)
                    Messages.Message(
                        $"{thing.LabelShortCap} is an item or sculpture — use Reforge instead.",
                        thing,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            if (thing.TryGetComp<CompQuality>() == null)
            {
                if (throwMessages)
                    Messages.Message(
                        $"{thing.LabelShortCap} has no quality to retrofit.",
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
            if (!IsRetrofitTarget(thing))
                return "Use Reforge";
            if (thing.TryGetComp<CompQuality>() == null)
                return "No quality";
            return null;
        }

        /// <summary>
        /// Retrofit handles installed, non-art buildings only. Sculptures (art
        /// buildings) and carried/equipped items are the Artificer's Reforge.
        /// </summary>
        private static bool IsRetrofitTarget(Thing thing)
        {
            return thing is Building && thing.TryGetComp<CompArt>() == null;
        }

        private static Thing? ResolveQualityThing(LocalTargetInfo target)
        {
            if (!target.HasThing)
                return null;
            Thing t = target.Thing;
            if (t == null || t.Destroyed)
                return null;
            return t;
        }
    }
}
