using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChTraits.Patches
{
    // Widely used method to get stat values for things (including pawns), so patch carefully
    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    public static class ChTraitsGetStatValuePatch
    {
        internal const float TexAimDelayMult = 0.10f;     // 0.10 = 90% reduction
        internal const float TexCooldownMult = 0.25f;     // 0.25 = 4x fire rate (if stat exists)
        internal const float TexAccuracyOffset = 0.50f;    // added to AccuracyTouch/Short/Medium/Long
        internal const string TexWeaponDefName = "Gun_Revolver";

        internal const float ArtificerSpeedMult = 5.0f;
        internal const float ArtificerSpeedCap = 8.0f;

        internal static bool IsBoxer(Pawn pawn)
        {
            return ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BoxerTrait) && pawn.equipment?.Primary == null;
        }

        internal static bool IsTex(Pawn pawn)
        {
            if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.TexTrait)) return false;
            if (pawn?.equipment?.Primary == null) return false;
            return pawn.equipment.Primary.def?.defName == TexWeaponDefName;
        }

        internal static bool IsArtificer(Pawn pawn)
        {
            return ChTraitsUtils.HasTrait(pawn, ChTraitsNames.ArtificerTrait);
        }

        internal static bool IsAscendant(Pawn pawn)
        {
            return ChTraitsUtils.HasTrait(pawn, ChTraitsNames.AscendantTrait);
        }

        public static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
        {
            if (thing is not Pawn pawn) return;

            // ------------------------
            // Ch Boxer: unarmed-only melee damage factor
            // ------------------------
            if (IsBoxer(pawn))
            {
                if (stat == StatDefOf.MeleeDamageFactor)
                    __result *= 10f; // tune
            }

            // ------------------------
            // Ch Tex: revolver-only bonuses
            // ------------------------
            if (IsTex(pawn))
            {
                // Weapon accuracy stats are safe to touch
                if (stat.defName is "ShootingAccuracyPawn")
                {
                    __result = Mathf.Clamp(__result + TexAccuracyOffset, 0f, 0.99f);
                }

                // Faster aim
                if (stat.defName == "AimingDelayFactor")
                {
                    __result *= TexAimDelayMult;
                }

                // Faster rate of fire (if the stat exists)
                if (stat.defName == "RangedCooldownFactor")
                {
                    __result *= TexCooldownMult;
                }
            }

            // ------------------------
            // Ch Artificer: construction and crafting speed boost
            // ------------------------
            if (IsArtificer(pawn))
            {
                if (stat.defName is "GeneralLaborSpeed" or "ConstructionSpeed")
                    __result = Mathf.Min(__result * ArtificerSpeedMult, ArtificerSpeedCap);
            }

            // ------------------------
            // Ch Ascendant: fertility boost
            // ------------------------
            if (IsAscendant(pawn))
            {
                if (stat.defName == "Fertility")
                {
                    __result = 1.0f; // always fertile
                }
            }
        }
    }
}
