using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Utilities
{
    public sealed class WavefrontFile
    {
        public List<WavefrontVertex[]> Faces { get; }


        internal WavefrontFile() => Faces = new List<WavefrontVertex[]>();

        public Vertex[] ToCVertex()
        {
            Vertex[] ret = new Vertex[Faces.Count * 3];

            for (int i = 0; i < Faces.Count; ++i)
                for (int j = 0; j < 3; ++j)
                    ret[(i * 3) + j] = Faces[i][j].ToCVertex();

            return ret;
        }

        public static WavefrontFile FromContent(string obj)
        {
            WavefrontFile wav = new WavefrontFile();
            List<WavefrontVertex> vertices = new List<WavefrontVertex>();
            WavefrontVertex curr = default;
            bool first = true;

            foreach (string raw in obj.Split('\r', '\n'))
            {
                string s = (raw.Contains('#') ? raw.Remove(raw.IndexOf('#')) : raw).Trim();

                if (s.Length > 0)
                {
                    string[] tokens = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    float _ = default;
                    float[] values = (from t in tokens.Skip(1)
                                      let tt = t.Contains('/') ? t.Remove(t.IndexOf('/')) : t
                                      let res = float.TryParse(tt, out _)
                                      select res ? _ : float.NaN).ToArray();

                    if (tokens.Length > 0)
                        switch (tokens[0].ToLower())
                        {
                            case "v":
                                if (!first)
                                    vertices.Add(curr);
                                else
                                    first = false;

                                curr.Position = new Vector3(values[0], values[1], values[2]);

                                break;
                            case "vt":
                                curr.Texcoord = new Vector2(values[0], values[1]);

                                break;
                            case "vn":
                                curr.Normal = new Vector3(values[0], values[1], values[2]);

                                break;
                            case "f":
                                int n0 = (int)values[0];
                                int n1 = (int)values[1];
                                int n2 = (int)values[2];
                            
                                if ((n0 < vertices.Count) && (n1 < vertices.Count) && (n2 < vertices.Count))
                                    wav.Faces.Add(new WavefrontVertex[3]
                                    {
                                        vertices[n0],
                                        vertices[n1],
                                        vertices[n2],
                                    });

                                break;
                        }
                }
            }

            return wav;
        }

        public static WavefrontFile FromPath(string path) => FromContent(File.ReadAllText(path));
    }

    public struct WavefrontVertex
    {
        public Vector3 Kd;
        public Vector3 Ks;
        public Vector3 Ka;
        public Vector3 Ns;
        public float Tr;

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texcoord;


        public Vertex ToCVertex() => new Vertex(Position, Normal, new Color4(Kd.X, Kd.Y, Kd.Z, 1 - Tr));
    }

    public sealed class OBJModel
        : GameObject
    {
        public OBJModel(ShaderProgram progr, WavefrontFile obj, Vector4 pos, Vector3 rot)
            : base(new TexturedVertexSet(obj.ToCVertex(), PrimitiveType.Triangles, progr), pos, new Vector4(rot, 0), rot, new Vector3(1), 0)
        {
        }
    }
}
