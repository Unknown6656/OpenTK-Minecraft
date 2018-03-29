using System.Runtime.InteropServices;
using System.IO.Compression;
using System.IO;

using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Minecraft
{
    public interface IStorable
    {
        void Store(BinaryWriter w);
        void Read(BinaryReader r);
    }

    public static class IO
    {
        private static int BUFFER_SIZE = 64 * 1024;


        public static T FromBinary<T>(byte[] array)
            where T : IStorable, new()
        {
            using (MemoryStream ms = new MemoryStream(array))
                return FromBinary<T>(ms);
        }

        public static T FromBinary<T>(Stream stream)
            where T : IStorable, new()
        {
            using (BinaryReader r = new BinaryReader(stream))
                return FromBinary<T>(r);
        }

        public static T FromBinary<T>(BinaryReader rd)
            where T : IStorable, new()
        {
            T t = new T();

            t.Read(rd);

            return t;
        }

        public static void StoreBinary<T>(this T instance, Stream stream)
            where T : IStorable
        {
            using (BinaryWriter w = new BinaryWriter(stream))
                instance.Store(w);
        }

        public static byte[] StoreBinary<T>(this T instance)
            where T : IStorable
        {
            using (MemoryStream ms = new MemoryStream())
            {
                StoreBinary(instance, ms);

                return ms.ToArray();
            }
        }

        public static Vector3 ReadV3(this BinaryReader r)
        {
            Vector3 v = Vector3.Zero;

            v.X = r.ReadSingle();
            v.Y = r.ReadSingle();
            v.Z = r.ReadSingle();

            return v;
        }

        public static void WriteV3(this BinaryWriter w, Vector3 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
        }

        public static Vector4 ReadV4(this BinaryReader r)
        {
            Vector4 v = Vector4.Zero;

            v.X = r.ReadSingle();
            v.Y = r.ReadSingle();
            v.Z = r.ReadSingle();
            v.W = r.ReadSingle();

            return v;
        }

        public static void WriteV4(this BinaryWriter w, Vector4 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
            w.Write(v.W);
        }

        public static Color4 ReadC4(this BinaryReader r)
        {
            Color4 c = Color4.Transparent;

            c.A = r.ReadSingle();
            c.R = r.ReadSingle();
            c.G = r.ReadSingle();
            c.B = r.ReadSingle();

            return c;
        }

        public static void WriteC4(this BinaryWriter w, Color4 c)
        {
            w.Write(c.A);
            w.Write(c.R);
            w.Write(c.G);
            w.Write(c.B);
        }

        public static Matrix4 ReadM4(this BinaryReader r)
        {
            Vector4 r0 = r.ReadV4();
            Vector4 r1 = r.ReadV4();
            Vector4 r2 = r.ReadV4();
            Vector4 r3 = r.ReadV4();

            return new Matrix4(r0, r1, r2, r3);
        }

        public static void WriteM4(this BinaryWriter w, Matrix4 m)
        {
            w.WriteV4(m.Row0);
            w.WriteV4(m.Row1);
            w.WriteV4(m.Row2);
            w.WriteV4(m.Row3);
        }

        public static byte[] Serialize(this Scene sc)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter wr = new BinaryWriter(ms))
            {
                sc.Store(wr);

                return ms.ToArray();
            }
        }

        public static void Deserialize(this Scene sc, byte[] arr)
        {
            using (MemoryStream ms = new MemoryStream(arr))
            using (BinaryReader rd = new BinaryReader(ms))
                sc.Read(rd);
        }

        public static byte[] Unzip(byte[] arr)
        {
            using (MemoryStream ms = new MemoryStream(arr))
            using (MemoryStream os = new MemoryStream())
            {
                using (BufferedStream zip = new BufferedStream(new GZipStream(ms, CompressionMode.Decompress), BUFFER_SIZE))
                    zip.CopyTo(os);

                return os.ToArray();
            }
        }

        public static byte[] Zip(byte[] arr)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress))
                using (BufferedStream bs = new BufferedStream(zip, BUFFER_SIZE))
                    bs.Write(arr, 0, arr.Length);

                return ms.ToArray();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RenderableBlockData
        : IStorable
    {
        public long X;
        public long Y;
        public long Z;
        public Vector3 Pos;
        public Vector3 Dir;
        public Vector3 Rot;
        public Vector3 Scl;
        public float Vel;
        public BlockMaterial Mat;
        public Vector3? AABBMin;
        public Vector3? AABBMax;


        public RenderableBlockData((long x, long y, long z) c, (long x, long y, long z) b, RenderableBlock r)
        {
            X = c.x * Chunk.CHUNK_SIZE * b.x;
            Y = c.y * Chunk.CHUNK_SIZE * b.y;
            Z = c.z * Chunk.CHUNK_SIZE * b.z;
            Scl = r.Scale;
            Mat = r.Material;
            Vel = r.Velocity;
            Pos = r.Position.Xyz;
            Dir = r.Direction.Xyz;
            Rot = r.Rotation;
            AABBMax = r.AABB?.Max;
            AABBMin = r.AABB?.Min;
        }

        public void Read(BinaryReader r)
        {
            X = r.ReadInt64();
            Y = r.ReadInt64();
            Z = r.ReadInt64();
            Mat = (BlockMaterial)r.ReadInt32();
            Vel = r.ReadSingle();
            Pos = r.ReadV3();
            Dir = r.ReadV3();
            Rot = r.ReadV3();
            Scl = r.ReadV3();

            if (r.ReadByte() == 0xff)
            {
                AABBMin = r.ReadV3();
                AABBMax = r.ReadV3();
            }
            else
            {
                AABBMin = null;
                AABBMax = null;
            }
        }

        public void Store(BinaryWriter w)
        {
            w.Write(X);
            w.Write(Y);
            w.Write(Z);
            w.Write((int)Mat);
            w.Write(Vel);
            w.WriteV3(Pos);
            w.WriteV3(Dir);
            w.WriteV3(Rot);
            w.WriteV3(Scl);

            if (AABBMin is Vector3 min && AABBMax is Vector3 max)
            {
                w.Write((byte)0xff);
                w.WriteV3(min);
                w.WriteV3(max);
            }
            else
                w.Write((byte)0x00);
        }

        public void SpawnInto(World world)
        {
            RenderableBlock block = world[X, Y, Z];

            block.Scale = Scl;
            block.Material = Mat;
            block.Rotation = Rot;
            block.Velocity = Vel;
            block.Position = new Vector4(Pos, 1);
            block.Direction = new Vector4(Dir, 0);
            block.AABB = AABBMax is Vector3 max && AABBMin is Vector3 min ? ((Vector3, Vector3)?)(max, min) : null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LightData
        : IStorable
    {
        public Vector3 Pos;
        public Vector3 Dir;
        public Color4 Col;
        public float Exp;
        public float Fao;
        public LightMode Mod;


        public LightData(Light l)
        {
            Pos = l.Position;
            Dir = l.Direction;
            Col = l.Color;
            Exp = l.Exponent;
            Fao = l.Falloff;
            Mod = l.Mode;
        }

        public void Read(BinaryReader r)
        {
            Pos = r.ReadV3();
            Dir = r.ReadV3();
            Col = r.ReadC4();
            Exp = r.ReadSingle();
            Fao = r.ReadSingle();
            Mod = (LightMode)r.ReadInt32();
        }

        public void Store(BinaryWriter w)
        {
            w.WriteV3(Pos);
            w.WriteV3(Dir);
            w.WriteC4(Col);
            w.Write(Exp);
            w.Write(Fao);
            w.Write((int)Mod);
        }

        public void TransferTo(out Light l) => l = new Light
        {
            Position = Pos,
            Direction = Dir,
            Color = Col,
            Exponent = Exp,
            Falloff = Fao,
            Mode = Mod,
            IsActive = true
        };
    }
}
