using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace UnboundLib.GameModes
{
    public class SandboxHandler : GameModeHandler<GM_Test>
    {
        public override string Name
        {
            get { return "Sandbox"; }
        }

        public override GameSettings Settings { get; protected set; }

        public override ReadOnlyDictionary<int, TeamScore> TeamScore
        {
            get
            {
                var dict = new Dictionary<int, TeamScore>();

                foreach (var player in PlayerManager.instance.players)
                {
                    dict.Add(player.teamID, new TeamScore(0, 0));
                }

                return new ReadOnlyDictionary<int, TeamScore>(dict);
            }
        }

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

        public override TeamScore GetTeamScore(int teamID)
        {
            return new TeamScore(0, 0);
        }

        public override void SetTeamScore(int teamID, TeamScore score) { }

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

        public override void ResetGame()
        {
            PlayerManager.instance.InvokeMethod("ResetCharacters");
        }
    }
}
