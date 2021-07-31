using HarmonyLib;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(EscapeMenuHandler))]
    public class EscapeMenuHandlerPath
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.activeInHierarchy)
            {
                UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Options(Clone)/Group").gameObject.SetActive(false);
                UIHandler.instance.transform.Find("Canvas/EscapeMenu/Main/Group").gameObject.SetActive(true);
                return false;
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) && LevelMenuHandler.instance.levelMenuCanvas.activeInHierarchy)
            {
                LevelMenuHandler.instance.levelMenuCanvas.SetActive (false);
                return false;
            }

            return true;
        }
    }
}