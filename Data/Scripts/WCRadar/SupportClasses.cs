using ProtoBuf;
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
            internal Vector2D textTopLeft;
            internal Vector3D worldCtr;
            internal double screenCoordsZ;
        }
    }
    [ProtoContract]
    public class BB2D
    {
        [ProtoMember(1)]
        internal Vector2D Min { get; set; }
        [ProtoMember(2)]
        internal Vector2D Max { get; set; }

        internal BB2D New(Vector2D min, Vector2D max)
        {
            return new BB2D() { Min = min, Max = max };
        }

        internal void Update(Vector2D offsetMin, Vector2D offsetMax)
        {
            Min += offsetMin;
            Max += offsetMax;
        }
        internal bool Contains(double x, double y)
        {
            return !(Min.X > x) && !(x > Max.X) && !(Min.Y > y) && !(y > Max.Y);
        }
    }
}