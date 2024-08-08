using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace WCRadar
{
    public partial class Session : MySessionComponentBase
    {
        internal class ContactInfo
        {
            internal MyEntity entity = null;
            internal bool noPower = false;
            internal bool enemy = false;
            internal bool friendly = false;
            internal string factionTag = "";
            internal int blockCount = 0;
        }
        internal class expandedMark
        {
            internal Color color;
            internal string label;
            internal Vector2D screenCoordsCtr;
            internal Vector2D topRight;
            internal Vector2D textBottomRight;
            internal Vector2D textTopLeft;
            internal Vector2D leadLine;
            internal Vector3D worldCtr;
            internal double screenCoordsZ;
        }

        internal class rollupInfo
        {
            internal float velocity;
            internal float distance;
        }
    }
}