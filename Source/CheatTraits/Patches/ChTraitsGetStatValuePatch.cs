using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Patches
{
    // Widely used method to get stat values for things (including pawns), so patch carefully
    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    public static class CheatTraitsGetStatValuePatch
    {
        internal const float TexBaseAimDelayMult = 0.55f;
        internal const float TexBaseCooldownMult = 0.85f;
        internal const float TexBaseAccuracyOffset = 0.45f;

        // Revolver-specific bonuses stack on top of the always-on Tex bonuses.
        // Aim/cooldown land on the intended final multipliers; accuracy is
        // pushed much harder and then clamped by the stat cap.
        internal const float TexRevolverAimDelayMult = 0.10f / TexBaseAimDelayMult;
        internal const float TexRevolverCooldownMult = 0.25f / TexBaseCooldownMult;
        internal const float TexRevolverAccuracyOffset = 0.85f;
        internal const string TexWeaponDefName = "Gun_Revolver";

        internal const float ArtificerSpeedMult = 5.0f;
        internal const float ArtificerSpeedCap = 8.0f;

        internal const float EngineerSpeedMult = 5.0f;
        internal const float EngineerSpeedCap = 8.0f;

        internal static bool IsBoxer(Pawn pawn)
        {
            return CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.BoxerTrait)
                && pawn.equipment?.Primary == null;
        }

        internal static bool IsTex(Pawn pawn)
        {
            return CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.TexTrait);
        }

        internal static bool HasRevolver(Pawn pawn)
        {
            if (!IsTex(pawn))
                return false;
            if (pawn?.equipment?.Primary == null)
                return false;

            ThingDef? weaponDef = pawn.equipment.Primary.def;
            if (weaponDef == null)
                return false;

            string? defName = weaponDef.defName;
            if (defName == null)
                return false;

            return defName == TexWeaponDefName
                || defName.IndexOf("Revolver", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsArtificer(Pawn pawn)
        {
            return CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.ArtificerTrait);
        }

        internal static bool IsEngineer(Pawn pawn)
        {
            return CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.EngineerTrait);
        }

        internal static bool IsAscendant(Pawn pawn)
        {
            return CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.AscendantTrait);
        }

        public static void Postfix(
            Thing thing,
            StatDef stat,
            bool applyPostProcess,
            ref float __result
        )
        {
            if (thing is not Pawn pawn)
                return;

            // ------------------------
            // Ch Boxer: unarmed-only melee damage factor
            // ------------------------
            if (IsBoxer(pawn))
            {
                if (stat == StatDefOf.MeleeDamageFactor)
                    __result *= 10f; // tune
            }

            // ------------------------
            // Ch Tex: always-on gunfighter bonuses
            // ------------------------
            if (IsTex(pawn))
            {
                if (stat.defName is "ShootingAccuracyPawn")
                {
                    __result = Mathf.Clamp(__result + TexBaseAccuracyOffset, 0f, 0.99f);
                }

                if (stat.defName == "AimingDelayFactor")
                {
                    __result *= TexBaseAimDelayMult;
                }

                if (stat.defName == "RangedCooldownFactor")
                {
                    __result *= TexBaseCooldownMult;
                }
            }

            // ------------------------
            // Ch Tex: revolver-only spike
            // ------------------------
            if (HasRevolver(pawn))
            {
                if (stat.defName is "ShootingAccuracyPawn")
                {
                    __result = Mathf.Clamp(__result + TexRevolverAccuracyOffset, 0f, 0.99f);
                }

                if (stat.defName == "AimingDelayFactor")
                {
                    __result *= TexRevolverAimDelayMult;
                }

                if (stat.defName == "RangedCooldownFactor")
                {
                    __result *= TexRevolverCooldownMult;
                }
            }

            // ------------------------
            // Ch Artificer: general crafting speed boost (construction now belongs to Engineer)
            // ------------------------
            if (IsArtificer(pawn))
            {
                if (stat.defName == "GeneralLaborSpeed")
                    __result = Mathf.Min(__result * ArtificerSpeedMult, ArtificerSpeedCap);
            }

            // ------------------------
            // Ch Engineer: construction speed boost + never-fail construction
            // ------------------------
            if (IsEngineer(pawn))
            {
                if (stat.defName == "ConstructionSpeed")
                    __result = Mathf.Min(__result * EngineerSpeedMult, EngineerSpeedCap);
                else if (stat.defName == "ConstructSuccessChance")
                    __result = Mathf.Max(__result, 1f); // frames never fail
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
