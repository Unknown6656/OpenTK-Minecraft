using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace OpenTKMinecraft.Components
{
    public interface IUpdatable
    {
        void Update(double time, double delta);
    }

    public interface IVisuallyUpdatable
    {
        void Update(double time, double delta, float aspectratio);
    }

    public interface IRenderable
    {
        void Render(Camera camera, CameraRenderData data);
    }

    public abstract class GameObject
        : IDisposable
        , IUpdatable
    {
        private static ulong _gameobjectcounter;
        private protected Matrix4 _modelview;

        public Renderable Model { get; internal protected set; }
        public Vector3 Rotation { get; internal protected set; }
        public Vector3 Scale { get; internal protected set; }
        public Vector4 Direction { get; internal protected set; }
        public Vector4 Position { get; internal protected set; }
        public float Velocity { get; internal protected set; }
        public Matrix4 ModelView => _modelview;
        public ulong ID { get; }



        public GameObject(Renderable model, Vector4 pos, Vector4 dir, Vector3 rot, Vector3 scale, float vel)
        {
            Model = model;
            Position = pos;
            Direction = dir;
            Rotation = rot;
            Velocity = vel;
            Scale = scale;

            ID = _gameobjectcounter++;
        }

        public virtual void Update(double time, double delta)
        {
            Position += Direction * (Velocity * (float)delta);

            UpdateModelView();
        }

        private void UpdateModelView() =>
            _modelview = Matrix4.CreateScale(Scale.X, Scale.Y, Scale.Z)
                       * Matrix4.CreateRotationZ(Rotation.X)
                       * Matrix4.CreateRotationY(Rotation.Y)
                       * Matrix4.CreateRotationX(Rotation.Z)
                       * Matrix4.CreateTranslation(Position.X, Position.Y, Position.Z);

        public virtual void Render(Camera camera, CameraRenderData data)
        {
            Model.Program.Use();
            Model.Bind();

            UpdateModelView();

            camera.Render(this, data);
        }

        public void Dispose() => Model?.Dispose();
    }

    public abstract class Renderable
        : IDisposable
    {
        public ShaderProgram Program { get; internal protected set; }
        public int VertexArray { get; private protected set; }
        public int Buffer { get; private protected set; }
        public int VerticeCount { get; private protected set; }


        protected Renderable(ShaderProgram program, int vertexCount)
        {
            Program = program;
            VerticeCount = vertexCount;

            if (vertexCount > 0)
            {
                VertexArray = GL.GenVertexArray();
                Buffer = GL.GenBuffer();

                GL.BindVertexArray(VertexArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Buffer);
            }
        }

        public virtual void Bind()
        {
            Program.Use();

            if (VerticeCount > 0)
                GL.BindVertexArray(VertexArray);
        }

        public virtual void Render() => Program.Use();

        protected virtual void InitBuffer()
        {
        }

        public void Dispose()
        {
            Dispose(true);

            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && (VerticeCount > 0))
            {
                GL.DeleteVertexArray(VertexArray);
                GL.DeleteBuffer(Buffer);
            }
        }
    }
}
