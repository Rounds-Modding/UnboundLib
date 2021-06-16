namespace UnboundLib.GameModes
{
    class ArmsRaceHandler : GameModeHandler<GM_ArmsRace>
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
            ((GM_ArmsRace)this.GameMode).PlayerJoined(player);
        }

        public override void PlayerDied(Player killedPlayer, int playersAlive)
        {
            ((GM_ArmsRace) this.GameMode).PlayerDied(killedPlayer, playersAlive);
        }

        public override void StartGame()
        {
            ((GM_ArmsRace) this.GameMode).StartGame();
        }

        public override void ChangeSetting(string name, object value)
        {
            base.ChangeSetting(name, value);

            if (name == "roundsToWinGame")
            {
                int roundsToWinGame = (int) value;
                ((GM_ArmsRace) this.GameMode).roundsToWinGame = roundsToWinGame;
                UIHandler.instance.InvokeMethod("SetNumberOfRounds", roundsToWinGame);
            }

            if (name == "pointsToWinRound")
            {
                this.GameMode.SetFieldValue("pointsToWinRound", (int) value);
            }
        }
    }
}
