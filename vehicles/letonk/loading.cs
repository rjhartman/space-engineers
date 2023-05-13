using System;
using System.Linq;
using System.Collections.Generic;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;

using VRageMath;


namespace Letonk
{

    public sealed class Program : MyGridProgram
    {

        #region Letonk

        TailLightGroup tailLights;
        List<IMyLightingBlock> parkingLights;
        IMyShipController controller;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            var tailLightsGroup = GridTerminalSystem.GetBlockGroupWithName("Tail Lights");
            if (tailLightsGroup == null)
                Log("No block group named \"Tail Lights\" was found.", "warning");
            else
            {
                tailLights = new TailLightGroup(tailLightsGroup);
                if (tailLights.Count() == 0) Log("No light blocks were found in the \"Tail Lights\" block group.", "warning");
            }


            var parkingLightsGroup = GridTerminalSystem.GetBlockGroupWithName("Parking Lights");
            if (parkingLightsGroup == null)
                Log("No block group named \"Parking Lights\" was found.", "warning");
            else
            {
                parkingLights = new List<IMyLightingBlock>();
                parkingLightsGroup.GetBlocksOfType(parkingLights, IsLight);
                if (parkingLights.Count() == 0) Log("No light blocks were found in the \"Parking Lights\" block group.", "warning");
            }

            var tmp = GridTerminalSystem.GetBlockWithName("Cockpit");
            if (tmp == null) Log("No block named \"Cockpit\" was found.");
            else if (!(tmp is IMyShipController)) Log("The block named \"Cockpit\" was not a cockpit.");
            else controller = GridTerminalSystem.GetBlockWithName("Cockpit") as IMyShipController;
        }

        private class TailLightGroup
        {
            // Constants
            Color BrakingColor = Color.Red;
            Color TailLightColor = new Color(26, 0, 0);
            Color ReversingColor = Color.White;
            float BrakingIntensity = 1;
            float DrivingIntensity = 0.3f;

            // Instance variables
            List<IMyLightingBlock> tailLights;

            public TailLightGroup(IMyBlockGroup blockGroup)
            {
                if (blockGroup == null) throw new ArgumentException("Arugment must not be null", nameof(blockGroup));
                tailLights = new List<IMyLightingBlock>();
                blockGroup.GetBlocksOfType(tailLights, IsLight);
            }

            bool IsLight(IMyFunctionalBlock block)
            {
                return block is IMyLightingBlock;
            }

            public void Brake(bool braking = false)
            {
                foreach (var light in tailLights)
                {
                    light.Color = braking ? BrakingColor : TailLightColor;
                    light.Intensity = braking ? BrakingIntensity : DrivingIntensity;
                }
            }

            public void Reverse(bool braking = false)
            {
                foreach (var light in tailLights)
                {
                    light.Color = ReversingColor;
                }
            }

            public int Count()
            {
                return tailLights.Count();
            }
        }

        void Log(String message, String level = "info")
        {
            Echo($"{level.ToUpper()}: {message}");
        }


        bool IsBraking()
        {
            return controller.HandBrake || controller.MoveIndicator.Y > 0;
        }

        bool IsReversing()
        {
            return controller.MoveIndicator.Z > 0;
        }

        private void Park()
        {
            foreach (var light in parkingLights)
            {
                light.Enabled = true;
            }
        }

        private void UnPark()
        {
            foreach (var light in parkingLights)
            {
                light.Enabled = false;
            }
        }

        bool IsLight(IMyFunctionalBlock block)
        {
            return block is IMyLightingBlock;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (controller != null)
            {
                // Automatically brake if the driver has exited the vehicle.
                if (!controller.IsUnderControl) controller.HandBrake = true;

                if (IsReversing()) tailLights.Reverse();
                else tailLights.Brake(IsBraking());

                if (controller.HandBrake) Park();
                else UnPark();
            }
        }

        #endregion // Vision
    }
}