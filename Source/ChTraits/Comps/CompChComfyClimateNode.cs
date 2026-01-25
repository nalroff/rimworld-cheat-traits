using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace ChTraits.Comps
{
    // Inherit defaultTargetTemperature + inspectString plumbing.
    public class CompProperties_ChComfyClimateNode : CompProperties_TempControl
    {
        public float deadbandC = 1.5f;
        public float gain = 0.25f;
        public float heatPerDegreeC = 120f;
        public float maxAbsHeatPerTickRare = 300f;
        public bool requireIndoors = true;

        // x = room cell count, y = multiplier
        public SimpleCurve roomSizeEfficacyCurve = new SimpleCurve
        {
            new CurvePoint(10f,  1.90f),
            new CurvePoint(25f,  1.55f),
            new CurvePoint(50f,  1.25f),
            new CurvePoint(100f, 1.00f),
            new CurvePoint(200f, 0.80f),
            new CurvePoint(400f, 0.60f),
            new CurvePoint(800f, 0.45f)
        };

        public CompProperties_ChComfyClimateNode()
        {
            compClass = typeof(CompChComfyClimateNode);
        }
    }

    // This gives you vanilla temp gizmos for free.
    public class CompChComfyClimateNode : CompTempControl
    {

        // for debug
        private float lastRoomTemp;
        private float lastHeatPushed;
        private bool lastWasOutdoors;
        private bool lastInBand;
        private float lastRoomSizeMult;
        
        private CompProperties_ChComfyClimateNode PropsEx => (CompProperties_ChComfyClimateNode)props;

        public override void CompTickRare()
        {
            base.CompTickRare();

            Map map = parent.Map;
            if (map == null) return;

            Room room = parent.GetRoom();
            if (room == null) return;

            bool outdoors = room.PsychologicallyOutdoors;
            if (PropsEx.requireIndoors && outdoors)
            {
                lastHeatPushed = 0f;
                lastWasOutdoors = outdoors;
                return;
            }

            float target = TargetTemperature;
            float cur = room.Temperature;

            lastRoomTemp = cur;
            lastWasOutdoors = outdoors;

            float low = target - PropsEx.deadbandC;
            float high = target + PropsEx.deadbandC;

            lastInBand = (cur >= low && cur <= high);
            if (lastInBand)
            {
                lastHeatPushed = 0f;
                return;
            }

            float delta = target - cur;

            float sizeMult = PropsEx.roomSizeEfficacyCurve.Evaluate(room.CellCount);

            float heat = delta * PropsEx.gain * PropsEx.heatPerDegreeC * sizeMult;
            heat = Mathf.Clamp(heat, -PropsEx.maxAbsHeatPerTickRare, PropsEx.maxAbsHeatPerTickRare);

            lastHeatPushed = heat;
            lastRoomSizeMult = sizeMult;   // add this field (below)

            GenTemperature.PushHeat(parent.Position, map, heat);
        }

        public override string CompInspectStringExtra()
        {
            string baseStr = base.CompInspectStringExtra().TrimEndNewlines();

            string extra =
                $"\nRoom: {lastRoomTemp.ToStringTemperature("F0")}" +
                $"\nOutdoors: {lastWasOutdoors}" +
                $"\nIn band: {lastInBand}" +
                $"\nRoom mult: {lastRoomSizeMult:F2}" +
                $"\nHeat push: {lastHeatPushed:F1}";

            return (baseStr + extra).TrimEndNewlines();
        }
    }
}
