using System;
using Microsoft.Xna.Framework.Graphics;
namespace Infiniminer
{
    public class configHelper
    {
        public static void floatTernaryConfig(ref float var, string key, DatafileLoader data, float min, float max)
        {
            if (min == max) // no point calculating the valid range if they're equal
            {
                var = min;
            }
            else if (max < min) // switch the variables around if someone is playing silly buggers
            {
                float _max = max;
                max = min;
                min = _max;
            }
            var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, float.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
        }
        public static void boolTernaryConfig(ref bool var, string key, DatafileLoader data)
        {
            var = data.Data.ContainsKey(key) ? bool.Parse(data.Data[key]) : var;
        }
        public static void intTernaryConfig(ref int var, string key, DatafileLoader data, int min, int max)
        {
            var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, int.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
        }
        public static void uintTernaryConfig(ref uint var, string key, DatafileLoader data, uint min, uint max)
        {
            var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, uint.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
        }
        public static void ushortTernaryConfig(ref ushort var, string key, DatafileLoader data, ushort min, ushort max)
        {
            var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, ushort.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
        }
        public static void stringTernaryConfig(ref string var, string key, DatafileLoader data)
        {
            var = data.Data.ContainsKey(key) ? data.Data[key] : var;
        }
        public static void colorTernaryConfig(ref Color var, string key, DatafileLoader data)
        {
            string _color = "";
            configHelper.stringTernaryConfig(ref _color, key, data);
            var = string2Color(_color);
        }
        public static Color string2Color(string _color)
        {
            _color = _color.Trim();
            string[] _color_RGB = _color.Split(',');
            if (_color_RGB.Length == 3)
            {
                try
                {
                    byte _color_R = byte.Parse(_color_RGB[0]);
                    byte _color_G = byte.Parse(_color_RGB[1]);
                    byte _color_B = byte.Parse(_color_RGB[2]);
                    return new Color(_color_R,_color_G,_color_B);
                }
                catch (Exception)
                {
                    throw new ArgumentException("Could not convert color arguments from string.");
                }
            }
            else
            {
                throw new ArgumentException("string2Color requires a CSV of 3 ushort arguments");
            }
        }
        public static string color2String(Color _color)
        {
            return _color.R + "," + _color.G + "," + _color.B;
        }
    }
    public class GlobalVariables
    {
        //Should be multiple of PACKETSIZE less than 256
        public const int MAPSIZE = 64;
    }
}