namespace UnboundLib.GameModes
{
    class SandboxHandler : GameModeHandler<GM_Test>
    {
        public override string Name
        {
            get { return "Sandbox"; }
        }

        public override GameSettings Settings { get; protected set; }

        public SandboxHandler(string id) : base(id) {
            this.Settings = new GameSettings();
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
