using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.IO;
using VRage.Game;
using VRageMath;

namespace WCRadar
{
    [ProtoContract]
    public class Settings
    {
        public static Settings Instance;
        public static readonly Settings Default = new Settings()
        {

            colorFwd = new Vector4(0, 0, 1, 0.01f),
            colorRev = new Vector4(1, 0, 0, 0.01f),
            colorGrav = new Vector4(0, 1, 0, 0.01f),
            enableLines = false,
            enableSymbols = true,
            symbolThickness = 5,
            symbolHeight = 40,
            symbolDrawDistance = 1000,
            lineLength = 80,
            lineThickness = 1,

        };

        [ProtoMember(1)]
        public Vector4 colorFwd { get; set; }
        [ProtoMember(2)]
        public Vector4 colorRev { get; set; }
        [ProtoMember(3)]
        public Vector4 colorGrav { get; set; }
        [ProtoMember(4)]
        public bool enableLines { get; set; }
        [ProtoMember(5)]
        public bool enableSymbols{ get; set; }

        [ProtoMember(6)]
        public float symbolThickness { get; set; }
        [ProtoMember(7)]
        public float symbolHeight { get; set; }
        [ProtoMember(8)]
        public float symbolDrawDistance { get; set; }
        [ProtoMember(9)]
        public float lineLength { get; set; }
        [ProtoMember(10)]
        public float lineThickness { get; set; }

    }
    public partial class Session
    {
        private void InitConfig()
        {
            Settings s = Settings.Default;
            var Filename = "Config.cfg";
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    Validate(ref s);
                    Save(s);
                }
                else
                {
                    s = Settings.Default;
                    Save(s);
                }
            }
            catch
            {
                Settings.Instance = Settings.Default;
                s = Settings.Default;
                Save(s);
                MyAPIGateway.Utilities.ShowNotification("WC Radar: Error with config file, overwriting with default.");
            }
        }

        public static void Validate(ref Settings s)
        {
            s.colorFwd = new Vector4(MathHelper.Clamp(s.colorFwd.X, 0, 255), MathHelper.Clamp(s.colorFwd.Y, 0, 255), MathHelper.Clamp(s.colorFwd.Z, 0, 255), MathHelper.Clamp(s.colorFwd.W, 0, 255));
            s.colorRev = new Vector4(MathHelper.Clamp(s.colorRev.X, 0, 255), MathHelper.Clamp(s.colorRev.Y, 0, 255), MathHelper.Clamp(s.colorRev.Z, 0, 255), MathHelper.Clamp(s.colorRev.W, 0, 255));
            s.colorGrav = new Vector4(MathHelper.Clamp(s.colorGrav.X, 0, 255), MathHelper.Clamp(s.colorGrav.Y, 0, 255), MathHelper.Clamp(s.colorGrav.Z, 0, 255), MathHelper.Clamp(s.colorGrav.W, 0, 255));
        }
        public static void Save(Settings settings)
        {
            var Filename = "Config.cfg";
            try
            {
                TextWriter writer;
                writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                writer.Close();
                Settings.Instance = settings;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("WC Radar","Error with cfg file");

            }
        }
    }
}

