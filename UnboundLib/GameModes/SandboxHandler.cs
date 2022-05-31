namespace UnboundLib.GameModes
{
    public class SandboxHandler : GameModeHandler<GM_Test>
    {
        public override string Name => "Sandbox";
        public override bool AllowTeams => true;
        public override UISettings UISettings => new UISettings("A sandbox mode where you can play around, test out builds, and fight bots.");

        public override GameSettings Settings { get; protected set; }

        public SandboxHandler() : base(GameModeManager.SandBoxID) {
            Settings = new GameSettings();
        }

        public override void PlayerJoined(Player player)
        {
            GameMode.InvokeMethod("PlayerWasAdded", player);
        }

        public override void PlayerDied(Player killedPlayer, int playersAlive)
        {
            GameMode.InvokeMethod("PlayerDied", killedPlayer, playersAlive);
        }

        public override TeamScore GetTeamScore(int teamID)
        {
            return new TeamScore(0, 0);
        }

        public override void SetTeamScore(int teamID, TeamScore score) { }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                GameMode.gameObject.SetActive(active);
            }
        }

        public override void StartGame()
        {
            GameMode.gameObject.SetActive(true);
        }

        public override void ResetGame()
        {
            PlayerManager.instance.InvokeMethod("ResetCharacters");
        }
    }
}
