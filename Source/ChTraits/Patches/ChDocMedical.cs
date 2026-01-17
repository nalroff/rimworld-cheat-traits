using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class DocUtil
    {
        internal const float ForcedTendQuality = 1.2f;

        internal static bool IsDoc(Pawn pawn)
            => ChTraitsUtils.HasTrait(pawn, ChTraitsNames.DocTrait);
    }

    // ---------------------------------------------------------------------
    // 1) ALL SURGERY SUCCEEDS (for any patient, including animals)
    //
    // Strategy: Patch the central surgery fail check and force "no fail"
    // when the surgeon has ChDoc.
    //
    // RimWorld versions sometimes expose:
    //   - Recipe_Surgery.CheckSurgeryFail(...)
    //   - Recipe_Surgery.TryCheckSurgeryFail(...)
    // so we resolve dynamically and handle both.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_RecipeSurgery_CheckSurgeryFail_Doc
    {
        static MethodBase TargetMethod()
        {
            var t = typeof(Recipe_Surgery);
            var methods = AccessTools.GetDeclaredMethods(t)
                .Where(m =>
                    (m.Name == "CheckSurgeryFail" || m.Name == "TryCheckSurgeryFail") &&
                    m.ReturnType == typeof(bool))
                .ToList();

            // Prefer an overload that includes a surgeon Pawn parameter.
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Any(p => p.ParameterType == typeof(Pawn)))
                    return m;
            }

            return methods.FirstOrDefault();
        }

        // We can't rely on a single signature, so we use Harmony's ability to receive args via object[].
        // If ChDoc surgeon is involved, we force the result to false ("did not fail"),
        // and skip the original.
        public static bool Prefix(ref bool __result, object[] __args)
        {
            try
            {
                if (__args == null) return true;

                // Find the first Pawn argument that is plausibly the surgeon.
                // In Recipe_Surgery methods, the surgeon is almost always a Pawn parameter.
                Pawn surgeon = null;
                for (int i = 0; i < __args.Length; i++)
                {
                    if (__args[i] is Pawn p)
                    {
                        // Heuristic: surgeon is the pawn doing the bill.
                        // Even if we accidentally pick patient in some odd signature,
                        // the effect would be "patient with Doc makes surgery succeed",
                        // which is still acceptable for a cheat trait.
                        surgeon = p;
                        break;
                    }
                }

                if (surgeon == null) return true;
                if (!DocUtil.IsDoc(surgeon)) return true;

                // "false" means "surgery did not fail"
                __result = false;
                return false; // skip original
            }
            catch (Exception ex)
            {
                Log.Error($"[ChTraits] ChDoc surgery patch error: {ex}");
                return true; // fail open to vanilla behavior
            }
        }
    }

    // ---------------------------------------------------------------------
    // 2) FORCE TEND QUALITY TO 0.98 (including self-tend)
    //
    // Strategy: Patch the tend quality calculation and clamp to 0.98
    // when the doctor has ChDoc.
    //
    // A stable hook across versions is typically:
    //   TendUtility.CalculateBaseTendQuality(...)
    // or similar. We'll resolve dynamically by name and parameters.
    // ---------------------------------------------------------------------
    [HarmonyPatch]
    internal static class Patch_TendUtility_CalculateBaseTendQuality_Doc
    {
        static MethodBase TargetMethod()
        {
            var t = typeof(TendUtility);

            var methods = AccessTools.GetDeclaredMethods(t)
                .Where(m =>
                    (m.Name.Contains("TendQuality") || m.Name.Contains("Calculate") || m.Name.Contains("BaseTend")) &&
                    m.ReturnType == typeof(float))
                .ToList();

            // Prefer methods that include a doctor Pawn parameter.
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Any(p => p.ParameterType == typeof(Pawn)))
                    return m;
            }

            // Common exact name in many builds:
            // "CalculateBaseTendQuality"
            var exact = methods.FirstOrDefault(m => m.Name == "CalculateBaseTendQuality");
            return exact ?? methods.FirstOrDefault();
        }

        public static void Postfix(ref float __result, object[] __args)
        {
            try
            {
                if (__args == null) return;

                // Find doctor Pawn in args
                Pawn doctor = null;
                for (int i = 0; i < __args.Length; i++)
                {
                    if (__args[i] is Pawn p)
                    {
                        doctor = p;
                        break;
                    }
                }

                if (doctor == null) return;
                if (!DocUtil.IsDoc(doctor)) return;

                // Force excellent tend quality; keep within [0..1]
                __result = Math.Min(Math.Max(__result, DocUtil.ForcedTendQuality), 1f);
            }
            catch (Exception ex)
            {
                Log.Error($"[ChTraits] ChDoc tend patch error: {ex}");
            }
        }
    }
}
