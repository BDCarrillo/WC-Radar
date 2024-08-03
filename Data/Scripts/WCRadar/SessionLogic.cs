using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Utils;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using Sandbox.Game.EntityComponents;
using Digi.Example_NetworkProtobuf;
using System.Linq;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        public static void ServerSendRequested(ulong playerID)
        {
            MyLog.Default.WriteLineAndConsole($"WC Radar: Client requested settings");
            Networking.SendToPlayer(new PacketSettings(Settings.Instance, ServerSettings.Instance), playerID);
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
                        foreach (var rwr in rwrDict.ToArray())
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
            var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerID);

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
                    bool friendly = false;

                    var objGrid = obj as MyCubeGrid;
                    if (objGrid == null)//Characters?
                    {
                        var character = obj as IMyCharacter;
                        if (character == null) continue;
                        var contactChar = new ContactInfo();
                        faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(character.ControllerInfo.ControllingIdentityId);
                        if (faction != null)
                        {
                            var reputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(playerID, faction.FactionId);
                            if (playerFaction == faction)
                            {
                                friendly = true;
                            }
                            else
                            {
                                enemy = reputation < -500;
                                friendly = reputation > 500;
                            }
                        }
                        else
                        {
                            enemy = true;
                        }
                        contactChar.entity = obj;
                        contactChar.enemy = enemy;
                        contactChar.friendly = friendly;
                        ListCleaned.Add(contactChar);
                        continue;
                    }
                    else if (!Settings.Instance.enableObstructions)
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
                            var reputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(playerID, faction.FactionId);
                            if (playerFaction == faction)
                            {
                                friendly = true;
                            }
                            else
                            {
                                enemy = reputation < -500;
                                friendly = reputation > 500;
                            }
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
                    contact.friendly = friendly;
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
    }
}

