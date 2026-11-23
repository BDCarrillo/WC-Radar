using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
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
                    focusTarget = null;
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
                        if (trackedBlock != null && (!trackedBlock.IsWorking || !controlledGrid.IsPowerSwitchOn))
                        {
                            trackedBlock = null;
                            serverSuppress = true;
                        }
                        if (trackedBlock == null && tick % 300 == 0 && controlledGrid.IsPowerSwitchOn)
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
                        if (trackedRWRBlock != null && (!trackedRWRBlock.IsWorking || !controlledGrid.IsPowerSwitchOn))
                        {
                            trackedRWRBlock = null;
                            serverSuppressRWR = true;
                        }
                        if (trackedRWRBlock == null && tick % 300 == 0 && controlledGrid.IsPowerSwitchOn)
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
                if (ListChecked.Contains(obj))
                    continue;
                ListChecked.Add(obj);

                if (!isThreat)
                {
                    var objPlanet = obj as MyPlanet;
                    if (objPlanet != null)
                        continue;
                    var objRoid = obj as MyVoxelBase;
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
                IMyFaction contactFaction = null;
                var factionTag = "";
                var objGrid = obj as MyCubeGrid;
                if (objGrid == null)//Characters?
                {
                    var character = obj as IMyCharacter;
                    if (character == null) continue;
                    var contactChar = new ContactInfo();
                    var characterOwner = character.ControllerInfo.ControllingIdentityId;
                    contactFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(characterOwner);
                    SetRelation(contactChar, playerID, playerFaction, characterOwner, contactFaction);

                    if (!isThreat && contactChar.enemy)
                        continue;

                    contactChar.entity = obj;
                    contactChar.blockCount = int.MaxValue;
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
                var contact = new ContactInfo();

                if (gridIMy.BigOwners != null && gridIMy.BigOwners.Count > 0)
                {
                    var gridOwner = gridIMy.BigOwners[0];
                    contactFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner);
                    factionTag = contactFaction != null ? contactFaction.Tag : "NONE";
                    SetRelation(contact, playerID, playerFaction, gridOwner, contactFaction);
                }                  

                contact.entity = addEnt;
                contact.noPower = noPowerFound;
                contact.factionTag = factionTag;
                contact.blockCount = gridMy.BlocksCount;
                ListCleaned.Add(contact);
            }

            return ListCleaned;
        }

        /// <summary>
        /// Marks a contact friendly or enemy the way SE itself judges two players for turrets, ownership and its own
        /// HUD (MyIDModule.GetRelationPlayerPlayer), which WeaponCore uses as well, so the radar shows what the guns
        /// will do:
        /// - the same player, or the same faction: friendly;
        /// - neither in a faction: enemy;
        /// - one of them in no faction: that player's own reputation with the other's faction;
        /// - one of them in an NPC faction: the other player's own reputation with that NPC faction;
        /// - two player factions: the reputation between the two factions.
        /// SE reads a player's own reputation for an NPC faction, not the faction pair: NPC factions whose definition
        /// pins players' reputation (StaticReputation) can hold a player at Neutral while the pair is still at war.
        /// On a client, another player's own reputation is only known where the server has sent it; SE's HUD on that
        /// client reads the same value.
        /// </summary>
        private static void SetRelation(ContactInfo contact, long playerId, IMyFaction playerFaction, long ownerId, IMyFaction ownerFaction)
        {
            contact.enemy = false;
            contact.friendly = false;

            if (ownerId == playerId || (playerFaction != null && playerFaction == ownerFaction))
            {
                contact.friendly = true;
                return;
            }
            if (playerFaction == null && ownerFaction == null)
            {
                contact.enemy = true;
                return;
            }

            var factions = MyAPIGateway.Session.Factions;
            int reputation;
            if (ownerFaction == null)
                reputation = factions.GetReputationBetweenPlayerAndFaction(ownerId, playerFaction.FactionId);
            else if (playerFaction == null)
                reputation = factions.GetReputationBetweenPlayerAndFaction(playerId, ownerFaction.FactionId);
            else
            {
                var playerFactionIsNpc = IsNpcFaction(playerFaction);
                var ownerFactionIsNpc = IsNpcFaction(ownerFaction);
                if (ownerFactionIsNpc && !playerFactionIsNpc)
                    reputation = factions.GetReputationBetweenPlayerAndFaction(playerId, ownerFaction.FactionId);
                else if (playerFactionIsNpc && !ownerFactionIsNpc)
                    reputation = factions.GetReputationBetweenPlayerAndFaction(ownerId, playerFaction.FactionId);
                else
                    reputation = factions.GetReputationBetweenFactions(playerFaction.FactionId, ownerFaction.FactionId);
            }

            // SE's default economy: below -500 is Enemies, 500 and above is Friends.
            contact.enemy = reputation < -500;
            contact.friendly = reputation >= 500;
        }

        /// <summary>
        /// SE's MyFactionCollection.IsNpcFaction(tag), which the mod API doesn't expose: a faction with a definition,
        /// or of a type other than None or PlayerMade.
        /// </summary>
        private static bool IsNpcFaction(IMyFaction faction)
        {
            if (MyDefinitionManager.Static.TryGetFactionDefinition(faction.Tag) != null) return true;
            return faction.FactionType != MyFactionTypes.None && faction.FactionType != MyFactionTypes.PlayerMade;
        }
    }
}

