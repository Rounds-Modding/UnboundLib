using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnboundLib.GameModes;

namespace UnboundLib.Patches
{

    [HarmonyPatch(typeof(Player), "Start")]
    class Player_Patch_Start
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /* Removes
             *   GM_ArmsRace instance = GM_ArmsRace.instance;
			 *   instance.StartGameAction += this.GetFaceOffline;
			 * from Player::Start
             */
            var f_gmInstance = AccessTools.Field(typeof(GM_ArmsRace), "instance");
            var f_startGameAction = ExtensionMethods.GetFieldInfo(typeof(GM_ArmsRace), "StartGameAction");
            var m_getFace = ExtensionMethods.GetMethodInfo(typeof(Player), "GetFaceOffline");

            var list = instructions.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_gmInstance) &&
                    list[i + 2].LoadsField(f_startGameAction) &&
                    list[i + 4].OperandIs(m_getFace) &&
                    list[i + 8].LoadsField(f_startGameAction))
                {
                    i += 8;
                }
                else
                {
                    yield return list[i];
                }
            }
        }

        static void Postfix(Player __instance)
        {
            if (__instance.data.view.IsMine)
            {
                GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => OnGameStart(gm, __instance));
            }
        }

        static IEnumerator OnGameStart(IGameModeHandler gm, Player player)
        {
            if (gm.Name != "Sandbox")
            {
                player.GetFaceOffline();
            }
            yield break;
        }
    }
}
