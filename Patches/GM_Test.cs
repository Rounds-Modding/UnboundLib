using HarmonyLib;
using UnboundLib.GameModes;

namespace UnboundLib.Patches
{

    [HarmonyPatch(typeof(GM_Test), "Start")]
    class GM_Test_Patch_Start
    {
        static void Prefix()
        {
            GameModeHandler.TriggerHook("Sandbox", GameModeHooks.HookInitStart);
            GameModeHandler.TriggerHook("Sandbox", GameModeHooks.HookInitEnd);
            GameModeHandler.TriggerHook("Sandbox", GameModeHooks.HookGameStart);
        }

        static void Postfix()
        {
            GameModeHandler.TriggerHook("Sandbox", GameModeHooks.HookRoundStart);
            GameModeHandler.TriggerHook("Sandbox", GameModeHooks.HookBattleStart);
        }
    }
}
