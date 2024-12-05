using Digi.Example_NetworkProtobuf;
using Draygo.API;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Utils;
using VRageMath;

namespace WCRadar
{
    [ProtoContract]
    public class Settings
    {
        public static Settings Instance;
        public static readonly Settings Default = new Settings()
        {
            enableLinesThreat = false,
            enableSymbolsThreat = true,
            enableObstructions = true,
            enableAsteroids = false,
            enableCollisionWarning = false,
            suppressObstructionDist = 20000,
            enableMissileWarning = true,
            missileWarningText = "Fast Movers Inbound",
            enableLabelsThreat = true,
            showFactionThreat = true,
            suppressSubgrids = true,
            enableLinesObs = false,
            enableSymbolsObs = true,
            enableLabelsObs = true,
            hideUnpowered = false,
            enemyColor = Color.Red,
            obsColor = Color.Goldenrod,
            enableMissileSymbols = true,
            enableMissileLines = false,
            missileColor = Color.Yellow,
            neutralColor = Color.LightGray,
            friendlyColor = Color.Green,
            enableMissileOffScreen = true,
            enableThreatOffScreen = true,
            enableObstructionOffScreen = false,
            OffScreenIndicatorLen = 0.15f, //was 0.05
            OffScreenIndicatorThick = 0.01f,  //was 0.0003
            hideLabelBlockThreshold = 10,
            rwrDisable = false,
            rwrDisplayTimeTicks = 180,
            rwrColor = Color.Yellow,
            speedRel = true,
            rollupPos = new Vector2D(-0.98f, 0.98f),
            rollupTextSize = 0.8f,
            rollupMaxNum = 5,
            rollupHideEmpty = false,
            rollupSort = 2,
            showRollup = false,
            rollupShowNum = true,
            labelTextSize = 1f,
            rollupShowFac = true,
            showThreatVectors = false,
        };

