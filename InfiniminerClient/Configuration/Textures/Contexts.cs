namespace Infiniminer
{
    public class Contexts
    {
        public const byte exposedToSunlight = 1;
        public const byte exposedToLava     = 2;

        public const byte obscuredBySolid   = 4; // if a block isn't exposed by these two,
        public const byte obscuredByTrans   = 8; // then it is exposed to air

        public const byte tunnelNorthSouth  = 16;
        public const byte tunnelEastWest    = 32;
        public const byte tunnelNES         = 64;
        public const byte tunnelNWS         = 128;
        public const ushort tunnelCrossroad = 256;

        public static BlockTexture Texture(ushort x, ushort y, ushort z, BlockFaceDirection faceDir, BlockType type, BlockType[, ,] downloadList, BlockTexture[,] blockTextureMap)
        {
            BlockTexture blockTexture = blockTextureMap[(byte)type, (byte)faceDir];
            switch (type)
            {
                case BlockType.Dirt:
                    {
                        if (configHelper.isAboveGround(x, y, z) && faceDir == BlockFaceDirection.YIncreasing && downloadList[x, y + 1, z] == BlockType.None)
                        {
                            blockTexture = BlockTexture.Grass;
                        }
                    }
                    break;
            }
            return blockTexture;
        }
    }
}