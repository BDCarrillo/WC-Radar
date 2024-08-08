using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using CoreSystems.Api;
using VRage.Game.Entity;
using Draygo.API;
using Digi.Example_NetworkProtobuf;
using Sandbox.Game;

namespace WCRadar
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session : MySessionComponentBase
    {
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
                if (!wcAPi.IsReady)
                    /* //Removed to cut down on chat spam
                    if(serverEnforcement || serverRWREnforcement)
                        MyAPIGateway.Utilities.ShowMessage("WC Radar", "Overlay requires a certain type of block set by the server.  Overlay options can be found by hitting enter and F2");
                    else
                        MyAPIGateway.Utilities.ShowMessage("WC Radar", "overlay options can be found by hitting enter and F2");
                else
                    */
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

        public override void HandleInput()
        {

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
                if (!hide && MyAPIGateway.Session.Config.HudState != 0 && hudAPI.Heartbeat)
                {
                    var s = Settings.Instance;
                    if (projInbound.Item1 && (s.enableMissileLines || s.enableMissileSymbols || s.enableMissileWarning))
                        DrawMissile();
                    if (threatListCleaned.Count > 0 || (obsListCleaned.Count > 0 && s.enableObstructions))
                    {
                        if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Control))
                            ExpandedDraw();
                        else
                            ProcessDraws();
                    }

                    if (s.showRollup && rollupText != null)
                        RollupData();
                }
                tick++;                
            }
        }

        protected override void UnloadData()
        {
            Clean();
        }
    }
}

