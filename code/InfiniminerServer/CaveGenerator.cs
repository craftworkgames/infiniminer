using System;
using System.Collections.Generic;

using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Infiniminer
{
    public class CaveGenerator
    {
        public static string CaveInfo = "";
        private static Random randGen = new Random();

        // Create a cave system.
        public static BlockType[, ,] GenerateCaveSystem(int size, bool includeLava, uint oreFactor)
        {
            float gradientStrength = (float)randGen.NextDouble();
            BlockType[, ,] caveData = CaveGenerator.GenerateConstant(size, BlockType.Dirt);

            // Add ore.
            float[, ,] oreNoise = CaveGenerator.GeneratePerlinNoise(32);
            oreNoise = InterpolateData(ref oreNoise, 32, size);
            for (int i = 0; i < oreFactor; i++)
                CaveGenerator.PaintWithRandomWalk(ref caveData, ref oreNoise, size, 1, BlockType.Ore, false);

            // Add minerals.
            AddGold(ref caveData, size);
            AddDiamond(ref caveData, size);

            // Level off everything above ground level, replacing it with mountains.
            float[, ,] mountainNoise = CaveGenerator.GeneratePerlinNoise(32);
            mountainNoise = InterpolateData(ref mountainNoise, 32, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z <= Defines.GROUND_LEVEL * 2; z++)
                        mountainNoise[x, y, z] = z < 3 ? 0 : Math.Min(1, z / (Defines.GROUND_LEVEL * 2));
            float[, ,] gradient = CaveGenerator.GenerateGradient(size);
            CaveGenerator.AddDataTo(ref mountainNoise, ref gradient, size, 0.1f, 0.9f);
            BlockType[, ,] mountainData = CaveGenerator.GenerateConstant(size, BlockType.None);
            int numMountains = randGen.Next(size, size * 3);
            for (int i = 0; i < numMountains; i++)
                CaveGenerator.PaintWithRandomWalk(ref mountainData, ref mountainNoise, size, randGen.Next(2, 3), BlockType.Dirt, false);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z <= Defines.GROUND_LEVEL; z++)
                        if (mountainData[x, y, z] == BlockType.None)
                            caveData[x, y, z] = BlockType.None;
            
            // Carve some caves into the ground.
            float[, ,] caveNoise = CaveGenerator.GeneratePerlinNoise(32);
            caveNoise = InterpolateData(ref caveNoise, 32, size);
            gradient = CaveGenerator.GenerateGradient(size);
            CaveGenerator.AddDataTo(ref caveNoise, ref gradient, size, 1 - gradientStrength, gradientStrength);
            int cavesToCarve = randGen.Next(size / 8, size / 2);
            for (int i = 0; i < cavesToCarve; i++)
                CaveGenerator.PaintWithRandomWalk(ref caveData, ref caveNoise, size, randGen.Next(1, 2), BlockType.None, false);

            // Carve the map into a sphere.
            float[, ,] sphereGradient = CaveGenerator.GenerateRadialGradient(size);
            cavesToCarve = randGen.Next(size / 8, size / 2);
            for (int i = 0; i < cavesToCarve; i++)
                CaveGenerator.PaintWithRandomWalk(ref caveData, ref sphereGradient, size, randGen.Next(1, 2), BlockType.None, true);

            // Add rocks.
            AddRocks(ref caveData, size);

            // Add lava.
            if (includeLava)
                AddLava(ref caveData, size);

            // Add starting positions.
            //AddStartingPosition(ref caveData, size, size - 5, size - 5, InfiniminerGame.GROUND_LEVEL, BlockType.HomeRed);
            //AddStartingPosition(ref caveData, size, 5, 5, InfiniminerGame.GROUND_LEVEL, BlockType.HomeBlue);

            oreNoise = null;
            caveNoise = null;
            gradient = null;
            GC.Collect();

            return caveData;
        }

        //public static void AddStartingPosition(ref BlockType[, ,] data, int size, int x, int y, int z, BlockType blockType)
        //{
        //    for (int i = 0; i < size; i++)
        //        for (int j = 0; j < size; j++)
        //            for (int k = 0; k < size; k++)
        //            {
        //                double dist = Math.Sqrt(Math.Pow(x - i, 2) + Math.Pow(y - j, 2) + Math.Pow(z - k, 2));
        //                if (dist < 4)
        //                {
        //                    if (k <= z)
        //                        data[i, j, k] = BlockType.None;
        //                    else if (k == z+1)
        //                        data[i, j, k] = BlockType.Metal;
        //                    else
        //                        data[i, j, k] = BlockType.Dirt;
        //                }
        //            }
        //    data[x, y, z] = blockType;
        //}

        public static void AddRocks(ref BlockType[, ,] data, int size)
        {
            int numRocks = randGen.Next(size, 2*size);
            CaveInfo += " numRocks=" + numRocks;
            for (int i = 0; i < numRocks; i++)
            {
                int x = randGen.Next(0, size);
                int y = randGen.Next(0, size);

                // generate a random z-value weighted toward a deep depth
                float zf = 0;
                for (int j = 0; j < 4; j++)
                    zf += (float)randGen.NextDouble();
                zf /= 2;
                zf = 1 - Math.Abs(zf - 1);
                int z = (int)(zf * size);

                int rockSize = (int)((randGen.NextDouble() + randGen.NextDouble() + randGen.NextDouble() + randGen.NextDouble()) / 4 * 8);

                PaintAtPoint(ref data, x, y, z, size, rockSize, BlockType.Rock);
            }
        }

        public static void AddLava(ref BlockType[, ,] data, int size)
        {
            int numFlows = randGen.Next(size / 16, size / 2);
            while (numFlows > 0)
            {
                int x = randGen.Next(0, size);
                int y = randGen.Next(0, size);

                //switch (randGen.Next(0, 4))
                //{
                //    case 0: x = 0; break;
                //    case 1: x = size - 1; break;
                //    case 2: y = 0; break;
                //    case 3: y = size - 1; break;
                //}

                // generate a random z-value weighted toward a medium depth
                float zf = 0;
                for (int j = 0; j < 4; j++)
                    zf += (float)randGen.NextDouble();
                zf /= 2;
                zf = 1 - Math.Abs(zf - 1);
                int z = (int)(zf * size);

                if (data[x, y, z] == BlockType.None && z+1 < size-1)
                {
                    data[x, y, z] = BlockType.Rock;
                    data[x, y, z+1] = BlockType.Lava;
                    numFlows -= 1;
                }
            }
        }

        public static void AddDiamond(ref BlockType[, ,] data, int size)
        {
            CaveInfo += "diamond";

            int numDiamonds = 16;
            for (int i = 0; i < numDiamonds; i++)
            {
                int x = randGen.Next(0, size);
                int y = randGen.Next(0, size);

                // generate a random z-value weighted toward a deep depth
                float zf = 0;
                for (int j = 0; j < 4; j++)
                    zf += (float)randGen.NextDouble();
                zf /= 2;
                zf = 1 - Math.Abs(zf - 1);
                int z = (int)(zf * size);

                data[x, y, z] = BlockType.Diamond;
            }
        }

        // Gold appears in fairly numerous streaks, located at medium depths.
        public static void AddGold(ref BlockType[, ,] data, int size)
        {
            CaveInfo += "gold";

            int numVeins = 16;
            for (int i = 0; i < numVeins; i++)
            {
                int fieldLength = randGen.Next(size/3, size);
                float x = randGen.Next(0, size);
                float y = randGen.Next(0, size);
                
                // generate a random z-value weighted toward a medium depth
                float zf = 0;
                for (int j = 0; j < 4; j++)
                    zf += (float)randGen.NextDouble();
                zf /= 2;
                zf = 1 - Math.Abs(zf - 1);
                float z = zf * size;

                float dx = (float)randGen.NextDouble() * 2 - 1;
                float dy = (float)randGen.NextDouble() * 2 - 1;
                float dz = (float)randGen.NextDouble() * 2 - 1;
                float dl = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                dx /= dl; dy /= dl; dz /= dl;

                for (int j = 0; j < fieldLength; j++)
                {
                    x += dx;
                    y += dy;
                    z += dz;
                    if (x >= 0 && y >= 0 && z >= 0 && x < size && y < size && z < size)
                        data[(int)x, (int)y, (int)z] = BlockType.Gold;
                    int tx = 0, ty = 0, tz = 0;
                    switch (randGen.Next(0, 3))
                    {
                        case 0:
                            tx += 1;
                            break;
                        case 1:
                            ty += 1;
                            break;
                        case 2:
                            tz += 1;
                            break;
                    }
                    if (x + tx >= 0 && y + ty>= 0 && z+tz >= 0 && x+tx < size && y+ty < size && z+tz < size)
                        data[(int)x+tx, (int)y+ty, (int)z+tz] = BlockType.Gold;
                }
            }
        }

        // Generates a cube of noise with sides of length size. Noise falls in a linear
        // distribution ranging from 0 to magnitude.
        public static float[, ,] GenerateNoise(int size, float magnitude)
        {
            float[,,] noiseArray = new float[size, size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                        noiseArray[x, y, z] = (float)randGen.NextDouble() * magnitude;
            return noiseArray;
        }

        // Generates some perlin noise!
        public static float[,,] GeneratePerlinNoise(int size)
        {
            float[,,] data = new float[size, size, size];

            float[,,] noise = null;
            for (int f = 4; f < 32; f *= 2)
            {
                noise = GenerateNoise(f, 2f / f);
                noise = InterpolateData(ref noise, f, size);
                AddDataTo(ref data, ref noise, size);
            }

            return data;
        }

        // Does a random walk of noiseData, setting cells to 0 in caveData in the process.
        public static void PaintWithRandomWalk(ref BlockType[, ,] caveData, ref float[, ,] noiseData, int size, int paintRadius, BlockType paintValue, bool dontStopAtEdge)
        {
            int x = randGen.Next(0, size);
            int y = randGen.Next(0, size);
            int z = randGen.Next(0, size);

            if (z < size/50)
                z = 0;

            int count = 0;

            while (dontStopAtEdge == false || count < size)
            {
                float oldNoise = noiseData[x, y, z];

                PaintAtPoint(ref caveData, x, y, z, size, paintRadius+1, paintValue);
                int dx = randGen.Next(0, paintRadius * 2 + 1) - paintRadius;
                int dy = randGen.Next(0, paintRadius * 2 + 1) - paintRadius;
                int dz = randGen.Next(0, paintRadius * 2 + 1) - paintRadius;

                x += dx;
                y += dy;
                z += dz;

                if (x < 0 || y < 0 || x >= size || y >= size || z >= size)
                {
                    if (dontStopAtEdge)
                    {
                        count += 1;
                        if (x < 0) x = 0;
                        if (y < 0) y = 0;
                        if (z < 0) z = 0;
                        if (x >= size) x = size - 1;
                        if (y >= size) y = size - 1;
                        if (z >= size) z = size - 1;
                    }
                    else
                        break;
                }

                if (z < 0)
                    z = 0;

                float newNoise = noiseData[x, y, z];

                // If we're jumping to a higher value on the noise gradient, move twice as far.
                if (newNoise > oldNoise)
                {
                    PaintAtPoint(ref caveData, x, y, z, size, paintRadius+1, paintValue);
                    x += dx;
                    y += dy;
                    z += dz;

                    if (x < 0 || y < 0 || x >= size || y >= size || z >= size)
                    {
                        if (dontStopAtEdge)
                        {
                            count += 1;
                            if (x < 0) x = 0;
                            if (y < 0) y = 0;
                            if (z < 0) z = 0;
                            if (x >= size) x = size - 1;
                            if (y >= size) y = size - 1;
                            if (z >= size) z = size - 1;
                        }
                        else
                            break;
                    }

                    if (z < 0)
                        z = 0;
                }  
            }
        }

        public static void PaintAtPoint(ref BlockType[, ,] caveData, int x, int y, int z, int size, int paintRadius, BlockType paintValue)
        {
            for (int dx = -paintRadius; dx <= paintRadius; dx++)
                for (int dy = -paintRadius; dy <= paintRadius; dy++)
                    for (int dz = -paintRadius; dz <= paintRadius; dz++)
                        if (x+dx >= 0 && y+dy>= 0 && z+dz >= 0 && x+dx < size && y+dy < size && z+dz < size)
                            if (dx*dx+dy*dy+dz*dz<paintRadius*paintRadius)
                                caveData[x + dx, y + dy, z + dz] = paintValue;
        }

        // Generates a set of constant values.
        public static BlockType[, ,] GenerateConstant(int size, BlockType value)
        {
            BlockType[, ,] data = new BlockType[size, size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                        data[x, y, z] = value;
            return data;
        }

        public static float[, ,] GenerateGradient(int size)
        {
            float[, ,] data = new float[size, size, size];

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                        data[x, y, z] = (float)z / size;

            return data;
        }

        // Radial gradient concentrated with high values at the outside.
        public static float[, ,] GenerateRadialGradient(int size)
        {
            float[, ,] data = new float[size, size, size];

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                    {
                        float dist = (float)Math.Sqrt(Math.Pow(x - size / 2, 2) + Math.Pow(y - size / 2, 2));
                        data[x, y, z] = MathHelper.Clamp(dist / size * 0.3f * (float)z / size, 0, 1);
                    }
            return data;
        }

        // Adds the values in dataSrc to the values in dataDst, storing the result in dataDst.
        public static void AddDataTo(ref float[, ,] dataDst, ref float[, ,] dataSrc, int size, float scalarDst, float scalarSrc)
        {
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                        dataDst[x, y, z] = Math.Max(Math.Min(dataDst[x, y, z]*scalarDst + dataSrc[x, y, z]*scalarSrc, 1), 0);
        }
        public static void AddDataTo(ref float[, ,] dataDst, ref float[, ,] dataSrc, int size)
        {
            AddDataTo(ref dataDst, ref dataSrc, size, 1, 1);
        }

        // Resizes dataIn, with size sizeIn, to be of size sizeOut.
        public static float[, ,] InterpolateData(ref float[, ,] dataIn, int sizeIn, int sizeOut)
        {
            Debug.Assert(sizeOut > sizeIn, "sizeOut must be greater than sizeIn");
            Debug.Assert(sizeOut % sizeIn == 0, "sizeOut must be a multiple of sizeIn");

            float[,,] dataOut = new float[sizeOut, sizeOut, sizeOut];

            int r = sizeOut / sizeIn;

            for (int x=0; x<sizeOut; x++)
                for (int y = 0; y < sizeOut; y++)
                    for (int z = 0; z < sizeOut; z++)
                    {
                        int xIn0 = x / r,       yIn0 = y / r,       zIn0 = z / r;
                        int xIn1 = xIn0 + 1,    yIn1 = yIn0 + 1,    zIn1 = zIn0 + 1;
                        if (xIn1 >= sizeIn)
                            xIn1 = 0;
                        if (yIn1 >= sizeIn)
                            yIn1 = 0;
                        if (zIn1 >= sizeIn)
                            zIn1 = 0;

                        float v000 = dataIn[xIn0, yIn0, zIn0];
                        float v100 = dataIn[xIn1, yIn0, zIn0];
                        float v010 = dataIn[xIn0, yIn1, zIn0];
                        float v110 = dataIn[xIn1, yIn1, zIn0];
                        float v001 = dataIn[xIn0, yIn0, zIn1];
                        float v101 = dataIn[xIn1, yIn0, zIn1];
                        float v011 = dataIn[xIn0, yIn1, zIn1];
                        float v111 = dataIn[xIn1, yIn1, zIn1];

                        float xS = ((float)(x % r)) / r;
                        float yS = ((float)(y % r)) / r;
                        float zS = ((float)(z % r)) / r;

                        dataOut[x, y, z] =  v000 * (1 - xS) * (1 - yS) * (1 - zS) +
                                            v100 * xS * (1 - yS) * (1 - zS) +
                                            v010 * (1 - xS) * yS * (1 - zS) +
                                            v001 * (1 - xS) * (1 - yS) * zS +
                                            v101 * xS * (1 - yS) * zS +
                                            v011 * (1 - xS) * yS * zS +
                                            v110 * xS * yS * (1 - zS) +
                                            v111 * xS * yS * zS;
                    }

            return dataOut;
        }

        // Renders a specific z-level of a 256x256x256 data array to a texture.
        private static uint[] pixelData = new uint[256 * 256];
        public static void RenderSlice(ref BlockType[, ,] data, int z, Texture2D renderTexture)
        {
            for (int x = 0; x < 256; x++)
                for (int y = 0; y < 256; y++)
                {
                    uint c = 0xFF000000;
                    if (data[x,y,z] == BlockType.Dirt)
                        c = 0xFFFFFFFF;
                    if (data[x, y, z] == BlockType.Ore)
                        c = 0xFF888888;
                    if (data[x, y, z] == BlockType.Gold)
                        c = 0xFFFF0000;
                    if (data[x, y, z] == BlockType.Rock)
                        c = 0xFF0000FF;
                    pixelData[y * 256 + x] = c;
                }
            renderTexture.GraphicsDevice.Textures[0] = null;
            renderTexture.SetData(pixelData);
        }
    }
}
