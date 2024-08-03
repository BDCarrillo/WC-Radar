using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using VRage.Game;
using VRage.Utils;
using System;
using Draygo.API;
using System.Text;
using VRage.Collections;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {

        private void ExpandedDraw()
        {
            var viewProjectionMat = Session.Camera.ViewMatrix * Session.Camera.ProjectionMatrix;
            var worldProjectionMat = MatrixD.Invert(viewProjectionMat);
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
                    if (s.enableMissileWarning && projInbound.Item1)
                    {
                        var message = new StringBuilder();
                        message.Append("<color=255,0,0>");
                        message.Append(projInbound.Item2 + " " + s.missileWarningText);
                        var warning = new HudAPIv2.HUDMessage(message, new Vector2D(-0.11, -0.5), null, 2, 1.3d, true, true, Color.Black);
                        warning.Visible = true;
                    }

                    if ((s.enableMissileLines || s.enableMissileSymbols) && projInbound.Item1)
                    {
                        projPosList.Clear();
                        wcAPi.GetProjectilesLockedOnPos(controlledGrid, projPosList);
                        foreach (var missile in projPosList)
                        {
                            var screenCoords = Vector3D.Transform(missile, viewProjectionMat);
                            var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                            if (s.enableMissileLines)
                                DrawLine(missile, line, s.missileColor);
                            if (s.enableMissileSymbols && !offscreen)
                            {
                                var symbolPosition = new Vector2D(screenCoords.X, screenCoords.Y);
                                var symbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, symbolPosition, Settings.Instance.missileColor, Width: symbolWidth, Height: symbolHeight, TimeToLive: 2, Rotation: 0.785398f, HideHud: true, Shadowing: true);
                            }
                            if (s.enableMissileOffScreen && offscreen)
                                DrawScreenEdge(screenCoords, s.missileColor);
                        }
                    }

                    var rollupList = new MyConcurrentList<expandedMark>();
                    var buffer = 0.035;
                    var ctrOffset = 0.25;
                   
                    #region Threats
                    foreach (var targ in threatListCleaned)
                    {
                        try
                        {
                            if (targ.entity.MarkedForClose || targ.entity.Closed) continue;
                            var parent = targ.entity.GetTopMostParent();
                            var position = parent.PositionComp.WorldAABB.Center;
                            var screenCoords = Vector3D.Transform(position, viewProjectionMat);
                            var offscreen = screenCoords.X > 1 || screenCoords.X < -1 || screenCoords.Y > 1 || screenCoords.Y < -1 || screenCoords.Z > 1;
                            var ctrscreen = screenCoords.X < ctrOffset && screenCoords.X > -ctrOffset && screenCoords.Y < ctrOffset && screenCoords.Y > -ctrOffset;
                            var topRightScreen = new Vector3D(screenCoords.X + symbolWidth * 0.5, screenCoords.Y + symbolWidth, screenCoords.Z);
                            if (s.enableThreatOffScreen && offscreen)
                                DrawScreenEdge(screenCoords, targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableLinesThreat)
                                DrawLine(position, line, targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());


                            if (ctrscreen && hudAPI.Heartbeat)
                            {
                                var labelString = (targ.factionTag.Length > 0? targ.factionTag + " - " : "") + parent.DisplayName + (targ.noPower ? " - No Power" : "");
                                var textTopLeft = new Vector2D(topRightScreen.X + 0.05, topRightScreen.Y + 1.05);
                                var temp = new expandedMark()
                                {
                                    topRight = new Vector2D(topRightScreen.X, topRightScreen.Y),
                                    screenCoordsZ = screenCoords.Z,
                                    worldCtr = position,
                                    color = targ.enemy ? s.enemyColor : s.neutralColor,
                                    label = labelString,
                                    screenCoordsCtr = new Vector2D(screenCoords.X, screenCoords.Y),
                                    textTopLeft = textTopLeft
                                };
                                rollupList.Add(temp);
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Error($"[WC Radar] Error while trying to process expanded draw threats {e}");
                            continue;
                        }
                    }
                    #endregion

                    //Sorting BS
                    var drawList = new MyConcurrentList<expandedMark>();
                    while (rollupList.Count > 0)
                    {
                        var maxY = -1d;
                        expandedMark max = null;
                        foreach(var mark in rollupList)
                        {
                            if (mark.textTopLeft.Y > maxY)
                            {
                                maxY = mark.textTopLeft.Y;
                                max = mark;
                            }
                        }
                        rollupList.Remove(max);
                        drawList.Add(max);
                    }
                    if (drawList.Count > 0)
                    {
                        var currentY = ctrOffset + 1;
                        foreach (var mark in drawList)
                        {
                            mark.textTopLeft.X = ctrOffset + 0.02;
                            mark.textTopLeft.Y = currentY;
                            currentY -= buffer;
                            DrawExpanded(mark, worldProjectionMat, playerPos);
                        }
                    }
                    //Draw highlight frame
                    var sizeMult = 0.75f;
                    var topRightDraw = new Vector2D(ctrOffset, ctrOffset);
                    var topLeftDraw = new Vector2D(-ctrOffset, ctrOffset);
                    var botRightDraw = new Vector2D(ctrOffset, -ctrOffset);
                    var botLeftDraw = new Vector2D(-ctrOffset, -ctrOffset);
                    var color = s.enemyColor;
                    var topLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topLeftDraw, color, Width: symbolWidth * sizeMult, Height: symbolHeight * sizeMult, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
                    var topRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, topRightDraw, color, Width: symbolWidth * sizeMult, Height: symbolHeight * sizeMult, TimeToLive: 2, Rotation: 1.5708f, HideHud: true, Shadowing: true);
                    var botRightSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botRightDraw, color, Width: symbolWidth * sizeMult, Height: symbolHeight * sizeMult, TimeToLive: 2, Rotation: 3.14159f, HideHud: true, Shadowing: true);
                    var botLeftSymbolObj = new HudAPIv2.BillBoardHUDMessage(frameCorner, botLeftDraw, color, Width: symbolWidth * sizeMult, Height: symbolHeight * sizeMult, TimeToLive: 2, Rotation: -1.5708f, HideHud: true, Shadowing: true);
                    
                }
            }
            catch (Exception e)
            {
                controlledGrid = null;
                MyLog.Default.Error($"[WC Radar] Error while trying to draw {e}");
            }
        }
        private void DrawExpanded(expandedMark mark, MatrixD worldProjectionMat, Vector3D playerPos)
        {
            //Take 1: Preferable for clarity- need to sort out rotation
            /*
            var distToText = (float)Vector2D.Distance(mark.topRight, mark.textTopLeft);
            var lineCtr = (mark.topRight + mark.textTopLeft) / 2;
            var rotation = (float)Math.Atan2(mark.textTopLeft.X, mark.textTopLeft.Y);           
            var ctrMark = new HudAPIv2.BillBoardHUDMessage(corner, lineCtr, mark.color, Width: 0.003f, Height: distToText, TimeToLive: 2, Rotation: rotation, HideHud: true, Shadowing: true);
            */

            //Take 2: Don't like that this one can get swamped by other billboards/effects or obscured by your grid
            var distance = Vector3D.Distance(mark.worldCtr, playerPos);
            var thickness = (float)distance * 0.002f * Session.Camera.FieldOfViewAngle / 70;
            var start = mark.worldCtr;// Vector3D.Transform(new Vector3D(mark.topRight.X, mark.topRight.Y, mark.screenCoordsZ), worldProjectionMat);
            var end = Vector3D.Transform(new Vector3D(mark.textTopLeft.X, mark.textTopLeft.Y-1, mark.screenCoordsZ), worldProjectionMat);
            var dir = end - start;
            var length = (float)dir.Normalize();

            var color = mark.color.ToVector4();
            //MySimpleObjectDraw.DrawLine(start, end, line, ref color, thickness);
            MyTransparentGeometry.AddLineBillboard(line, mark.color, start, dir, length, thickness, VRageRender.MyBillboard.BlendTypeEnum.AdditiveTop, intensity: 5);
            

            var info = new StringBuilder($"<color={mark.color.R}, {mark.color.G}, {mark.color.B}>");
            info.Append(mark.label);
            info.Append($" - {(distance > 1000 ? (distance / 1000).ToString("0.0") + " km" : (int)distance + " m")}");
            var textLocation = mark.textTopLeft;
            textLocation.Y -= 0.985;
            //var shadowOffset = new Vector2D(0.002 / aspectRatio, -0.002 / aspectRatio);
            //var shadowLabel = new HudAPIv2.HUDMessage(info, textLocation, shadowOffset, 2, 1, true);
            //shadowLabel.InitialColor = Color.Black;
            //shadowLabel.Visible = false;
            //var info2 = new StringBuilder($"<color={mark.color.R}, {mark.color.G}, {mark.color.B}>" + info);
            var label = new HudAPIv2.HUDMessage(info, textLocation, null, 2, 1, true, true);
            label.Visible = true;



            //Mini frame draw
            var ctrSymbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, mark.screenCoordsCtr, mark.color, Width: symbolWidth, Height: symbolHeight, TimeToLive: 2, Rotation: 0, HideHud: true, Shadowing: true);
        }
    }
}


