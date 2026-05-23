using RimWorld;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChBlink : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChBlink()
        {
            compClass = typeof(CompAbilityEffect_ChBlink);
        }
    }

    public class CompAbilityEffect_ChBlink : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (caster == null || map == null)
                return;

            IntVec3 origin = caster.Position;
            IntVec3 landing = target.Cell;
            if (!landing.IsValid || !landing.InBounds(map) || landing == origin)
                return;

            FleckMaker.Static(origin.ToVector3Shifted(), map, FleckDefOf.PsycastSkipFlashEntry, 1f);
            FleckMaker.Static(landing.ToVector3Shifted(), map, FleckDefOf.PsycastSkipInnerExit, 1f);
            FleckMaker.Static(landing.ToVector3Shifted(), map, FleckDefOf.PsycastSkipOuterRingExit, 1f);

            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(origin, map, false));
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(landing, map, false));

            caster.Position = landing;
            caster.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (caster == null || map == null)
                return false;

            IntVec3 cell = target.Cell;
            if (!cell.IsValid || !cell.InBounds(map))
            {
                if (throwMessages)
                    Messages.Message(
                        "Blink: invalid target cell.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            if (!cell.Standable(map))
            {
                if (throwMessages)
                    Messages.Message(
                        "Blink: target cell is not standable.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }

            // Disallow landing on top of another pawn even if the cell is otherwise standable.
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn other && other != caster)
                {
                    if (throwMessages)
                        Messages.Message(
                            "Blink: target cell is occupied.",
                            MessageTypeDefOf.RejectInput,
                            historical: false
                        );
                    return false;
                }
            }

            return base.Valid(target, throwMessages);
        }
    }
}
