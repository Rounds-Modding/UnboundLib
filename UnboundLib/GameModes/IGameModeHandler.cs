using UnityEngine;

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
        MonoBehaviour GameMode { get; }

        GameSettings Settings { get; }

        UISettings UISettings { get; }

        bool OnlineOnly { get; }
        bool AllowTeams { get; }

        string Name { get; }

        void SetSettings(GameSettings settings);

        void ChangeSetting(string name, object value);

        void PlayerJoined(Player player);

        void PlayerDied(Player killedPlayer, int playersAlive);

        /// <summary>
        ///     Should handle cleanup when a player leaves the game mid-match, ideally in such a way that allows the game to continue.
        ///     The default implementation takes care of reassigning playerIDs and teamIDs to ensure they are sequential, but any extra
        ///     handling must be done here.
        /// </summary>
        void PlayerLeft(Player leftPlayer);

        /// <summary>
        ///     Should return the current score of a team. This value should reflect the state of the actual game when applicable.
        /// </summary>
        /// <param name="teamID">The ID of the team whose score should be returned.</param>
        /// <returns></returns>
        TeamScore GetTeamScore(int teamID);

        /// <summary>
        ///     Sets the current score of a team. Changing the score should reflect in the actual game when applicable.
        /// </summary>
        /// <param name="teamID">ID of the team whose score should be changed.</param>
        /// <param name="score">Score to set for the team.</param>
        void SetTeamScore(int teamID, TeamScore score);

        /// <summary>
        ///     Returns an array of all teamIDs for which the team has satisfied the win condition.
        ///     Should return an empty array when there are no winners
        /// </summary>
        int[] GetGameWinners();

        /// <summary>
        ///     Returns an array of all teamIDs for which won the most recent round
        ///     Should return an empty array when there has yet to be any round winners in the game so far
        /// </summary>
        int[] GetRoundWinners();

        /// <summary>
        ///     Returns an array of all teamIDs for which won the most recent point
        ///     Should return an empty array when there has yet to be any point winners in the game so far
        /// </summary>
        int[] GetPointWinners();

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

        /// <summary>
        ///     Should tell the game mode to reset any state, such as player score and cards. The game should NOT restart.
        /// </summary>
        void ResetGame();
    }

    public interface IGameModeHandler<T> : IGameModeHandler where T : MonoBehaviour
    {
        new T GameMode { get; }
    }
}
