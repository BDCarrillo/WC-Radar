using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Utils;
using CoreSystems.Api;
using System.Collections.Generic;
using VRage;
using VRage.Game.Entity;
using Draygo.API;
using Sandbox.Game.EntityComponents;
using Digi.Example_NetworkProtobuf;
using Sandbox.Game;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        WcApi wcAPi;
        HudAPIv2 hudAPI;
        internal bool hide = false;
        internal bool client;
        internal static bool isHost = false;
        internal bool menuInit = false;
        internal int tick;
        public static bool serverEnforcement = false;
        public static bool serverRWREnforcement = false;

        internal bool serverSuppress = false;
        internal bool serverSuppressRWR = false;

        internal static bool registeredController = false;
        internal MyCubeBlock trackedBlock = null;
        internal MyCubeBlock trackedRWRBlock = null;

        internal MyStringId corner = MyStringId.GetOrCompute("SharpEdge"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId line = MyStringId.GetOrCompute("particle_laser"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId missileOutline = MyStringId.GetOrCompute("MissileOutline"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId frameCorner = MyStringId.GetOrCompute("FrameCorner"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow
        internal MyStringId dash = MyStringId.GetOrCompute("WCRadarDash"); //Square  GizmoDrawLine  particle_laser ReflectorConeNarrow

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
        internal bool expandedDrawActive;
        internal ICollection<long> Players = new List<long>();
        internal HudAPIv2.HUDMessage rollupText = null;
        internal List<double> sortList = new List<double>();
        internal Dictionary<double, ContactInfo> sortDict = new Dictionary<double, ContactInfo>();
        internal List<string> sortFakeEnum = new List<string>() { "Closest First", "Furthest First", "Closest then ID", "Furthest then ID" };

        private void Clean()
        { 
            Networking?.Unregister();
            Networking = null;
            if (client)
            {
                rwrDict.Clear();
                threatList.Clear();
                threatListEnt.Clear();
                obsList.Clear();
                threatListCleaned.Clear();
                obsListCleaned.Clear();
                threatListEnt.Clear();
                projPosList.Clear();
                controlledGrid = null;
                trackedBlock = null;
                trackedRWRBlock = null;
                Save(Settings.Instance);

                if (wcAPi != null)
                    wcAPi.Unload();
                if (hudAPI != null)
                    hudAPI.Unload();


                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
                if (registeredController)
                {
                    MyAPIGateway.Session.Player.Controller.ControlledEntityChanged -= GridChange;
                    registeredController = false;
                }
            }
            if(MyAPIGateway.Utilities.IsDedicated)
            {
                Players.Clear();            
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            }
        }
    }
}

