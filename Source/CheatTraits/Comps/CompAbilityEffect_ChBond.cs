using CheatTraits.Patches;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChBond : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityChBond()
        {
            compClass = typeof(CompAbilityEffect_ChBond);
        }
    }

    public class CompAbilityEffect_ChBond : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Pawn? first = target.Pawn;
            if (caster == null || first == null || parent == null)
                return;

            // PreActivate has already deducted nothing (no charges) and started the
            // 12500-tick cooldown. If the player aborts the second pick, we refund
            // by calling parent.ResetCooldown(). actionWhenFinished fires on both
            // success and cancel, so the bool flag distinguishes the two.
            Ability ability = parent;
            bool secondPicked = false;

            Find.Targeter.BeginTargeting(
                BondTargetingParameters(first),
                (LocalTargetInfo second) =>
                {
                    Pawn? secondPawn = second.Pawn;
                    if (secondPawn == null)
                        return;
                    secondPicked = true;
                    ApplyBond(first, secondPawn);
                },
                caster: caster,
                actionWhenFinished: () =>
                {
                    if (!secondPicked)
                        ability.ResetCooldown();
                }
            );
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? subject = target.Pawn;
            if (subject == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Bond requires a humanlike pawn.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (subject.Dead || !subject.RaceProps.Humanlike)
            {
                if (throwMessages)
                    Messages.Message(
                        "Bond only works on living humanlike pawns.",
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
                        "Bond cannot target the caster.",
                        subject,
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        private static TargetingParameters BondTargetingParameters(Pawn excludePawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetSelf = false,
                validator = (TargetInfo info) =>
                {
                    if (!info.HasThing)
                        return false;
                    Pawn? p = info.Thing as Pawn;
                    if (p == null || p.Dead || !p.Spawned)
                        return false;
                    if (!p.RaceProps.Humanlike)
                        return false;
                    if (p == excludePawn)
                        return false;
                    return true;
                },
            };
        }

        private static void ApplyBond(Pawn a, Pawn b)
        {
            ChDiplomatBondsGameComponent? bonds = ChDiplomatBondsGameComponent.Instance;
            if (bonds == null || a == null || b == null || a == b)
                return;

            if (bonds.IsBonded(a, b))
            {
                bonds.RemoveBond(a, b);
                Messages.Message(
                    $"{a.LabelShortCap} and {b.LabelShortCap} are no longer bonded.",
                    new LookTargets(new Pawn[] { a, b }),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                bonds.AddBond(a, b);
                Messages.Message(
                    $"{a.LabelShortCap} and {b.LabelShortCap} are now bonded — their compatibility is near-maximum.",
                    new LookTargets(new Pawn[] { a, b }),
                    MessageTypeDefOf.PositiveEvent,
                    historical: false
                );
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
    }
}
