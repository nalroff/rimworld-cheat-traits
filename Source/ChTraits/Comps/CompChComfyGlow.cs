using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace ChTraits.Comps
{
    public class CompProperties_ComfyGlow : CompProperties_Glower
    {
        public CompProperties_ComfyGlow()
        {
            compClass = typeof(CompComfyGlow);
        }
    }
    
    public class CompComfyGlow : CompGlower
    {
        private ColorInt lastColorInt;

        private CompProperties_Glower GlowerProps => (CompProperties_Glower)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            lastColorInt = GlowerProps.glowColor;
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            Map map = parent.Map;
            if (map == null) return;

            Room room = parent.GetRoom();
            if (room == null) return;

            float tempF = room.Temperature * 1.8f + 32f;

            // Compute desired color in UnityEngine.Color
            Color desired;
            if (tempF < 60f)
            {
                desired = Color.Lerp(Color.blue, Color.cyan, Mathf.InverseLerp(32f, 60f, tempF));
            }
            else if (tempF <= 80f)
            {
                desired = Color.white;
            }
            else
            {
                desired = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(80f, 100f, tempF));
            }

            // Convert UnityEngine.Color -> Verse.ColorInt (0..255 per channel)
            ColorInt desiredInt = ToColorInt(desired);

            // Compare with a small threshold to avoid spam re-registering the glower
            if (!NearlyEqual(desiredInt, lastColorInt))
            {
                lastColorInt = desiredInt;
                GlowerProps.glowColor = desiredInt;

                // Refresh glow emitter registration
                map.glowGrid.DeRegisterGlower(this);
                map.glowGrid.RegisterGlower(this);
            }
        }

        private static ColorInt ToColorInt(Color c)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            return new ColorInt(r, g, b, 180);
        }

        private static bool NearlyEqual(ColorInt a, ColorInt b)
        {
            // Threshold of ~2 steps per channel; tune if you want fewer updates.
            const int thresh = 2;
            return Mathf.Abs(a.r - b.r) <= thresh
                && Mathf.Abs(a.g - b.g) <= thresh
                && Mathf.Abs(a.b - b.b) <= thresh;
        }
    }
}