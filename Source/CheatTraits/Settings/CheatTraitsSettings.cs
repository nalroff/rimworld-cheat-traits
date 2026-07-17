using System.Collections.Generic;
using Verse;

namespace CheatTraits.Settings
{
    /// <summary>
    /// User-configurable commonality for the Ch* traits. Default is 0 (off) so the
    /// traits never appear during pawn generation unless the player opts in.
    /// </summary>
    public class CheatTraitsSettings : ModSettings
    {
        /// <summary>Commonality applied to every Ch* trait that has no per-trait override.</summary>
        public float globalCommonality = 0f;

        /// <summary>Per-trait commonality keyed by TraitDef.defName. Absence means "follow global".</summary>
        public Dictionary<string, float> perTraitOverrides = new Dictionary<string, float>();

        /// <summary>UI-only: whether the per-trait override editor is expanded.</summary>
        public bool showPerTrait = false;

        /// <summary>Effective commonality for a trait: its override if present, otherwise the global value.</summary>
        public float Effective(string defName)
        {
            if (perTraitOverrides.TryGetValue(defName, out float v))
            {
                return v;
            }
            return globalCommonality;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref globalCommonality, "globalCommonality", 0f);
            Scribe_Values.Look(ref showPerTrait, "showPerTrait", false);
            Scribe_Collections.Look(ref perTraitOverrides, "perTraitOverrides", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && perTraitOverrides == null)
            {
                perTraitOverrides = new Dictionary<string, float>();
            }
        }
    }
}
