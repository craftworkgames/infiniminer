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
        BankRed,
        BankBlue,
        BeaconRed,
        BeaconBlue,
        Road,
        SolidRed,
        SolidBlue,
        Metal,
        DirtSign,
        Lava,
        TransRed,
        TransBlue,
        MAXIMUM
    }

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
        HomeRed,
        HomeBlue,
        BankTopRed,
        BankTopBlue,
        BankFrontRed,
        BankFrontBlue,
        BankLeftRed,
        BankLeftBlue,
        BankRightRed,
        BankRightBlue,
        BankBackRed,
        BankBackBlue,
        TeleTop,
        TeleBottom,
        TeleSideA,
        TeleSideB,
        SolidRed,
        SolidBlue,
        Metal,
        DirtSign,
        Lava,
        Road,
        RoadTop,
        RoadBottom,
        BeaconRed,
        BeaconBlue,
        TransRed,   // THESE MUST BE THE LAST TWO TEXTURES
        TransBlue,
        MAXIMUM
    }

    public enum BlockFaceDirection : byte
    {
        XIncreasing,
        XDecreasing,
        YIncreasing,
        YDecreasing,
        ZIncreasing,
        ZDecreasing,
        MAXIMUM
    }

    public class BlockInformation
    {
        public static uint GetCost(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.BankRed:
                case BlockType.BankBlue:
                case BlockType.BeaconRed:
                case BlockType.BeaconBlue:
                    return 50;

                case BlockType.SolidRed:
                case BlockType.SolidBlue:
                    return 10;

                case BlockType.TransRed:
                case BlockType.TransBlue:
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
            return GetTexture(blockType, faceDir, BlockType.None);
        }

        public static BlockTexture GetTexture(BlockType blockType, BlockFaceDirection faceDir, BlockType blockAbove)
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

                case BlockType.BankRed:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.XIncreasing: return BlockTexture.BankFrontRed;
                        case BlockFaceDirection.XDecreasing: return BlockTexture.BankBackRed;
                        case BlockFaceDirection.ZIncreasing: return BlockTexture.BankLeftRed;
                        case BlockFaceDirection.ZDecreasing: return BlockTexture.BankRightRed;
                        default: return BlockTexture.BankTopRed;
                    }

                case BlockType.BankBlue:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.XIncreasing: return BlockTexture.BankFrontBlue;
                        case BlockFaceDirection.XDecreasing: return BlockTexture.BankBackBlue;
                        case BlockFaceDirection.ZIncreasing: return BlockTexture.BankLeftBlue;
                        case BlockFaceDirection.ZDecreasing: return BlockTexture.BankRightBlue;
                        default: return BlockTexture.BankTopBlue;
                    }

                case BlockType.BeaconRed:
                case BlockType.BeaconBlue:
                    switch (faceDir)
                    {
                        case BlockFaceDirection.YDecreasing:
                            return BlockTexture.LadderTop;
                        case BlockFaceDirection.YIncreasing:
                            return blockType == BlockType.BeaconRed ? BlockTexture.BeaconRed : BlockTexture.BeaconBlue;
                        case BlockFaceDirection.XDecreasing:
                        case BlockFaceDirection.XIncreasing:
                            return BlockTexture.TeleSideA;
                        case BlockFaceDirection.ZDecreasing:
                        case BlockFaceDirection.ZIncreasing:
                            return BlockTexture.TeleSideB;
                    }
                    break;

                case BlockType.Road:
                    if (faceDir == BlockFaceDirection.YIncreasing)
                        return BlockTexture.RoadTop;
                    else if (faceDir == BlockFaceDirection.YDecreasing||blockAbove!=BlockType.None) //Looks better but won't work with current graphics setup...
                        return BlockTexture.RoadBottom;
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

                case BlockType.SolidRed:
                    return BlockTexture.SolidRed;
                case BlockType.SolidBlue:
                    return BlockTexture.SolidBlue;
                case BlockType.TransRed:
                    return BlockTexture.TransRed;
                case BlockType.TransBlue:
                    return BlockTexture.TransBlue;

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
    }
}
