using OpenTK;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Minecraft
{
    public sealed class Player
        : IUpdatable
    {
        public bool IsFlying { set; get; }
        public Vector3 Position { set; get; }
        public Vector3 Velocity { set; get; }
        public float HAngle { set; get; }
        public float VAngle { set; get; }


        public void Update(double time, double delta)
        {
            // TODO
        }
    }
}
