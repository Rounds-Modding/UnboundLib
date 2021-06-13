namespace UnboundLib.GameModes
{
    class SandboxHandler : GameModeHandler<GM_Test>
    {
        public override string Name
        {
            get { return "Sandbox"; }
        }

        public override GameSettings Settings { get; protected set; }

        public SandboxHandler() : base("Test") {
            this.Settings = new GameSettings();
        }

        public override void PlayerJoined(Player player)
        {
            this.GameMode.InvokeMethod("PlayerWasAdded", player);
        }

        public override void PlayerDied(Player killedPlayer, int playersAlive)
        {
            this.GameMode.InvokeMethod("PlayerDied", killedPlayer, playersAlive);
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                this.GameMode.gameObject.SetActive(active);
            }
        }

        public override void StartGame()
        {
            this.GameMode.gameObject.SetActive(true);
        }
    }
}
