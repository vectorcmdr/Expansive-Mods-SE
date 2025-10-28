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

namespace Expansive.DriveSigs
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockBeacon", "SmallBlockBeacon", "LargeBlockBeaconReskin", "SmallBlockBeaconReskin")]
    public class DriveSigs : MyGameLogicComponent
    {
        public static IMyTerminalBlock termBlock = null;
        private List<IMyPowerProducer> powerProviders = new List<IMyPowerProducer>();
        private List<IMyEntity> thrustProducers = new List<IMyEntity>();
        private List<IMyEntity> heatRadiators = new List<IMyEntity>();
        private List<IMyEntity> atmoThrusters = new List<IMyEntity>();
        private List<IMyEntity> commsAntennae = new List<IMyEntity>();
        private MyObjectBuilder_EntityBase objBuilderEntBase;
        private bool handleUnloads = false;
        private bool isStaticGrid = false;
        private int delayCheck = 0;
        private float lastOutput = 0;
        private float outputCutLarge = 0.6f;
        private float outputCutSmall = 0.4f;
        private float outputCutRadiatorLarge = 0.3f;
        private float outputCutRadiatorSmall = 0.2f;
        private string stringID = "";
        IMyTerminalBlock Beacon;
        IMyBeacon BeaconBlock = null;
        private IMyCubeGrid CubeGrid = null;
        List<IMyGravityGenerator> gravity = new List<IMyGravityGenerator>();

        private float GetBeaconRange()
        {
            if (Beacon == null) return 0.0f;
            return ((IMyBeacon)Beacon).Radius;
        }

        private float GetThrustValue(IMyTerminalBlock block)
        {
            double rawThrustOutput = 0.0d;

            if (thrustProducers.Count != 0)
            {
                foreach (var thermalProducer in thrustProducers)
                {
                    if (thermalProducer is IMyThrust)
                    {
                        var thruster = thermalProducer as IMyThrust;
                        var thrusterName = thruster.BlockDefinition.SubtypeId.ToString();
                        int divisor;

                        if (thrusterName == "LargeBlockLargeHydrogenThrust" || thrusterName == "LargeBlockLargeHydrogenThrustIndustrial" || thrusterName == "LargeBlockPrototechThruster")
                            divisor = 600;

                        else if (thrusterName == "LargeBlockLargeThrust" || thrusterName == "LargeBlockLargeThrustSciFi" || thrusterName == "LargeBlockLargeModularThruster")
                            divisor = 700;

                        else if (thrusterName == "LargeBlockSmallHydrogenThrust" || thrusterName == "LargeBlockSmallHydrogenThrustIndustrial")
                            divisor = 3000;

                        else if (thrusterName == "LargeBlockSmallThrust" || thrusterName == "LargeBlockSmallThrustSciFi" || thrusterName == "LargeBlockSmallModularThruster")
                            divisor = 4000;

                        else if (thrusterName == "SmallBlockLargeHydrogenThrust" || thrusterName == "SmallBlockLargeHydrogenThrustIndustrial")
                            divisor = 1200;

                        else if (thrusterName == "SmallBlockLargeThrust" || thrusterName == "SmallBlockLargeThrustSciFi" || thrusterName == "SmallBlockLargeModularThruster" || thrusterName == "SmallBlockPrototechThruster")
                            divisor = 1400;

                        else if (thrusterName == "SmallBlockSmallHydrogenThrust" || thrusterName == "SmallBlockSmallHydrogenThrustIndustrial")
                            divisor = 4000;

                        else if (thrusterName == "SmallBlockSmallThrust" || thrusterName == "SmallBlockSmallThrustSciFi" || thrusterName == "SmallBlockSmallModularThruster")
                            divisor = 5000;

                        else divisor = 6000;
                        rawThrustOutput += thruster.CurrentThrust / divisor;

                        if (thruster.Enabled && thruster.GridThrustDirection != Vector3I.Zero && thruster.GridThrustDirection != Vector3I.Backward)
                        {
                            if (thrusterName == "LargeBlockLargeHydrogenThrust" || thrusterName == "LargeBlockLargeHydrogenThrustIndustrial" || thrusterName == "LargeBlockPrototechThruster")
                                thruster.Enabled = false;

                            else if (thrusterName == "LargeBlockLargeThrust" || thrusterName == "LargeBlockLargeThrustSciFi" || thrusterName == "LargeBlockLargeModularThruster")
                                thruster.Enabled = false;

                            else if (thrusterName == "SmallBlockLargeHydrogenThrust" || thrusterName == "SmallBlockLargeHydrogenThrustIndustrial" || thrusterName == "SmallBlockPrototechThruster")
                                thruster.Enabled = false;

                            else if (thrusterName == "SmallBlockLargeThrust" || thrusterName == "SmallBlockLargeThrustSciFi" || thrusterName == "SmallBlockLargeModularThruster")
                                thruster.Enabled = false;  
                            else;
                        }

                        //if(rawThrustOutput!=0)
                        //{
                        //  //PostDebugNotif(rawThrustOutput.ToString());
                        //}
                    }
                }
            }

            if (block.CubeGrid.IsStatic)
            {
                rawThrustOutput *= 0.25d;
            }

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

            return (float)rawThrustOutput;
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
            gridTerm.GetBlocksOfType(powerProviders, block =>
            {
                if (block.IsSameConstructAs(Beacon))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

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

            gridTerm.GetBlocksOfType(atmoThrusters, block =>
            {
                if (block is IMyThrust && ((IMyThrust)block).BlockDefinition.SubtypeId.Contains("Atmospheric"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

            gridTerm.GetBlocksOfType(heatRadiators, block =>
            {
                if (block is IMyHeatVent)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

            gridTerm.GetBlocksOfType(commsAntennae, block =>
            {
                if (block is IMyRadioAntenna && ((IMyRadioAntenna)block).IsBroadcasting && ((IMyRadioAntenna)block).Enabled)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });

            if (powerProviders.Count != 0 || thrustProducers.Count != 0 || heatRadiators.Count != 0 || commsAntennae.Count !=0)
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
            try
            {
                if (Beacon.IsWorking)
                {
                    UpdateBeaconThrustState(Beacon);
                }
                else
                {
                    UpdateGridPower(Beacon.SlimBlock);
                    if (GetThrustValue(Beacon) != 0.0f && GetThrustValue(Beacon) != lastOutput && GetThrustValue(Beacon) != GetThrustValue(Beacon))
                    {
                        ToggleBeaconPowerState(Beacon);
                    }
                }
            }
            catch (Exception exc)
            {
                MyLog.Default.WriteLine(exc);
            }
        }

        private void UpdateBeaconThrustState(VRage.ModAPI.IMyEntity obj)
        {
            if (Beacon == null) return;
            var subtype = Beacon.Name;
            if (obj == null) return;
            if (!(obj is IMyBeacon)) return;
            var output = GetThrustValue(Beacon);
            var beacon = obj as IMyBeacon;

            if (lastOutput > output)
            {
                delayCheck = 0;
                if (heatRadiators.Count == 0)
                {
                    if (beacon.CubeGrid.IsStatic)
                    {
                        output = 0f;
                        isStaticGrid = true;
                    }
                    else if (beacon.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                    {
                        output = lastOutput * outputCutLarge;
                        isStaticGrid = false;
                    }
                    else
                    {
                        output = lastOutput * outputCutSmall;
                        isStaticGrid = false;
                    }
                }
                else
                {
                    foreach (var sb in heatRadiators)
                    {
                        if (!((IMyFunctionalBlock)sb).IsWorking) continue;
                        if (beacon.CubeGrid.IsStatic)
                        {
                            output = 0f;
                            isStaticGrid = true;
                        }
                        else if (beacon.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                        {
                            output = lastOutput * outputCutRadiatorLarge;
                            isStaticGrid = false;
                        }
                        else
                        {
                            output = lastOutput * outputCutRadiatorSmall;
                            isStaticGrid = false;
                        }
                    }
                }
            }

            if (output <= 1.0f)
            {
                output = 1.0f;
            }

            if (GetBeaconRange() < output && output != 0.0f && (GetBeaconRange() != lastOutput))
            {
                delayCheck += 1;
                UpdateGridPower(Beacon.SlimBlock);
                if (delayCheck == 5 && output <= 1000000)
                {
                    ToggleBeaconPowerState(Beacon);
                }
            }

            lastOutput = output;
            beacon.Radius = output;
            beacon.Enabled = true;
            float n = beacon.Radius;
            
            if(commsAntennae.Count > 0)
            {
                stringID = CubeGrid.CustomName;
            }
            else
            {
                stringID = "No Tx";
            }

            if (atmoThrusters.Count != 0){
                beacon.HudText = "SIG: " + stringID;
                //PostDebugNotif("isAtmo");
            }
            else if(isStaticGrid)
            {
                beacon.HudText = "SIG: " + stringID + " - Station";
            }
            else if (n < 100f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Idle";
            }
            else if (n >= 100f && n < 8000f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Small";
            }
            else if (n >= 8001f && n < 25000f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Medium";
            }
            else if (n >= 25001f && n < 100000f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Large";
            }
            else if (n >= 100001f && n < 250000f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Very Large";
            }
            else if (n >= 250001f && n < 500000f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Massive";
            }
            else if (n >= 500001f)
            {
                beacon.HudText = "SIG: " + stringID + " - Drive: Enormous";
            }
        }

        private void ToggleBeaconPowerState(IMyTerminalBlock block)
        {
            if (block == null) return;
            foreach (var power in powerProviders)
            {
                var grid = power.Parent as MyCubeGrid;
                if (grid == null) return;
                try
                {
                    if (power == null)
                        return;
                    BroadcastBeaconTamper(block);
                    power.Enabled = false;
                }
                catch (Exception exc)
                {
                    MyLog.Default.WriteLine(exc);
                }
            }
        }

        private void BroadcastBeaconTamper(IMyTerminalBlock block)
        {
            if (block == null) return;
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            IMyPlayer BlockOwner = players.Find(p => p.IdentityId == block.OwnerId);
            if (BlockOwner == null) return;
            if (BlockOwner.IsBot) return;
            if (BlockOwner.IdentityId != MyAPIGateway.Session.Player.IdentityId) return;
            MyAPIGateway.Utilities.ShowNotification("Tampering Detected: Power Systems Disabled!", 9600, "Red");
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