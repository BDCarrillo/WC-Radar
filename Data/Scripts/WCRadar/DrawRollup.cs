using VRageMath;
using System.Text;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Draygo.API;

namespace WCRadar
{
    public partial class Session
    {
        private void RollupData()
        {
            var viewProjectionMat = Session.Camera.ViewMatrix * Session.Camera.ProjectionMatrix;
            var camMat = Session.Camera.WorldMatrix;

            if (threatListCleaned.Count > 0)
            {
                var updateText = tick % 15 == 0;
                var s = Settings.Instance;
                var info = new StringBuilder();
                if (updateText)
                {
                    sortDict.Clear();
                    sortList.Clear();

                    foreach (var targ in threatListCleaned)
                    {
                        if (targ.entity.MarkedForClose || targ.entity.Closed || targ.blockCount < s.hideLabelBlockThreshold || s.hideUnpowered && targ.noPower || !(targ.entity is MyCubeGrid)) continue;
                        var sortVal = Vector3D.Distance(targ.entity.PositionComp.WorldAABB.Center, controlledGrid.PositionComp.WorldAABB.Center);

                        sortList.Add(sortVal);
                        sortDict.Add(sortVal, targ);
                    }
                    sortList.Sort();//Default is closest first
                    if (s.rollupSort == 1 || s.rollupSort == 3)
                        sortList.Reverse(); //Flip to furthest
                    if (sortList.Count > s.rollupMaxNum)
                        sortList.RemoveRange(s.rollupMaxNum, sortList.Count - s.rollupMaxNum);
                    if (s.rollupSort > 1)//Rearrange by ID
                    {
                        var numList = new List<string>();
                        var numDict = new Dictionary<string, double>();
                        for (var i = 0; i < sortList.Count; i++)
                        {
                            var name = sortDict[sortList[i]].entity.EntityId.ToString().Substring(0, 16);
                            numList.Add(name);
                            numDict.Add(name, sortList[i]);
                        }
                        numList.Sort();
                        sortList.Clear();
                        for (var i = 0; i < numList.Count; i++)
                            sortList.Add(numDict[numList[i]]);
                    }
                }

                for (var i = 0; i < sortList.Count; i++)
                {
                    var targ = sortDict[sortList[i]];
                    var targGrid = targ.entity as MyCubeGrid;
                    var focus = focusTarget == targGrid;
                    var name = targ.entity.EntityId.ToString().Substring(0, 4);
                    if (updateText)
                    {
                        var distance = sortList[i];
                        var distStr = distance > 1000 ? (distance / 1000).ToString("0.0") + " km" : (int)distance + " m";
                        var speedStr = "";
                        if (s.speedRel)
                            speedStr = $"^{(int)(controlledGrid.LinearVelocity - targGrid.LinearVelocity).Length()} m/s";
                        else
                            speedStr = $"{(int)targGrid.LinearVelocity.Length()} m/s";
                        var color = focus ? s.focusColor : rwrDict.ContainsKey(targ.entity) ? s.rwrColor : targ.enemy ? s.enemyColor : s.neutralColor;
                        info.AppendLine($"<color={color.R}, {color.G}, {color.B}> {(s.rollupShowNum ? name + " - " : "")}{(s.rollupShowFac && targ.factionTag != "" ? targ.factionTag + " - " : "")}{targGrid.DisplayName} - {distStr} - {speedStr}{(targ.noPower ? " - NO PWR" : "")}");
                    }

                    //Draw the corresponding number
                    if (s.rollupShowNum)
                    {
                        var color2 = focus ? s.focusColor : rwrDict.ContainsKey(targ.entity) ? s.rwrColor : targ.enemy ? s.enemyColor : s.neutralColor;
                        var position = targGrid.PositionComp.WorldAABB.Center;
                        var targSize = targGrid.PositionComp.LocalVolume.Radius;
                        targSize *= 1.1f;
                        var ctr = Vector3D.Transform(position, viewProjectionMat);
                        var offscreen = ctr.X > 1 || ctr.X < -1 || ctr.Y > 1 || ctr.Y < -1 || ctr.Z > 1;

                        if (offscreen)
                            continue;

                        var topRightScreen = Vector3D.Transform(position + camMat.Up * targSize + camMat.Right * targSize, viewProjectionMat);
                        var label = new HudAPIv2.HUDMessage(new StringBuilder($"<color={color2.R}, {color2.G}, {color2.B}>" + name), new Vector2D(ctr.X, ctr.Y), null, 2, s.rollupTextSize, true, true);
                        label.Font = "monospace";
                        var offset = label.GetTextLength();
                        var offsetX = topRightScreen.X - ctr.X;
                        if (offsetX > symbolWidth * 0.55f)
                            label.Offset = new Vector2D(offset.X * -0.5f, offset.Y * -1.01f + topRightScreen.Y - ctr.Y);
                        else
                            label.Offset = new Vector2D(offset.X * -0.5f, offset.Y * -1.01f + symbolHeight * 0.5f);
                        label.Visible = true;
                    }
                }

                if (updateText)
                    rollupText.Message = info;
                rollupText.Visible = true;
            }
            else if (Settings.Instance.rollupHideEmpty)
                rollupText.Visible = false;
            else
            {
                rollupText.Message = new StringBuilder("No Targets");
                rollupText.Visible = true;
            }
        }
    }
}