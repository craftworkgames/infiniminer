namespace Infiniminer
{
    public class GlobalVariables
    {
        //Should be multiple of PACKETSIZE less than 256
        public const byte MAPSIZE = 64;
        public const ushort goldCash = 100;
        public const ushort diamondCash = 1000;
        public const byte goldWeight = 1;
        public const byte diamondWeight = 1;

        // Display constants
        public const ushort minScreenWidth = 320;
        public const ushort maxScreenWidth = 1440;
        public const ushort minScreenHeight = 240;
        public const ushort maxScreenHeight = 1080;
    }
}