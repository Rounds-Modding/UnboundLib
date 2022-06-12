using System;
using System.Collections;
namespace UnboundLib.GameModes
{
    // When possible, use these keys when adding game hooks to a game mode
    public static class GameModeHooks
    {
        public static class Priority
        {
            public const int Last = 0;
            public const int VeryLow = 100;
            public const int Low = 200;
            public const int LowerThanNormal = 300;
            public const int Normal = 400;
            public const int HigherThanNormal = 500;
            public const int High = 600;
            public const int VeryHigh = 700;
            public const int First = 800;
        }
        public class Hook
        {
            public Func<IGameModeHandler, IEnumerator> Action;
            public int Priority;
            public Hook(Func<IGameModeHandler, IEnumerator> hook, int priority)
            {
                this.Action = hook;
                this.Priority = priority;
            }
        }

        /// <summary>
        ///     Should be called before the game mode does any initialization.
        /// </summary>
        public const string HookInitStart = "InitStart";

        /// <summary>
        ///     Should be called after the game mode has done its initialization.
        /// </summary>
        public const string HookInitEnd = "InitEnd";

        /// <summary>
        ///     Should be called when the game begins for the first time or after a rematch.
        /// </summary>
        public const string HookGameStart = "GameStart";

        /// <summary>
        ///     Should be called after the last round of the game ends. A rematch can be issued after this hook.
        /// </summary>
        public const string HookGameEnd = "GameEnd";

        /// <summary>
        ///     Should be called right after a round begins, after players have picked new cards.
        /// </summary>
        public const string HookRoundStart = "RoundStart";

        /// <summary>
        ///     Should be called right after a round ends, before players can pick new cards.
        /// </summary>
        public const string HookRoundEnd = "RoundEnd";

        /// <summary>
        ///     Should be called right after a player or team has received a point and all players have been revived for the next battle.
        ///     Should be called after RoundStart hook when applicable.
        /// </summary>
        public const string HookPointStart = "PointStart";

        /// <summary>
        ///     Should be called right after a player or team has received a point.
        ///     Should be called before RoundEnd hook when applicable.
        /// </summary>
        public const string HookPointEnd = "PointEnd";

        /// <summary>
        ///     Should be called when all players are vulnerable and can start fighting.
        /// </summary>
        public const string HookBattleStart = "BattleStart";

        /// <summary>
        ///     Should be called when players or teams are presented with new cards.
        /// </summary>
        public const string HookPickStart = "PickStart";

        /// <summary>
        ///     Should be called after all players or teams have picked new cards, before the next round begins.
        /// </summary>
        public const string HookPickEnd = "PickEnd";

        /// <summary>
        ///     Should be called each time a player or team is presented with new cards.
        /// </summary>
        public const string HookPlayerPickStart = "PlayerPickStart";

        /// <summary>
        ///     Should be called each time a player or team has chosen a new card.
        /// </summary>
        public const string HookPlayerPickEnd = "PlayerPickEnd";
    }
}
