using System;
using System.Linq;
using System.Collections.Generic;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

using VRageMath;


namespace BasicCrane
{

    public sealed class Program : MyGridProgram
    {

        #region BasicCrane

        float RetractVelocity = -0.25f;
        float ExtendVelocity = 0.25f;

        IMyPistonBase piston;
        IMyLandingGear landingGear;
        String mode;
        Boolean disabled = false;
        readonly IMyTextSurface LCD;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            piston = GetBlockWithName<IMyPistonBase>("Crane Piston");
            landingGear = GetBlockWithName<IMyLandingGear>("Crane Landing Gear");
            LCD = Me.GetSurface(0);
            LCD.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            LCD.FontColor = Color.White;
            LCD.BackgroundColor = Color.Black;
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

        void Retract()
        {
            piston.Enabled = true;
            piston.Velocity = RetractVelocity;
        }

        void Extend()
        {
            if (landingGear.IsLocked) Stop();
            else
            {
                piston.Enabled = true;
                piston.Velocity = landingGear.IsLocked ? 0 : ExtendVelocity;
            }
        }

        void Stop()
        {
            piston.Velocity = 0;
            piston.Enabled = false;
            mode = "";
        }

        void ParseArgument(string argument)
        {
            if (argument == "disable")
            {
                Stop();
                disabled = true;
            }
            else if (argument == "enable")
                disabled = false;
            else if (argument == "toggle")
            {
                if (!disabled)
                {
                    Stop();
                }
                disabled = !disabled;
            }
            else if (argument != "" && argument != null)
                mode = argument;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            DisplayTick();
            ParseArgument(argument);
            if (disabled) return;

            if (mode == "retract") Retract();
            else if (mode == "extend") Extend();
            else Stop();
        }

        void DisplayTick()
        {
            String message = $"Locked: {landingGear.IsLocked}\nMode: {mode}";
            LCD.WriteText(message);
        }

        #endregion // Vision
    }
}