using System;
using System.Collections.Generic;

using System.Text;

namespace Infiniminer
{
    public enum BlockType : byte
    {
        None,
        Dirt,
        Ore,
        Gold,
        Diamond,
        Rock,
        Ladder,
        Explosive,
        Jump,
        Shock,
        BankA,
        BankB,
        BeaconA,
        BeaconB,
        Road,
        SolidA,
        SolidB,
        Metal,
        DirtSign,
        Lava,
        TransA,
        TransB,
        MAXIMUM
    }

    public class BlockInformation
    {
        public static uint GetCost(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.BankA:
                case BlockType.BankB:
                case BlockType.BeaconA:
                case BlockType.BeaconB:
                    return 50;

                case BlockType.SolidA:
                case BlockType.SolidB:
                    return 10;

                case BlockType.TransA:
                case BlockType.TransB:
                    return 25;

                case BlockType.Road:
                    return 10;
                case BlockType.Jump:
                    return 25;
                case BlockType.Ladder:
                    return 25;
                case BlockType.Shock:
                    return 50;
                case BlockType.Explosive:
                    return 100;
            }
            return 1000;
        }

        public static BlockTexture GetTexture(BlockType blockType, BlockFaceDirection faceDir)
        {
            switch (blockType)
            {
                case BlockType.Metal:
                    return BlockTexture.Metal;
                case BlockType.Dirt:
                    return BlockTexture.Dirt;
                case BlockType.Lava:
                    return BlockTexture.Lava;
                case BlockType.Rock:
                    return BlockTexture.Rock;
                case BlockType.Ore:
                    return BlockTexture.Ore;
                case BlockType.Gold:
                    return BlockTexture.Gold;
                case BlockType.Diamond:
                    return BlockTexture.Diamond;
                case BlockType.DirtSign:
                    return BlockTexture.DirtSign;

                case BlockType.BankA:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.XIncreasing: return BlockTexture.BankFrontA;
                        case BlockFaceDirection.XDecreasing: return BlockTexture.BankBackA;
                        case BlockFaceDirection.ZIncreasing: return BlockTexture.BankLeftA;
                        case BlockFaceDirection.ZDecreasing: return BlockTexture.BankRightA;
                        default: return BlockTexture.BankTopA;
                    }

                case BlockType.BankB:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.XIncreasing: return BlockTexture.BankFrontB;
                        case BlockFaceDirection.XDecreasing: return BlockTexture.BankBackB;
                        case BlockFaceDirection.ZIncreasing: return BlockTexture.BankLeftB;
                        case BlockFaceDirection.ZDecreasing: return BlockTexture.BankRightB;
                        default: return BlockTexture.BankTopB;
                    }

                case BlockType.BeaconA:
                case BlockType.BeaconB:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.YDecreasing:
                            return BlockTexture.LadderTop;
                        case BlockFaceDirection.YIncreasing:
                            return blockType == BlockType.BeaconA ? BlockTexture.BeaconA : BlockTexture.BeaconB;
                        case BlockFaceDirection.XDecreasing:
                        case BlockFaceDirection.XIncreasing:
                            return BlockTexture.TeleSideA;
                        case BlockFaceDirection.ZDecreasing:
                        case BlockFaceDirection.ZIncreasing:
                            return BlockTexture.TeleSideB;
                    }
                    break;

                case BlockType.Road:
                    return BlockTexture.Road;

                case BlockType.Shock:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.YDecreasing:
                            return BlockTexture.Spikes;
                        case BlockFaceDirection.YIncreasing:
                            return BlockTexture.TeleBottom;
                        case BlockFaceDirection.XDecreasing:
                        case BlockFaceDirection.XIncreasing:
                            return BlockTexture.TeleSideA;
                        case BlockFaceDirection.ZDecreasing:
                        case BlockFaceDirection.ZIncreasing:
                            return BlockTexture.TeleSideB;
                    }
                    break;

                case BlockType.Jump:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.YDecreasing:
                            return BlockTexture.TeleBottom;
                        case BlockFaceDirection.YIncreasing:
                            return BlockTexture.JumpTop;
                        case BlockFaceDirection.XDecreasing:
                        case BlockFaceDirection.XIncreasing:
                            return BlockTexture.Jump;
                        case BlockFaceDirection.ZDecreasing:
                        case BlockFaceDirection.ZIncreasing:
                            return BlockTexture.Jump;
                    }
                    break;

                case BlockType.SolidA:
                    return BlockTexture.SolidA;
                case BlockType.SolidB:
                    return BlockTexture.SolidB;
                case BlockType.TransA:
                    return BlockTexture.TransA;
                case BlockType.TransB:
                    return BlockTexture.TransB;

                case BlockType.Ladder:
                    if (faceDir == BlockFaceDirection.YDecreasing || faceDir == BlockFaceDirection.YIncreasing)
                        return BlockTexture.LadderTop;
                    else
                        return BlockTexture.Ladder;

                case BlockType.Explosive:
                    return BlockTexture.Explosive;
            }

            return BlockTexture.None;
        }

        public static bool indestructable(BlockType block)
        {
            switch (block)
            {
                case BlockType.Gold:
                case BlockType.Diamond:
                case BlockType.BankA:
                case BlockType.BankB:
                case BlockType.BeaconA:
                case BlockType.BeaconB:
                case BlockType.Metal:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}
