using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChDiplomatThoughtUtil
    {
        public static bool HasPresence(Pawn p)
        {
            if (p?.health?.hediffSet == null) return false;
            var def = ChDiplomatDefOf.ChDiplomat_Presence;
            return def != null && p.health.hediffSet.HasHediff(def);
        }
    }

    // Mood: pawn has the presence hediff
    public class ThoughtWorker_ChDiplomatMoodAura : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null || p.Dead || !p.Spawned) return ThoughtState.Inactive;
            if (p.Faction != Faction.OfPlayer) return ThoughtState.Inactive;
            return ChDiplomatThoughtUtil.HasPresence(p) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    // Social opinion: both pawns have presence (meaning “diplomat has been around us recently”)
    public class ThoughtWorker_ChDiplomatSocialAura : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (p == null || other == null) return ThoughtState.Inactive;
            if (p == other) return ThoughtState.Inactive;
            if (p.Dead || other.Dead) return ThoughtState.Inactive;
            if (!p.Spawned || !other.Spawned) return ThoughtState.Inactive;
            if (p.Map != other.Map) return ThoughtState.Inactive;

            if (p.Faction != Faction.OfPlayer || other.Faction != Faction.OfPlayer)
                return ThoughtState.Inactive;

            if (!ChDiplomatThoughtUtil.HasPresence(p)) return ThoughtState.Inactive;
            if (!ChDiplomatThoughtUtil.HasPresence(other)) return ThoughtState.Inactive;

            return ThoughtState.ActiveAtStage(0);
        }
    }
}
