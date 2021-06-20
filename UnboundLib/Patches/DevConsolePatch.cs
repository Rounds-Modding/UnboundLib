using HarmonyLib;
using UnityEngine;

namespace UnboundLib.Patches
{
    internal class DevConsolePatch
    {

        [HarmonyPatch(typeof (DevConsole), "Send")]
        private class Patch_Send
        {
            // ReSharper disable once UnusedMember.Local
            private static void Postfix(string message)
            {
                if (Application.isEditor || (GM_Test.instance && GM_Test.instance.gameObject.activeSelf))
                {
                    Unbound.SpawnMap(message);
                }
            }
        }

        [HarmonyPatch(typeof(DevConsole), "SpawnCard")]
        private class Patch_SpawnCard
        {
            private static bool Prefix(string message)
            {
                return !message.Contains("/");
            }
        }
        
        

        
    }
}