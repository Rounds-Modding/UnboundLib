using HarmonyLib;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(GoBack))]
    public class GoBackPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && LevelMenuHandler.instance.levelMenuCanvas.activeInHierarchy)
            {
                LevelMenuHandler.instance.levelMenuCanvas.SetActive (false);
                return false;
            }

            return true;
        }
    }
}