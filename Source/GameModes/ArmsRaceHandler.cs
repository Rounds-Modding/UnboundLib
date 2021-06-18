namespace UnboundLib.GameModes
{
    public class ArmsRaceHandler : GameModeHandler<GM_ArmsRace>
    {
        public override string Name
        {
            get { return "ArmsRace"; }
        }

        public override GameSettings Settings { get; protected set; }

        public ArmsRaceHandler() : base("Arms race")
        {
            this.Settings = new GameSettings()
            {
                { "pointsToWinRound", 2 },
                { "roundsToWinGame", 5 }
            };
        }

        public override void SetActive(bool active)
        {
            this.GameMode.gameObject.SetActive(active);
        }

        public override void PlayerJoined(Player player)
        {
            this.GameMode.PlayerJoined(player);
        }

        public override void PlayerDied(Player killedPlayer, int playersAlive)
        {
            this.GameMode.PlayerDied(killedPlayer, playersAlive);
        }

        public override TeamScore GetTeamScore(int teamID)
        {
            if (teamID != 0 && teamID != 1)
            {
                return new TeamScore(0, 0);
            }

            return teamID == 0
                ? new TeamScore(GM_ArmsRace.instance.p1Points, GM_ArmsRace.instance.p1Rounds)
                : new TeamScore(GM_ArmsRace.instance.p2Points, GM_ArmsRace.instance.p2Rounds);
        }

        public override void SetTeamScore(int teamID, TeamScore score)
        {
            if (teamID == 0)
            {
                this.GameMode.p1Points = score.points;
                this.GameMode.p1Rounds = score.rounds;
            }
            if (teamID == 1)
            {
                this.GameMode.p2Points = score.points;
                this.GameMode.p2Rounds = score.rounds;
            }
        }

        public override void StartGame()
        {
            this.GameMode.StartGame();
        }

        public override void ResetGame()
        {
            PlayerManager.instance.InvokeMethod("ResetCharacters");
            GM_ArmsRace.instance.InvokeMethod("ResetMatch");
        }

        public override void ChangeSetting(string name, object value)
        {
            base.ChangeSetting(name, value);

            if (name == "roundsToWinGame")
            {
                int roundsToWinGame = (int) value;
                this.GameMode.roundsToWinGame = roundsToWinGame;
                UIHandler.instance.InvokeMethod("SetNumberOfRounds", roundsToWinGame);
            }

            if (name == "pointsToWinRound")
            {
                this.GameMode.SetFieldValue("pointsToWinRound", (int) value);
            }
        }
    }
}
