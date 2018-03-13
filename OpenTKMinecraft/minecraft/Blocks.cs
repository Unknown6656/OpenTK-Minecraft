using System.Collections.Generic;

using OpenTKMinecraft.Components;

namespace OpenTKMinecraft.Minecraft
{
    public class BlockInfo
    {
        public static Dictionary<BlockMaterial, BlockInfo> Blocks { get; }
        public (string, TextureType)[] Textures { get; }
        public bool Gravity { get; private set; }
        public bool Visible { get; private set; }
        public string Name { get; }


        static BlockInfo() => Blocks = new Dictionary<BlockMaterial, BlockInfo>
        {
            [BlockMaterial.Air] = new BlockInfo("Air") { Visible = false },
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
            [BlockMaterial.Sand] = new BlockInfo("Sand",
                ("resources/sand-diff.png", TextureType.Diffuse),
                ("resources/sand-disp.png", TextureType.Displacement),
                ("resources/sand-ambt.png", TextureType.AmbientOcclusion),
                ("resources/sand-spec.png", TextureType.Specular),
                ("resources/sand-norm.png", TextureType.Normal)
            ) {  Gravity = true },

            // TODO
        };

        public BlockInfo(string name, params (string, TextureType)[] tex)
        {
            Name = name;
            Visible = true;
            Textures = tex;
        }
    }

    public enum BlockMaterial
        : uint
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Sand = 3,
    }
}
