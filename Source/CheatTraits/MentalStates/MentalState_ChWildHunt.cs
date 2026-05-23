using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.MentalStates
{
    // Subclasses MentalState_Manhunter so the vanilla animal think tree branch
    // (ThinkNode_ConditionalMentalStateClass stateClass=MentalState_Manhunter in
    // Core SubTrees_Misc.xml) picks it up via IsInstanceOfType — no think-tree
    // patch required. JobGiver_Manhunter drives target selection via
    // AttackTargetFinder.BestAttackTarget; the overrides below narrow what counts
    // as "hostile" so candidates are restricted to pawns hostile to the player.
    public class MentalState_ChWildHunt : MentalState_Manhunter
    {
        public override bool ForceHostileTo(Thing t)
        {
            if (t == null || t == pawn)
                return false;

            Pawn? other = t as Pawn;
            if (other != null)
            {
                if (other.Faction != null && other.Faction == pawn.Faction)
                    return false;
                if (other.Faction == Faction.OfPlayerSilentFail)
                    return false;
                return other.HostileTo(Faction.OfPlayer);
            }

            if (t.Faction == null)
                return false;
            return ForceHostileTo(t.Faction);
        }

        public override bool ForceHostileTo(Faction f)
        {
            if (f == null)
                return false;
            if (f == pawn.Faction)
                return false;
            if (f == Faction.OfPlayerSilentFail)
                return false;
            return f.HostileTo(Faction.OfPlayer);
        }
    }
}
