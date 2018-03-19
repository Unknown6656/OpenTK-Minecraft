using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK;

using OpenTKMinecraft.Minecraft;
using OpenTKMinecraft.Native;

namespace OpenTKMinecraft.Components
{
    using static SHADER_BIND_LOCATIONS;


    public class TexturedVertexSet
        : Renderable
    {
        public PrimitiveType PrimitiveType { set; get; }
        internal TextureSet TextureSet { get; }


        unsafe public TexturedVertexSet(Vertex[] vertices, PrimitiveType type, ShaderProgram program, params (string Path, TextureType Type)[] textures)
            : this(vertices, type, program, null, textures)
        {
        }

        unsafe public TexturedVertexSet(Vertex[] vertices, PrimitiveType type, ShaderProgram program, BlockMaterial? mat, params (string Path, TextureType Type)[] textures)
            : base(program, vertices.Length)
        {
            Vector3[] tangents = new Vector3[vertices.Length];
            Vector3[] bitangents = new Vector3[vertices.Length];

            if (type == PrimitiveType.Triangles)
            {
                int[] perm = { 1, 1, 2 };

                for (int i = 0; i < vertices.Length; i += 3)
                    for (int j = 0; j < 3; ++j)
                    {
                        Vector3 tang = Vector3.Normalize(vertices[i + perm[j]].position - vertices[i].position);

                        bitangents[i + j] = Vector3.Normalize(Vector3.Cross(vertices[i + j].normal, tang));
                        tangents[i + j] = Vector3.Normalize(Vector3.Cross(bitangents[i + j], vertices[i + j].normal));
                    }
            }
            // else if (type == PrimitiveType.Quads)
            //      for (int i = 0; i < vertices.Length; i += 4)
            //      {
            //          Vector3 tang1 = Vector3.Normalize(vertices[i + 1].position - vertices[i].position);
            //          Vector3 tang2 = Vector3.Normalize(vertices[i + 2].position - vertices[i + 3].position);
            //
            //          Vector3 bitang1 = Vector3.Normalize(Vector3.Cross(vertices[i].normal, tang1));
            //          Vector3 bitang2 = Vector3.Normalize(Vector3.Cross(vertices[i + 3].normal, tang2));
            //
            //          for (int j = 0; j < 4; ++j)
            //              (vertices[i + j].tangent, vertices[i + j].bitangent) = j < 2 ? (tang1, bitang1) : (tang2, bitang2);
            //      }
            else if (type != PrimitiveType.Quads)
                throw new NotImplementedException($"The primitive type '{type}' is currently not yet supported by the tangent space calculator.");

            GL.NamedBufferStorage(Buffer, sizeof(Vertex) * vertices.Length, vertices, BufferStorageFlags.MapWriteBit);
            GL.VertexArrayAttribBinding(VertexArray, SCENE_VERTEX_POSITION, 0);
            GL.EnableVertexArrayAttrib(VertexArray, SCENE_VERTEX_POSITION);
            GL.VertexArrayAttribFormat(VertexArray, SCENE_VERTEX_POSITION, 3, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(VertexArray, SCENE_VERTEX_NORMAL, 0);
            GL.EnableVertexArrayAttrib(VertexArray, SCENE_VERTEX_NORMAL);
            GL.VertexArrayAttribFormat(VertexArray, SCENE_VERTEX_NORMAL, 3, VertexAttribType.Float, false, 12);
            GL.VertexArrayAttribBinding(VertexArray, SCENE_VERTEX_COLOR, 0);
            GL.EnableVertexArrayAttrib(VertexArray, SCENE_VERTEX_COLOR);
            GL.VertexArrayAttribFormat(VertexArray, SCENE_VERTEX_COLOR, 4, VertexAttribType.Float, false, 24);
            GL.VertexArrayAttribBinding(VertexArray, SCENE_VERTEX_TANGENT, 0);
            GL.EnableVertexArrayAttrib(VertexArray, SCENE_VERTEX_TANGENT);
            GL.VertexArrayAttribFormat(VertexArray, SCENE_VERTEX_TANGENT, 3, VertexAttribType.Float, false, 40);
            GL.VertexArrayAttribBinding(VertexArray, SCENE_VERTEX_BITANGENT, 0);
            GL.EnableVertexArrayAttrib(VertexArray, SCENE_VERTEX_BITANGENT);
            GL.VertexArrayAttribFormat(VertexArray, SCENE_VERTEX_BITANGENT, 3, VertexAttribType.Float, false, 52);
            GL.VertexArrayVertexBuffer(VertexArray, 0, Buffer, IntPtr.Zero, sizeof(Vertex));

            PrimitiveType = type;

            if (mat is BlockMaterial m && TextureSet.KnownTextures.ContainsKey(m))
                TextureSet = TextureSet.KnownTextures[m];
            else
                TextureSet = new TextureSet(program, mat, textures);
        }

        public override void Bind()
        {
            base.Bind();

            TextureSet.Bind();
        }

        public override void Render()
        {
            if (VerticeCount > 0)
                GL.DrawArrays(PrimitiveType, 0, VerticeCount);
        }

        protected override void Dispose(bool disposing)
        {
            TextureSet.Dispose();

            base.Dispose(disposing);
        }
    }

    public sealed class TextureSet
        : Renderable
    {
        private const int SZCNT = 4;

        internal static readonly Dictionary<BlockMaterial, TextureSet> KnownTextures = new Dictionary<BlockMaterial, TextureSet>();
        private static readonly List<int> _disposed = new List<int>();
        private readonly Bitmap[] _imgs = new Bitmap[SZCNT * SZCNT];
        private int _size = int.MaxValue;

        public BlockMaterial AssociatedMaterial { get; private set; }
        public int TextureID { get; private set; }


        ~TextureSet() => Dispose();

        internal TextureSet(ShaderProgram program, BlockMaterial? assoc, params (string Path, TextureType Type)[] textures)
            : base(program, 0)
        {
            TextureID = -1;

            UpdateTexture(false, assoc, textures);
        }

        internal void UpdateTexture(BlockMaterial? assoc, params (string Path, TextureType Type)[] textures) => UpdateTexture(true, assoc, textures);

        private void UpdateTexture(bool forced, BlockMaterial? assoc, params (string Path, TextureType Type)[] textures)
        {
            if (assoc is BlockMaterial m)
            {
                if (m == BlockMaterial.Air)
                    return;

                AssociatedMaterial = m;

                if (KnownTextures.ContainsKey(m))
                    if (forced)
                        KnownTextures[m]?.Dispose();
                    else
                    {
                        TextureID = KnownTextures[m].TextureID;

                        return;
                    }

                KnownTextures[m] = this;
            }

            if (TextureID != -1)
                GL.DeleteTexture(TextureID);

            foreach ((string Path, TextureType Type) in textures)
                if ((Type >= 0) && ((int)Type < _imgs.Length))
                    try
                    {
                        if (Path is string s && Image.FromFile(s.Trim()) is Bitmap bmp)
                        {
                            if (bmp.Width != bmp.Height)
                                throw null;

                            _imgs[(int)Type] = bmp;
                            _size = Math.Min(_size, bmp.Width);
                        }
                    }
                    catch
                    {
                        throw new ArgumentException("All texture images must have a valid source path and be squared.", nameof(textures));
                    }

            if (_size < 1)
                _size = 16;
            else if (_size > 16384)
                _size = 16384;

            TextureID = InitTexture();
        }

        public override void Bind()
        {
            GL.Uniform1(SCENE_VERTEX_TEXTURESIZE, _size);
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        [HandleProcessCorruptedStateExceptions]
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed.Contains(TextureID))
                try
                {
                    GL.DeleteTexture(TextureID);

                    _disposed.Add(TextureID);

                    KnownTextures.Remove(AssociatedMaterial);
                }
                catch
                {
                }

            base.Dispose(disposing);
        }

        private unsafe int InitTexture()
        {
            int sz = _size * SZCNT;
            Bitmap bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            float[] data = new float[sz * sz * 4];

            using (Graphics g = Graphics.FromImage(bmp))
                for (int y = 0; y < SZCNT; ++y)
                    for (int x = 0; x < SZCNT; ++x)
                        if (_imgs[(y * SZCNT) + x] is Bitmap b)
                            g.DrawImage(b, x * _size, y * _size, _size, _size);

            BitmapData dat = bmp.LockBits(new Rectangle(0, 0, sz, sz), ImageLockMode.ReadOnly, bmp.PixelFormat);
            byte* ptr = (byte*)dat.Scan0;
            int index = 0;

            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    //  ptr: BGRA
                    // data: RGBA
                    data[index + 0] = ptr[index + 2] / 255f;
                    data[index + 1] = ptr[index + 1] / 255f;
                    data[index + 2] = ptr[index + 0] / 255f;
                    data[index + 3] = ptr[index + 3] / 255f;

                    index += 4;
                }

            bmp.UnlockBits(dat);
#if DEBUG
            bmp.Save($"{OpenTKMinecraft.Program.TEMP_DIR}/texture-{AssociatedMaterial}.png");
#endif
            bmp.Dispose();
            bmp = null;

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int tex);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexParameterI(TextureTarget.ProxyTexture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToEdge });
            GL.TexParameterI(TextureTarget.ProxyTexture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToEdge });
            GL.TexParameterI(TextureTarget.ProxyTexture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.ProxyTexture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.LinearMipmapLinear });
            GL.TextureStorage2D(tex, 1, SizedInternalFormat.Rgba32f, sz, sz);
            GL.TextureSubImage2D(tex, 0, 0, 0, sz, sz, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.Float, data);
            GL.TexImage2D(TextureTarget.Texture2D, (int)Math.Log(_size, 2), PixelInternalFormat.Rgba, sz, sz, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.Float, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return tex;
        }
    }

    public enum TextureType
        : byte
    {
        Diffuse                 = 0x0,
        AmbientOcclusion        = 0x1,
        Displacement            = 0x2,
        Glow                    = 0x3,
        Normal                  = 0x4,
        Gloss                   = 0x5,
        Specular                = 0x6,
        SubsurfaceScattering    = 0x7,
        Reflection              = 0x8,
        Parallax                = 0x9,
        Details                 = 0xa,
        Flow                    = 0xb,
    }
}
