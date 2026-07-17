using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Settings
{
    /// <summary>
    /// Mod entry point. Exposes the commonality settings screen and stamps the configured
    /// values onto the private <c>TraitDef.commonality</c> field of the mod's own traits.
    ///
    /// TraitDef.commonality is read live on every pawn generation
    /// (PawnGenerator.GenerateTraitsFor -> RandomElementByWeight), so writing the field is
    /// enough to change how often the traits roll — no pawn-generation patch is needed.
    /// </summary>
    public class CheatTraitsMod : Mod
    {
        public static CheatTraitsMod Instance { get; private set; } = null!;
        public static CheatTraitsSettings Settings { get; private set; } = null!;

        private Vector2 scrollPos;

        private const float MaxCommonality = 1f;
        private const float RowHeight = 30f;
        private const float ResetButtonWidth = 24f;
        private const float ValueWidth = 40f;
        private const float Epsilon = 0.001f;

        public CheatTraitsMod(ModContentPack content)
            : base(content)
        {
            Instance = this;
            Settings = GetSettings<CheatTraitsSettings>();
        }

        public override string SettingsCategory()
        {
            return "Cheat Traits";
        }

        /// <summary>All TraitDefs that belong to this mod, ordered by label for stable UI display.</summary>
        private static IEnumerable<TraitDef> OwnTraits()
        {
            if (Instance == null)
            {
                yield break;
            }
            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (def.modContentPack == Instance.Content)
                {
                    yield return def;
                }
            }
        }

        /// <summary>
        /// Writes each trait's effective commonality into the private <c>commonality</c> field.
        /// Called at startup (from the Harmony bootstrap) and whenever the settings window closes.
        /// </summary>
        public static void ApplyCommonality()
        {
            if (Instance == null || Settings == null)
            {
                return;
            }
            foreach (TraitDef def in OwnTraits())
            {
                Traverse.Create(def).Field("commonality").SetValue(Settings.Effective(def.defName));
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplyCommonality();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            CheatTraitsSettings s = Settings;

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            s.globalCommonality = listing.SliderLabeled(
                "Cheat trait commonality: " + s.globalCommonality.ToString("0.00"),
                s.globalCommonality,
                0f,
                MaxCommonality,
                0.35f,
                "Chance weight for each cheat trait to appear on generated pawns. 0 = off (default)."
            );

            Text.Font = GameFont.Tiny;
            listing.Label(
                "0 = off (default). Higher values make cheat traits more likely on newly generated pawns "
                    + "— colonists, allies, and raiders alike. A value near 1.0 makes each cheat trait roughly as "
                    + "common as a typical vanilla trait, which is very frequent across 17 traits. Small values "
                    + "(0.05–0.20) are recommended. Existing pawns are unaffected."
            );
            Text.Font = GameFont.Small;

            listing.Gap(6f);

            int overrideCount = s.perTraitOverrides.Count;
            string customizeLabel =
                overrideCount > 0
                    ? "Customize individual traits (" + overrideCount + " overridden)"
                    : "Customize individual traits";
            listing.CheckboxLabeled(
                customizeLabel,
                ref s.showPerTrait,
                "Set commonality per trait. Any trait left at the global value follows the slider above."
            );

            if (s.showPerTrait)
            {
                // Always shown while the panel is open so moving a slider off-default doesn't shift the layout.
                if (listing.ButtonText("Reset all to global"))
                {
                    s.perTraitOverrides.Clear();
                }
            }

            float listingBottom = listing.CurHeight;
            listing.End();

            if (s.showPerTrait)
            {
                List<TraitDef> traits = OwnTraits().OrderBy(t => t.label ?? t.defName).ToList();

                Rect outRect = new Rect(
                    inRect.x,
                    inRect.y + listingBottom + 4f,
                    inRect.width,
                    inRect.height - listingBottom - 4f
                );
                Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, traits.Count * RowHeight);

                Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
                float y = 0f;
                foreach (TraitDef def in traits)
                {
                    Rect row = new Rect(0f, y, viewRect.width, RowHeight);

                    Rect labelRect = new Rect(row.x, row.y, row.width * 0.38f, row.height);
                    Widgets.Label(labelRect, (def.label ?? def.defName).CapitalizeFirst());

                    Rect resetRect = new Rect(
                        row.xMax - ResetButtonWidth,
                        row.y + 3f,
                        ResetButtonWidth,
                        row.height - 6f
                    );
                    Rect valueRect = new Rect(
                        resetRect.x - ValueWidth - 4f,
                        row.y,
                        ValueWidth,
                        row.height
                    );
                    Rect sliderRect = new Rect(
                        labelRect.xMax + 8f,
                        row.y + (row.height - 10f) / 2f,
                        valueRect.x - labelRect.xMax - 16f,
                        10f
                    );

                    float current = s.Effective(def.defName);
                    float updated = Widgets.HorizontalSlider(
                        sliderRect,
                        current,
                        0f,
                        MaxCommonality,
                        false,
                        null,
                        null,
                        null,
                        0.01f
                    );

                    TextAnchor prevAnchor = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(valueRect, updated.ToString("0.00"));
                    Text.Anchor = prevAnchor;

                    if (Mathf.Abs(updated - s.globalCommonality) <= Epsilon)
                    {
                        s.perTraitOverrides.Remove(def.defName);
                    }
                    else if (
                        Mathf.Abs(updated - current) > Epsilon
                        || s.perTraitOverrides.ContainsKey(def.defName)
                    )
                    {
                        s.perTraitOverrides[def.defName] = updated;
                    }

                    if (
                        s.perTraitOverrides.ContainsKey(def.defName)
                        && Widgets.ButtonText(resetRect, "↺")
                    )
                    {
                        s.perTraitOverrides.Remove(def.defName);
                    }

                    y += RowHeight;
                }
                Widgets.EndScrollView();
            }
        }
    }
}
