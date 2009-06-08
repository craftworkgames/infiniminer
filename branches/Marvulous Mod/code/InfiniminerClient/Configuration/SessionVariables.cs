namespace Infiniminer
{
    public class SessionVariables
    {
        public static void reset()
        {
            gZip = false;
            connectionPort = 5565;

            byte team = 0;
            while (team < teams.Length)
            {
                teams[team].name        = Team.defaultTeams()[team].name;
                teams[team].color        = Team.defaultTeams()[team].color;
                teams[team].blood        = Team.defaultTeams()[team].blood;
                teams[team].oreCount     = Team.defaultTeams()[team].oreCount;
                teams[team].goldCount    = Team.defaultTeams()[team].goldCount;
                teams[team].diamondCount = Team.defaultTeams()[team].diamondCount;
                ++team;
            }

            goldCash = GlobalVariables.goldCash;
            goldWeight = GlobalVariables.goldWeight;
            diamondCash = GlobalVariables.diamondCash;
            diamondWeight = GlobalVariables.diamondWeight;
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

        private static Team[] _teams = new Team[3] {
                new Team(
                    Team.defaultTeams()[(byte)PlayerTeam.None].name,
                    Team.defaultTeams()[(byte)PlayerTeam.None].color,
                    Team.defaultTeams()[(byte)PlayerTeam.None].blood
                ),
                new Team(
                    Team.defaultTeams()[(byte)PlayerTeam.A].name,
                    Team.defaultTeams()[(byte)PlayerTeam.A].color,
                    Team.defaultTeams()[(byte)PlayerTeam.A].blood
                ),
                new Team(
                    Team.defaultTeams()[(byte)PlayerTeam.B].name,
                    Team.defaultTeams()[(byte)PlayerTeam.B].color,
                    Team.defaultTeams()[(byte)PlayerTeam.B].blood
                ),
            };
        public static Team[] teams
        {
            get { return _teams; }
        }
        public static Team[] playableTeams()
        {
            Team[] playableTeams = new Team[teams.Length - 1];
            ushort i = 1;
            while (i < teams.Length)
            {
                playableTeams[i - 1] = teams[i];
                ++i;
            }
            return playableTeams;
        }

        private static ushort _goldCash = GlobalVariables.goldCash;
        public static ushort goldCash
        {
            get { return _goldCash; }
            set { _goldCash = value; }
        }
        private static byte _goldWeight = GlobalVariables.goldWeight;
        public static byte goldWeight
        {
            get { return _goldWeight; }
            set { _goldWeight = value; }
        }

        private static ushort _diamondCash = GlobalVariables.diamondCash;
        public static ushort diamondCash
        {
            get { return _diamondCash; }
            set { _diamondCash = value; }
        }
        private static byte _diamondWeight = GlobalVariables.diamondWeight;
        public static byte diamondWeight
        {
            get { return _diamondWeight; }
            set { _diamondWeight = value; }
        }
    }
}