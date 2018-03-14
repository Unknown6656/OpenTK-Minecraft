using System.Runtime.InteropServices;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

namespace OpenTKMinecraft.Components
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Light
    {
        private Vector4 _pos;
        private Vector4 _dir;
        private Color4 _col;
        private float _exp;
        private float _falloff;
        private LightMode _mode;
        private uint _is_act;


        public Vector3 Position
        {
            set => _pos = new Vector4(value, 1);
            get => _pos.Xyz;
        }

        public Vector3 Direction
        {
            set => _dir = new Vector4(value, 0);
            get => _dir.Xyz;
        }

        public Color4 Color
        {
            set => _col = new Color4(value.R, value.G, value.B, _col.A);
            get => new Color4(_col.R, _col.G, _col.B, 1);
        }

        public float Intensity
        {
            set => _col.A = value;
            get => _col.A;
        }

        public float Exponent
        {
            set => _exp = Math.Max(1, _exp);
            get => _exp;
        }

        public float Falloff
        {
            set => _falloff = Math.Max(0, _exp);
            get => _falloff;
        }

        public LightMode Mode
        {
            set => _mode = value;
            get => _mode;
        }

        public bool IsActive
        {
            set => _is_act = value ? 1u : 0;
            get => _is_act == 1;
        }


        public override string ToString() => $"({(IsActive ? "" : "in")}active) P:{Position}  D:{Direction}  C:{Color}  I:{Intensity}  E:{Exponent}  F:{Falloff}  M:{Mode}";

        public static Light CreateEnvironmentLight(Color4 color, float intensity = 1) => new Light
        {
            Falloff = 0,
            Exponent = 0,
            Color = color,
            IsActive = true,
            Intensity = intensity,
            Mode = LightMode.Ambient,
        };

        public static Light CreateDirectionalLight(Vector3 direction, Color4 color, float intensity = 1) => new Light
        {
            Falloff = 0,
            Exponent = 0,
            Color = color,
            IsActive = true,
            Direction = direction,
            Intensity = intensity,
            Mode = LightMode.Directional,
        };

        public static Light CreatePointLight(Vector3 position, Color4 color, float intensity = 1, float falloff = .02f) => new Light
        {
            Exponent = 0,
            Falloff = falloff,
            Color = color,
            IsActive = true,
            Position = position,
            Intensity = intensity,
            Mode = LightMode.PointLight,
        };

        public static Light CreateSpotLight(Vector3 position, Vector3 direction, Color4 color, float intensity = 1, float falloff = .02f, float exponent = 100) => new Light
        {
            Color = color,
            Falloff = falloff,
            Intensity = intensity,
            Direction = direction,
            Position = position,
            IsActive = true,
            Mode = LightMode.SpotLight,
            Exponent = exponent
        };
    }

    public enum LightMode
        : uint
    {
        Ambient = 0,
        PointLight = 1,
        SpotLight = 2,
        Directional = 3
    }

    public unsafe sealed class Lights
        : Renderable
    {
        public const int MAX_LIGHTS = 256;
        private readonly Light[] LightData = new Light[MAX_LIGHTS];
        private int _index, _buffer, _bindingpoint;

        private int FirstFreeIndex
        {
            get
            {
                int i = 0;

                while ((i < MAX_LIGHTS) && LightData[i].IsActive)
                    ++i;

                return i;
            }
        }

        public ref Light this[int i] => ref LightData[i];


        public Lights(ShaderProgram program)
            : base(program, 0)
        {
            program.Use();

            _buffer = GL.GenBuffer();
            _index = GL.GetUniformBlockIndex(program.ID, "LightBlock");
            _bindingpoint = 1;
        }

        public override void Bind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, _buffer);
            GL.BufferData(BufferTarget.UniformBuffer, sizeof(Light) * MAX_LIGHTS, LightData, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, _bindingpoint, _buffer);
            GL.UniformBlockBinding(Program.ID, _index, _bindingpoint);
        }

        public override void Render()
        {
            base.Render();

            GL.Uniform1(31, FirstFreeIndex);
        }

        public void Remove(int i)
        {
            if ((i < MAX_LIGHTS) && (i >= 0))
                LightData[i].IsActive = false;
        }

        public int Add(Light l)
        {
            int i = FirstFreeIndex;

            if (i >= MAX_LIGHTS)
                return -1;

            LightData[i] = l;

            return i;
        }

        protected override void Dispose(bool disposing)
        {
            GL.DeleteBuffer(_buffer);

            base.Dispose(disposing);
        }
    }
}
