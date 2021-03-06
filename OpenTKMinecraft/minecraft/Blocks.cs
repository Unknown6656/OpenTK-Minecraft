﻿using System.Collections.Generic;

using OpenTK.Graphics;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Minecraft
{
    public class BlockInfo
    {
        public static Dictionary<BlockMaterial, BlockInfo> Blocks { get; }
        public (Color4 color, float intensity, float falloff)? Glow { private set; get; }
        public (string path, TextureType type)[] Textures { get; }
        public bool IsActivelyGlowing => Glow != null;
        public bool Translucent { get; private set; }
        public bool Gravity { get; private set; }
        public bool Visible { get; private set; }
        public bool Liquid { get; private set; }
        public string Name { get; }

        static BlockInfo() => Blocks = new Dictionary<BlockMaterial, BlockInfo>
        {
            [BlockMaterial.Air] = new BlockInfo("Air") { Visible = false, Translucent = true },
            [BlockMaterial.Stone] = new BlockInfo("Stone",
                ("resources/stone-diff.png", TextureType.Diffuse),
                ("resources/stone-disp.png", TextureType.Displacement),
                ("resources/stone-ambt.png", TextureType.AmbientOcclusion),
                ("resources/stone-spec.png", TextureType.Specular),
                ("resources/stone-norm.png", TextureType.Normal)
            ),
            [BlockMaterial.Grass] = new BlockInfo("Grass",
                ("resources/grass-diff.png", TextureType.Diffuse),
                ("resources/grass-disp.png", TextureType.Displacement),
                ("resources/grass-ambt.png", TextureType.AmbientOcclusion),
                ("resources/grass-spec.png", TextureType.Specular),
                ("resources/grass-norm.png", TextureType.Normal)
            ),
            [BlockMaterial.Water] = new BlockInfo("Water",
                ("resources/water-diff.png", TextureType.Diffuse),
                ("resources/water-disp.png", TextureType.Displacement),
                ("resources/water-ambt.png", TextureType.AmbientOcclusion),
                ("resources/water-spec.png", TextureType.Specular),
                ("resources/water-norm.png", TextureType.Normal),
                ("resources/water-flow.png", TextureType.Flow)
            ) { Liquid = true, Translucent = true },
            [BlockMaterial.Sand] = new BlockInfo("Sand",
                ("resources/sand-diff.png", TextureType.Diffuse),
                ("resources/sand-disp.png", TextureType.Displacement),
                ("resources/sand-ambt.png", TextureType.AmbientOcclusion),
                ("resources/sand-spec.png", TextureType.Specular),
                ("resources/sand-norm.png", TextureType.Normal)
            ) { Gravity = true },
            [BlockMaterial.Diamond] = new BlockInfo("Diamond",
                ("resources/diamond-diff.png", TextureType.Diffuse),
                ("resources/diamond-disp.png", TextureType.Displacement),
                ("resources/diamond-ambt.png", TextureType.AmbientOcclusion),
                ("resources/diamond-spec.png", TextureType.Specular),
                ("resources/diamond-norm.png", TextureType.Normal)
            ),
            [BlockMaterial.Glowstone] = new BlockInfo("Glowstone",
                ("resources/glowstone-diff.png", TextureType.Diffuse),
                ("resources/glowstone-disp.png", TextureType.Displacement),
                ("resources/glowstone-ambt.png", TextureType.AmbientOcclusion),
                ("resources/glowstone-spec.png", TextureType.Specular),
                ("resources/glowstone-norm.png", TextureType.Normal),
                ("resources/glowstone-glow.png", TextureType.Glow)
            ) { Glow = (Color4.Gold, 6, 0.005f) },

            // TODO

            [BlockMaterial.__DEBUG__] = new BlockInfo("__DEBUG__",
                ("resources/debug-diff.png", TextureType.Diffuse),
                ("resources/debug-disp.png", TextureType.Displacement),
                ("resources/debug-ambt.png", TextureType.AmbientOcclusion),
                ("resources/debug-spec.png", TextureType.Specular),
                ("resources/debug-norm.png", TextureType.Normal)
            )
        };

        public BlockInfo(string name, params (string, TextureType)[] tex)
        {
            Name = name;
            Visible = true;
            Textures = tex;
        }

        public Light? CreateAssociatedLight(RenderableBlock block)
        {
            if (IsActivelyGlowing)
            {
                (Color4 color, float intensity, float falloff) g = Glow.Value;

                return Light.CreatePointLight(block.Center, g.color, g.intensity, g.falloff);
            }
            else
                return null;
        }
    }

    public enum BlockMaterial
        : uint
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Water = 10,
        Sand = 12,
        Diamond = 56,
        Glowstone = 89,
        __DEBUG__ = 0xffffffff
    }
}
