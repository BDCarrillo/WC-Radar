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
using Draygo.API;
using System.Text;
using Sandbox.Game.EntityComponents;
using Digi.Example_NetworkProtobuf;
using Sandbox.Game;

namespace WCRadar
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session : MySessionComponentBase
    {
        WcApi wcAPi;
        HudAPIv2 hudAPI;
        internal bool hide = false;
        internal bool client;
        internal static bool isHost = false;
        internal bool menuInit = false;
        internal int tick = -299;
        public static bool serverEnforcement = false;
        public static bool serverRWREnforcement = false;

        internal bool serverSuppress = false;
        internal bool serverSuppressRWR = false;

        internal bool registeredController = false;
        internal MyCubeBlock trackedBlock = null;
        internal MyCubeBlock trackedRWRBlock = null;

        internal MyStringId corner = MyStringId.GetOrCompute("SharpEdge"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId line = MyStringId.GetOrCompute("particle_laser"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId missileOutline = MyStringId.GetOrCompute("MissileOutline"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId frameCorner = MyStringId.GetOrCompute("FrameCorner"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow

        internal Dictionary<MyEntity, int> rwrDict = new Dictionary<MyEntity, int>();
        internal ICollection<MyTuple<MyEntity, float>> threatList = new List<MyTuple<MyEntity, float>>();
        internal ICollection<ContactInfo> threatListCleaned = new List<ContactInfo>();
        internal ICollection<MyEntity> threatListEnt = new List<MyEntity>();
        internal ICollection<MyEntity> obsList = new List<MyEntity>();
        internal ICollection<ContactInfo> obsListCleaned = new List<ContactInfo>();
        internal ICollection<Vector3D> projPosList = new List<Vector3D>();
        internal MyTuple<bool, int, int> projInbound = new MyTuple<bool, int, int>();
        internal MyCubeGrid controlledGrid;
        internal readonly MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;
        public static Networking Networking = new Networking(0632);
        internal float symbolHeight = 0f;//Leave this as zero, monitor aspect ratio is figured in later
        internal float aspectRatio = 0f;//Leave this as zero, monitor aspect ratio is figured in later
        internal float symbolWidth = 0.03f;



        public override void BeforeStart()
        {
            var IsServer = MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Session.IsServer;
            var MpActive = MyAPIGateway.Multiplayer.MultiplayerActive;
            var DedicatedServer = MyAPIGateway.Utilities.IsDedicated;
            var IsClient = !IsServer && !DedicatedServer && MpActive;
            isHost = IsServer && !DedicatedServer && MpActive || !MpActive;
            client = isHost || IsClient || !MpActive;
            InitConfig();
            Networking.Register();
            MyLog.Default.WriteLineAndConsole($"WC Radar: Initializing");

            if (client)
            {
                MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEnteredSender;

                try
                {
                    MyAPIGateway.Session.Player.Controller.ControlledEntityChanged += GridChange;
                    registeredController = true;
                    MyLog.Default.WriteLineAndConsole($"WC Radar: Registered ControlledEntityChanged in BeforeStart");

                }
                catch
                { }               
                
                wcAPi = new WcApi();
                wcAPi.Load();
                hudAPI = new HudAPIv2(InitMenu);
                if (wcAPi.IsReady)
                    if(serverEnforcement || serverRWREnforcement)
                        MyAPIGateway.Utilities.ShowMessage("WC Radar", "Overlay requires a certain type of block set by the server.  Overlay options can be found by hitting enter and F2");
                    else
                        MyAPIGateway.Utilities.ShowMessage("WC Radar", "overlay options can be found by hitting enter and F2");
                else
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "no WeaponCore, no radar :/");
                if (hudAPI == null)
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "TextHudAPI failed to register");
            }
            else if(DedicatedServer || isHost)
            {
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected; 
                MyLog.Default.WriteLineAndConsole($"WC Radar: Registered server events");
            }
        }
        private void GridChange(VRage.Game.ModAPI.Interfaces.IMyControllableEntity previousEnt, VRage.Game.ModAPI.Interfaces.IMyControllableEntity newEnt)
        {
            var newGrid = newEnt?.Entity?.GetTopMostParent() as MyCubeGrid;
            //controlledGrid = newGrid;
            threatList.Clear();
            obsList.Clear();
            threatListCleaned.Clear();
            obsListCleaned.Clear();
            threatListEnt.Clear();
            projInbound.Item1 = false;
            trackedBlock = null;
            trackedRWRBlock = null;
        }
        private void OnMessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            messageText.ToLower();

            switch(messageText.ToLower())
            {
                case "/radar":
                    MyAPIGateway.Utilities.ShowMessage("WC Radar", "Options can be found in the Mod Settings Menu.  Press F2 with a chat line open and it should appear in the top left of your screen.  '/radar reset' to restore defaults or '/radar hide' to show/hide HUD elements");
                    sendToOthers = false;
                    break;
                case "/radar hide":
                    hide = !hide;
                    if (hide)
                    {
                        MyAPIGateway.Utilities.ShowNotification("WC Radar hidden, re-enable with '/radar hide' again");
                        projInbound = new MyTuple<bool, int, int>(false, 0, -1);
                        projPosList.Clear();
                        obsListCleaned.Clear();
                        threatListCleaned.Clear();
                    }
                    else
                        if (serverEnforcement || serverRWREnforcement)
                        MyAPIGateway.Utilities.ShowNotification("WC Radar visible - subject to server block requirements");
                    else
                        MyAPIGateway.Utilities.ShowNotification("WC Radar visible");
                    sendToOthers = false;
                    break;
                case "/radar reset":
                    MyAPIGateway.Utilities.ShowNotification("WC Radar - Options reset to default");
                    Settings.Instance = Settings.Default;
                    sendToOthers = false;
                    break;
                case "/radar resettoserver":
                    if(!isHost)
                    {
                        MyAPIGateway.Utilities.ShowNotification("WC Radar: Client requested server settings");
                        localCfg = false;
                        Networking.SendToServer(new RequestSettings(MyAPIGateway.Multiplayer.MyId));
                        MyLog.Default.WriteLineAndConsole($"WC Radar: Client requested server settings");
                    }
                    sendToOthers = false;
                    break;
                    /*
                case "/radar resetmenu":
                    if (!menuInit)
                    {
                        InitMenu();
                        menuInit = true;
                        MyAPIGateway.Utilities.ShowNotification("WC Radar - Forced TextHudAPI to re-register F2 menu");
                    }
                    else
                        MyAPIGateway.Utilities.ShowNotification("WC Radar - Already attempted menu re-register");
                    sendToOthers = false;
                    break;
                    */
                default:
                    break;
            }
            return;
        }
        private void UpdateLists()
        {
            try
            {
                if (Session == null || Session.Player == null || Session.Player.Character == null)
                {
                    controlledGrid = null;
                    trackedBlock = null;
                    trackedRWRBlock = null;
                    return;
                }
                var entity = Session.Player.Controller.ControlledEntity as MyCubeBlock;
                if (entity == null)
                {
                    controlledGrid = null;
                    if (serverEnforcement)
                    {
                        trackedBlock = null;
                        serverSuppress = true;
                    }
                    if(serverRWREnforcement)
                    {
                        trackedRWRBlock = null;
                        serverSuppressRWR = true;
                    }
                    return;
                }
                else
                {
                    threatList.Clear();
                    obsList.Clear();
                    threatListCleaned.Clear();
                    obsListCleaned.Clear();
                    threatListEnt.Clear();
                    projInbound.Item1 = false;

                    controlledGrid = entity.CubeGrid;
                    
                    if (serverEnforcement)
                    {
                        if (trackedBlock != null && (!trackedBlock.IsWorking || !controlledGrid.IsPowered))
                        {
                            trackedBlock = null;
                            serverSuppress = true;
                        }
                        if (trackedBlock == null && tick % 300 == 0 && controlledGrid.IsPowered)
                        {
                            serverSuppress = true;
                            foreach (MyCubeBlock block in controlledGrid.GetFatBlocks())
                            {
                                if (block.BlockDefinition.Id.SubtypeName == null)
                                    continue;
                                if (block.IsWorking && ServerSettings.Instance.blockSubtypeList.Contains(block.BlockDefinition.Id.SubtypeName))
                                {
                                    trackedBlock = block;
                                    serverSuppress = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (serverRWREnforcement)
                    {
                        if (trackedRWRBlock != null && (!trackedRWRBlock.IsWorking || !controlledGrid.IsPowered))
                        {
                            trackedRWRBlock = null;
                            serverSuppressRWR = true;
                        }
                        if (trackedRWRBlock == null && tick % 300 == 0 && controlledGrid.IsPowered)
                        {
                            serverSuppressRWR = true;
                            foreach (MyCubeBlock block in controlledGrid.GetFatBlocks())
                            {
                                if (block.BlockDefinition.Id.SubtypeName == null)
                                    continue;
                                if (block.IsWorking && ServerSettings.Instance.rwrSubtypeList.Contains(block.BlockDefinition.Id.SubtypeName))
                                {
                                    trackedRWRBlock = block;
                                    serverSuppressRWR = false;
                                    break;
                                }
                            }
                        }
                    }

                    if(!(serverSuppressRWR && serverSuppress))
                    {
                        wcAPi.GetSortedThreats(entity, threatList);
                        foreach (var threat in threatList)
                            threatListEnt.Add(threat.Item1);
                    }

                    if (!Settings.Instance.rwrDisable && !serverSuppressRWR)
                    {
                        var tempDict = rwrDict;
                        foreach (var rwr in tempDict)
                        {
                            if (rwr.Key == null || rwr.Key.Closed || rwr.Key.MarkedForClose)
                                rwrDict.Remove(rwr.Key);
                        }

                        foreach (var threat in threatListEnt)
                        {
                            if (threat == null || threat.Closed || threat.MarkedForClose)
                            {
                                continue;
                            }

                            var focus = wcAPi.GetAiFocus(threat);
                            if (focus == null)
                            {
                                if (rwrDict.ContainsKey(threat))
                                    rwrDict.Remove(threat);
                                continue;
                            }

                            if (focus.GetTopMostParent() == controlledGrid.GetTopMostParent())
                            {
                                if (!rwrDict.ContainsKey(threat))
                                    rwrDict.Add(threat, tick);
                            }
                            else if (rwrDict.ContainsKey(threat))
                                rwrDict.Remove(threat);
                        }
                    }
                    
                    if (serverSuppress) return;
                    

                    var gridPos = controlledGrid.PositionComp.WorldAABB.Center;
                    var obsDistSqr = Settings.Instance.suppressObstructionDist * Settings.Instance.suppressObstructionDist;

                    if (Settings.Instance.enableMissileWarning || Settings.Instance.enableMissileSymbols || Settings.Instance.enableMissileLines)
                    {
                        projInbound = wcAPi.GetProjectilesLockedOn(entity);
                    }

                    if (Settings.Instance.enableLabelsThreat || Settings.Instance.enableLinesThreat || Settings.Instance.enableSymbolsThreat)
                    {
                        threatListCleaned = ValidateList(threatListEnt, true);
                    }

                    if (Settings.Instance.enableObstructions || Settings.Instance.enableAsteroids)
                    {
                        wcAPi.GetObstructions(entity, obsList);
                        obsListCleaned = ValidateList(obsList, false);
                    }
                }
            }
            catch (Exception e)
            {
                controlledGrid = null;
                MyLog.Default.Error($"[WC Radar] Well something went wrong in Update Lists{e}");
            }
        }
        
        private ICollection<ContactInfo> ValidateList(ICollection<MyEntity> list, bool isThreat)
        {
            var gridPos = controlledGrid.PositionComp.WorldAABB.Center;
            var obsDistSqr = Settings.Instance.suppressObstructionDist * Settings.Instance.suppressObstructionDist;
            ICollection<ContactInfo> ListCleaned = new List<ContactInfo>();
            ICollection<MyEntity> ListChecked = new List<MyEntity>();
            ICollection<MyEntity> ListTemp = new List<MyEntity>();
            var playerID = MyAPIGateway.Session.Player.IdentityId;

            foreach (var obj in list)
            {
                try
                {
                    if (ListChecked.Contains(obj))
                        continue;
                    ListChecked.Add(obj);

                    if (!isThreat)
                    {
                        var objPlanet = obj as MyPlanet;
                        var objRoid = obj as MyVoxelBase;
                        if (objPlanet != null)
                            continue;
                        if (objRoid != null)
                        {
                            if (!Settings.Instance.enableAsteroids)
                                continue;
                            if (Vector3D.DistanceSquared(obj.PositionComp.WorldAABB.Center, gridPos) < obsDistSqr)
                            {
                                var contactNotThreat = new ContactInfo();
                                contactNotThreat.entity = obj;
                                contactNotThreat.blockCount = int.MaxValue;
                                ListCleaned.Add(contactNotThreat);
                            }
                            continue;
                        }
                    }
                    IMyFaction faction = null;
                    var factionTag = "";
                    bool enemy = false;

                    var objGrid = obj as MyCubeGrid;
                    if (objGrid == null)//Characters?
                    {
                        var character = obj as IMyCharacter;
                        if (character == null) continue;
                        var contactChar = new ContactInfo();
                        faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(character.ControllerInfo.ControllingIdentityId);
                        if (faction != null)
                        {
                            //factionTag = faction.Tag;
                            //enemy = faction.IsEnemy(playerID);
                            enemy = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(playerID, faction.FactionId) < -500;
                        }
                        else
                        {
                            //factionTag = "NONE";
                            enemy = true;
                        }
                        contactChar.entity = obj;
                        contactChar.enemy = enemy;
                        ListCleaned.Add(contactChar);
                        continue;
                    }
                    else if (!isThreat && !Settings.Instance.enableObstructions)
                        continue;

                    MyEntity addEnt = obj;
                    if (Settings.Instance.suppressSubgrids)
                    {
                        ListTemp.Clear();
                        foreach (var checkObj in list)
                        {
                            if (obj == checkObj || ListChecked.Contains(checkObj)) continue;
                            var checkGrid = checkObj as MyCubeGrid;

                            if (checkGrid != null && objGrid.IsInSameLogicalGroupAs(checkGrid))
                            {
                                ListTemp.Add(checkGrid);
                                ListChecked.Add(checkGrid);
                            }
                        }

                        if (ListTemp.Count > 0)
                        {
                            float largestSize = obj.PositionComp.LocalVolume.Radius;
                            foreach (var temp in ListTemp)
                            {
                                if (temp.PositionComp.LocalVolume.Radius > largestSize)
                                {
                                    largestSize = temp.PositionComp.LocalVolume.Radius;
                                    addEnt = temp;
                                }
                            }
                        }
                    }

                    bool noPowerFound = true;
                    var gridIMy = addEnt as IMyCubeGrid;
                    var gridMy = addEnt as MyCubeGrid;
                    if (gridIMy.MarkedForClose || gridIMy.Closed) continue;
                    var powerDist = (MyResourceDistributorComponent)gridIMy.ResourceDistributor;
                    noPowerFound = powerDist.MaxAvailableResourceByType(GId, gridIMy) <= 0;
                    if (Settings.Instance.hideUnpowered && noPowerFound)
                        continue;

                    if (gridIMy.BigOwners != null && gridIMy.BigOwners.Count > 0)
                    {
                        faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridIMy.BigOwners[0]);
                        if (faction != null)
                        {
                            factionTag = faction.Tag;
                            enemy = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(playerID, faction.FactionId) < -500;
                        }
                        else
                        {
                            factionTag = "NONE";
                            enemy = true;
                        }
                    }                  

                    var contact = new ContactInfo();
                    contact.entity = addEnt;
                    contact.noPower = noPowerFound;
                    contact.factionTag = factionTag;
                    contact.enemy = enemy;
                    contact.blockCount = gridMy.BlocksCount;


                    ListCleaned.Add(contact);
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"[WC Radar] Exception in Validate {e}");
                    continue;
                }
            }

            return ListCleaned;
        }
        public override void Draw()
        {
            if (client && symbolHeight == 0)
            {
                aspectRatio = Session.Camera.ViewportSize.X / Session.Camera.ViewportSize.Y;
                symbolHeight = symbolWidth * aspectRatio;
            }
            if (client && wcAPi.IsReady)
            {
                if (tick % 60 == 0)
                {
                    UpdateLists();
                    if (!registeredController)
                    {
                        try
                        {
                            MyAPIGateway.Session.Player.Controller.ControlledEntityChanged += GridChange;
                            registeredController = true;
                            MyLog.Default.WriteLineAndConsole($"WC Radar: Registered ControlledEntityChanged in Draw");
                        }
                        catch
                        { }
                    }

                }
                if (MyAPIGateway.Session.Config.HudState != 0)
                {
                    if (controlledGrid != null && MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Control))
                        ExpandedDraw();
                    else
                        ProcessDraws();
                }
                tick++;                
            }
        }
        private void ProcessDraws()
        {
            var viewProjectionMat = Session.Camera.ViewMatrix * Session.Camera.ProjectionMatrix;
            var camMat = Session.Camera.WorldMatrix;
            try
            {
                if (hide) return;

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
                        var warning = new HudAPIv2.HUDMessage(message, new Vector2D(-0.11,-0.5), null, 2, 1.3d, true, true, Color.Black);
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

                    #region Obstructions
                    if (s.enableObstructions || s.enableAsteroids)
                    {
                        foreach (var obs in obsListCleaned)
                        {
                            try
                            {
                                if (obs.entity.MarkedForClose || obs.entity.Closed) continue;
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
                                    DrawScreenEdge(screenCoords, s.obsColor);
                                if (s.enableSymbolsObs && !offscreen)
                                    DrawFrame(topRightScreen, screenCoords, s.obsColor);
                                if (s.enableLinesObs)
                                    DrawLine(position, line, s.obsColor);

                                if (s.enableCollisionWarning && controlledGrid.LinearVelocity.LengthSquared() > 10) //TODO take a look at dampening these messages or intermittently flash them?
                                {
                                    var shipDir = Vector3D.Normalize(controlledGrid.LinearVelocity);
                                    var shipDirRay = new RayD(controlledGrid.PositionComp.WorldAABB.Center, shipDir);
                                    if (shipDirRay.Intersects(obs.entity.PositionComp.WorldAABB) <= controlledGrid.LinearVelocity.Length() * 30 || obs.entity.PositionComp.WorldAABB.Contains(controlledGrid.PositionComp.WorldAABB) != ContainmentType.Disjoint)
                                        MyAPIGateway.Utilities.ShowNotification("!! Collision Warning !!", 14, "Red");
                                }

                                if (s.enableLabelsObs && !offscreen && obs.blockCount > s.hideLabelBlockThreshold)
                                {
                                    var parent = obs.entity.GetTopMostParent();
                                    var parentGrid = parent as MyCubeGrid;
                                    if (hudAPI.Heartbeat)
                                    {
                                        DrawLabel(parentGrid, position, parent, obsSize, s.obsColor, false, "", obs.noPower, new Vector2D(topRightScreen.X, topRightScreen.Y));
                                    }
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
                            if (targ.entity.MarkedForClose || targ.entity.Closed) continue;
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

                            if (s.enableThreatOffScreen && offscreen)
                                DrawScreenEdge(screenCoords, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableLinesThreat)
                                DrawLine(position, line, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableSymbolsThreat && !offscreen)
                                DrawFrame(topRightScreen, screenCoords, rwr ? s.rwrColor.ToVector4() : targ.enemy ? s.enemyColor.ToVector4() : s.neutralColor.ToVector4());
                            if (s.enableLabelsThreat && !offscreen && hudAPI.Heartbeat && targ.blockCount > s.hideLabelBlockThreshold)
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
                                    var rwr = rwrDict.ContainsKey(contact.Key);
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
            if (parentGrid != null) info.AppendLine($"  ^{(int)(controlledGrid.LinearVelocity - parentGrid.LinearVelocity).Length()} m/s");
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
        private void DrawFrame(Vector3D topRight, Vector3D center, Vector4 color)
        {
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


        protected override void UnloadData()
        {
            Networking?.Unregister();
            Networking = null;
            if (client)
            {
                rwrDict.Clear();
                threatList.Clear();
                obsList.Clear();
                threatListCleaned.Clear();
                obsListCleaned.Clear();
                threatListEnt.Clear();
                projPosList.Clear();
                controlledGrid = null;
                trackedBlock = null;
                trackedRWRBlock = null;
                Save(Settings.Instance);
                MyLog.Default.WriteLineAndConsole($"WC Radar: Saved local settings and cleared vars");

                if (wcAPi != null)
                    wcAPi.Unload();
                if (hudAPI != null)
                    hudAPI.Unload();
                MyLog.Default.WriteLineAndConsole($"WC Radar: Unloaded APIs");


                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
                MyLog.Default.WriteLineAndConsole($"WC Radar: Deregistered MessageEnteredSender");
                if (registeredController)
                    MyAPIGateway.Session.Player.Controller.ControlledEntityChanged -= GridChange;
                MyLog.Default.WriteLineAndConsole($"WC Radar: Deregistered ControlledEntityChanged");

                /*
                if (ServerSettings.Instance?.blockSubtypeList != null)
                    ServerSettings.Instance.blockSubtypeList.Clear();
                if (ServerSettings.Instance?.rwrSubtypeList != null)
                    ServerSettings.Instance.rwrSubtypeList.Clear();
                */
            }
            if(MyAPIGateway.Utilities.IsDedicated)
            {
                Players.Clear();            
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            }
        }

        internal class ContactInfo
        {
            internal MyEntity entity = null;
            internal bool noPower = false;
            internal bool enemy = false;
            internal string factionTag = "";
            internal int blockCount = 0;
        }
    }
}

