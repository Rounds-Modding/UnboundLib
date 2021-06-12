namespace UnboundLib.GameModes
{
    class ArmsRaceHandler : GameModeHandler<GM_ArmsRace>
    {
        public override string Name
        {
            get { return "ArmsRace"; }
        }

        public override GameSettings Settings { get; protected set; }

        public ArmsRaceHandler(string id) : base(id)
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

        public override void StartGame()
        {
            this.GameMode.StartGame();
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
