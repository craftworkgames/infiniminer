namespace Infiniminer
{
    public class SessionVariables
    {
        public static void reset()
        {
            gZip = false;
            connectionPort = 5565;

            teams[(byte)PlayerTeam.None].name = Team.defaultTeams()[(byte)PlayerTeam.None].name;
            teams[(byte)PlayerTeam.None].color = Team.defaultTeams()[(byte)PlayerTeam.None].color;
            teams[(byte)PlayerTeam.None].blood = Team.defaultTeams()[(byte)PlayerTeam.None].blood;

            teams[(byte)PlayerTeam.A].name = Team.defaultTeams()[(byte)PlayerTeam.A].name;
            teams[(byte)PlayerTeam.A].color = Team.defaultTeams()[(byte)PlayerTeam.A].color;
            teams[(byte)PlayerTeam.A].blood = Team.defaultTeams()[(byte)PlayerTeam.A].blood;

            teams[(byte)PlayerTeam.B].name = Team.defaultTeams()[(byte)PlayerTeam.B].name;
            teams[(byte)PlayerTeam.B].color = Team.defaultTeams()[(byte)PlayerTeam.B].color;
            teams[(byte)PlayerTeam.B].blood = Team.defaultTeams()[(byte)PlayerTeam.B].blood;
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
    }
}