using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Utils;
using System;
using VRage.Game.Entity;
using Draygo.API;
using System.Text;
using Sandbox.ModAPI;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        
        private void ProcessDraws()
        {
            var viewProjectionMat = Session.Camera.ViewMatrix * Session.Camera.ProjectionMatrix;
            var camMat = Session.Camera.WorldMatrix;
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
                    /*
                    var playerPos = controlledGrid.PositionComp.WorldAABB.Center;
                    var Up = Session.Camera.WorldMatrix.Up;
                    */
                    var lineScale = (float)(0.1 * Math.Tan(Session.Camera.FovWithZoom * 0.5));
                    var fovScale = Session.Camera.FieldOfViewAngle / 70;



                    #region Combined
                    for (var i = 1; i < 3; i++)
                    {
                        var checkList = i == 1 ? obsListCleaned : threatListCleaned;
                        bool earlyExit = false;
                        //Early exit if neither obstructions or asteroids are enabled
                        if(i == 1 && !(s.enableObstructions || s.enableAsteroids || s.enableLinesObs || s.enableLabelsObs || s.enableSymbolsObs))
                            earlyExit = true;
                        if(i == 2 && !(s.enableLinesThreat || s.enableLabelsThreat || s.enableSymbolsThreat))
                            earlyExit = true;
                        if (earlyExit) 
                            continue;
                        //Contextual vars depending on which list is being looked at
                        bool drawOffScreen = i == 1 ? s.enableObstructionOffScreen : s.enableThreatOffScreen;
                        bool drawLabel = i == 1 ? s.enableLabelsObs : s.enableLabelsThreat;
                        bool drawSymbol = i == 1 ? s.enableSymbolsObs : s.enableSymbolsThreat;
                        bool drawLine = i == 1 ? s.enableLinesObs : s.enableLinesThreat;
                        bool showFaction = i == 1 ? false : s.showFactionThreat;
                        bool showCollisionWarning = i == 1 ? s.enableCollisionWarning : false;

                        //Iterate contacts
                        foreach (var contact in checkList)
                        {
                            if (contact.entity.MarkedForClose || contact.entity.Closed || contact.blockCount < s.hideLabelBlockThreshold || contact.noPower && s.hideUnpowered) 
                                continue;
                            var position = contact.entity.PositionComp.WorldAABB.Center;
                            var objSize = contact.entity.PositionComp.LocalVolume.Radius;
                            var voxel = contact.entity as MyVoxelBase;
                            if (voxel != null)
                                objSize *= 0.5f; //Since 'roid LocalVolumes can be massive.  Unsure if there's a more accurate source of size or center point of actual voxel material                                
                            else
                                objSize *= 1.1f;
                            var grid = contact.entity as MyCubeGrid;
                            var rwr = rwrDict.ContainsKey(contact.entity);
                            var parent = contact.entity.GetTopMostParent();
                            var parentGrid = parent as MyCubeGrid;
                            var focusSymbol = i == 2 ? parent == focusTarget : false;

                            var screenCoords = Vector3D.Transform(position, viewProjectionMat);
                            var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                            var rgbColor = i == 1 ? contact.friendly ? s.friendlyColor : s.obsColor : rwr ? s.rwrColor : focusSymbol ? s.focusColor : contact.enemy ? s.enemyColor : s.neutralColor;
                            var drawColor = rgbColor.ToVector4();            

                            if (offscreen && drawOffScreen) 
                                DrawScreenEdge(screenCoords, drawColor);

                            if (offscreen) 
                                continue;
                            
                            //Basic corner
                            var topRightScreen = Vector3D.Transform(position + camMat.Up * objSize + camMat.Right * objSize, viewProjectionMat);
                            var topRightDraw = new Vector2D(topRightScreen.X, topRightScreen.Y);
                            var offsetX = topRightScreen.X - screenCoords.X;
                            bool minimalSymbol = false;
                            if (offsetX < symbolWidth * 0.55f)
                            {
                                topRightDraw = new Vector2D(screenCoords.X + symbolWidth * 0.5, screenCoords.Y + symbolWidth);
                                minimalSymbol = true;
                            }
                            var topRightText = topRightDraw;

                            //Conformal corners
                            Vector2D topLeftDraw = Vector2D.Zero;
                            Vector2D botRightDraw = Vector2D.Zero;
                            Vector2D botLeftDraw = Vector2D.Zero;
                            var symHalfX = symbolWidth * 0.25f;
                            var symHalfY = symbolHeight * 0.25f;
                            if (grid != null && !minimalSymbol && !Settings.Instance.disableConformal)
                            {
                                var obb = new MyOrientedBoundingBoxD(grid.PositionComp.LocalAABB, grid.PositionComp.WorldMatrixRef);
                                obb.GetCorners(corners, 0);
                                var screenBounds = BoundingBox2D.CreateInvalid();
                                foreach (var corner in corners)
                                {
                                    var cornerScreenCoords = Vector3D.Transform(corner, viewProjectionMat);
                                    var cornerScreenVector2 = new Vector2D(cornerScreenCoords.X, cornerScreenCoords.Y);
                                    screenBounds.Include(ref cornerScreenVector2);
                                }
                                topRightDraw = new Vector2D(screenBounds.Center.X + screenBounds.HalfExtents.X, screenBounds.Center.Y + screenBounds.HalfExtents.Y);
                                topLeftDraw = new Vector2D(screenBounds.Center.X - screenBounds.HalfExtents.X, screenBounds.Center.Y + screenBounds.HalfExtents.Y);
                                botRightDraw = new Vector2D(screenBounds.Center.X + screenBounds.HalfExtents.X, screenBounds.Center.Y - screenBounds.HalfExtents.Y);
                                botLeftDraw = new Vector2D(screenBounds.Center.X - screenBounds.HalfExtents.X, screenBounds.Center.Y - screenBounds.HalfExtents.Y);
                                topRightText = new Vector2D(topRightDraw.X + symHalfX, topRightDraw.Y + symHalfY);
                            }
                            else if (!minimalSymbol)
                            {
                                var offsetY = topRightDraw.Y - screenCoords.Y;
                                topRightDraw = new Vector2D(topRightDraw.X - symHalfX, topRightDraw.Y - symHalfY);
                                topLeftDraw = new Vector2D(screenCoords.X - offsetX + symHalfX, screenCoords.Y + offsetY - symHalfY);
                                botRightDraw = new Vector2D(screenCoords.X + offsetX - symHalfX, screenCoords.Y - offsetY + symHalfY);
                                botLeftDraw = new Vector2D(screenCoords.X - offsetX + symHalfX, screenCoords.Y - offsetY + symHalfY);
                            }

                            //Targ movement vector line
                            if (i == 2 && s.showThreatVectors && parentGrid != null && parentGrid.Physics != null)
                            {
                                var culledStartDotPos = new Vector2D(screenCoords.X * lineScale * aspectRatio, screenCoords.Y * lineScale);
                                var lineStartWorldPos = Vector3D.Transform(new Vector3D(culledStartDotPos.X, culledStartDotPos.Y, -0.1), camMat);
                                var screenCoordsEnd = Vector3D.Transform(position + parentGrid.Physics.LinearVelocity, viewProjectionMat);
                                MyTransparentGeometry.AddLineBillboard(screenCoordsEnd.Z > screenCoords.Z ? dash : corner, s.enemyColor, lineStartWorldPos, Vector3D.Normalize(parentGrid.Physics.LinearVelocity), 0.01f * fovScale, 0.00025f * fovScale);
                            }

                            if (drawLabel)
                            {
                                var distance = Vector3D.Distance(position, controlledGrid.PositionComp.WorldAABB.Center);
                                var info = new StringBuilder($"<color={rgbColor.R}, {rgbColor.G}, {rgbColor.B}>");
                                if (showFaction) info.AppendLine($"  {contact.factionTag}");
                                if (!Settings.Instance.hideName && parent.DisplayName != null) info.AppendLine($"  {parent.DisplayName}");
                                if (contact.noPower) info.AppendLine($"  No Pwr");
                                info.AppendLine($"  {(distance > 1000 ? (distance / 1000).ToString("0.0") + " km" : (int)distance + " m")}");
                                if (parentGrid != null)
                                {
                                    if (Settings.Instance.speedRel)
                                        info.AppendLine($"  ^{(int)(controlledGrid.LinearVelocity - parentGrid.LinearVelocity).Length()} m/s");
                                    else
                                        info.AppendLine($"  {(int)parentGrid.LinearVelocity.Length()} m/s");
                                }
                                var label = new HudAPIv2.HUDMessage(info, topRightText, null, 2, Settings.Instance.labelTextSize, true, true);
                                label.Visible = true;
                            }

                            if (drawSymbol)
                            {
                                if (minimalSymbol)
                                {
                                    var ctrSymbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, new Vector2D(screenCoords.X, screenCoords.Y), drawColor, Width: symbolWidth, Height: symbolHeight, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
                                }
                                else
                                {
                                    var topLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topLeftDraw, drawColor, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
                                    var topRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topRightDraw, drawColor, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 1.5708f, HideHud: true, Shadowing: true);
                                    var botRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botRightDraw, drawColor, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 3.14159f, HideHud: true, Shadowing: true);
                                    var botLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botLeftDraw, drawColor, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: -1.5708f, HideHud: true, Shadowing: true);
                                }
                                if (focusSymbol)
                                {
                                    var angle = (tick % 100) * .015708f;
                                    var focusSymbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, new Vector2D(screenCoords.X, screenCoords.Y), drawColor, Width: symbolWidth, Height: symbolHeight, TimeToLive: 2, Rotation: angle, HideHud: true, Shadowing: true);
                                }
                            }

                            if (drawLine)
                                DrawLine(position, line, drawColor);

                            if (showCollisionWarning && (voxel != null || grid != null) && controlledGrid.LinearVelocity.LengthSquared() > 10) //TODO take a look at dampening these messages or intermittently flash them?
                            {
                                var shipDirRay = new RayD(controlledGrid.PositionComp.WorldAABB.Center, Vector3D.Normalize(controlledGrid.LinearVelocity));
                                if (shipDirRay.Intersects(contact.entity.PositionComp.WorldAABB) <= controlledGrid.LinearVelocity.Length() * 30 || contact.entity.PositionComp.WorldAABB.Contains(controlledGrid.PositionComp.WorldAABB) != ContainmentType.Disjoint)
                                    MyAPIGateway.Utilities.ShowNotification("!! Collision Warning !!", 14, "Red");
                            }
                        }
                    }
                    #endregion
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
                return;
            var dirToTarg = Vector3D.Normalize(position - controlledGrid.PositionComp.WorldAABB.Center);
            var offsetStart = controlledGrid.PositionComp.WorldAABB.Center + dirToTarg * lineOffset;
            MySimpleObjectDraw.DrawLine(offsetStart, offsetStart + dirToTarg * lineLength, texture, ref color, 1, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop);
        }
        private void DrawScreenEdge(Vector3D screenCoords, Vector4 color)
        {
            if (screenCoords.Z > 1)//Camera is between player and target
                screenCoords *= -1;
            var screenEdgeX = 0f;
            var screenEdgeY = 0f;
            if (Math.Abs(screenCoords.X) > Math.Abs(screenCoords.Y))
            {
                if (screenCoords.X < 0)//left edge
                {
                    screenEdgeX = -1;
                    screenEdgeY = (float)(screenCoords.Y / -screenCoords.X);
                }
                else//right edge
                {
                    screenEdgeX = 1;
                    screenEdgeY = (float)(screenCoords.Y / screenCoords.X);
                }
            }
            else
            {
                if (screenCoords.Y < 0)//bottom edge
                {
                    screenEdgeY = -1;
                    screenEdgeX = (float)(screenCoords.X / -screenCoords.Y);
                }
                else//top edge
                {
                    screenEdgeY = 1;
                    screenEdgeX = (float)(screenCoords.X / screenCoords.Y);
                }
            }
            //var screenEdge
            var rotation = (float)Math.Atan2(screenEdgeX, screenEdgeY);
            var symbolObj = new HudAPIv2.BillBoardHUDMessage(line, new Vector2D(screenEdgeX, screenEdgeY), color, Width: Settings.Instance.OffScreenIndicatorThick, Height: Settings.Instance.OffScreenIndicatorLen, TimeToLive: 2, Rotation: rotation);
        }
    }
}