        [ProtoMember(1)]
        public bool enableLinesThreat { get; set; } = false;
        [ProtoMember(2)]
        public bool enableSymbolsThreat{ get; set; } = true;
        [ProtoMember(3)]
        public bool enableObstructions { get; set; } = true;
        [ProtoMember(4)]
        public bool enableAsteroids { get; set; } = false;
        [ProtoMember(5)]
        public bool enableCollisionWarning { get; set; } = false;
        [ProtoMember(6)]
        public bool enableMissileWarning { get; set; } = true;
        [ProtoMember(7)]
        public string missileWarningText { get; set; } = "Fast Movers Inbound";
        [ProtoMember(8)]
        public bool enableLabelsThreat { get; set; } = true;
        [ProtoMember(9)]
        public bool suppressSubgrids { get; set; } = true;
        [ProtoMember(10)]
        public bool showFactionThreat { get; set; } = true;
        [ProtoMember(11)]
        public int suppressObstructionDist { get; set; } = 20000;
        [ProtoMember(12)]
        public bool enableLinesObs { get; set; } = false;
        [ProtoMember(13)]
        public bool enableSymbolsObs { get; set; } = true;
        [ProtoMember(14)]
        public bool enableLabelsObs { get; set; } = true;
        [ProtoMember(15)]
        public bool hideUnpowered { get; set; } = false;
        [ProtoMember(16)]
        public Color enemyColor { get; set; } = Color.Red;
        [ProtoMember(17)]
        public Color obsColor { get; set; } = Color.Goldenrod;
        [ProtoMember(18)]
        public bool enableMissileSymbols { get; set; } = true;
        [ProtoMember(19)]
        public bool enableMissileLines { get; set; } = false;
        [ProtoMember(20)]
        public Color missileColor { get; set; } = Color.Yellow;
        [ProtoMember(21)]
        public Color neutralColor { get; set; } = Color.LightGray;
        [ProtoMember(22)]
        public bool enableThreatOffScreen { get; set; } = true;
        [ProtoMember(23)]
        public bool enableObstructionOffScreen { get; set; } = true;
        [ProtoMember(24)]
        public bool enableMissileOffScreen { get; set; } = true;
        [ProtoMember(25)]
        public float OffScreenIndicatorLen { get; set; } = 0.15f;
        [ProtoMember(26)]
        public float OffScreenIndicatorThick { get; set; } = 0.01f;
        [ProtoMember(27)]
        public int hideLabelBlockThreshold { get; set; } = 10;
        [ProtoMember(28)]
        public bool hideName { get; set; } = false;
        [ProtoMember(29)]
        public bool rwrDisable { get; set; } = false;
        [ProtoMember(30)]
        public int rwrDisplayTimeTicks { get; set; } = 180;
        [ProtoMember(31)]
        public Color rwrColor { get; set; } = Color.Yellow;
        [ProtoMember(32)]
        public bool showObsLabel { get; set; } = false;
        [ProtoMember(33)]
        public Color friendlyColor { get; set; } = Color.Green;
        [ProtoMember(34)]
        public bool speedRel { get; set; } = true ;
        [ProtoMember(35)]
        public Vector2D rollupPos { get; set; } = new Vector2D(-0.98f, 0.98f);
        [ProtoMember(36)]
        public float rollupTextSize { get; set; } = 0.8f;
        [ProtoMember(37)]
        public int rollupMaxNum { get; set; } = 5;
        [ProtoMember(38)]
        public bool rollupHideEmpty { get; set; } = false;
        [ProtoMember(39)]
        public int rollupSort { get; set; } = 2;
        [ProtoMember(40)]
        public bool showRollup { get; set; } = false;
        [ProtoMember(41)]
        public bool rollupShowNum { get; set; } = true;
        [ProtoMember(42)]
        public float labelTextSize { get; set; } = 1f;
        [ProtoMember(43)]
        public bool rollupShowFac { get; set; } = true;
        [ProtoMember(44)]
        public bool showThreatVectors { get; set; } = false;
        [ProtoMember(45)]
        public bool disableConformal { get; set; } = false;
    }
    [ProtoContract]
    public class ServerSettings
    {
        public static ServerSettings Instance;
        public static readonly ServerSettings Default = new ServerSettings()
        {
            blockSubtypeList = null,
            rwrSubtypeList = null,
        };
        [ProtoMember(1)]
        public List<string> blockSubtypeList { get; set; }
        [ProtoMember(2)]
        public List<string> rwrSubtypeList { get; set; }
    }
    public partial class Session
    {
        public bool worldCfg = false;
        public static bool localCfg = false;
        private void InitConfig()
        {
            Settings s = Settings.Default;
            ServerSettings ss = ServerSettings.Default;

            var Filename = "Config.cfg";
            var ServerFilename = "ServerConfig.cfg";
            try
            {
                var localFileExists = MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings));
                var worldFileExists = MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(Settings));
                var serverFileExists = MyAPIGateway.Utilities.FileExistsInWorldStorage(ServerFilename, typeof(ServerSettings));
                MyLog.Default.WriteLineAndConsole($"WC Radar: Starting settings  Local: {localFileExists}  World: {worldFileExists}  Server:{serverFileExists}");

