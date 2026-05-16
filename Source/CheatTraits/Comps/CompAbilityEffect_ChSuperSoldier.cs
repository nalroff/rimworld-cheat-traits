using CheatTraits.Hediffs;
using CheatTraits.Patches;
using RimWorld;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChSuperSoldier : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChSuperSoldier()
        {
            compClass = typeof(CompAbilityEffect_ChSuperSoldier);
        }
    }

    public class CompAbilityEffect_ChSuperSoldier : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Pawn? subject = target.Pawn;
            if (caster == null || subject == null || caster.Map == null || subject.Dead || !subject.Spawned)
                return;
            if (subject.health == null)
                return;

            // Don't double-apply. If the target already has the hediff (rare —
            // they'd have to be in the middle of a previous super-soldier buff
            // when cast on), refresh nothing and bail.
            if (subject.health.hediffSet?.GetFirstHediffOfDef(ChHediffDefOf.ChSuperSoldier) != null)
                return;

            Hediff_ChSuperSoldier hediff = (Hediff_ChSuperSoldier)
                HediffMaker.MakeHediff(ChHediffDefOf.ChSuperSoldier, subject);
            // AddHediff fires PostAdd which spawns the gear and pegs skills.
            subject.health.AddHediff(hediff);

            // Visual marker on the target — reuse the skip-flash fleck the rest
            // of the kit uses for tonal consistency.
            FleckMaker.Static(
                subject.DrawPos,
                subject.Map,
                FleckDefOf.PsycastSkipFlashEntry,
                2.5f
            );
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? subject = target.Pawn;
            if (subject == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Super Soldier requires a pawn target.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (!subject.RaceProps.Humanlike)
            {
                if (throwMessages)
                    Messages.Message(
                        "Super Soldier only works on humanlike pawns.",
                        subject,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            Pawn? caster = parent?.pawn;
            if (caster != null && subject != caster && subject.HostileTo(caster))
            {
                if (throwMessages)
                    Messages.Message(
                        "Super Soldier cannot target a hostile pawn.",
                        subject,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }
}
