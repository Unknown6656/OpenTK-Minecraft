using System;

using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Utilities
{
    public static class ObjectFactory
    {
        public static Vertex[] CreateSolidTriangleCube(float side, Color4 color)
        {
            side /= 2f;

            return new Vertex[]
            {
                new Vertex(new Vector3(-side, -side, -side), new Vector3(-1, 0, 0), color),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(-1, 0, 0), color),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(-1, 0, 0), color),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(-1, 0, 0), color),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(-1, 0, 0), color),
                new Vertex(new Vector3(-side, side, side),   new Vector3(-1, 0, 0), color),

                new Vertex(new Vector3(side, -side, -side),  new Vector3(1, 0, 0), color),
                new Vertex(new Vector3(side, side, -side),   new Vector3(1, 0, 0), color),
                new Vertex(new Vector3(side, -side, side),   new Vector3(1, 0, 0), color),
                new Vertex(new Vector3(side, -side, side),   new Vector3(1, 0, 0), color),
                new Vertex(new Vector3(side, side, -side),   new Vector3(1, 0, 0), color),
                new Vertex(new Vector3(side, side, side),    new Vector3(1, 0, 0), color),

                new Vertex(new Vector3(-side, -side, -side), new Vector3(0, -1, 0), color),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, -1, 0), color),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, -1, 0), color),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, -1, 0), color),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, -1, 0), color),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, -1, 0), color),

                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 1, 0), color),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 1, 0), color),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 1, 0), color),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 1, 0), color),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 1, 0), color),
                new Vertex(new Vector3(side, side, side),    new Vector3(0, 1, 0), color),

                new Vertex(new Vector3(-side, -side, -side), new Vector3(0, 0, -1), color),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 0, -1), color),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, 0, -1), color),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, 0, -1), color),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 0, -1), color),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 0, -1), color),

                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, 0, 1), color),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, 0, 1), color),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 0, 1), color),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 0, 1), color),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, 0, 1), color),
                new Vertex(new Vector3(side, side, side),    new Vector3(0, 0, 1), color),
            };
        }

        [Obsolete("Use '" + nameof(CreateTexturedQuadCube) + "' instead.")]
        public static Vertex[] CreateTexturedTriangleCube(float side)
        {
            side /= 2f;

            return new Vertex[]
            {
                new Vertex(new Vector3(-side, -side, -side), new Vector3(-1, 0, 0), new Vector2(0, 1)),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(-1, 0, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(-1, 0, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(-1, 0, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(-1, 0, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(-side, side, side),   new Vector3(-1, 0, 0), new Vector2(1, 0)),

                new Vertex(new Vector3(side, -side, -side),  new Vector3(1, 0, 0), new Vector2(1, 0)),
                new Vertex(new Vector3(side, side, -side),   new Vector3(1, 0, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(side, -side, side),   new Vector3(1, 0, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(side, -side, side),   new Vector3(1, 0, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(side, side, -side),   new Vector3(1, 0, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(side, side, side),    new Vector3(1, 0, 0), new Vector2(0, 1)),

                new Vertex(new Vector3(-side, -side, -side), new Vector3(0, -1, 0), new Vector2(1, 0)),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, -1, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, -1, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, -1, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, -1, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, -1, 0), new Vector2(0, 1)),

                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 1, 0), new Vector2(1, 0)),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 1, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 1, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 1, 0), new Vector2(1, 1)),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 1, 0), new Vector2(0, 0)),
                new Vertex(new Vector3(side, side, side),    new Vector3(0, 1, 0), new Vector2(0, 1)),

                new Vertex(new Vector3(-side, -side, -side), new Vector3(0, 0, -1), new Vector2(0, 1)),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 0, -1), new Vector2(1, 1)),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, 0, -1), new Vector2(0, 0)),
                new Vertex(new Vector3(side, -side, -side),  new Vector3(0, 0, -1), new Vector2(0, 0)),
                new Vertex(new Vector3(-side, side, -side),  new Vector3(0, 0, -1), new Vector2(1, 1)),
                new Vertex(new Vector3(side, side, -side),   new Vector3(0, 0, -1), new Vector2(1, 0)),

                new Vertex(new Vector3(-side, -side, side),  new Vector3(0, 0, 1), new Vector2(0, 1)),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, 0, 1), new Vector2(1, 1)),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 0, 1), new Vector2(0, 0)),
                new Vertex(new Vector3(-side, side, side),   new Vector3(0, 0, 1), new Vector2(0, 0)),
                new Vertex(new Vector3(side, -side, side),   new Vector3(0, 0, 1), new Vector2(1, 1)),
                new Vertex(new Vector3(side, side, side),    new Vector3(0, 0, 1), new Vector2(1, 0)),
            };
        }

        public static Vertex[] CreateFontQuad()
        {
            // TODO : smth

            return new Vertex[]
            {
                new Vertex(new Vector3(-.5f, -.5f, -.5f), new Vector3(-1,  0,  0), new Vector2(0, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-.5f, -.5f,  .5f), new Vector3(-1,  0,  0), new Vector2(0, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-.5f,  .5f,  .5f), new Vector3(-1,  0,  0), new Vector2(1, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-.5f,  .5f, -.5f), new Vector3(-1,  0,  0), new Vector2(1, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
            };
        }

        public static Vertex[] CreateTexturedQuadCube(float side)
        {
            side /= 2f;

            return new Vertex[]
            {
                new Vertex(new Vector3(-side, -side, -side), new Vector3(-1,  0,  0), new Vector2(0, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side, -side,  side), new Vector3(-1,  0,  0), new Vector2(0, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side,  side,  side), new Vector3(-1,  0,  0), new Vector2(1, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side,  side, -side), new Vector3(-1,  0,  0), new Vector2(1, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(0, 1, 0) },

                new Vertex(new Vector3(-side, -side,  side), new Vector3( 0,  0,  1), new Vector2(0, 0)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side, -side,  side), new Vector3( 0,  0,  1), new Vector2(0, 1)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side,  side,  side), new Vector3( 0,  0,  1), new Vector2(1, 1)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side,  side,  side), new Vector3( 0,  0,  1), new Vector2(1, 0)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 1, 0) },

                new Vertex(new Vector3( side, -side,  side), new Vector3( 1,  0,  0), new Vector2(0, 0)) { tangent = new Vector3(0, 0, -1), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side, -side, -side), new Vector3( 1,  0,  0), new Vector2(0, 1)) { tangent = new Vector3(0, 0, -1), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side,  side, -side), new Vector3( 1,  0,  0), new Vector2(1, 1)) { tangent = new Vector3(0, 0, -1), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side,  side,  side), new Vector3( 1,  0,  0), new Vector2(1, 0)) { tangent = new Vector3(0, 0, -1), bitangent = new Vector3(0, 1, 0) },

                new Vertex(new Vector3( side, -side, -side), new Vector3( 0,  0, -1), new Vector2(0, 0)) { tangent = new Vector3(-1, 0, 0), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side, -side, -side), new Vector3( 0,  0, -1), new Vector2(0, 1)) { tangent = new Vector3(-1, 0, 0), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3(-side,  side, -side), new Vector3( 0,  0, -1), new Vector2(1, 1)) { tangent = new Vector3(-1, 0, 0), bitangent = new Vector3(0, 1, 0) },
                new Vertex(new Vector3( side,  side, -side), new Vector3( 0,  0, -1), new Vector2(1, 0)) { tangent = new Vector3(-1, 0, 0), bitangent = new Vector3(0, 1, 0) },

                new Vertex(new Vector3(-side, -side, -side), new Vector3( 0, -1,  0), new Vector2(0, 0)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 0, 1) },
                new Vertex(new Vector3( side, -side, -side), new Vector3( 0, -1,  0), new Vector2(0, 1)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 0, 1) },
                new Vertex(new Vector3( side, -side,  side), new Vector3( 0, -1,  0), new Vector2(1, 1)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 0, 1) },
                new Vertex(new Vector3(-side, -side,  side), new Vector3( 0, -1,  0), new Vector2(1, 0)) { tangent = new Vector3(1, 0, 0),  bitangent = new Vector3(0, 0, 1) },

                new Vertex(new Vector3(-side,  side, -side), new Vector3( 0,  1,  0), new Vector2(0, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(1, 0, 0) },
                new Vertex(new Vector3(-side,  side,  side), new Vector3( 0,  1,  0), new Vector2(0, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(1, 0, 0) },
                new Vertex(new Vector3( side,  side,  side), new Vector3( 0,  1,  0), new Vector2(1, 1)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(1, 0, 0) },
                new Vertex(new Vector3( side,  side, -side), new Vector3( 0,  1,  0), new Vector2(1, 0)) { tangent = new Vector3(0, 0, 1),  bitangent = new Vector3(1, 0, 0) },
            };
        }

        public static Vertex[] FromWaveFrontOBJ(string content) => WavefrontFile.FromContent(content).ToCVertex();
    }
}
