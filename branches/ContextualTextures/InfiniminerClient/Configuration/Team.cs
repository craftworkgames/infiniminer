using Microsoft.Xna.Framework.Graphics;
namespace Infiniminer
{
    public enum PlayerTeam : byte
    {
        None,
        A,
        B
    }
    public class Team
    {
        private static Team[] _defaultTeams = new Team[3] {
            new Team("",Color.White,Color.White),
            new Team("RED",new Color(222, 24, 24),Color.Red),
            new Team("BLUE",new Color(80, 150, 255), Color.Blue)
        };
        public static Team[] defaultTeams()
        {
            return _defaultTeams;
        }
        public static byte numTeams()
        {
            return (byte)PlayerTeam.B + 1;
        }
        private static PlayerTeam[] _playerTeams = null;
        public static PlayerTeam[] playerTeams
        {
            get
            {
                if (_playerTeams == null)
                {
                    _playerTeams = new PlayerTeam[numTeams()];
                    for (byte x = 0; x < numTeams(); ++x)
                    {
                        _playerTeams.SetValue((PlayerTeam)x, x);
                    }
                }
                return _playerTeams;
            }
        }
        public static string vs(Team[] teams)
        {
            string[] teamNames = new string[teams.Length];
            ushort i = 0;
            while (i < teams.Length)
            {
                teamNames[i] = teams[i].name;
                ++i;
            }
            return string.Join(" vs. ", teamNames);
        }
        public Team(string name, Color color, Color blood)
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

        public uint cash()
        {
            return (goldCount * SessionVariables.goldCash) + (diamondCount * SessionVariables.diamondCash);
        }
        private uint _oreCount = 0;
        public uint oreCount
        {
            get { return _oreCount; }
            set { _oreCount = value; }
        }
        private uint _goldCount = 0;
        public uint goldCount
        {
            get { return _goldCount; }
            set { _goldCount = value; }
        }
        private uint _diamondCount = 0;
        public uint diamondCount
        {
            get { return _diamondCount; }
            set { _diamondCount = value; }
        }
    }
}