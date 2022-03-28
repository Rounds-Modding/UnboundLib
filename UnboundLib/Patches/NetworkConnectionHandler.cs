using HarmonyLib;
using UnboundLib.GameModes;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(NetworkConnectionHandler), "QuickMatch")]
    class NetworkConnectionHandler_Patch_QuickMatch
    {
        static void Prefix()
        {
            GameModeManager.SetGameMode(GameModeManager.ArmsRaceID, false);
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "TwitchJoin")]
    class NetworkConnectionHandler_Patch_TwitchJoin
    {
        static void Prefix()
        {
            GameModeManager.SetGameMode(GameModeManager.ArmsRaceID, false);
        }
    }

    [HarmonyPatch(typeof(NetworkConnectionHandler), "HostPrivateAndInviteFriend")]
    class NetworkConnectionHandler_Patch_HostPrivateAndInviteFriend
    {
        static void Prefix()
        {
            GameModeManager.SetGameMode(GameModeManager.ArmsRaceID, false);
        }
    }
}
