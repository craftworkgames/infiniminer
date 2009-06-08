using System;
using Microsoft.Xna.Framework.Graphics;
namespace Infiniminer
{
    public class configHelper
    {
        public static bool floatTernaryConfig(ref float var, string key, DatafileLoader data, float min, float max)
        {
#if !DEBUG
            try
            {
#endif
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
                return true;
#if !DEBUG
            }
            catch(Exception){ return false;}
#endif
        }
        public static bool boolTernaryConfig(ref bool var, string key, DatafileLoader data)
        {
#if !DEBUG
            try
            {
#endif
                var = data.Data.ContainsKey(key) ? bool.Parse(data.Data[key]) : var;
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static bool intTernaryConfig(ref int var, string key, DatafileLoader data, int min, int max)
        {
#if !DEBUG
            try
            {
#endif
                var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, int.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static bool uintTernaryConfig(ref uint var, string key, DatafileLoader data, uint min, uint max)
        {
#if !DEBUG
            try
            {
#endif
                var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, uint.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static bool ushortTernaryConfig(ref ushort var, string key, DatafileLoader data, ushort min, ushort max)
        {
#if !DEBUG
            try
            {
#endif
                var = data.Data.ContainsKey(key) ? Math.Max(min, Math.Min(max, ushort.Parse(data.Data[key], System.Globalization.CultureInfo.InvariantCulture))) : var;
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static bool stringTernaryConfig(ref string var, string key, DatafileLoader data)
        {
#if !DEBUG
            try
            {
#endif
                var = data.Data.ContainsKey(key) ? data.Data[key] : var;
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static bool colorTernaryConfig(ref Color var, string key, DatafileLoader data)
        {
#if !DEBUG
            try
            {
#endif
                string _color = "";
                configHelper.stringTernaryConfig(ref _color, key, data);
                var = string2Color(_color);
                return true;
#if !DEBUG
            }
            catch (Exception) { return false; }
#endif
        }
        public static Color string2Color(string _color)
        {
            _color = _color.Trim();
            string[] _color_RGB = _color.Split(',');
            if (_color_RGB.Length == 3)
            {
#if !DEBUG
                try
                {
#endif
                    byte _color_R = byte.Parse(_color_RGB[0]);
                    byte _color_G = byte.Parse(_color_RGB[1]);
                    byte _color_B = byte.Parse(_color_RGB[2]);
                    return new Color(_color_R,_color_G,_color_B);
#if !DEBUG
                }
                catch (Exception)
                {
                    throw new ArgumentException("Could not convert color arguments from string.");
                }
#endif
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
        public static string teamsVs(InfiniminerTeam[] teams)
        {
            string[] teamNames = new string[teams.Length];
            uint i = 0;
            while(i < teams.Length)
            {
                teamNames[i] = teams[i].name;
                ++i;
            }
            return string.Join(" vs. ",teamNames);
        }
    }
    public class GlobalVariables
    {
        //Should be multiple of PACKETSIZE less than 256
        public const int MAPSIZE = 64;
    }
    public class SessionVariables
    {
        public static void reset()
        {
            gZip = false;
            connectionPort = 5565;

            teams[0].name = InfiniminerTeam.defaultTeams()[0].name;
            teams[0].color = InfiniminerTeam.defaultTeams()[0].color;
            teams[0].blood = InfiniminerTeam.defaultTeams()[0].blood;

            teams[1].name = InfiniminerTeam.defaultTeams()[1].name;
            teams[1].color = InfiniminerTeam.defaultTeams()[1].color;
            teams[1].blood = InfiniminerTeam.defaultTeams()[1].blood;
        }

        private static bool gzip = false;
        public static bool gZip
        {
            get { return gzip; }
            set { gzip = value; }
        }
        private static ushort _connectionPort = 5565;
        public static ushort connectionPort
        {
            get
            {
                return _connectionPort;
            }
            set
            {
                _connectionPort = value;
            }
        }

        private static InfiniminerTeam[] _teams = new InfiniminerTeam[2] {
            new InfiniminerTeam(
                InfiniminerTeam.defaultTeams()[0].name,
                InfiniminerTeam.defaultTeams()[0].color,
                InfiniminerTeam.defaultTeams()[0].blood
            ),
            new InfiniminerTeam(
                InfiniminerTeam.defaultTeams()[1].name,
                InfiniminerTeam.defaultTeams()[1].color,
                InfiniminerTeam.defaultTeams()[1].blood
            ),
        };
        public static InfiniminerTeam[] teams
        {
            get { return _teams; }
        }
    }
    public class InfiniminerTeam
    {
        private static InfiniminerTeam[] _defaultTeams = new InfiniminerTeam[2] {
            new InfiniminerTeam("RED",new Color(222, 24, 24),Color.Red),
            new InfiniminerTeam("BLUE",new Color(80, 150, 255), Color.Blue)
        };
        public static InfiniminerTeam[] defaultTeams()
        {
            return _defaultTeams;
        }
        public InfiniminerTeam(string name, Color color, Color blood)
        {
            this.name = name;
            this.color = color;
            this.blood = blood;
        }
        private string _name;
        private Color _color;
        private Color _blood;
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }
        public Color color
        {
            get { return _color; }
            set { _color = value; }
        }
        public Color blood
        {
            get { return _blood; }
            set { _blood = value; }
        }
    }
}