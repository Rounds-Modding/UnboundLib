using System;

namespace UnboundLib.GameModes
{
    /// <summary>
    ///     A GameModeHandler is the interface between a mod and a game mode component. The game mode component, for example GM_ArmsRace,
    ///     will implement the bulk of the gameplay logic. A GameModeHandler will store game settings that the game mode should respect,
    ///     and provides hooks that the game mode should call at appropriate times. A GameModeHandler will tell the game mode when to
    ///     activate and start the game, but that is it.
    ///     
    ///     Game settings and hooks should allow mods to modify basic aspects of gameplay with little effort without needing to know the
    ///     internals of said game mode, and so a lot of responsibility is placed on game modes to provide sufficient setting and hook support.
    /// </summary>
    public interface IGameModeHandler
    {
        GameSettings Settings { get; }

        string Name { get; }

        void RemoveHook(string key, Action<IGameModeHandler> action);

        void AddHook(string key, Action<IGameModeHandler> action);

        void TriggerHook(string key);

        void SetSettings(GameSettings settings);

        void ChangeSetting(string name, object value);

        void PlayerJoined(Player player);

        void PlayerDied(Player killedPlayer, int playersAlive);

        /// <summary>
        ///     When true, should tell the game mode to activate and run any initialization code it might have.
        ///     When false, should tell the game mode to deactivate and hide any possible visual elements it might've drawn.
        ///     Typical behaviour is to activate or disable the game mode's gameobject.
        /// </summary>
        /// <param name="active">Activate or deactivate game mode</param>
        void SetActive(bool active);

        /// <summary>
        ///     Should tell the game mode to start the game.
        /// </summary>
        void StartGame();
    }
}
