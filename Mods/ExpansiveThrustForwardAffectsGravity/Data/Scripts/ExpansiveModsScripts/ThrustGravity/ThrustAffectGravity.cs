using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Expansive.ThrustAffectGravity
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockBeacon", "SmallBlockBeacon", "LargeBlockBeaconReskin", "SmallBlockBeaconReskin")]
    public class ThrustAffectGravity : MyGameLogicComponent
    {
        public static IMyTerminalBlock termBlock = null;
        private List<IMyEntity> thrustProducers = new List<IMyEntity>();
        private MyObjectBuilder_EntityBase objBuilderEntBase;
        private bool handleUnloads = false;
        IMyTerminalBlock Beacon;
        IMyBeacon BeaconBlock = null;
        private IMyCubeGrid CubeGrid = null;

        private float GetBeaconRange()
        {
            if (Beacon == null) return 0.0f;
            return ((IMyBeacon)Beacon).Radius;
        }

        private void GetThrustValue(IMyTerminalBlock block)
        {
            CubeGrid = block.CubeGrid;
            var gridTerm = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(CubeGrid);
            
            var seat = new List<IMyShipController>();
            gridTerm.GetBlocksOfType(seat);
            var gravity = new List<IMyGravityGenerator>();
            gridTerm.GetBlocksOfType(gravity);
            
            if (seat.Count != 0 && gravity.Count != 0)
            {
                foreach (IMyGravityGenerator thisGravity in gravity)
                {
                    var myString = thisGravity.CustomName;
                    for (int i = 0; i < myString.Length; i++)
                    {
                        if (myString[i].ToString() == "+")
                        {
                            thisGravity.GravityAcceleration = (float)seat[0].GetShipSpeed() / 10;
                        }
                        else if (myString[i].ToString() == "^")
                        {
                            thisGravity.GravityAcceleration = ((float)seat[0].GetShipSpeed() / 10) * -1;
                        }
                    }
                }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            objBuilderEntBase = objectBuilder;
            termBlock = Entity as IMyTerminalBlock;
            BeaconBlock = termBlock as IMyBeacon;
            BeaconBlock.EnabledChanged += BeaconEnabledStateChanged;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void PostDebugNotif(string debugString)
        {
            MyAPIGateway.Utilities.ShowNotification(debugString, 9600, "Red");
        }

        private void BeaconEnabledStateChanged(IMyTerminalBlock obj)
        {
            BeaconBlock = BeaconBlock as IMyBeacon;

            if (BeaconBlock.Enabled == true)
                return;

            BeaconBlock.Enabled = true;
        }

        public override void Close()
        {
            BeaconBlock.EnabledChanged -= BeaconEnabledStateChanged;
        }

        private void UpdateGridPower(IMySlimBlock obj)
        {
            CubeGrid = obj.CubeGrid;
            var gridTerm = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(CubeGrid);

            gridTerm.GetBlocksOfType(thrustProducers, block =>
            {
                if (block is IMyThrust)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

            if (thrustProducers.Count != 0)
            {
                CubeGrid.OnBlockAdded += UpdateThermals;
                CubeGrid.OnBlockRemoved += UpdateThermals;
                CubeGrid.OnBlockIntegrityChanged += UpdateThermals;
                handleUnloads = true;
            }
        }

        private void UpdateThermals(IMySlimBlock obj)
        {
            if (obj is IMyBeacon)
            {
                UpdateBeaconThrustState((IMyEntity)obj);
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (Beacon == null)
            {
                Beacon = Entity as IMyTerminalBlock;
                CubeGrid = Beacon.CubeGrid;
            }

            UpdateGridPower(Beacon.SlimBlock);

            if (Beacon == null) return;

            GetThrustValue(Beacon);
        }

        private void UpdateBeaconThrustState(VRage.ModAPI.IMyEntity obj)
        {
            if (Beacon == null) return;

            UpdateGridPower(Beacon.SlimBlock);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
            if (handleUnloads)
            {
                CubeGrid.OnBlockAdded -= UpdateThermals;
                CubeGrid.OnBlockRemoved -= UpdateThermals;
                CubeGrid.OnBlockIntegrityChanged -= UpdateThermals;
            }
        }
    }
}