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
        private void DrawMissile()
        {
            var viewProjectionMat = Session.Camera.ViewMatrix * Session.Camera.ProjectionMatrix;
            try
            {
                var s = Settings.Instance;
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
                            float distAdjFactor = screenCoords.Z < 0.99995f ? 1 : (float)(-14000f * screenCoords.Z + 14000.3); //wtf
                            float distAdjSymWidth = symbolWidth * distAdjFactor;
                            float distAdjSymHeight = distAdjSymWidth * aspectRatio;
                            var symbolObj = new HudAPIv2.BillBoardHUDMessage(missileOutline, symbolPosition, Settings.Instance.missileColor, Width: distAdjSymWidth, Height: distAdjSymHeight, TimeToLive: 2, Rotation: 0.785398f, HideHud: true, Shadowing: true);
                        }
                        if (s.enableMissileOffScreen && offscreen)
                            DrawScreenEdge(screenCoords, s.missileColor);
                    }
                }
            }
            catch (Exception e)
            {
                controlledGrid = null;
                MyLog.Default.Error($"[WC Radar] Error while trying to draw {e}");
            }
        }
    }
}


