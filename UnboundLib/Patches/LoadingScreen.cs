using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnboundLib.GameModes;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(LoadingScreen), "IDoLoading")]
    class LoadingScreen_Patch_IDoLoading
    {
        static void ActivateGameMode()
        {
            GameModeManager.CurrentHandler.SetActive(true);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Remove the default player joined and died -hooks. We'll add them back through the GameMode abstraction layer.
            var list = instructions.ToList();
            var newInstructions = new List<CodeInstruction>();

            var f_gameMode = ExtensionMethods.GetFieldInfo(typeof(LoadingScreen), "gameMode");
            var m_objectSetActive = ExtensionMethods.GetMethodInfo(typeof(GameObject), "SetActive");
            var m_gmSetActive = ExtensionMethods.GetMethodInfo(typeof(LoadingScreen_Patch_IDoLoading), "ActivateGameMode");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(f_gameMode) && list[i + 2].Calls(m_objectSetActive))
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, m_gmSetActive));
                    i += 2;
                }
                else
                {
                    newInstructions.Add(list[i]);
                }
            }

            return newInstructions;
        }
    }
}
