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
                MyAPIGateway.Utilities.ShowMessage("WC Radarr", "To cycle displays- \n /radar symbols \n/radar lines");
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


                    foreach (var targ in threatList)
                    {
                        var parent = targ.Item1.GetTopMostParent();                     
                        var position = parent.PositionComp.WorldAABB.Center;

                        Session.Camera.IsInFrustum(parent.PositionComp.WorldAABB);

                        var distToTarg = (int)Vector3D.Distance(position, playerPos);
                        var dirToTarg = Vector3D.Normalize(position - playerPos);
                        var size = parent.PositionComp.LocalVolume.Radius;
                        size += 10;
                        var name = parent.DisplayName;

                        #region Direction Lines
                        //Line to target from player pos
                        var lineLength = 100 + controlledGrid.PositionComp.LocalVolume.Radius;
                        var lineOffset = controlledGrid.PositionComp.LocalVolume.Radius;                       
                        MySimpleObjectDraw.DrawLine(playerPos + dirToTarg * lineOffset, playerPos + dirToTarg * lineLength, particle, ref colorEnemy, 1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                        #endregion



                        #region Symbol Drawing
                        //transparentMaterial symbol drawn on target
                        //Explore using left or right instead of up to correct for 90* rotation of symbol?
                        //This approach is likely to have issues at extreme range, as the symbol is drawn on the target itself and may be hard to see at distance
                        var targCtr = playerPos + dirToTarg * distToTarg;
                        var targTop = targCtr + Up * size;
                        MySimpleObjectDraw.DrawLine(targTop, targTop - Up * size * 2, MyStringId.GetOrCompute("CrosshairWCRad"), ref colorEnemy, size, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
                        #endregion



                        #region Transparent Box Drawing
                        //Outlines will need to be scaled based on distance to camera
                        var matrix = parent.WorldMatrix;
                        var camerapos = Session.Camera.Position;


                        //smushes boxes to align to current camera pos, has some popping effect for close stuff
                        var dirFromCam = Vector3D.Normalize(position - camerapos); 
                        //var dirFromCam = Vector3D.Normalize(position - playerPos); //Aligns well when in cockpit
                        matrix.SetDirectionVector(matrix.GetClosestDirection(dirFromCam), dirFromCam);//Squares up the matrices to align to camera view direction
                        var offset = new Vector3D(5, 5, 5);
                        BoundingBoxD box = new BoundingBoxD(parent.PositionComp.LocalAABB.Min - offset, parent.PositionComp.LocalAABB.Max + offset);
                        var color = Color.Red;
                        MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, MyStringId.GetOrCompute("GizmoDrawLineRed"), false, -1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop, 5);
                        #endregion
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

