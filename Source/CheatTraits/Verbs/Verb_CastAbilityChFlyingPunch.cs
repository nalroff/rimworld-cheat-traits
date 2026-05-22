using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Verbs
{
    /// <summary>
    /// Custom jump verb for Flying Punch. Unlike vanilla Verb_CastAbilityJump,
    /// the picked target is the thing to strike (pawn/building/mech/animal) and
    /// the landing cell is computed as an adjacent walkable cell next to it —
    /// so the Boxer can leap into buildings or onto mechs without the engine
    /// rejecting the target cell.
    /// </summary>
    public class Verb_CastAbilityChFlyingPunch : Verb_CastAbility
    {
        public override bool MultiSelect => true;

        protected override bool TryCastShot()
        {
            Pawn caster = CasterPawn;
            if (caster == null)
                return false;

            LocalTargetInfo picked = currentTarget;
            IntVec3 landing = FindLandingCell(caster, picked);
            if (!landing.IsValid)
                return false;

            // Fire ability effects (cooldown, Apply) — base.TryCastShot calls ability.Activate.
            if (!base.TryCastShot())
                return false;

            // Pass the picked target as the flyer's `target` so OnJumpCompleted on
            // the comp can recover it and strike.
            return JumpUtility.DoJump(
                caster,
                landing,
                ReloadableCompSource,
                verbProps,
                ability,
                picked,
                ThingDefOf.PawnFlyer
            );
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            ability.QueueCastingJob(target, default(LocalTargetInfo));
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null)
                return false;
            if (!CanHitTarget(target))
                return false;
            if (!FindLandingCell(CasterPawn, target).IsValid)
            {
                if (showMessages)
                    Messages.Message(
                        "No clear space to land next to the target.",
                        target.ToTargetInfo(caster.Map),
                        MessageTypeDefOf.RejectInput,
                        historical: false
                    );
                return false;
            }
            return true;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            float r = EffectiveRange;
            if (root.DistanceToSquared(targ.Cell) > r * r)
                return false;
            if (verbProps.requireLineOfSight && !GenSight.LineOfSight(root, targ.Cell, caster.Map))
                return false;
            return true;
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target) && FindLandingCell(CasterPawn, target).IsValid)
            {
                base.OnGUI(target);
                return;
            }
            GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white);
            if (!target.IsValid)
                return;
            IntVec3 landing = FindLandingCell(CasterPawn, target);
            if (landing.IsValid)
                GenDraw.DrawTargetHighlightWithLayer(landing.ToVector3Shifted(), AltitudeLayer.MetaOverlays);
        }

        /// <summary>
        /// Finds a standable cell adjacent to (or on, if walkable) the picked
        /// target. Prefers the cell nearest to the caster so the jump lands at
        /// the closest tactical position.
        /// </summary>
        public static IntVec3 FindLandingCell(Pawn caster, LocalTargetInfo target)
        {
            if (caster == null || !target.IsValid)
                return IntVec3.Invalid;

            Map map = caster.Map;
            if (map == null)
                return IntVec3.Invalid;

            Thing? thing = target.Thing;

            // If the target is the caster's own cell (shouldn't happen for hostile),
            // bail.
            if (target.Cell == caster.Position)
                return IntVec3.Invalid;

            // For pawns and other walkable-tile targets, prefer landing on the
            // target cell itself (matches vanilla Verb_CastAbilityJump behavior).
            // PawnFlyer.CheckDestination handles the final disambiguation.
            if (thing != null && thing is Pawn && JumpUtility.ValidJumpTarget(caster, map, target.Cell))
                return target.Cell;

            IntVec3 best = IntVec3.Invalid;
            int bestDistSq = int.MaxValue;

            // Iterate cells adjacent to the target thing's full footprint
            // (buildings can be multi-cell). For a non-Thing target, fall back
            // to the 8 cells around target.Cell.
            if (thing != null)
            {
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(thing))
                {
                    if (!JumpUtility.ValidJumpTarget(caster, map, cell))
                        continue;
                    int d = caster.Position.DistanceToSquared(cell);
                    if (d < bestDistSq)
                    {
                        bestDistSq = d;
                        best = cell;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 cell = target.Cell + GenAdj.AdjacentCells[i];
                    if (!JumpUtility.ValidJumpTarget(caster, map, cell))
                        continue;
                    int d = caster.Position.DistanceToSquared(cell);
                    if (d < bestDistSq)
                    {
                        bestDistSq = d;
                        best = cell;
                    }
                }
            }

            return best;
        }
    }
}
