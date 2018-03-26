using System.Collections.Generic;
using System.Linq;
using System;

using OpenTKMinecraft.Components;
using OpenTKMinecraft.Utilities;

using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace OpenTKMinecraft.Minecraft
{
    using static Chunk;


    public sealed class World
        : IUpdatable
        , IRenderable
        , IDisposable
    {
        private Chunk[] _chunks; // for speed-up only

        public Dictionary<(long x, long y, long z), Chunk> Chunks { get; }
        public Scene Scene { get; }

        public RenderableBlock this[long x, long y, long z]
        {
            get => ChunkFunction(x, y, z, (c, _x, _y, _z) => c[_x, _y, _z]);
            set => ChunkFunction(x, y, z, (c, _x, _y, _z) => c[_x, _y, _z] = value);
        }


        internal World(Scene scene)
        {
            Scene = scene;
            Chunks = new Dictionary<(long, long, long), Chunk>();
        }

        private T ChunkFunction<T>(long x, long y, long z, Func<Chunk, long, long, long, T> func)
        {
            (long cx, long cy, long cz, long _x, long _y, long _z) = (x / CHUNK_SIZE, y / CHUNK_SIZE, z / CHUNK_SIZE, x % CHUNK_SIZE, y % CHUNK_SIZE, z % CHUNK_SIZE);
            bool dirty = false;
            Chunk c;

            _x = (_x + CHUNK_SIZE) % CHUNK_SIZE;
            _y = (_y + CHUNK_SIZE) % CHUNK_SIZE;
            _z = (_z + CHUNK_SIZE) % CHUNK_SIZE;

            if (x < 0) --cx;
            if (y < 0) --cy;
            if (z < 0) --cz;

            if (!Chunks.ContainsKey((cx, cy, cz)))
            {
                Chunks[(cx, cy, cz)] = c = new SparseChunk(this, cx, cy, cz);
                dirty = true;
            }
            else
                c = Chunks[(cx, cy, cz)];

            int cnt1 = c.BlockCount;

            T res = func(c, _x, _y, _z);

            int cnt2 = c.BlockCount;

            if ((cnt1 < CHUNK_SWITCH_COUNT) && (cnt2 >= CHUNK_SWITCH_COUNT))
            {
                (c as SparseChunk)?.TransferTo(Chunks[(cx, cy, cz)] = new DenseChunk(this, cx, cy, cz));

                dirty = true;
            }
            else if ((cnt1 >= CHUNK_SWITCH_COUNT) && (cnt2 < CHUNK_SWITCH_COUNT))
            {
                (c as DenseChunk)?.TransferTo(Chunks[(cx, cy, cz)] = new SparseChunk(this, cx, cy, cz));

                dirty = true;
            }

            if (dirty)
                _chunks = Chunks.Values.ToArray();

            return res;
        }

        private void ChunkFunction(long x, long y, long z, Action<Chunk, long, long, long> func) =>
            ChunkFunction(x, y, z, (c, _x, _y, _z) =>
            {
                func(c, _x, _y, _z);

                return false;
            });

        public void Render(Camera camera, CameraRenderData data)
        {
            foreach (Chunk c in _chunks)
                c.Render(camera, data);
        }

        public void Update(double time, double delta)
        {
            foreach (Chunk c in _chunks)
                c.Update(time, delta);
        }

        public CustomBlock PlaceCustomBlock(long x, long y, long z, WavefrontFile m) =>
            ChunkFunction(x, y, z, (c, _x, _y, _z) => c.PlaceCustomBlock(_x, _y, _z, m));

        public void Dispose()
        {
            foreach (Chunk c in Chunks?.Values)
                c?.Dispose();
        }

        public void RemoveBlock(long x, long y, long z) =>
            ChunkFunction(x, y, z, (c, _x, _y, _z) => c.RemoveBlock(_x, _y, _z));

        public RenderableBlock Raymarch(Vector3 position, Vector3 direction, float maxdist = 8)
        {
            direction = Vector3.Normalize(direction) / 2;
            maxdist = Math.Max(0, Math.Min(maxdist, 128));

            float dist = 0;
            float step = direction.Length;

            while (dist < maxdist)
            {
                // TODO : check for hit with AABB and return block


                position += direction;
                dist += step;
            }

            return null;
        }
    }

    public abstract class Chunk
        : IUpdatable
        , IRenderable
        , IDisposable
    {
        public const int CHUNK_SWITCH_COUNT = (int)(CHUNK_SIZE * CHUNK_SIZE);
        public const long CHUNK_SIZE = 16;

        public int BlockCount { get; private set; }
        public World World { get; }
        public long XIndex { get; }
        public long YIndex { get; }
        public long ZIndex { get; }

        public RenderableBlock this[long xloc, long yloc, long zloc]
        {
            internal set
            {
                RenderableBlock old = GetBlock(xloc, yloc, zloc, true);

                if (old is null && value != null)
                    ++BlockCount;
                else if (old != null && value is null)
                    --BlockCount;

                SetBlock(xloc, yloc, zloc, value);
            }
            get => GetBlock(xloc, yloc, zloc);
        }


        internal Chunk(World world, long ix, long iy, long iz)
        {
            BlockCount = 0;
            World = world;
            XIndex = ix;
            YIndex = iy;
            ZIndex = iz;
        }

        protected abstract void SetBlock(long xloc, long yloc, long zloc, RenderableBlock b);

        protected abstract RenderableBlock GetBlock(long xloc, long yloc, long zloc, bool retnull = false);

        public abstract void Update(double time, double delta);

        public abstract void Render(Camera camera, CameraRenderData data);

        public CustomBlock PlaceCustomBlock(long x, long y, long z, WavefrontFile m)
        {
            CustomBlock b = new CustomBlock(m, World, this, (XIndex * CHUNK_SIZE) + x, (YIndex * CHUNK_SIZE) + y, (ZIndex * CHUNK_SIZE) + z);

            this[x, y, z]?.Dispose();
            this[x, y, z] = b;

            return b;
        }

        protected abstract void InternalDispose();

        public void Dispose()
        {
            InternalDispose();

            BlockCount = 0;
        }

        internal MinecraftBlock GetDefaultBlock(long x, long y, long z) => new MinecraftBlock(World, this, (XIndex * CHUNK_SIZE) + x, (YIndex * CHUNK_SIZE) + y, (ZIndex * CHUNK_SIZE) + z);

        internal void RemoveBlock(long x, long y, long z)
        {
            this[x, y, z]?.Dispose();
            this[x, y, z] = null;
        }
    }

    public sealed class SparseChunk
        : Chunk
    {
        private readonly Dictionary<(long, long, long), RenderableBlock> _blocks = new Dictionary<(long, long, long), RenderableBlock>();


        public SparseChunk(World world, long ix, long iy, long iz)
            : base(world, ix, iy, iz)
        {
        }

        protected override void InternalDispose()
        {
            foreach ((long, long, long) key in _blocks.Keys.ToArray())
            {
                _blocks[key]?.Dispose();
                _blocks.Remove(key);
            }
        }

        public override void Render(Camera camera, CameraRenderData data)
        {
            foreach (RenderableBlock b in _blocks.Values.ToArray())
                b.Render(camera, data);
        }

        public override void Update(double time, double delta)
        {
            foreach (RenderableBlock b in _blocks.Values)
                b.Update(time, delta);
        }

        protected override void SetBlock(long xloc, long yloc, long zloc, RenderableBlock b) => _blocks[(xloc, yloc, zloc)] = b;

        protected override RenderableBlock GetBlock(long xloc, long yloc, long zloc, bool retnull = false)
        {
            (long, long, long) pos = (xloc, yloc, zloc);
            RenderableBlock b = _blocks.ContainsKey(pos) ? _blocks[pos] : null;

            if (b is null)
                return retnull ? null : this[xloc, yloc, zloc] = GetDefaultBlock(xloc, yloc, zloc);
            else
                return b;
        }

        internal void TransferTo(Chunk chunk)
        {
            foreach (var kvp in _blocks)
            {
                (long x, long y, long z) = kvp.Key;

                chunk[x, y, z] = kvp.Value;
            }
        }
    }

    public sealed class DenseChunk
        : Chunk
    {
        public RenderableBlock[,,] Blocks { get; }


        public DenseChunk(World world, long ix, long iy, long iz)
            : base(world, ix, iy, iz) => Blocks = new RenderableBlock[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];

        public override void Update(double time, double delta)
        {
            for (int x = 0; x<CHUNK_SIZE; ++x)
                for (int z = 0; z<CHUNK_SIZE; ++z)
                    for (int y = 0; y<CHUNK_SIZE; ++y)
                        if (Blocks[x, y, z] is RenderableBlock b)
                            b.Update(time, delta);
        }

        public override void Render(Camera camera, CameraRenderData data)
        {
            for (int x = 0; x<CHUNK_SIZE; ++x)
                for (int z = 0; z<CHUNK_SIZE; ++z)
                    for (int y = 0; y<CHUNK_SIZE; ++y)
                        if (Blocks[x, y, z] is RenderableBlock b)
                            b.Render(camera, data);
        }

        protected override void InternalDispose()
        {
            for (int x = 0; x < CHUNK_SIZE; ++x)
                for (int z = 0; z < CHUNK_SIZE; ++z)
                    for (int y = 0; y < CHUNK_SIZE; ++y)
                    {
                        Blocks[x, y, z]?.Dispose();
                        Blocks[x, y, z] = null;
                    }
        }

        protected override void SetBlock(long xloc, long yloc, long zloc, RenderableBlock b) => Blocks[xloc, yloc, zloc] = b;

        protected override RenderableBlock GetBlock(long xloc, long yloc, long zloc, bool retnull = false)
        {
            RenderableBlock b = Blocks[xloc, yloc, zloc];

            if (b is null)
                return retnull ? null : this[xloc, yloc, zloc] = GetDefaultBlock(xloc, yloc, zloc);

            return b;
        }

        internal void TransferTo(Chunk chunk)
        {
            for (int x = 0; x < CHUNK_SIZE; ++x)
                for (int z = 0; z < CHUNK_SIZE; ++z)
                    for (int y = 0; y < CHUNK_SIZE; ++y)
                        if (Blocks[x, y, z] is RenderableBlock b)
                            chunk[x, y, z] = b;
        }
    }

    public abstract class RenderableBlock
        : GameObject
        , IRenderable
    {
        private BlockMaterial _mat;
        private int _lightindex = -1;
        private float _falling;

        public (Vector3 Min, Vector3 Max)? AABB { get; private protected set; }
        public Chunk Chunk { get; }
        public World World { get; }
        public long X { get; }
        public long Y { get; }
        public long Z { get; }

        public BlockMaterial Material
        {
            get => _mat;
            set
            {
                if (BlockInfo.Blocks[_mat].IsActivelyGlowing)
                    World.Scene.Lights.Remove(_lightindex);

                BlockInfo nfo = BlockInfo.Blocks[_mat = value];

                Texture.UpdateTexture(false, value, nfo.Textures);

                if (!IsSolid)
                    AABB = null;

                _lightindex = nfo.IsActivelyGlowing ? World.Scene.Lights.Add(nfo.CreateAssociatedLight(this), this) : -1;
            }
        }

        public Vector3 Center => AABB is null ? new Vector3(X, Y, Z) : (AABB.Value.Max + AABB.Value.Min) / 2;

        public BlockInfo MaterialInfo => BlockInfo.Blocks[Material];

        internal TextureSet Texture => (Model as TexturedVertexSet)?.TextureSet;

        public bool HasBlockBelow => World[X, Y - 1, Z]?.IsSolid ?? false;

        public bool IsSolid => Material != BlockMaterial.Air;


        internal RenderableBlock(Renderable model, World w, Chunk c, long x, long y, long z)
            : this(model, w, c, x, y, z, BlockMaterial.Air)
        {
        }

        internal RenderableBlock(Renderable model, World w, Chunk c, long x, long y, long z, BlockMaterial matr)
            : base(model, new Vector4(x, y, z, 1), Vector4.Zero, Vector3.Zero, new Vector3(1), 0)
        {
            Material = matr;
            World = w;
            Chunk = c;
            X = x;
            Y = y;
            Z = z;
        }

        public void SetRotation(float x, float y, float z) => Rotation = new Vector3(x, y, z);

        public void SetScale(float x, float y, float z) => Scale = new Vector3(x, y, z);

        public void Move(float δx, float δy, float δz) => Position += new Vector4(δx, δy, δz, 0);

        public bool HasCollision(Vector3 point)
        {
            if (AABB is ValueTuple<Vector3, Vector3> aabb)
            {
                Vector3 min = aabb.Item1;
                Vector3 max = aabb.Item2;

                return (point.X >= min.X)
                    && (point.Y >= min.Y)
                    && (point.Z >= min.Z)
                    && (point.X <= max.X)
                    && (point.Y <= max.Y)
                    && (point.Z <= max.Z);
            }
            else
                return false;
        }

        public override void Update(double time, double delta)
        {
            if ((_falling > 0) || MaterialInfo.Gravity)
            {
                if (!HasBlockBelow)
                {
                    _falling = Math.Min(_falling, .025f);
                    _falling *= (float)(1 + delta * 9.81 * _falling);

                    Vector4 npos = Position - _falling * Vector4.UnitY;
                    long nx = (long)npos.X;
                    long ny = (long)npos.Y;
                    long nz = (long)npos.Z;

                    while (World[nx, ny, nz].IsSolid)
                        ++ny;

                    RenderableBlock newblock = World[nx, ny, nz];

                    newblock._falling = 0;

                    MoveDataTo(newblock, time, delta);
                }
            }
            else
                _falling = 0;

            base.Update(time, delta);
        }

        public override string ToString() => $"({X}, {Y}, {Z}) {Material}  [{base.ToString()}]";

        private void MoveDataTo(RenderableBlock newblock, double time, double delta)
        {
            newblock.Material = Material;
            newblock.Update(time, delta);

            Delete();
        }

        public void Delete() => World.RemoveBlock(X, Y, Z);
    }

    public class MinecraftBlock
        : RenderableBlock
    {
        private static readonly Vertex[] _vertices = ObjectFactory.CreateTexturedQuadCube();


        public MinecraftBlock(World w, Chunk c, long x, long y, long z)
            : this(w, c, x, y, z, BlockMaterial.Air)
        {
        }

        public MinecraftBlock(World w, Chunk c, long x, long y, long z, BlockMaterial matr)
            : base(new TexturedVertexSet(_vertices, PrimitiveType.Quads, w.Scene.Program, matr), w, c, x, y, z, matr)
        {
        }

        public override void Update(double time, double delta)
        {
            base.Update(time, delta);

            if (IsSolid)
            {
                Vector3 max = Vector3.Zero;
                Vector3 min = Vector3.Zero;
                Matrix3 transf = Matrix3.CreateScale(Scale.X, Scale.Y, Scale.Z)
                               * Matrix3.CreateRotationZ(Rotation.X)
                               * Matrix3.CreateRotationY(Rotation.Y)
                               * Matrix3.CreateRotationX(Rotation.Z);

                for (int i = 0; i < 8; ++i)
                {
                    Vector3 vec = new Vector3(
                        (i & 1) != 0 ? -.5f : .5f,
                        (i & 2) != 0 ? -.5f : .5f,
                        (i & 4) != 0 ? -.5f : .5f
                    ) + Position.Xyz;

                    vec = transf * vec;
                    min = Vector3.ComponentMin(min, vec);
                    max = Vector3.ComponentMax(max, vec);
                }

                AABB = (min, max);
            }
            else
                AABB = null;
        }
    }

    public class CustomBlock
        : RenderableBlock
    {
        public CustomBlock(WavefrontFile model, World w, Chunk c, long x, long y, long z)
            : this(model, w, c, x, y, z, BlockMaterial.Air)
        {
        }

        public CustomBlock(WavefrontFile model, World w, Chunk c, long x, long y, long z, BlockMaterial matr)
            : base(new OBJModel(w.Scene.Program, model, new Vector4(x, y, z, 0), Vector3.Zero).Model, w, c, x, y, z, matr) => AABB = null;
    }
}
