using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using WCRadar;

namespace Digi.Example_NetworkProtobuf
{
    /// <summary>
    /// Simple network communication example.
    /// 
    /// Always send to server as clients can't send to eachother directly.
    /// Then decide in the packet if it should be relayed to everyone else (except sender and server of course).
    /// 
    /// Security note:
    ///  SenderId is not reliable and can be altered by sender to claim they're someone else (like an admin).
    ///  If you need senderId to be secure, a more complicated process is required involving sending
    ///   every player a unique random ID and they sending that ID would confirm their identity.
    /// </summary>
    public class Networking
    {
        public readonly ushort ChannelId;
        private List<IMyPlayer> tempPlayers = new List<IMyPlayer>();

        /// <summary>
        /// <paramref name="channelId"/> must be unique from all other mods that also use network packets.
        /// </summary>
        public Networking(ushort channelId)
        {
            ChannelId = channelId;
        }

        /// <summary>
        /// Register packet monitoring, not necessary if you don't want the local machine to handle incomming packets.
        /// </summary>
        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ChannelId, ReceivedPacket);
        }

        /// <summary>
        /// This must be called on world unload if you called <see cref="Register"/>.
        /// </summary>
        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(ChannelId, ReceivedPacket);
            tempPlayers.Clear();
        }

        private void ReceivedPacket(byte[] rawData) // executed when a packet is received on this machine
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);

                HandlePacket(packet, rawData);
            }
            catch (Exception e)
            {
                // Handle packet receive errors however you prefer, this is with logging. Remove try-catch to allow it to crash the game.
                // If another mod uses the same channel as your mod, this will throw errors being unable to deserialize their stuff.
                // In that case, one of you must change the channelId and NOT ignoring the error as it can noticeably impact performance.

                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
            }
        }

        private void HandlePacket(PacketBase packet, byte[] rawData = null)
        {
            var relay = packet.Received();

            if (relay)
                RelayToClients(packet, rawData);
        }

        /// <summary>
        /// Send a packet to the server.
        /// Works from clients and server.
        /// </summary>
        public void SendToServer(PacketBase packet)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                HandlePacket(packet);
                return;
            }

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(ChannelId, bytes);
        }

        /// <summary>
        /// Send a packet to a specific player.
        /// Only works server side.
        /// </summary>
        public void SendToPlayer(PacketSettings packet, ulong steamId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, bytes, steamId);
        }

        /// <summary>
        /// Sends packet (or supplied bytes) to all players except server player and supplied packet's sender.
        /// Only works server side.
        /// </summary>
        public void RelayToClients(PacketBase packet, byte[] rawData = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            if (tempPlayers == null)
                tempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
            else
                tempPlayers.Clear();

            MyAPIGateway.Players.GetPlayers(tempPlayers);

            foreach (var p in tempPlayers)
            {
                if (p.IsBot)
                    continue;

                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId)
                    continue;

                if (p.SteamUserId == packet.SenderId)
                    continue;

                if (rawData == null)
                    rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(ChannelId, rawData, p.SteamUserId);
            }

            tempPlayers.Clear();
        }
    }




    // tag numbers in ProtoInclude collide with numbers from ProtoMember in the same class, therefore they must be unique.
    [ProtoInclude(1000, typeof(PacketSettings))]
    [ProtoInclude(2000, typeof(RequestSettings))]

    [ProtoContract]
    public abstract class PacketBase
    {
        // this field's value will be sent if it's not the default value.
        // to define a default value you must use the [DefaultValue(...)] attribute.
        [ProtoMember(1)]
        public ulong SenderId;

        public PacketBase()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }

        /// <summary>
        /// Called when this packet is received on this machine.
        /// </summary>
        /// <returns>Return true if you want the packet to be sent to other clients (only works server side)</returns>
        public abstract bool Received();
    }
    [ProtoContract]
    public partial class PacketSettings : PacketBase
    {
        // tag numbers in this class won't collide with tag numbers from the base class
        [ProtoMember(2)]
        public Settings cSettings;
        [ProtoMember(3)]
        public ServerSettings sSettings;

        public PacketSettings() { } // Empty constructor required for deserialization

        public PacketSettings(Settings csettings, ServerSettings ssettings)
        {
            cSettings = csettings;
            sSettings = ssettings;
        }

        public override bool Received()
        {
            if (!MyAPIGateway.Utilities.IsDedicated) //client crap
            {
                MyLog.Default.WriteLineAndConsole($"WC Radar: Received packet");
                Session.registeredController = false;
                try
                {
                    bool display = false;
                    bool server = false;
                    if (!Session.localCfg && cSettings != null)
                    {
                        Settings.Instance = cSettings;
                        MyLog.Default.WriteLineAndConsole($"WC Radar: Received server display defaults");
                        display = true;                       
                    }
                    if (sSettings != null && sSettings.blockSubtypeList != null && sSettings.blockSubtypeList.Count != 0)
                    {
                        ServerSettings.Instance = sSettings;
                        Session.serverEnforcement = true;
                        MyLog.Default.WriteLineAndConsole($"WC Radar: Received server settings");
                        server = true;
                    }
                    if (sSettings != null && sSettings.rwrSubtypeList != null && sSettings.rwrSubtypeList.Count != 0)
                    {
                        ServerSettings.Instance = sSettings;
                        Session.serverRWREnforcement = true;
                        MyLog.Default.WriteLineAndConsole($"WC Radar: Received server RWR settings");
                        server = true;
                    }
                    /*
                    string message = "WC Radar: ";
                    if (display)
                        message += "Received server default config.";
                    if (server)
                        message += "Received server block list.";
                    if(server || display)MyAPIGateway.Utilities.ShowNotification(message);
                    */
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLineAndConsole($"WC Radar: Failed to process packet");
                }
            }
            else//Client requested settings (I think?)
            {
                MyLog.Default.WriteLineAndConsole($"WC Radar: Client requested settings");
            }

            return false; // relay packet to other clients (only works if server receives it)
        }
    }
    [ProtoContract]
    public partial class RequestSettings : PacketBase
    {
        // tag numbers in this class won't collide with tag numbers from the base class
        [ProtoMember(4)]
        public ulong playerID;

        public RequestSettings() { } // Empty constructor required for deserialization

        public RequestSettings(ulong playerid)
        {
            playerID = playerid;
        }

        public override bool Received()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                Session.ServerSendRequested(playerID);
            }
            return false; // relay packet to other clients (only works if server receives it)
        }
    }
}