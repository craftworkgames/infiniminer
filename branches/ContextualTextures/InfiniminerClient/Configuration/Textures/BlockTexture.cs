using Microsoft.Xna.Framework.Graphics;
namespace Infiniminer
{
    public enum BlockTexture : byte
    {
        None,
        Dirt,
        Ore,
        Gold,
        Diamond,
        Rock,
        Jump,
        JumpTop,
        Ladder,
        LadderTop,
        Explosive,
        Spikes,
        HomeA,
        HomeB,
        BankTopA,
        BankTopB,
        BankFrontA,
        BankFrontB,
        BankLeftA,
        BankLeftB,
        BankRightA,
        BankRightB,
        BankBackA,
        BankBackB,
        TeleTop,
        TeleBottom,
        TeleSideA,
        TeleSideB,
        SolidA,
        SolidB,
        Metal,
        DirtSign,
        Lava,
        Road,
        BeaconA,
        BeaconB,
        Grass,
        DirtGrass,
        TransA,   // THESE MUST BE THE LAST TWO TEXTURES-- why ? ~ Marv.
        TransB,
        MAXIMUM
    }
    public class IMTexture
    {
        public Texture2D Texture = null;
        public Color LODColor = Color.Black;

        public IMTexture(Texture2D texture)
        {
            Texture = texture;
            LODColor = Color.Black;

            // If this is a null texture, use a black LOD color.
            if (Texture == null)
                return;

            // Calculate the load color dynamically.
            float r = 0, g = 0, b = 0;
            Color[] pixelData = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(pixelData);
            for (int i = 0; i < texture.Width; i++)
            {
                for (int j = 0; j < texture.Height; j++)
                {
                    r += pixelData[i + j * texture.Width].R;
                    g += pixelData[i + j * texture.Width].G;
                    b += pixelData[i + j * texture.Width].B;
                }
            }
            r /= texture.Width * texture.Height;
            g /= texture.Width * texture.Height;
            b /= texture.Width * texture.Height;
            LODColor = new Color(r / 256, g / 256, b / 256);
        }
    }
}