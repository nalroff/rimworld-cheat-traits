using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace ChTraits.Comps
{
    // Inherit defaultTargetTemperature + inspectString plumbing.
    public class CompProperties_ChComfyClimateNode : CompProperties_TempControl
    {
        public float deadbandC = 0.25f;
        public float gain = 1.0f;
        public float maxAbsHeatPerTickRare = 100_000f;
        public bool requireIndoors = true;

        public CompProperties_ChComfyClimateNode()
        {
            compClass = typeof(CompChComfyClimateNode);
        }
    }

    // This gives you vanilla temp gizmos for free.
    public class CompChComfyClimateNode : CompTempControl
    {
        private bool lastInBand = false;
        private CompProperties_ChComfyClimateNode PropsEx => (CompProperties_ChComfyClimateNode)props;

        public override void CompTickRare()
        {
            base.CompTickRare();

            Map map = parent.Map;
            if (map == null) return;

            Room room = parent.GetRoom();
            if (room == null) return;

            bool outdoors = room.PsychologicallyOutdoors;
            if (PropsEx.requireIndoors && outdoors) return;

            float target = TargetTemperature;
            float cur = room.Temperature;

            float low = target - PropsEx.deadbandC;
            float high = target + PropsEx.deadbandC;

            lastInBand = (cur >= low && cur <= high);
            if (lastInBand) return;

            float error = target - cur;
            if (Mathf.Abs(error) <= PropsEx.deadbandC) return;

            room.Temperature = target;
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra().TrimEndNewlines();
        }
    }
}
