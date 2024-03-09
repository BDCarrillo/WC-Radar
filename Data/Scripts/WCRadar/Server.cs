using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using Digi.Example_NetworkProtobuf;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        internal ICollection<long> Players = new List<long>();
        private void PlayerConnected(long id)
        {
            MyLog.Default.WriteLineAndConsole($"WC Radar Server: Player connected " + id);
            var steamId = MyAPIGateway.Multiplayer.Players.TryGetSteamId(id);
            //MyLog.Default.WriteLineAndConsole($"WC Radar Server: Trying to find steam ID " + steamId);
            Networking.SendToPlayer(new PacketSettings(Settings.Instance, ServerSettings.Instance), steamId);
            MyLog.Default.WriteLineAndConsole($"WC Radar Server: Sent settings to player " + steamId);
        }
        public static void ServerSendRequested(ulong playerID)
        {
            MyLog.Default.WriteLineAndConsole($"WC Radar: Client requested settings");
            Networking.SendToPlayer(new PacketSettings(Settings.Instance, ServerSettings.Instance), playerID);
        }

    }
}