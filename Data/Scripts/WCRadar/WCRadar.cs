using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Utils;
using System;
using CoreSystems.Api;
using System.Collections.Generic;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using System.Diagnostics;
using Draygo.API;
using System.Text;

namespace WCRadar
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session : MySessionComponentBase
    {
        WcApi wcAPi;
        HudAPIv2 hudAPI;

        internal bool client;
        internal int tick = -300;
        internal MyStringId particle = MyStringId.GetOrCompute("particle_laser"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal ICollection<MyTuple<MyEntity, float>> threatList = new List<MyTuple<MyEntity, float>>();
        internal ICollection<MyEntity> threatListCleaned = new List<MyEntity>();
        internal ICollection<MyEntity> threatListChecked = new List<MyEntity>();
        internal ICollection<MyEntity> obsList = new List<MyEntity>();
        internal ICollection<MyEntity> obsListCleaned = new List<MyEntity>();
        internal ICollection<MyEntity> obsListChecked = new List<MyEntity>();
        internal MyTuple<bool, int, int> projInbound = new MyTuple<bool, int, int>();
        internal ICollection<MyEntity> tempList = new List<MyEntity>();
        internal MyCubeGrid controlledGrid;

        public override void BeforeStart()
        {
            var IsServer = MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Session.IsServer;
            var MpActive = MyAPIGateway.Multiplayer.MultiplayerActive;
            var DedicatedServer = MyAPIGateway.Utilities.IsDedicated;
            var IsClient = !IsServer && !DedicatedServer && MpActive;
            var IsHost = IsServer && !DedicatedServer && MpActive;
            client = IsHost || IsClient || !MpActive;
            if (client)
            {
                MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEnteredSender;
                InitConfig();
                wcAPi = new WcApi();
                wcAPi.Load();
                hudAPI = new HudAPIv2();
                if(wcAPi.IsReady)
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "cycle overlays by entering /radar symbols or /radar lines in chat");
                else
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "no WeaponCore, no radar :/");
                if (hudAPI == null)
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "TextHudAPI failed to register");
            }
        }
        private void OnMessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            messageText.ToLower();
            var s = Settings.Instance;
            if (messageText == "/radar")
            {
                MyAPIGateway.Utilities.ShowMessage("WC Radar", "To cycle displays- \n /radar symbols \n/radar lines");
                sendToOthers = false;
            }
            if (messageText == "/radar symbols")
            {
                s.enableSymbols = !s.enableSymbols;
                MyAPIGateway.Utilities.ShowNotification("WC Radar symbols " + (s.enableSymbols == true ? "on" : "off" ));
                sendToOthers = false;
            }
            if (messageText == "/radar lines")
            {
                s.enableLines = !s.enableLines;
                MyAPIGateway.Utilities.ShowNotification("WC Radar lines " + (s.enableLines == true ? "on" : "off"));
                sendToOthers = false;
            }
            return;
        }


        private void UpdateGrid()
        {
            try
            {
                if (Session == null || Session.Player == null || Session.Player.Character == null)
                {
                    controlledGrid = null;
                    return;
                }
                var entity = Session.Player.Controller.ControlledEntity as MyCubeBlock;
                if (entity == null)
                {
                    controlledGrid = null;
                    return;
                }
                else
                {
                    controlledGrid = entity.CubeGrid;
                    threatList.Clear();
                    obsList.Clear();
                    threatListCleaned.Clear();
                    threatListChecked.Clear();
                    obsListCleaned.Clear();
                    obsListChecked.Clear();

                    wcAPi.GetSortedThreats(entity, threatList);
                    wcAPi.GetObstructions(entity, obsList);


                    foreach (var threat in threatList)//This gross mess is to cull subgrids.  Yes the naming is a lazy copy from obstructions  Culling approximately triples runtime
                    {
                        var obs = threat.Item1;
                        if (threatListCleaned.Contains(obs) || threatListChecked.Contains(obs))
                            continue;

                        var obsGrid = obs as MyCubeGrid;
                        if (obsGrid == null)//Roids/other
                        {
                            threatListCleaned.Add(obs);
                            continue;
                        }


                        tempList.Clear();

                        foreach (var threat2 in threatList)
                        {
                            var checkObs = threat2.Item1;
                            if (obs == checkObs || threatListChecked.Contains(checkObs)) continue;
                            var checkGrid = checkObs as MyCubeGrid;

                            if (checkGrid != null && obsGrid.IsInSameLogicalGroupAs(checkGrid))
                            {
                                tempList.Add(checkGrid);
                                threatListChecked.Add(checkGrid);
                            }
                        }
                        threatListChecked.Add(obs);
                        if (tempList.Count > 0)
                        {
                            MyEntity biggestEnt = obs;
                            float largestSize = obs.PositionComp.LocalVolume.Radius;
                            foreach (var temp in tempList)
                            {
                                if (temp.PositionComp.LocalVolume.Radius > largestSize)
                                {
                                    largestSize = temp.PositionComp.LocalVolume.Radius;
                                    biggestEnt = temp;
                                }
                            }
                            threatListCleaned.Add(biggestEnt);

                        }
                        else
                            threatListCleaned.Add(obsGrid);
                    }                                   
                    foreach (var obs in obsList)//This gross mess is to cull subgrids
                    {
                        if (obsListCleaned.Contains(obs) || obsListChecked.Contains(obs))
                            continue;
                        obsListChecked.Add(obs);
                        var obsPlanet = obs as MyPlanet;
                        if (obsPlanet != null)
                            continue;

                        var obsGrid = obs as MyCubeGrid;
                        if (obsGrid == null)//Roids/other
                        {
                            obsListCleaned.Add(obs);
                            continue;
                        }


                        tempList.Clear();

                        foreach (var checkObs in obsList)
                        {
                            if (obs == checkObs || obsListChecked.Contains(checkObs)) continue;
                            var checkGrid = checkObs as MyCubeGrid;
                            
                            if (checkGrid != null && obsGrid.IsInSameLogicalGroupAs(checkGrid))
                            {
                                tempList.Add(checkGrid);
                                obsListChecked.Add(checkGrid);
                            }
                        }
                        
                        if (tempList.Count > 0)
                        {
                            MyEntity biggestEnt = obs;
                            float largestSize = obs.PositionComp.LocalVolume.Radius;
                            foreach (var temp in tempList)
                            {
                                if (temp.PositionComp.LocalVolume.Radius > largestSize)
                                {
                                    largestSize = temp.PositionComp.LocalVolume.Radius;
                                    biggestEnt = temp;
                                }
                            }
                            obsListCleaned.Add(biggestEnt);

                        }
                        else
                            obsListCleaned.Add(obsGrid);
                    }
                    
                    projInbound = wcAPi.GetProjectilesLockedOn(entity);

                }
            }
            catch (Exception e)
            {
                controlledGrid = null;
                MyLog.Default.Error($"[WC Radar] Well something went wrong in Update {e}");
            }
        }
        public override void Draw()
        {
            if (client && wcAPi.IsReady && (Settings.Instance.enableLines || Settings.Instance.enableSymbols))
            {
                if (tick % 60 == 0)
                {
                    UpdateGrid();
                }
                DrawMarkers();
                tick++;
            }
        }

        private void DrawMarkers()
        {
            try
            {
                var s = Settings.Instance;

                if (controlledGrid != null && !controlledGrid.MarkedForClose)
                {
                    if (Session == null || Session.Player == null)
                    {
                        MyLog.Default.Error($"[WC Radar] Draw Session or player is null");
                        controlledGrid = null;
                        return;
                    }
                    var playerPos = controlledGrid.PositionComp.WorldAABB.Center;
                    var colorEnemy = s.colorRev; //Temp, pull to settings
                    var colorObs = new Vector4(1, 1, 1, 0.01f);//Temp, pull to settings

                    var Up = MyAPIGateway.Session.Camera.WorldMatrix.Up;


                    if (projInbound.Item1)
                    {
                        MyAPIGateway.Utilities.ShowNotification(projInbound.Item2 + " Fast Movers Inbound", 14, "Red");
                        //TODO configurable text?  audio alert?
                    }

                    foreach (var obs in obsListCleaned)
                    {
                        var position = obs.PositionComp.WorldAABB.Center;
                        if (s.enableSymbols && Session.Camera.IsInFrustum(obs.PositionComp.WorldAABB))
                        {
                            var obsSize = obs.PositionComp.LocalVolume.Radius;
                            var voxel = obs as MyVoxelBase;
                            if (voxel != null)
                            {
                                obsSize *= 0.25f; //Since 'roid LocalVolumes can be massive.  Unsure if there's a more accurate source of size or center point of actual voxel material                                
                            }
                            DrawBoxCorners(obsSize, position, MyStringId.GetOrCompute("particle_laser"), colorObs);
                        }
                        if (s.enableLines)
                        {
                            DrawLine(position, particle, colorObs);
                        }

                        if (controlledGrid.LinearVelocity.LengthSquared() > 0.1) //TODO take a look at dampening these messages or intermittently flash them?
                        {
                            var shipDir = Vector3D.Normalize(controlledGrid.LinearVelocity);
                            var shipDirRay = new RayD(controlledGrid.PositionComp.WorldAABB.Center, shipDir);
                            if (shipDirRay.Intersects(obs.PositionComp.WorldAABB) <= controlledGrid.LinearVelocity.Length() * 30 || obs.PositionComp.WorldAABB.Contains(controlledGrid.PositionComp.WorldAABB) != ContainmentType.Disjoint)
                                MyAPIGateway.Utilities.ShowNotification("!! Collision Warning !!", 14, "Red");
                        }


                    }

                    foreach (var targ in threatListCleaned)
                    {
                        var parent = targ.GetTopMostParent();
                        var parentGrid = parent as MyCubeGrid;
                        var position = parent.PositionComp.WorldAABB.Center;
                        var offscreen = false;

                        if (!Session.Camera.IsInFrustum(parent.PositionComp.WorldAABB))//use screencoords?
                        {
                            //TODO mess with the particle for this 
                            //TODO Sort out clamping and value treatment when it's way above 1 or -1
                            //TODO resolve issues if camera is between playerpos and target
                            var screenCoords = Session.Camera.WorldToScreen(ref position);
                            if (screenCoords.Z > 1)//Camera is between playerpos and target?
                            {

                            }


                            var edgeX = (float)(Session.Camera.ViewportSize.X * 0.5 + (MathHelper.Clamp(screenCoords.X, -0.97, 0.97) * Session.Camera.ViewportSize.X * 0.5));
                            var edgeY = (float)(Session.Camera.ViewportSize.Y * 0.5 - (MathHelper.Clamp(screenCoords.Y, -0.97, 0.97) * Session.Camera.ViewportSize.Y * 0.5));
                            var edgeDrawLine = Session.Camera.WorldLineFromScreen(new Vector2(edgeX, edgeY));
                            var dirToPlayer = Vector3D.Normalize(edgeDrawLine.From - playerPos);
                            //MyAPIGateway.Utilities.ShowNotification("Screen coords: " + screenCoords, 120, "Red");

                            MySimpleObjectDraw.DrawLine(edgeDrawLine.From, edgeDrawLine.From + dirToPlayer, MyStringId.GetOrCompute("square"), ref colorEnemy, 0.0005f, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
                            offscreen = true;
                        }


                        if (s.enableLines)
                        {
                            DrawLine(position, particle, colorEnemy);
                        }

                        if (s.enableSymbols && !offscreen)
                        {
                            DrawBoxCorners(parent.PositionComp.LocalVolume.Radius, position, MyStringId.GetOrCompute("particle_laser"), colorEnemy);                           
                        
                            if(hudAPI.Heartbeat)
                            {

                                IMyFaction faction = null;
                                if (parentGrid!=null && parentGrid.BigOwners != null && parentGrid.BigOwners.Count > 0)
                                {
                                    faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(parentGrid.BigOwners[0]);
                                }
                                var topRightPos = position + Session.Camera.WorldMatrix.Up * parent.PositionComp.LocalVolume.Radius + Session.Camera.WorldMatrix.Right * parent.PositionComp.LocalVolume.Radius ;
                                var screenCoords = Session.Camera.WorldToScreen(ref topRightPos);
                                var info = new StringBuilder();
                               
                                info.AppendLine($"  <color=Red>{(faction != null ? faction.Tag + "-" : "")}{parent.DisplayName}");
                                info.AppendLine($"  {(int)Vector3D.Distance(position, controlledGrid.PositionComp.WorldAABB.Center)} m");
                                if(parentGrid != null) info.AppendLine($"  {parentGrid.LinearVelocity.Length()} m/s");
                                var labelposition = new Vector2D(screenCoords.X, screenCoords.Y);
                                var label = new HudAPIv2.HUDMessage(info, labelposition, null, 2, 1, true, true, Color.Black);
                                label.Visible = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                controlledGrid = null;
                MyLog.Default.Error($"[WC Radar] Error while trying to draw {e}");
            }
        }

        private void DrawLine(Vector3D position, MyStringId texture, Vector4 color)
        {

            var lineLength = 50 + controlledGrid.PositionComp.LocalVolume.Radius;
            var lineOffset = controlledGrid.PositionComp.LocalVolume.Radius * 0.5;
            var distToTarg = Vector3D.Distance(controlledGrid.PositionComp.WorldAABB.Center, position);

            if (distToTarg < lineLength + lineOffset)
                lineLength = (float)(distToTarg - lineOffset);
            if (lineLength <= 0)
            {
                return;
            }
            var dirToTarg = Vector3D.Normalize(position - controlledGrid.PositionComp.WorldAABB.Center);
            var offsetStart = controlledGrid.PositionComp.WorldAABB.Center + dirToTarg * lineOffset;
            MySimpleObjectDraw.DrawLine(offsetStart, offsetStart + dirToTarg * lineLength, texture, ref color, 1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

        }

        private void DrawBoxCorners(float size, Vector3D position, MyStringId texture, Vector4 color)
        {
            var rangeScaledSize = (float)Vector3D.Distance(Session.Camera.Position, position) / 300;
            var camMat = MyAPIGateway.Session.Camera.WorldMatrix;
            var symLen = 6 * rangeScaledSize;
            //Corners
            var targTopLeft = position + camMat.Up * size + camMat.Left * size;
            var targTopRight = position + camMat.Up * size + camMat.Right * size;
            var targBotLeft = position + camMat.Down * size + camMat.Left * size;
            var targBotRight = position + camMat.Down * size + camMat.Right * size;

            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Right * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Down * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);

            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Left * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Down * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);

            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Right * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Up * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);

            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Left * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Up * symLen, texture, ref color, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.PostPP);
        }


        protected override void UnloadData()
        {
            if (client)
            {
                controlledGrid = null;
                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
                Save(Settings.Instance);
                if (wcAPi != null)
                    wcAPi.Unload();
                if (hudAPI != null)
                    hudAPI.Unload();
                threatList.Clear();
                obsList.Clear();
                threatListCleaned.Clear();
                threatListChecked.Clear();
                obsListCleaned.Clear();
                obsListChecked.Clear();
                tempList.Clear();
            }
        }
    }

}

