using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnboundLib.GameModes
{
    public abstract class GameModeHandler<T> : IGameModeHandler where T : Component
    {
        public void AddHook(string key, Action<IGameModeHandler> action)
        {
            // Case-insensitive keys for QoL
            key = key.ToLower();

            if (!this.hooks.ContainsKey(key))
            {
                this.hooks.Add(key, action);
            }
            else
            {
                this.hooks[key] += action;
            }
        }

        public void RemoveHook(string key, Action<IGameModeHandler> action)
        {
            this.hooks[key.ToLower()] -= action;
        }

        public void TriggerHook(string key)
        {
            Action<IGameModeHandler> hook;
            this.hooks.TryGetValue(key.ToLower(), out hook);

            if (hook != null)
            {
                hook(this);
            }
        }

        public T GameMode {
            get
            {
                return GameModeManager.GetGameMode<T>(this.gameModeId);
            }
        }

        public abstract GameSettings Settings { get; protected set; }
        public abstract string Name { get; }

        // Used to find the correct game mode from scene
        private readonly string gameModeId;

        private Dictionary<string, Action<IGameModeHandler>> hooks = new Dictionary<string, Action<IGameModeHandler>>();

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

        public abstract void SetActive(bool active);

        public abstract void StartGame();
    }
}