                //server configs
                if (MyAPIGateway.Utilities.IsDedicated || isHost)
                {
                    try
                    {
                        if (serverFileExists)//localhost, sp, or dedi server config
                        {
                            TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(ServerFilename, typeof(ServerSettings));
                            string text = reader.ReadToEnd();
                            reader.Close();
                            ss = MyAPIGateway.Utilities.SerializeFromXML<ServerSettings>(text);
                            if (ss.blockSubtypeList != null && ss.blockSubtypeList.Count != 0)
                            {
                                serverEnforcement = true;
                                serverSuppress = true;
                            }
                            if (ss.rwrSubtypeList != null && ss.rwrSubtypeList.Count != 0)
                            {
                                serverRWREnforcement = true;
                                serverSuppressRWR = true;
                            }
                            ServerSettings.Instance = ss;
                        }
                        else //Write out a default server config
                        {
                            TextWriter writer;
                            writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ServerFilename, typeof(ServerSettings));
                            writer.Write(MyAPIGateway.Utilities.SerializeToXML(ServerSettings.Default));
                            writer.Close();
                            ServerSettings.Instance = ServerSettings.Default;
                        }
                    }
                    catch
                    {
                        MyLog.Default.WriteLineAndConsole($"WC Radar: Error with Server Config, overwriting with default null.  See example in mod folder.");

                        TextWriter writer;
                        writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ServerFilename, typeof(ServerSettings));
                        writer.Write(MyAPIGateway.Utilities.SerializeToXML(ServerSettings.Default));
                        writer.Close();
                        ServerSettings.Instance = ServerSettings.Default;
                    }
                }

                //Display options
                if (worldFileExists && !localFileExists) //Localhost or sp initial client cfg
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();
                    s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    Save(s);
                    worldCfg = true;
                }
                else if (client && localFileExists) //client already has an established cfg
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    if (text.Length == 0) //Corner case catch of a blank Config.cfg
                    {
                        MyAPIGateway.Utilities.ShowMessage("WC Radar", "Error with config file, overwriting with default.");
                        MyLog.Default.Error($"WC Radar: Error with config file, overwriting with default");
                        Settings.Instance = Settings.Default;
                        s = Settings.Default;
                        Save(s);
                    }
                    else
                    {
                        s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                        Save(s);
                        localCfg = true;
                    }
                }
                else //Default/initial client cfg
                {
                    s = Settings.Default;
                    Save(s);
                }
            }
            catch (Exception e)
            {
                Settings.Instance = Settings.Default;
                s = Settings.Default;
                Save(s);
                MyAPIGateway.Utilities.ShowMessage("WC Radar", "Error with config file, overwriting with default." + e);
                MyLog.Default.Error($"WC Radar: Error with config file, overwriting with default {e}");
            }
        }
        public void Save(Settings settings)
        {
            var Filename = "Config.cfg";
            try
            {
                if (settings.neutralColor.PackedValue == 0) settings.neutralColor = Color.LightGray;
                if (settings.friendlyColor.PackedValue == 0) settings.friendlyColor = Color.Green;
                if (settings.OffScreenIndicatorLen == 0 || settings.OffScreenIndicatorLen == 0.05f) settings.OffScreenIndicatorLen = 0.15f;
                if (settings.OffScreenIndicatorThick == 0 || settings.OffScreenIndicatorThick == 0.0003f) settings.OffScreenIndicatorThick = 0.01f;
                if (settings.rwrDisplayTimeTicks == 0) settings.rwrDisplayTimeTicks = Settings.Default.rwrDisplayTimeTicks;
                if (settings.rwrColor.PackedValue == 0) settings.rwrColor = Settings.Default.rwrColor;

                if (client)
                {
                    TextWriter writer;
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                    writer.Close();
                }
                Settings.Instance = settings;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("WC Radar","Error with cfg file");
            }
        }

        HudAPIv2.MenuRootCategory SettingsMenu;
        HudAPIv2.MenuSubCategory ThreatMenu, ObstructionMenu, MissileMenu, OffscreenMenu, ConfirmReset, ResetServerConfirm, RWR, Rollup, SummaryListPos, LabelSize;
        HudAPIv2.MenuItem LineEnableThreat, SymbolEnableThreat, LabelEnableThreat, ObstructionEnable, AsteroidEnable, CollisionEnable, MissileEnable, SuppressSubgrid, ShowFactionThreat, HideName;
        HudAPIv2.MenuItem LineEnableObs, SymbolEnableObs, LabelEnableObs, HideUnpowered, Reset, ServerReset, Blank, ResetConfirm, DisableConformal;
        HudAPIv2.MenuItem LineEnableMissile, SymbolEnableMissile, OffscreenMissileEnable, OffscreenThreatEnable, OffscreenObstructionEnable, LabelUp, LabelDown;
        HudAPIv2.MenuItem RWREnable, SpeedSetting, MoveLeft, MoveRight, MoveUp, MoveDown, SizeUp, SizeDown, HideEmpty, SortClosest, RollupShow, ShowNum, RollupFac, ThreatVec;

        HudAPIv2.MenuTextInput ObstructionRange, MissileText, OffscreenLength, OffscreenWidth, HideLabelThreshold, RWRTime, RollupMax;
        HudAPIv2.MenuColorPickerInput EnemyColor, ObsColor, MissileColor, NeutralColor, FriendlyColor, RWRColor;
        
        private void InitMenu()//callback
        {
            SettingsMenu = new HudAPIv2.MenuRootCategory("WC Radar", HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "WC Radar Settings");
            ThreatMenu = new HudAPIv2.MenuSubCategory("Threat Display Options >>", SettingsMenu, "Threat Options");
                LineEnableThreat = new HudAPIv2.MenuItem("Show lines: " + Settings.Instance.enableLinesThreat, ThreatMenu, ShowLinesThreat);
                SymbolEnableThreat = new HudAPIv2.MenuItem("Show symbols: " + Settings.Instance.enableSymbolsThreat, ThreatMenu, ShowSymbolsThreat);
                LabelEnableThreat = new HudAPIv2.MenuItem("Show labels: " + Settings.Instance.enableLabelsThreat, ThreatMenu, ShowLabelsThreat);
                ShowFactionThreat = new HudAPIv2.MenuItem("Show faction on label: " + Settings.Instance.showFactionThreat, ThreatMenu, ShowFactionOnThreat);
                EnemyColor = new HudAPIv2.MenuColorPickerInput("Set enemy color >>", ThreatMenu, Settings.Instance.enemyColor, "Select color", ChangeEnemyColor);
                NeutralColor = new HudAPIv2.MenuColorPickerInput("Set neutral color >>", ThreatMenu, Settings.Instance.neutralColor, "Select color", ChangeNeutralColor);
                SpeedSetting = new HudAPIv2.MenuItem("Velocity shown as: " + (Settings.Instance.speedRel ? "Relative" : "Absolute"), ThreatMenu, ChangeSpeedType);
                ThreatVec = new HudAPIv2.MenuItem("Show direction vectors on threats (WIP): " + Settings.Instance.showThreatVectors, ThreatMenu, ShowVecOnThreat);


            ObstructionMenu = new HudAPIv2.MenuSubCategory("Obstruction Display Options >>", SettingsMenu, "Obstruction Options");
                LineEnableObs = new HudAPIv2.MenuItem("Show lines: " + Settings.Instance.enableLinesObs, ObstructionMenu, ShowLinesObs);
                SymbolEnableObs = new HudAPIv2.MenuItem("Show symbols: " + Settings.Instance.enableSymbolsObs, ObstructionMenu, ShowSymbolsObs);
                LabelEnableObs = new HudAPIv2.MenuItem("Show labels: " + Settings.Instance.enableLabelsObs, ObstructionMenu, ShowLabelsObs);
                ObstructionEnable = new HudAPIv2.MenuItem("Show friendlies: " + Settings.Instance.enableObstructions, ObstructionMenu, ShowObstructions);
                AsteroidEnable = new HudAPIv2.MenuItem("Show asteroids: " + Settings.Instance.enableAsteroids, ObstructionMenu, ShowAsteroids);
                ObstructionRange = new HudAPIv2.MenuTextInput("Hide obstructions beyond: " + Settings.Instance.suppressObstructionDist + "m", ObstructionMenu, "Enter a value", HideObstructions);
                FriendlyColor = new HudAPIv2.MenuColorPickerInput("Set friendly color >>", ObstructionMenu, Settings.Instance.friendlyColor, "Select color", ChangeFriendlyColor);
                ObsColor = new HudAPIv2.MenuColorPickerInput("Set obstruction color >>", ObstructionMenu, Settings.Instance.obsColor, "Select color", ChangeObsColor);

            MissileMenu = new HudAPIv2.MenuSubCategory("Missile Display Options >>", SettingsMenu, "Missile Options");
                LineEnableMissile = new HudAPIv2.MenuItem("Show lines: " + Settings.Instance.enableMissileLines, MissileMenu, ShowLinesMissile);
                SymbolEnableMissile = new HudAPIv2.MenuItem("Show symbols: " + Settings.Instance.enableMissileSymbols, MissileMenu, ShowSymbolsMissile);
                MissileColor = new HudAPIv2.MenuColorPickerInput("Set missile color >>", MissileMenu, Settings.Instance.missileColor, "Select color", ChangeMissileColor);
                MissileEnable = new HudAPIv2.MenuItem("Show missile warning: " + Settings.Instance.enableMissileWarning, MissileMenu, ShowMissile);
                MissileText = new HudAPIv2.MenuTextInput("Missile warning: " + Settings.Instance.missileWarningText, MissileMenu, "Enter new missile warning message", MissileWarning);

            OffscreenMenu = new HudAPIv2.MenuSubCategory("Offscreen Indicator Options >", SettingsMenu, "Offscreen Indicators");
                OffscreenThreatEnable = new HudAPIv2.MenuItem("Show for threats: " + Settings.Instance.enableThreatOffScreen, OffscreenMenu, ShowOffscreenThreat);
                OffscreenObstructionEnable = new HudAPIv2.MenuItem("Show for obstructions: " + Settings.Instance.enableObstructionOffScreen, OffscreenMenu, ShowOffscreenObstruction);
                OffscreenMissileEnable = new HudAPIv2.MenuItem("Show for missiles: " + Settings.Instance.enableMissileOffScreen, OffscreenMenu, ShowOffscreenMissile);
                OffscreenLength = new HudAPIv2.MenuTextInput("Indicator length (edge toward camera) " + Settings.Instance.OffScreenIndicatorLen, OffscreenMenu, "Enter indicator length.  Default is 0.15", ChangeIndicatorLength);
                OffscreenWidth = new HudAPIv2.MenuTextInput("Indicator thickness " + Settings.Instance.OffScreenIndicatorThick, OffscreenMenu, "Enter indicator thickness.  Default is 0.01", ChangeIndicatorThickness);

            RWR = new HudAPIv2.MenuSubCategory("Lock On Warning Options >>", SettingsMenu, "Warning for grids locked on to you");
                RWREnable = new HudAPIv2.MenuItem("Disable Warning: " + Settings.Instance.rwrDisable, RWR, RWRToggle);
                RWRTime = new HudAPIv2.MenuTextInput("New Warning Display Time: " + Settings.Instance.rwrDisplayTimeTicks + " ticks", RWR, "Enter new display time in ticks (60 per second)", ChangeRWRTime);
                RWRColor = new HudAPIv2.MenuColorPickerInput("Set Warning color >>", RWR, Settings.Instance.rwrColor, "Select color", ChangeRWRColor);

            Rollup = new HudAPIv2.MenuSubCategory("Summary List Options >>", SettingsMenu, "Summary list of targets");
                RollupShow = new HudAPIv2.MenuItem("Show summary list: " + Settings.Instance.showRollup, Rollup, ShowRollup, true);
                RollupMax = new HudAPIv2.MenuTextInput("Max number shown: " + Settings.Instance.rollupMaxNum, Rollup, "Enter new max number to show", UpdateMax);
                ShowNum = new HudAPIv2.MenuItem("Use last 4 of entity ID: " + Settings.Instance.rollupShowNum, Rollup, changeRollupNumShow, true);
                RollupFac = new HudAPIv2.MenuItem("Show faction in summary: " + Settings.Instance.rollupShowFac, Rollup, FacRollup);
                SummaryListPos = new HudAPIv2.MenuSubCategory("Summary list location/text size >>", Rollup, "Hold shift to move 10x per click");
                    MoveLeft = new HudAPIv2.MenuItem("Move Left", SummaryListPos, LeftMove);
                    MoveRight = new HudAPIv2.MenuItem("Move Right", SummaryListPos, RightMove);
                    MoveUp = new HudAPIv2.MenuItem("Move Up", SummaryListPos, UpMove);
                    MoveDown = new HudAPIv2.MenuItem("Move Down", SummaryListPos, DownMove);
                    SizeUp = new HudAPIv2.MenuItem("Increase text size", SummaryListPos, UpSize);
                    SizeDown = new HudAPIv2.MenuItem("Decrease text size", SummaryListPos, DownSize);
                HideEmpty = new HudAPIv2.MenuItem("Hide if no targets: " + Settings.Instance.rollupHideEmpty, Rollup, HideRollup, true);
                SortClosest = new HudAPIv2.MenuItem("Sort by: " + sortFakeEnum[Settings.Instance.rollupSort], Rollup, RollupSort, true);


            Blank = new HudAPIv2.MenuItem("- - - - - - - - - - -", SettingsMenu, null);
            HideName = new HudAPIv2.MenuItem("Hide grid name: " + Settings.Instance.hideName, SettingsMenu, HideGridName);
            HideUnpowered = new HudAPIv2.MenuItem("Hide unpowered grids: " + Settings.Instance.hideUnpowered, SettingsMenu, HideUnpoweredGrids);
            SuppressSubgrid = new HudAPIv2.MenuItem("Hide subgrids: " + Settings.Instance.suppressSubgrids, SettingsMenu, SuppressSubgrids);
            HideLabelThreshold = new HudAPIv2.MenuTextInput("Hide grids <" + Settings.Instance.hideLabelBlockThreshold + " blocks", SettingsMenu, "Enter threshold to show symbols/labels.  Default is 10", ChangeLabelThreshold);

            CollisionEnable = new HudAPIv2.MenuItem("Show collision alert: " + Settings.Instance.enableCollisionWarning, SettingsMenu, ShowCollision);
            LabelSize = new HudAPIv2.MenuSubCategory("Change label text size >>", SettingsMenu, "Change label text size");
                LabelUp = new HudAPIv2.MenuItem("Increase text size", LabelSize, UpSizeLabel);
                LabelDown = new HudAPIv2.MenuItem("Decrease text size", LabelSize, DownSizeLabel);
            DisableConformal = new HudAPIv2.MenuItem("Disable conformal box draws: " + Settings.Instance.disableConformal, SettingsMenu, ChangeConformal);
            Blank = new HudAPIv2.MenuItem("- - - - - - - - - - -", SettingsMenu, null);
            ConfirmReset = new HudAPIv2.MenuSubCategory("Reset to defaults", SettingsMenu, "Confirm");
            Reset = new HudAPIv2.MenuItem("Reset defaults", ConfirmReset, ResetDefaults);
            ResetServerConfirm = new HudAPIv2.MenuSubCategory("Reset to server defaults (if any)", SettingsMenu, "Confirm");
            if (!MyAPIGateway.Multiplayer.MultiplayerActive) ResetServerConfirm.Interactable = false;
            ServerReset = new HudAPIv2.MenuItem("Reset to server default (if any)", ResetServerConfirm, ResetServerDefaults);

            //Init rollup text box
            rollupText = new HudAPIv2.HUDMessage(new System.Text.StringBuilder(), new Vector2D(0, 0), Shadowing: true, ShadowColor: Color.Black);
            rollupText.InitialColor = Color.White;
            rollupText.Origin = Settings.Instance.rollupPos;
            rollupText.Scale = Settings.Instance.rollupTextSize;
            rollupText.Visible = false;
            rollupText.Font = "monospace";
        }

        private void ChangeConformal()
        {
            Settings.Instance.disableConformal = !Settings.Instance.disableConformal;
            DisableConformal.Text = "Disable conformal box draws: " + Settings.Instance.disableConformal;
        }

        private void ShowVecOnThreat()
        {
            Settings.Instance.showThreatVectors = !Settings.Instance.showThreatVectors;
            ThreatVec.Text = "Show direction vectors on threats (WIP): " + Settings.Instance.showThreatVectors;
        }
        private void FacRollup()
        {
            Settings.Instance.rollupShowFac = !Settings.Instance.rollupShowFac;
            RollupFac.Text = "Show faction in summary: " + Settings.Instance.rollupShowFac;
        }

        private void changeRollupNumShow()
        {
            Settings.Instance.rollupShowNum = !Settings.Instance.rollupShowNum;

            if(!Settings.Instance.rollupShowNum)
            {
                if (Settings.Instance.rollupSort == 2)
                    Settings.Instance.rollupSort = 0;
                else if (Settings.Instance.rollupSort == 3)
                    Settings.Instance.rollupSort = 1;
                SortClosest.Text = "Sort by: " + sortFakeEnum[Settings.Instance.rollupSort];
            }

            ShowNum.Text = "Use last 4 of entity ID: " + Settings.Instance.rollupShowNum;
        }

        private void ShowRollup()
        {
            Settings.Instance.showRollup = !Settings.Instance.showRollup;
            if (!Settings.Instance.showRollup && rollupText.Visible)
                rollupText.Visible = false;
            RollupShow.Text = "Show summary list: " + Settings.Instance.showRollup;
        }

        private void RollupSort()
        {
            Settings.Instance.rollupSort++;
            if(Settings.Instance.rollupSort > 3 || (!Settings.Instance.rollupShowNum && Settings.Instance.rollupSort > 1))
                Settings.Instance.rollupSort = 0;
            SortClosest.Text = "Sort by: " + sortFakeEnum[Settings.Instance.rollupSort];
        }
        private void HideRollup()
        {
            Settings.Instance.rollupHideEmpty = !Settings.Instance.rollupHideEmpty;
            HideEmpty.Text = "Hide if no targets: " + Settings.Instance.rollupHideEmpty;
        }

        private void UpdateMax(string obj)
        {
            int getter;
            if (!int.TryParse(obj, out getter))
                return;
            Settings.Instance.rollupMaxNum = getter;
            RollupMax.Text = "Max number shown: " + Settings.Instance.rollupMaxNum;
        }

        private void UpSizeLabel()
        {
            Settings.Instance.labelTextSize += 0.025f;
        }
        private void DownSizeLabel()
        {
            Settings.Instance.labelTextSize -= 0.025f;
        }
        private void UpSize()
        {
            Settings.Instance.rollupTextSize += 0.025f;
            rollupText.Scale = Settings.Instance.rollupTextSize;
        }
        private void DownSize()
        {
            Settings.Instance.rollupTextSize -= 0.025f;
            rollupText.Scale = Settings.Instance.rollupTextSize;
        }

        private void LeftMove()
        {
            var mult = MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Shift) ? 10 : 1;
            Settings.Instance.rollupPos += new Vector2D(-0.005 * mult, 0);
            UpdateRollupCoords();
        }
        private void RightMove()
        {
            var mult = MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Shift) ? 10 : 1;
            Settings.Instance.rollupPos += new Vector2D(0.005 * mult, 0);
            UpdateRollupCoords();
        }
        private void UpMove()
        {
            var mult = MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Shift) ? 10 : 1;
            Settings.Instance.rollupPos += new Vector2D(0, 0.005 * mult);
            UpdateRollupCoords();
        }
        private void DownMove()
        {
            var mult = MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Shift) ? 10 : 1;
            Settings.Instance.rollupPos += new Vector2D(0, -0.005 * mult);
            UpdateRollupCoords();
        }
        private void UpdateRollupCoords()
        {
            rollupText.Origin = Settings.Instance.rollupPos;
        }

        private void ChangeSpeedType()
        {
            Settings.Instance.speedRel = !Settings.Instance.speedRel;
            SpeedSetting.Text = "Velocity shown as: " + (Settings.Instance.speedRel ? "Relative" : "Absolute");
        }
        private void ChangeRWRTime(string obj)
        {
            int getter;
            if (!int.TryParse(obj, out getter))
                return;
            Settings.Instance.rwrDisplayTimeTicks = getter;
            RWRTime.Text = "New Warning Display Time: " + Settings.Instance.rwrDisplayTimeTicks + " ticks";
        }        
        private void RWRToggle()
        {
            Settings.Instance.rwrDisable = !Settings.Instance.rwrDisable;
            RWREnable.Text = "Disable Radar Warning: " + Settings.Instance.rwrDisable;
            rwrDict.Clear();
        }

        private void ResetDefaults()
        {
            MyAPIGateway.Utilities.ShowNotification("WC Radar - Options reset to default");
            var tempSettings = new Settings();
            tempSettings = Settings.Default;
            Settings.Instance = tempSettings;
            rollupText.Scale = Settings.Instance.rollupTextSize;
            rollupText.Origin = Settings.Instance.rollupPos;
            rollupText.Visible = false;
        }

        private void ResetServerDefaults()
        {
            if (client)
            {
                MyAPIGateway.Utilities.ShowNotification("WC Radar - Request sent to server for defaults");
                localCfg = false;
                Networking.SendToServer(new RequestSettings(MyAPIGateway.Multiplayer.MyId));
            }
        }

        private void ChangeLabelThreshold(string obj)
        {
            int getter;
            if (!int.TryParse(obj, out getter))
                return;
            Settings.Instance.hideLabelBlockThreshold = getter;
            HideLabelThreshold.Text = "Hide grids <" + Settings.Instance.hideLabelBlockThreshold + " blocks";
        }
        private void ChangeIndicatorLength(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            Settings.Instance.OffScreenIndicatorLen = getter;
            OffscreenLength.Text = "Indicator length (edge toward camera) " + Settings.Instance.OffScreenIndicatorLen;
        }
        private void ChangeIndicatorThickness(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            Settings.Instance.OffScreenIndicatorThick = getter;
            OffscreenWidth.Text = "Indicator thickness " + Settings.Instance.OffScreenIndicatorThick;
        }


        private void ChangeEnemyColor(Color obj)
        {
            Settings.Instance.enemyColor = obj;
        }
        private void ChangeNeutralColor(Color obj)
        {
            Settings.Instance.neutralColor = obj;
        }
        private void ChangeFriendlyColor(Color obj)
        {
            Settings.Instance.friendlyColor = obj;
        }
        private void ChangeObsColor(Color obj)
        {
            Settings.Instance.obsColor = obj;
        }
        private void ChangeMissileColor(Color obj)
        {
            Settings.Instance.missileColor = obj;
        }
        private void ChangeRWRColor(Color obj)
        {
            Settings.Instance.rwrColor = obj;
        }


        private void ShowLinesThreat()
        {
            Settings.Instance.enableLinesThreat = !Settings.Instance.enableLinesThreat;
            LineEnableThreat.Text = "Show lines: " + Settings.Instance.enableLinesThreat;
        }

        private void ShowLinesMissile()
        {
            Settings.Instance.enableMissileLines = !Settings.Instance.enableMissileLines;
            LineEnableMissile.Text = "Show lines: " + Settings.Instance.enableMissileLines;
        }
        private void ShowSymbolsMissile()
        {
            Settings.Instance.enableMissileSymbols = !Settings.Instance.enableMissileSymbols;
            SymbolEnableMissile.Text = "Show symbols: " + Settings.Instance.enableMissileSymbols;
        }
        private void HideUnpoweredGrids()
        {
            Settings.Instance.hideUnpowered = !Settings.Instance.hideUnpowered;
            HideUnpowered.Text = "Hide unpowered grids: " + Settings.Instance.hideUnpowered;
        }

        private void HideGridName()
        {
            Settings.Instance.hideName = !Settings.Instance.hideName;
            HideName.Text = "Hide grid name: " + Settings.Instance.hideName;
        }

        private void ShowSymbolsThreat()
        {
            Settings.Instance.enableSymbolsThreat = !Settings.Instance.enableSymbolsThreat;
            SymbolEnableThreat.Text = "Show symbols: " + Settings.Instance.enableSymbolsThreat;
        }
        private void ShowLinesObs()
        {
            Settings.Instance.enableLinesObs = !Settings.Instance.enableLinesObs;
            LineEnableObs.Text = "Show lines: " + Settings.Instance.enableLinesObs;
        }

        private void ShowSymbolsObs()
        {
            Settings.Instance.enableSymbolsObs = !Settings.Instance.enableSymbolsObs;
            SymbolEnableObs.Text = "Show symbols: " + Settings.Instance.enableSymbolsObs;
        }
        private void ShowObstructions()
        {
            Settings.Instance.enableObstructions = !Settings.Instance.enableObstructions;
            ObstructionEnable.Text = "Show friendlies: " + Settings.Instance.enableObstructions;
        }
        private void ShowAsteroids()
        {
            Settings.Instance.enableAsteroids = !Settings.Instance.enableAsteroids;
            AsteroidEnable.Text = "Show asteroids: " + Settings.Instance.enableAsteroids;
        }
        private void ShowCollision()
        {
            Settings.Instance.enableCollisionWarning = !Settings.Instance.enableCollisionWarning;
            CollisionEnable.Text = "Show collision alert: " + Settings.Instance.enableCollisionWarning;
        }
        private void ShowMissile()
        {
            Settings.Instance.enableMissileWarning = !Settings.Instance.enableMissileWarning;
            MissileEnable.Text = "Show missile warning: " + Settings.Instance.enableMissileWarning;
        }
        private void ShowLabelsThreat()
        {
            Settings.Instance.enableLabelsThreat = !Settings.Instance.enableLabelsThreat;
            LabelEnableThreat.Text = "Show labels: " + Settings.Instance.enableLabelsThreat;
        }
        private void ShowLabelsObs()
        {
            Settings.Instance.enableLabelsObs = !Settings.Instance.enableLabelsObs;
            LabelEnableObs.Text = "Show labels: " + Settings.Instance.enableLabelsObs;
        }
        private void SuppressSubgrids()
        {
            Settings.Instance.suppressSubgrids = !Settings.Instance.suppressSubgrids;
            SuppressSubgrid.Text = "Hide subgrids: " + Settings.Instance.suppressSubgrids;
        }
        private void ShowFactionOnThreat()
        {
            Settings.Instance.showFactionThreat = !Settings.Instance.showFactionThreat;
            ShowFactionThreat.Text = "Show faction on label: " + Settings.Instance.showFactionThreat;
        }
        private void HideObstructions(string obj)
        {
            int getter;
            if (!int.TryParse(obj, out getter))
                return;
            Settings.Instance.suppressObstructionDist = getter;
            
            ObstructionRange.Text = "Hide obstructions beyond: " + Settings.Instance.suppressObstructionDist + "m";
        }
        private void MissileWarning(string obj)
        {
            Settings.Instance.missileWarningText = obj;
            MissileText.Text = "Missile warning: " + Settings.Instance.missileWarningText;
        }
        private void ShowOffscreenMissile()
        {
            Settings.Instance.enableMissileOffScreen = !Settings.Instance.enableMissileOffScreen;
            OffscreenMissileEnable.Text = "Show for missiles: " + Settings.Instance.enableMissileOffScreen;
        }

        private void ShowOffscreenThreat()
        {
            Settings.Instance.enableThreatOffScreen = !Settings.Instance.enableThreatOffScreen;
            OffscreenThreatEnable.Text = "Show for threats: " + Settings.Instance.enableThreatOffScreen;
        }

        private void ShowOffscreenObstruction()
        {
            Settings.Instance.enableObstructionOffScreen = !Settings.Instance.enableObstructionOffScreen;
            OffscreenObstructionEnable.Text = "Show for obstructions: " + Settings.Instance.enableObstructionOffScreen;
        }
    }
}

