using System;
using System.Linq;
using System.Collections.Generic;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

using VRageMath;


namespace theDestroyerOfWorldsAndDrills
{

    public sealed class Program : MyGridProgram
    {

        #region theDestroyerOfWorldsAndDrills

        // The smallest thrust-to-weight ratio that the vehicle must maintain at all times.
        float SMALLEST_ACCEPTABLE_TWR = 1.1f;
        // The buffer zone. Ideally, the vehicle will only fill until you are at SMALLEST_ACCEPTABLE_TWR + TWR_BUFFER.
        // If you exhaust this buffer amount, the vehicle will automatically dump cargo until it is back at an acceptable TWR.
        float TWR_BUFFER = 0.4f;

        float maxEffectiveThrust = 0;
        List<IMyThrust> verticalThrusters;
        IMyShipController cockpit;
        IMyShipConnector connector;
        IMyLightingBlock statusLight;
        IMySoundBlock siren;
        readonly IMyTextSurface LCD;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            cockpit = GetBlockWithName<IMyShipController>("Cockpit");
            connector = GetBlockWithName<IMyShipConnector>("Connector");
            siren = GetBlockWithName<IMySoundBlock>("Siren");
            statusLight = GetBlockWithName<IMyLightingBlock>("Status Light");

            siren.SelectedSound = "Alert 3";
            siren.LoopPeriod = 60 * 30;

            LCD = Me.GetSurface(0);
            LCD.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            LCD.FontColor = Color.White;
            LCD.BackgroundColor = Color.Black;

            verticalThrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(verticalThrusters, IsVerticalThruster);
            ComputeMaxEffectiveThrust();

        }

        void ComputeMaxEffectiveThrust()
        {
            float thrust = 0;
            foreach (var thruster in verticalThrusters)
            {
                if (thruster.IsWorking)
                    thrust += thruster.MaxEffectiveThrust;
            }
            maxEffectiveThrust = thrust;
            Write($"Max thrust: {thrust}");
        }

        void Write(string message)
        {
            LCD.WriteText(message);
        }

        bool IsVerticalThruster(IMyFunctionalBlock block)
        {
            return block is IMyThrust && block.Orientation.Forward.ToString() == "Down";
        }

        T GetBlockWithName<T>(String name)
        {
            var block = GridTerminalSystem.GetBlockWithName(name);
            if (block == null)
                throw new ArgumentException($"No block named \"{name}\" was found.");
            if (!(block is T))
                throw new ArgumentException($"Block named \"{name}\" was not an instance of {nameof(T)}.");
            return (T)block;
        }

        void Alarm()
        {
            siren.Play();
            siren.SelectedSound = "";
            statusLight.Color = Color.Red;
        }

        void ClearAlarms()
        {
            siren.Stop();
            siren.Enabled = false;
            statusLight.Color = Color.Green;
        }

        void EmergencyDumpCargo()
        {
            Alarm();
            connector.CollectAll = true;
            connector.ThrowOut = true;
        }

        void CheckCargo(double weight)
        {
            connector.ThrowOut = false;
            connector.CollectAll = false;
            ClearAlarms();
            if (SMALLEST_ACCEPTABLE_TWR * TWR_BUFFER > weight)
                connector.Disconnect();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ComputeMaxEffectiveThrust();
            var mass = cockpit.CalculateShipMass().TotalMass;
            var gravity = cockpit.GetTotalGravity().Z;
            if (SMALLEST_ACCEPTABLE_TWR > maxEffectiveThrust / (gravity * mass))
                // The ship has too much mass to fly stable, must dump!
                EmergencyDumpCargo();
            else
                CheckCargo(gravity * mass);

        }

        #endregion
    }
}