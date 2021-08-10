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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ToggleLevelMenuHandler.instance.levelMenuCanvas.transform.Find("LevelMenu/InfoMenu").gameObject.activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.levelMenuCanvas.transform.Find("LevelMenu/InfoMenu").gameObject
                        .SetActive(false);
                    return false;
                }
                
                if (ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/InfoMenu").gameObject.activeInHierarchy)
                {
                    ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu/InfoMenu").gameObject
                        .SetActive(false);
                    return false;
                }

                if (ToggleLevelMenuHandler.instance.levelMenuCanvas.activeInHierarchy)
                {
                    ToggleLevelMenuHandler.instance.levelMenuCanvas.SetActive (false);
                    return false;
                }
                if (ToggleCardsMenuHandler.IsActive(ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu")))
                {
                    ToggleCardsMenuHandler.SetActive(ToggleCardsMenuHandler.toggleCardsCanvas.transform.Find("CardMenu"), false);
                    return false;
                }
            }
            

            return false;
        }
    }
}