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

namespace WCRadar
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session : MySessionComponentBase
    {
        WcApi wcAPi;

        internal bool client;
        internal int tick = -300;
        internal MyStringId particle = MyStringId.GetOrCompute("particle_laser"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow

        internal ICollection<MyTuple<MyEntity, float>> threatList = new List<MyTuple<MyEntity, float>>();
        internal ICollection<MyEntity> obsList = new List<MyEntity>();
        internal MyTuple<bool, int, int> projInbound = new MyTuple<bool, int, int>();


        internal MyCubeGrid controlledGrid;
        internal Vector3 gravDir;

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
                MyAPIGateway.Utilities.ShowMessage("WC Radar", "cycle overlays by entering /radar symbols or /radar lines in chat");
                InitConfig();
                wcAPi = new WcApi();
                wcAPi.Load();
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
                    
                    wcAPi.GetSortedThreats(entity, threatList);
                    wcAPi.GetObstructions(entity, obsList);
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
            if (client && (Settings.Instance.enableLines || Settings.Instance.enableSymbols))
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
                    var playerPos = Session.Player.GetPosition();
                    var colorEnemy = s.colorRev; //temp
                    var Up = MyAPIGateway.Session.Camera.WorldMatrix.Up;


                    if (projInbound.Item1)
                    {
                        MyAPIGateway.Utilities.ShowNotification(projInbound.Item2 + " Fast Movers Inbound", 480, "Red");
                        //TODO configurable text?  audio alert?
                    }

                    //TODO Obstructions: Check dot of dirToward for a collision alert?
                    foreach (var obs in obsList)
                    {
                        var colorObs = new Vector4(1, 1, 1, 0.01f);//Temp, pull to settings
                        var position = obs.PositionComp.WorldAABB.Center;

                        if (s.enableSymbols && Session.Camera.IsInFrustum(obs.PositionComp.WorldAABB))
                        {
                            var size = obs.PositionComp.LocalVolume.Radius;
                            var rangeScaledSize = (float)Vector3D.Distance(Session.Camera.Position, position) / 300;
                            var camMat = MyAPIGateway.Session.Camera.WorldMatrix;
                            var symLen = 6 * rangeScaledSize;
                            var texture = MyStringId.GetOrCompute("particle_laser");
                            //Corners
                            var targTopLeft = position + camMat.Up * size + camMat.Left * size;
                            var targTopRight = position + camMat.Up * size + camMat.Right * size;
                            var targBotLeft = position + camMat.Down * size + camMat.Left * size;
                            var targBotRight = position + camMat.Down * size + camMat.Right * size;

                            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Right * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Down * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Left * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Down * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Right * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Up * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Left * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Up * symLen, texture, ref colorObs, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                        }
                        if (s.enableLines)
                        {
                            //Line to target from player pos
                            var dirToTarg = Vector3D.Normalize(position - playerPos);
                            var lineLength = 100 + controlledGrid.PositionComp.LocalVolume.Radius;
                            var lineOffset = controlledGrid.PositionComp.LocalVolume.Radius * 1.1;
                            MySimpleObjectDraw.DrawLine(playerPos + dirToTarg * lineOffset, playerPos + dirToTarg * lineLength, particle, ref colorObs, 1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                        }

                       // if(controlledGrid.PositionComp.WorldAABB.Center + controlledGrid.LinearVelocity)
                       // MyAPIGateway.Utilities.ShowNotification("Collision Warning", 480, "Gray");


                    }

                    foreach (var targ in threatList)
                    {
                        if (targ.Item1.MarkedForClose) continue;
                        var parent = targ.Item1.GetTopMostParent();
                        if (parent.MarkedForClose || parent == null) continue;
                        var position = parent.PositionComp.WorldAABB.Center;
                        var offscreen = false;

                        if (!Session.Camera.IsInFrustum(parent.PositionComp.WorldAABB))
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

                            MySimpleObjectDraw.DrawLine(edgeDrawLine.From, edgeDrawLine.From + dirToPlayer, MyStringId.GetOrCompute("square"), ref colorEnemy, 0.0005f, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            offscreen = true;
                        }


                        if (s.enableLines)
                        {
                            //Line to target from player pos
                            var dirToTarg = Vector3D.Normalize(position - playerPos);
                            var lineLength = 100 + controlledGrid.PositionComp.LocalVolume.Radius;
                            var lineOffset = controlledGrid.PositionComp.LocalVolume.Radius * 1.1;
                            MySimpleObjectDraw.DrawLine(playerPos + dirToTarg * lineOffset, playerPos + dirToTarg * lineLength, particle, ref colorEnemy, 1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                        }

                        if (s.enableSymbols && !offscreen)
                        {
                            var size = parent.PositionComp.LocalVolume.Radius;
                            var rangeScaledSize = (float)Vector3D.Distance(Session.Camera.Position, position) / 300;
                            var camMat = MyAPIGateway.Session.Camera.WorldMatrix;
                            var symLen = 6 * rangeScaledSize;
                            var texture = MyStringId.GetOrCompute("particle_laser");
                            //Corners
                            var targTopLeft = position + camMat.Up * size + camMat.Left * size;
                            var targTopRight = position + camMat.Up * size + camMat.Right * size;
                            var targBotLeft = position + camMat.Down * size + camMat.Left * size;
                            var targBotRight = position + camMat.Down * size + camMat.Right * size;

                            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Right * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targTopLeft, targTopLeft + camMat.Down * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Left * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targTopRight, targTopRight + camMat.Down * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Right * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targBotLeft, targBotLeft + camMat.Up * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);

                            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Left * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                            MySimpleObjectDraw.DrawLine(targBotRight, targBotRight + camMat.Up * symLen, texture, ref colorEnemy, rangeScaledSize, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
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
        protected override void UnloadData()
        {
            if (client)
            {
                controlledGrid = null;
                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
                Save(Settings.Instance);
                if (wcAPi != null)
                {
                    wcAPi.Unload();
                }
            }
        }
    }

}

