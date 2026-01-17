using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ChTraits.Patches
{
    [HarmonyPatch]
    public static class ChBeastmasterFloatMenuTame
    {
        public static bool Prepare()
        {
            return AccessTools.GetDeclaredMethods(typeof(FloatMenuMakerMap))
                .Any(m => m.Name == "AddHumanlikeOrders");
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(FloatMenuMakerMap))
                .Where(m => m.Name == "AddHumanlikeOrders")
                .Cast<MethodBase>();
        }

        public static void Postfix(object[] __args)
        {
            try
            {
                if (__args == null || __args.Length == 0) return;

                Pawn pawn = null;
                List<FloatMenuOption> opts = null;
                IntVec3 cell = IntVec3.Invalid;

                foreach (object arg in __args)
                {
                    if (arg is IntVec3 iv)
                        cell = iv;
                    else if (arg is Vector3 v)
                        cell = IntVec3.FromVector3(v);
                    else if (arg is Pawn p && p.RaceProps?.Humanlike == true)
                        pawn = p;
                    else if (arg is List<FloatMenuOption> list)
                        opts = list;
                }

                if (pawn == null || opts == null) return;
                if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.BeastmasterTrait)) return;
                if (pawn.Map == null) return;

                if (!cell.IsValid || !cell.InBounds(pawn.Map)) return;

                Pawn animal = cell.GetThingList(pawn.Map).OfType<Pawn>().FirstOrDefault();
                if (animal == null) return;

                if (animal.RaceProps?.Animal != true) return;
                if (animal.Dead) return;

                // Wild only
                if (animal.Faction != null) return;

                if (!pawn.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("Cannot tame (beastmaster): no path", null));
                    return;
                }

                opts.Add(new FloatMenuOption("Tame (beastmaster)", () =>
                {
                    Job job = JobMaker.MakeJob(ChJobDefOf.ChBeastmaster_TameNoFood, animal);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }));
            }
            catch (Exception e)
            {
                Log.Error($"[CheatTraits] Beastmaster floatmenu patch exception: {e}");
            }
        }
    }
}
