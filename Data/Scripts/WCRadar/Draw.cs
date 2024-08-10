using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Utils;
using System;
using VRage.Game.Entity;
using Draygo.API;
using System.Text;
using Sandbox.Game;

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
                    var playerPos = controlledGrid.PositionComp.WorldAABB.Center;
                    var Up = MyAPIGateway.Session.Camera.WorldMatrix.Up;

                    #region Obstructions
                    if (s.enableObstructions || s.enableAsteroids)
                    {
                        foreach (var obs in obsListCleaned)
                        {
                            try
                            {
                                if (obs.entity.MarkedForClose || obs.entity.Closed || obs.blockCount < s.hideLabelBlockThreshold) continue;
                                var position = obs.entity.PositionComp.WorldAABB.Center;
                                var obsSize = obs.entity.PositionComp.LocalVolume.Radius;
                                var voxel = obs.entity as MyVoxelBase;
                                if (voxel != null)
                                    obsSize *= 0.5f; //Since 'roid LocalVolumes can be massive.  Unsure if there's a more accurate source of size or center point of actual voxel material                                
                                else
                                    obsSize *= 1.1f;

                                var screenCoords = Vector3D.Transform(position, viewProjectionMat);
                                var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                                var topRightScreen = Vector3D.Transform(position + camMat.Up * obsSize + camMat.Right * obsSize, viewProjectionMat);
                                var offsetX = topRightScreen.X - screenCoords.X;
                                if (offsetX < symbolWidth * 0.55f)
                                    topRightScreen = new Vector3D(screenCoords.X + symbolWidth * 0.5, screenCoords.Y + symbolWidth, screenCoords.Z);

                                if (s.enableObstructionOffScreen && offscreen && Vector3D.DistanceSquared(position, controlledGrid.PositionComp.WorldAABB.Center) >= 90000)
                                    DrawScreenEdge(screenCoords, obs.friendly ? s.friendlyColor.ToVector4() : s.obsColor.ToVector4());
                                if (s.enableSymbolsObs && !offscreen)
                                    DrawFrame(topRightScreen, screenCoords, obs.friendly ? s.friendlyColor.ToVector4() : s.obsColor.ToVector4());
                                if (s.enableLinesObs)
                                    DrawLine(position, line, obs.friendly ? s.friendlyColor.ToVector4() : s.obsColor.ToVector4());

                                if (s.enableCollisionWarning && controlledGrid.LinearVelocity.LengthSquared() > 10) //TODO take a look at dampening these messages or intermittently flash them?
                                {
                                    var shipDir = Vector3D.Normalize(controlledGrid.LinearVelocity);
                                    var shipDirRay = new RayD(controlledGrid.PositionComp.WorldAABB.Center, shipDir);
                                    if (shipDirRay.Intersects(obs.entity.PositionComp.WorldAABB) <= controlledGrid.LinearVelocity.Length() * 30 || obs.entity.PositionComp.WorldAABB.Contains(controlledGrid.PositionComp.WorldAABB) != ContainmentType.Disjoint)
                                        MyAPIGateway.Utilities.ShowNotification("!! Collision Warning !!", 14, "Red");
                                }

                                if (s.enableLabelsObs && !offscreen)
                                {
                                    var parent = obs.entity.GetTopMostParent();
                                    var parentGrid = parent as MyCubeGrid;
                                    DrawLabel(parentGrid, position, parent, obsSize, obs.friendly ? s.friendlyColor.ToVector4() : s.obsColor.ToVector4(), false, "", obs.noPower, new Vector2D(topRightScreen.X, topRightScreen.Y));
                                }
                            }
                            catch (Exception e)
                            {
                                MyLog.Default.Error($"[WC Radar] Error while trying to draw obstructions {e}");
                                continue;
                            }
                        }
                    }
                    #endregion

                    #region Threats
                    foreach (var targ in threatListCleaned)
                    {
                        try
                        {
                            if (targ.entity.MarkedForClose || targ.entity.Closed || targ.blockCount < s.hideLabelBlockThreshold) continue;
                            var parent = targ.entity.GetTopMostParent();
                            var parentGrid = parent as MyCubeGrid;
                            var position = parent.PositionComp.WorldAABB.Center;
                            var screenCoords = Vector3D.Transform(position, viewProjectionMat);
                            var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                            var targSize = parent.PositionComp.LocalVolume.Radius;
                            targSize *= 1.1f;
                            var topRightScreen = Vector3D.Transform(position + camMat.Up * targSize + camMat.Right * targSize, viewProjectionMat);
                            var offsetX = topRightScreen.X - screenCoords.X;
                            var rwr = rwrDict.ContainsKey(targ.entity);
                            if (offsetX < symbolWidth * 0.55f)
                                topRightScreen = new Vector3D(screenCoords.X + symbolWidth * 0.5, screenCoords.Y + symbolWidth, screenCoords.Z);

                            
                            //Targ movement vector line
                            /*
                            if (parentGrid.Physics != null)
                            {
                                var ScaleFov = Math.Tan(Session.Camera.FovWithZoom * 0.5);
                                var culledStartScreenPos = screenCoords;
                                var lineScale = (float)(0.1 * ScaleFov);
                                var culledStartDotPos = new Vector2D(culledStartScreenPos.X, culledStartScreenPos.Y);
                                culledStartDotPos.X *= lineScale * aspectRatio;
                                culledStartDotPos.Y *= lineScale;
                                var lineStartWorldPos = Vector3D.Transform(new Vector3D(culledStartDotPos.X, culledStartDotPos.Y, -0.1), camMat);
                                var vector = Vector3D.Normalize(parentGrid.Physics.LinearVelocity); //Need to do the same transform hoops as the screen ratio would distort directionality
                                MyTransparentGeometry.AddLineBillboard(corner, s.enemyColor, lineStartWorldPos, vector, 0.01f, 0.0005f);
                            }
                            */
                            /*
                             when there are a lot of contacts close together i feel that the vector lines will make is much messier and maybe having them 
                            automatically hide when it gets too cluttered and only show when you press control. but when there are only a few it shows the 
                            line and I know this is still a WIP but make the vector line a bit thinner, closer to the thickness of the radar box
                             */


                            if (s.enableThreatOffScreen && offscreen)
                                DrawScreenEdge(screenCoords, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableLinesThreat)
                                DrawLine(position, line, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableSymbolsThreat && !offscreen)
                                DrawFrame(topRightScreen, screenCoords, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableLabelsThreat && !offscreen)
                                DrawLabel(parentGrid, position, parent, targSize, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4(), s.showFactionThreat, targ.factionTag, targ.noPower, new Vector2D(topRightScreen.X, topRightScreen.Y));
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Error($"[WC Radar] Error while trying to draw threats {e}");
                            continue;
                        }
                    }
                    #endregion

                    #region RWR
                    if (!s.rwrDisable)
                    {
                        bool display = false;
                        var message = new StringBuilder();
                        message.Append($"<color={s.rwrColor.R},{s.rwrColor.G},{s.rwrColor.B}>");

                        if (serverSuppress) //Limited draw for RWR only
                        {
                            foreach (var contact in rwrDict)
                            {
                                if (contact.Value + s.rwrDisplayTimeTicks > tick)
                                {
                                    display = true;
                                    message.AppendLine($"Target Locked by {contact.Key.DisplayName}");
                                }
                                try
                                {
                                    if (contact.Key.MarkedForClose || contact.Key.Closed) continue;
                                    var parent = contact.Key.GetTopMostParent();
                                    var parentGrid = parent as MyCubeGrid;
                                    var position = parent.PositionComp.WorldAABB.Center;
                                    var screenCoords = Vector3D.Transform(position, viewProjectionMat);
                                    var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                                    var targSize = parent.PositionComp.LocalVolume.Radius;
                                    targSize *= 1.1f;
                                    var topRightScreen = Vector3D.Transform(position + camMat.Up * targSize + camMat.Right * targSize, viewProjectionMat);
                                    var offsetX = topRightScreen.X - screenCoords.X;
                                    if (offsetX < symbolWidth * 0.55f)
                                        topRightScreen = new Vector3D(screenCoords.X + symbolWidth * 0.5, screenCoords.Y + symbolWidth, screenCoords.Z);

                                    if (offscreen)
                                        DrawScreenEdge(screenCoords, s.rwrColor.ToVector4());
                                    if (!offscreen)
                                    {
                                        DrawFrame(topRightScreen, screenCoords, s.rwrColor.ToVector4());
                                        DrawLabel(parentGrid, position, parent, targSize, s.rwrColor.ToVector4(), false, "", false, new Vector2D(topRightScreen.X, topRightScreen.Y));
                                    }
                                }
                                catch (Exception e)
                                {
                                    MyLog.Default.Error($"[WC Radar] Error while trying to draw threats {e}");
                                    continue;
                                }
                            }
                        }
                        if (display)
                        {
                            var warning = new HudAPIv2.HUDMessage(message, new Vector2D(-0.11, -0.6), null, 2, 1.3d, true, true, Color.Black);
                            warning.Visible = true;
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
        private void DrawLabel(MyCubeGrid parentGrid, Vector3D position, MyEntity parent, float size, Color color, bool showFaction, string factionTag, bool noPower, Vector2D labelposition)
        {           
            var distance = Vector3D.Distance(position, controlledGrid.PositionComp.WorldAABB.Center);
            var info = new StringBuilder($"<color={color.R}, {color.G}, {color.B}>");
            if (showFaction && factionTag != "") info.AppendLine($"  {factionTag}");
            if (!Settings.Instance.hideName && parent.DisplayName != null) info.AppendLine($"  {parent.DisplayName}");
            if (noPower) info.AppendLine($"  No Power");
            info.AppendLine($"  {(distance > 1000 ? (distance / 1000).ToString("0.0") + " km" : (int)distance + " m")}");
            if (parentGrid != null)
            {
                if (Settings.Instance.speedRel)
                    info.AppendLine($"  ^{(int)(controlledGrid.LinearVelocity - parentGrid.LinearVelocity).Length()} m/s");
                else
                    info.AppendLine($"  {(int)parentGrid.LinearVelocity.Length()} m/s");
            }
            var label = new HudAPIv2.HUDMessage(info, labelposition, null, 2, 1, true, true);
            label.Visible = true;
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
        private void DrawFrame(Vector3D topRight, Vector3D center, Vector4 rawColor)
        {
            var color = rawColor * 0.95f;
            var offsetX = topRight.X - center.X;
            if (offsetX > symbolWidth * 0.55f)
            {
                var offsetY = topRight.Y - center.Y;
                var symHalfX = symbolWidth * 0.25f;
                var symHalfY = symbolHeight * 0.25f;
                var topRightDraw = new Vector2D(topRight.X - symHalfX, topRight.Y - symHalfY);
                var topLeftDraw = new Vector2D(center.X - offsetX + symHalfX, center.Y + offsetY - symHalfY);
                var botRightDraw = new Vector2D(center.X + offsetX - symHalfX, center.Y - offsetY + symHalfY);
                var botLeftDraw = new Vector2D(center.X - offsetX + symHalfX, center.Y - offsetY + symHalfY);

                var topLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topLeftDraw, color, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
                var topRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topRightDraw, color, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 1.5708f, HideHud: true, Shadowing: true);
                var botRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botRightDraw, color, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: 3.14159f, HideHud: true, Shadowing: true);
                var botLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botLeftDraw, color, Width: symbolWidth * 0.5f, Height: symbolHeight * 0.5f, TimeToLive: 2, Rotation: -1.5708f, HideHud: true, Shadowing: true);
            }
            else
            {
                var ctrSymbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, new Vector2D(center.X, center.Y), color, Width: symbolWidth, Height: symbolHeight, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
            }
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

