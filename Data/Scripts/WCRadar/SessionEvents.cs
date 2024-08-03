using Sandbox.ModAPI;
using VRage.Game.Components;
using Sandbox.Game.Entities;
using VRage.Utils;
using VRage;
using Digi.Example_NetworkProtobuf;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        private void PlayerConnected(long id)
        {
            var steamId = MyAPIGateway.Multiplayer.Players.TryGetSteamId(id);
            Networking.SendToPlayer(new PacketSettings(Settings.Instance, ServerSettings.Instance), steamId);
            MyLog.Default.WriteLineAndConsole($"WC Radar Server: Sent settings to player " + steamId);
        }
        private void GridChange(VRage.Game.ModAPI.Interfaces.IMyControllableEntity previousEnt, VRage.Game.ModAPI.Interfaces.IMyControllableEntity newEnt)
        {
            var newGrid = newEnt?.Entity?.GetTopMostParent() as MyCubeGrid;
            //controlledGrid = newGrid;
            threatList.Clear();
            threatListEnt.Clear();
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
    }
}

