using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChMiracleHeal : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChMiracleHeal()
        {
            compClass = typeof(CompAbilityEffect_ChMiracleHeal);
        }
    }

    /// <summary>
    /// Three-stage heal on the targeted pawn:
    ///   1. Remove the worst disease/infection hediff (tendable + makesSickThought).
    ///   2. Close every non-permanent Hediff_Injury via RemoveHediff.
    ///   3. RestorePart on one missing body part (worst by health, skipping vital parts).
    /// Vanilla Pawn_HealthTracker.RestorePart handles child-part cleanup and the
    /// removal of attached injuries/scars on the restored part — see
    /// features/restore-body-part.md in the analysis project.
    /// </summary>
    public class CompAbilityEffect_ChMiracleHeal : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Pawn? subject = target.Pawn;
            if (caster == null || subject == null || subject.Dead || subject.health == null)
                return;

            bool didAnything = false;

            didAnything |= CureWorstDisease(subject);
            didAnything |= CloseAllInjuries(subject);
            didAnything |= RestoreOneMissingPart(subject);

            if (subject.Spawned && subject.Map != null)
            {
                FleckMaker.Static(
                    subject.DrawPos,
                    subject.Map,
                    FleckDefOf.PsycastSkipFlashEntry,
                    2.5f
                );
            }

            if (!didAnything)
            {
                Messages.Message(
                    $"{subject.LabelShortCap} had nothing to heal.",
                    subject,
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? subject = target.Pawn;
            if (subject == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Miracle Heal requires a pawn target.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (subject.Dead)
            {
                if (throwMessages)
                    Messages.Message(
                        "Miracle Heal cannot target a dead pawn.",
                        subject,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        private static bool CureWorstDisease(Pawn pawn)
        {
            Pawn_HealthTracker? health = pawn.health;
            HediffSet? set = health?.hediffSet;
            if (health == null || set == null)
                return false;

            // Worst-first: prefer hediffs that make the pawn sick, then by severity.
            Hediff? worst = set
                .hediffs.Where(h =>
                    h != null
                    && h.def != null
                    && !(h is Hediff_Injury)
                    && !(h is Hediff_MissingPart)
                    && !(h is Hediff_AddedPart)
                    && (h.def.makesSickThought || h.def.tendable)
                    && h.Visible
                )
                .OrderByDescending(h => h.def.makesSickThought ? 1 : 0)
                .ThenByDescending(h => h.Severity)
                .FirstOrDefault();

            if (worst == null)
                return false;

            health.RemoveHediff(worst);
            return true;
        }

        private static bool CloseAllInjuries(Pawn pawn)
        {
            Pawn_HealthTracker? health = pawn.health;
            HediffSet? set = health?.hediffSet;
            if (health == null || set == null)
                return false;

            List<Hediff_Injury> injuries = set
                .hediffs.OfType<Hediff_Injury>()
                .Where(i => !i.IsPermanent())
                .ToList();

            if (injuries.Count == 0)
                return false;

            foreach (Hediff_Injury injury in injuries)
                health.RemoveHediff(injury);
            return true;
        }

        private static bool RestoreOneMissingPart(Pawn pawn)
        {
            Pawn_HealthTracker? health = pawn.health;
            HediffSet? set = health?.hediffSet;
            if (health == null || set == null)
                return false;

            // GetMissingPartsCommonAncestors collapses redundant child entries —
            // a missing arm hides the also-missing hand/fingers underneath it.
            List<Hediff_MissingPart> candidates = set
                .GetMissingPartsCommonAncestors()
                .Where(h => h?.Part != null && !IsVitalPart(h.Part))
                .ToList();

            if (candidates.Count == 0)
                return false;

            // Worst = highest part health (the biggest chunk of body to grow back).
            Hediff_MissingPart pick = candidates
                .OrderByDescending(h => h.Part.def.GetMaxHealth(pawn))
                .First();

            health.RestorePart(pick.Part);
            return true;
        }

        private static bool IsVitalPart(BodyPartRecord part)
        {
            if (part?.def == null)
                return false;
            // A pawn cannot be alive with these missing — vanilla rolls them into
            // Dead at missing-part-creation time. Filtering by defName is
            // defensive: keeps the heal from "regrowing" something the engine
            // would never expose as missing on a living target.
            string n = part.def.defName;
            return n == "Brain" || n == "Heart" || n == "Liver" || n == "Stomach";
        }
    }
}
