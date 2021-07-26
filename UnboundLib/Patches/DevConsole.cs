using HarmonyLib;
using UnboundLib.Utils;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(DevConsole))]
    internal class DevConsolePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Send")]
        private static void Send_Postfix(string message)
        {
            if (Application.isEditor || (GM_Test.instance && GM_Test.instance.gameObject.activeSelf))
            {
                LevelManager.SpawnMap(message);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SpawnCard")]
        private static bool SpawnCard_Prefix(string message)
        {
            return !message.Contains("/");
        }
    }
}