using UnityEngine;

namespace UnboundLib.GameModes
{
    /// <inheritdoc/>
    public abstract class GameModeHandler<T> : IGameModeHandler<T> where T : MonoBehaviour
    {
        public T GameMode {
            get
            {
                return GameModeManager.GetGameMode<T>(this.gameModeId);
            }
        }

        MonoBehaviour IGameModeHandler.GameMode
        {
            get
            {
                return this.GameMode;
            }
        }

        public abstract GameSettings Settings { get; protected set; }
        public abstract string Name { get; }

        // Used to find the correct game mode from scene
        private readonly string gameModeId;

        protected GameModeHandler(string gameModeId)
        {
            this.gameModeId = gameModeId;
        }

        public void SetSettings(GameSettings settings)
        {
            this.Settings = settings;

            foreach (var entry in this.Settings)
            {
                this.ChangeSetting(entry.Key, entry.Value);
            }
        }

        public virtual void ChangeSetting(string name, object value)
        {
            var newSettings = new GameSettings();

            foreach (var entry in this.Settings)
            {
                newSettings.Add(entry.Key, entry.Key == name ? value : entry.Value);
            }

            this.Settings = newSettings;
        }

        public abstract void PlayerJoined(Player player);

        public abstract void PlayerDied(Player killedPlayer, int playersAlive);

        public abstract TeamScore GetTeamScore(int teamID);

        public abstract void SetTeamScore(int teamID, TeamScore score);

        public abstract void SetActive(bool active);

        public abstract void StartGame();

        public abstract void ResetGame();
    }
}
