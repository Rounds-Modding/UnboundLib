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
            if (Input.GetKeyDown(KeyCode.Escape) && ToggleLevelMenuHandler.instance.levelMenuCanvas.activeInHierarchy)
            {
                ToggleLevelMenuHandler.instance.levelMenuCanvas.SetActive (false);
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && ToggleCardsMenuHandler.IsActive(ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu")))
            {
                ToggleCardsMenuHandler.SetActive(ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu"), false);
                return false;
            }

            return true;
        }
    }
}