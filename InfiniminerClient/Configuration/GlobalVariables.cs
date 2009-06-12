namespace Infiniminer
{
    //3D point of 3 ushorts
    public struct Point3D
    {
        public ushort X, Y, Z;
    }
    public class GlobalVariables
    {
        public const byte MAPSIZE = 64;
        public const string INFINIMINER_VERSION = "v1.5";
        public const string INFINIMINER_BRANCH = "Contextual Textures";
        public const int GROUND_LEVEL = 8;

        public const ushort goldCash = 100;
        public const ushort diamondCash = 1000;
        public const byte goldWeight = 1;
        public const byte diamondWeight = 1;

        public const bool sandboxMode = false;

        // block type factors
        public const uint oreFactor = 20;

        // Display constants
        public const ushort minScreenWidth = 320;
        public const ushort maxScreenWidth = 1440;
        public const ushort minScreenHeight = 240;
        public const ushort maxScreenHeight = 1080;

        // Shader effects
        public const string fx__effect_skyplane    = "effects/effect_skyplane";
        public const string fx__effect_basic       = "effects/effect_basic";
        public const string fx__effect_particle    = "effects/effect_particle";
        public const string fx__effect_spritemodel = "effects/effect_spritemodel";
        public const string fx__BloomExtract       = "effects/BloomExtract";
        public const string fx__BloomCombine       = "effects/BloomCombine";
        public const string fx__GaussianBlur       = "effects/GaussianBlur";

        // fonts
        public const string fontUI    = "fonts/04b08";
        public const string fontRadar = "fonts/04b03b";
        public const string fontName  = fontUI;

    }
}