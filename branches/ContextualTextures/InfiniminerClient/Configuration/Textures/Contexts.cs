namespace Infiniminer
{
    public class Contexts
    {
        public const byte exposedToSunlight = 1;
        public const byte exposedToLava     = 2;

        public const byte obscuredBySolid   = 4; // if a block isn't exposed by these two,
        public const byte obscuredByTrans   = 8; // then it is exposed to air

        public const byte tunnelNorth  = 16;
        public const byte tunnelEast   = 32;
        public const byte tunnelWest   = 64;
        public const byte tunnelSouth  = 128;

        public static BlockTexture Texture(ushort x, ushort y, ushort z, BlockFaceDirection faceDir, BlockType type, BlockType[, ,] downloadList, BlockTexture[,] blockTextureMap)
        {
            BlockTexture blockTexture = blockTextureMap[(byte)type, (byte)faceDir];
            switch (type)
            {
                case BlockType.Dirt:
                    {
                        if (
                            configHelper.isAboveGround(x,y,z)
                        ){
                            switch (faceDir)
                            {
                                case BlockFaceDirection.YIncreasing:
                                {
                                    if (configHelper.isOutOfBounds(x, y + 2, z) || downloadList[x, y + 1, z] == BlockType.None)
                                    {
                                        blockTexture = BlockTexture.Grass;
                                    }
                                }
                                break;
                                case BlockFaceDirection.XDecreasing:
                                {
                                    if (configHelper.isOutOfBounds(x - 1, y, z) || downloadList[x - 1, y, z] == BlockType.None)
                                    {
                                        blockTexture = BlockTexture.Grass;
                                    }
                                }
                                break;
                                case BlockFaceDirection.XIncreasing:
                                {
                                    if (configHelper.isOutOfBounds(x + 1, y, z) || downloadList[x + 1, y, z] == BlockType.None)
                                    {
                                        blockTexture = BlockTexture.Grass;
                                    }
                                }
                                break;
                                case BlockFaceDirection.ZDecreasing:
                                {
                                    if (configHelper.isOutOfBounds(x, y, z - 1) || downloadList[x, y, z - 1] == BlockType.None)
                                    {
                                        blockTexture = BlockTexture.Grass;
                                    }
                                }
                                break;
                                case BlockFaceDirection.ZIncreasing:
                                {
                                    if (configHelper.isOutOfBounds(x, y, z + 1) || downloadList[x, y, z + 1] == BlockType.None)
                                    {
                                        blockTexture = BlockTexture.Grass;
                                    }
                                }
                                break;
                            }
                        }
                        else if (configHelper.isGround(x, y, z))
                        {
                            switch (faceDir)
                            {
                                case BlockFaceDirection.YIncreasing:
                                    blockTexture = BlockTexture.Grass;
                                    break;
                                case BlockFaceDirection.XDecreasing:
                                case BlockFaceDirection.XIncreasing:
                                case BlockFaceDirection.ZDecreasing:
                                case BlockFaceDirection.ZIncreasing:
                                    blockTexture = BlockTexture.DirtGrass;
                                    break;
                            }
                        }
                    }
                    break;
            }
            return blockTexture;
        }
    }
}