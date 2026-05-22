using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChDeadeye : CompProperties_AbilityEffect
    {
        public float damage = 150f;
        public float armorPenetration = 2.0f;

        public CompProperties_AbilityChDeadeye()
        {
            compClass = typeof(CompAbilityEffect_ChDeadeye);
        }
    }

    public class CompAbilityEffect_ChDeadeye : CompAbilityEffect
    {
        private new CompProperties_AbilityChDeadeye Props =>
            (CompProperties_AbilityChDeadeye)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Pawn? subject = target.Pawn;
            if (caster == null || subject == null || subject.Destroyed)
                return;
            if (caster.Map == null || subject.Map != caster.Map)
                return;

            // Direction-of-attack vector from caster to target — gives the bullet a
            // believable hit direction on the target's armor/body for the damage roll.
            Vector3 dir = (subject.Position - caster.Position).ToVector3();
            float angle = dir == Vector3.zero ? 0f : dir.AngleFlat();

            // Capture position/map before TakeDamage — a lethal hit despawns the
            // pawn, which nulls Position/Map and crashes the fleck call.
            Map impactMap = subject.Map;
            IntVec3 impactCell = subject.Position;

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Bullet,
                Props.damage,
                Props.armorPenetration,
                angle,
                caster,
                hitPart: null,
                weapon: caster.equipment?.Primary?.def,
                category: DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget: subject
            );

            subject.TakeDamage(dinfo);

            if (impactMap != null && impactCell.IsValid)
                FleckMaker.Static(impactCell.ToVector3Shifted(), impactMap, FleckDefOf.ShotFlash, 6f);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? caster = parent?.pawn;
            if (caster != null)
            {
                ThingWithComps? primary = caster.equipment?.Primary;
                if (primary == null || !primary.def.IsRangedWeapon)
                {
                    if (throwMessages)
                        Messages.Message(
                            "Deadeye requires a ranged weapon to be equipped.",
                            caster,
                            MessageTypeDefOf.RejectInput,
                            historical: false
                        );
                    return false;
                }
            }

            Pawn? subject = target.Pawn;
            if (subject == null)
            {
                if (throwMessages)
                    Messages.Message(
                        "Deadeye requires a pawn target.",
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            if (subject == parent?.pawn)
                return false;
            return base.Valid(target, throwMessages);
        }
    }
}
