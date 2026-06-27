using CheatTraits.Patches;
using Verse;

namespace CheatTraits.Hediffs
{
    /// <summary>
    /// Invisible state carrier for the ChBard support-hero aura. Holds which
    /// of the four aura stances (modes) is currently active and the tick the
    /// mode-switch cooldown ends. Persisted via ExposeData so the chosen stance
    /// and any running cooldown survive save/load and travel with the pawn
    /// across maps and caravans.
    ///
    /// The hediff itself has no stat stages — it is only a bookkeeping anchor.
    /// The actual buffs are separate ramping hediffs applied to nearby allies
    /// by ChBardAuraSystem, which reads <see cref="ModeIndex"/> from here.
    /// ChBardHediffApplier adds/removes this hediff with the trait; the
    /// gizmos in ChBardGizmoPatch read and mutate it.
    /// </summary>
    public class Hediff_ChBardConductor : HediffWithComps
    {
        private int modeIndex;
        private int switchCooldownEndTick = -1;

        /// <summary>Active aura stance (index into <see cref="ChBardModes"/>).</summary>
        public int ModeIndex => ChBardModes.Clamp(modeIndex);

        /// <summary>Ticks left before another stance change is allowed; 0 if ready.</summary>
        public int CooldownRemainingTicks
        {
            get
            {
                int now = Find.TickManager.TicksGame;
                return switchCooldownEndTick > now ? switchCooldownEndTick - now : 0;
            }
        }

        public bool CanSwitch => CooldownRemainingTicks <= 0;

        /// <summary>
        /// Switches to a new stance and starts the switch cooldown. No-ops (and
        /// starts no cooldown) if the requested stance is already active.
        /// </summary>
        public void SetMode(int newIndex, int cooldownTicks)
        {
            newIndex = ChBardModes.Clamp(newIndex);
            if (newIndex == ModeIndex)
                return;

            modeIndex = newIndex;
            switchCooldownEndTick = Find.TickManager.TicksGame + cooldownTicks;
        }

        // Show the active stance next to the hediff name in the health tab,
        // e.g. "bard's aura (War Anthem)".
        public override string LabelInBrackets => ChBardModes.Labels[ModeIndex];

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref modeIndex, "chBardModeIndex", 0);
            Scribe_Values.Look(ref switchCooldownEndTick, "chBardSwitchCooldownEndTick", -1);
        }
    }
}
