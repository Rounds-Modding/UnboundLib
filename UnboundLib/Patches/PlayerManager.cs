using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnboundLib.Extensions;


namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.GetColorFromPlayer))]
    class PlayerManager_Patch_GetColorFromPlayer
    {
        static void Prefix(ref int playerID)
        {
            playerID = PlayerManager.instance.players[playerID].colorID();
        }
    }
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.GetColorFromTeam))]
    class PlayerManager_Patch_GetColorFromTeam
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_playerID = UnboundLib.ExtensionMethods.GetFieldInfo(typeof(Player), "playerID");
            var m_colorID = UnboundLib.ExtensionMethods.GetMethodInfo(typeof(PlayerExtensions), nameof(PlayerExtensions.colorID));

            foreach (var ins in instructions)
            {
                if (ins.LoadsField(f_playerID))
                {
                    // we want colorID instead of teamID
                    yield return new CodeInstruction(OpCodes.Call, m_colorID); // call the colorID method, which pops the player instance off the stack and leaves the result [colorID, ...]
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }
}
