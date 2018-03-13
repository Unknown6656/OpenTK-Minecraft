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
            get
            {
                (long cx, long cy, long cz, long _x, long _y, long _z) = UpdateChunk(x, y, z);

                return Chunks[(cx, cy, cz)][_x, _y, _z];
            }
            set
            {
                (long cx, long cy, long cz, long _x, long _y, long _z) = UpdateChunk(x, y, z);

                Chunks[(cx, cy, cz)][_x, _y, _z] = value;
            }
        }

        internal World(Scene scene)
        {
            Scene = scene;
            Chunks = new Dictionary<(long, long, long), Chunk>();
        }

        private (long, long, long, long, long, long) UpdateChunk(long x, long y, long z)
        {
            (long cx, long cy, long cz, long _x, long _y, long _z) = (x / CHUNK_SIZE, y / CHUNK_SIZE, z / CHUNK_SIZE, x % CHUNK_SIZE, y % CHUNK_SIZE, z % CHUNK_SIZE);

            _x = (_x + CHUNK_SIZE) % CHUNK_SIZE;
            _y = (_y + CHUNK_SIZE) % CHUNK_SIZE;
            _z = (_z + CHUNK_SIZE) % CHUNK_SIZE;

            if (x < 0) --cx;
            if (y < 0) --cy;
            if (z < 0) --cz;

            if (!Chunks.ContainsKey((cx, cy, cz)))
            {
                Chunks[(cx, cy, cz)] = new Chunk(this, cx, cy, cz);

                _chunks = Chunks.Values.ToArray();
            }

            return (cx, cy, cz, _x, _y, _z);
        }

        public void Render(Camera camera)
        {
            foreach (Chunk c in _chunks)
                c.Render(camera);
        }

        public void Update(double time, double delta)
        {
            foreach (Chunk c in _chunks)
                c.Update(time, delta);
        }

        public CustomBlock PlaceCustomBlock(long x, long y, long z, WavefrontFile m)
        {
            (long cx, long cy, long cz, long _x, long _y, long _z) = UpdateChunk(x, y, z);

            return Chunks[(cx, cy, cz)].PlaceCustomBlock(_x, _y, _z, m);
        }

        public void Dispose()
        {
            foreach (Chunk c in Chunks?.Values)
                c?.Dispose();
        }
    }

    public sealed class Chunk
        : IUpdatable
        , IRenderable
        , IDisposable
    {
        public const long CHUNK_SIZE = 16;

        public RenderableBlock[,,] Blocks { get; }
        public World World { get; }
        public long XIndex { get; }
        public long YIndex { get; }
        public long ZIndex { get; }

        internal RenderableBlock this[long xloc, long yloc, long zloc]
        {
            set => Blocks[xloc, yloc, zloc] = value;
            get
            {
                if (Blocks[xloc, yloc, zloc] is null)
                    Blocks[xloc, yloc, zloc] = new MinecraftBlock(World, (XIndex * CHUNK_SIZE) + xloc, (YIndex * CHUNK_SIZE) + yloc, (ZIndex * CHUNK_SIZE) + zloc);

                return Blocks[xloc, yloc, zloc];
            }
        }


        internal Chunk(World world, long ix, long iy, long iz)
        {
            Blocks = new RenderableBlock[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
            World = world;
            XIndex = ix;
            YIndex = iy;
            ZIndex = iz;
        }

        public void Update(double time, double delta)
        {
            for (int x = 0; x < CHUNK_SIZE; ++x)
                for (int z = 0; z < CHUNK_SIZE; ++z)
                    for (int y = 0; y < CHUNK_SIZE; ++y)
                        if (Blocks[x, y, z] is RenderableBlock b)
                            b.Update(time, delta);
        }

        public void Render(Camera camera)
        {
            for (int x = 0; x < CHUNK_SIZE; ++x)
                for (int z = 0; z < CHUNK_SIZE; ++z)
                    for (int y = 0; y < CHUNK_SIZE; ++y)
                        if (Blocks[x, y, z] is RenderableBlock b)
                            b.Render(camera);
        }

        public CustomBlock PlaceCustomBlock(long x, long y, long z, WavefrontFile m)
        {
            CustomBlock b = new CustomBlock(m, World, (XIndex * CHUNK_SIZE) + x, (YIndex * CHUNK_SIZE) + y, (ZIndex * CHUNK_SIZE) + z);

            Blocks[x, y, z]?.Dispose();
            Blocks[x, y, z] = b;

            return b;
        }

        public void Dispose()
        {
            for (int x = 0; x < CHUNK_SIZE; ++x)
                for (int z = 0; z < CHUNK_SIZE; ++z)
                    for (int y = 0; y < CHUNK_SIZE; ++y)
                        Blocks[x, y, z]?.Dispose();
        }
    }

    public abstract class RenderableBlock
        : GameObject
        , IRenderable
    {
        private BlockMaterial _mat;

        internal TextureSet Texture => (Model as TexturedVertexSet)?._tex;
        public bool ApplyGravity { private set; get; }
        public World World { get; }

        public BlockMaterial Material
        {
            get => _mat;
            set
            {
                BlockInfo nfo = BlockInfo.Blocks[_mat = value];

                Texture.UpdateTexture(nfo.Textures);
                ApplyGravity = nfo.Gravity;
            }
        }


        internal RenderableBlock(Renderable model, World w, long x, long y, long z)
            : this(model, w, x, y, z, BlockMaterial.Air)
        {
        }

        internal RenderableBlock(Renderable model, World w, long x, long y, long z, BlockMaterial matr)
            : base(model, new Vector4(x, y, z, 1), Vector4.Zero, Vector4.Zero, 0)
        {
            Material = matr;
            World = w;
        }

        public void SetRotation(float x, float y, float z) => Rotation = new Vector4(x, y, z, 0);

        public override void Update(double time, double delta)
        {
            {
                // TODO : gravity update
            }

            base.Update(time, delta);
        }
    }

    public class MinecraftBlock
        : RenderableBlock
    {
        public MinecraftBlock(World w, long x, long y, long z)
            : this(w, x, y, z, BlockMaterial.Air)
        {
        }

        public MinecraftBlock(World w, long x, long y, long z, BlockMaterial matr)
            : base(new TexturedVertexSet(ObjectFactory.CreateTexturedQuadCube(1), PrimitiveType.Quads, w.Scene.Program), w, x, y, z, matr)
        {
        }
    }

    public class CustomBlock
        : RenderableBlock
    {
        public CustomBlock(WavefrontFile model, World w, long x, long y, long z)
            : this(model, w, x, y, z, BlockMaterial.Air)
        {
        }

        public CustomBlock(WavefrontFile model, World w, long x, long y, long z, BlockMaterial matr)
            : base(new OBJModel(w.Scene.Program, model, new Vector4(x, y, z, 0), Vector4.Zero).Model, w, x, y, z, matr)
        {
        }
    }
}
