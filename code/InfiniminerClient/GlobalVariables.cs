using System;
using System.Collections.Generic;
using System.Text;

namespace Infiniminer
{
    public class GlobalVariables
    {
        //Should be multiple of PACKETSIZE less than 256
        public const int MAPSIZE = 64;
        //Probably best to set equal to MAPSIZE
        public const int PACKETSIZE = 64;
    }
}
