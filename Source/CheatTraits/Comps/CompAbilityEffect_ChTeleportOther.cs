using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChTeleportOther : CompProperties_AbilityEffect
    {
        // Radius (in cells) around the caster searched for a standable destination.
        // 12 covers a generous landing pad even when the caster is in a tight room
        // surrounded by colonists, walls, or stockpiled gear.
        public float searchRadius = 12f;

        // Brief disorientation stun on the teleported pawn — mirrors how vanilla
        // Skip leaves the target stunned for a beat so they can't immediately act.
        public IntRange stunTicks = new IntRange(30, 60);

        public CompProperties_AbilityChTeleportOther()
        {
            compClass = typeof(CompAbilityEffect_ChTeleportOther);
        }
    }

    public class CompAbilityEffect_ChTeleportOther : CompAbilityEffect
    {
        private new CompProperties_AbilityChTeleportOther Props =>
            (CompProperties_AbilityChTeleportOther)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Pawn? subject = target.Pawn;
            if (caster == null || subject == null || caster.Map == null)
                return;
            if (subject == caster || subject.Destroyed)
                return;

            Map map = caster.Map;
            IntVec3 origin = subject.Position;

            // Find the nearest standable cell to the caster. StandableCellNear walks
            // GenRadial.RadialPattern from the center outward, so the first hit is
            // already the closest valid spot — no extra distance sort needed.
            // Exclude the caster's own cell so we never try to overlap with them.
            IntVec3 landing = CellFinder.StandableCellNear(
                caster.Position,
                map,
                Props.searchRadius,
                c => c != caster.Position && c.InBounds(map) && c.Walkable(map)
            );

            if (!landing.IsValid)
            {
                Messages.Message(
                    "No clear space near the caster to teleport the target.",
                    caster,
                    MessageTypeDefOf.RejectInput,
                    historical: false
                );
                return;
            }

            FleckMaker.Static(origin.ToVector3Shifted(), map, FleckDefOf.PsycastSkipFlashEntry, 1f);
            FleckMaker.Static(landing.ToVector3Shifted(), map, FleckDefOf.PsycastSkipInnerExit, 1f);
            FleckMaker.Static(
                landing.ToVector3Shifted(),
                map,
                FleckDefOf.PsycastSkipOuterRingExit,
                1f
            );

            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(origin, map, false));
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(landing, map, false));

            subject.Position = landing;
            subject.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);

            if (subject.stances?.stunner != null)
                subject.stances.stunner.StunFor(
                    Props.stunTicks.RandomInRange,
                    caster,
                    addBattleLog: false,
                    showMote: false,
                    disableRotation: false
                );
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? subject = target.Pawn;
            if (subject == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Teleport Other requires a pawn target.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (!subject.RaceProps.Humanlike)
            {
                if (throwMessages)
                    Messages.Message(
                        "Teleport Other only works on humanlike pawns.",
                        subject,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (subject == parent?.pawn)
            {
                if (throwMessages)
                    Messages.Message(
                        "Teleport Other cannot target the caster.",
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
