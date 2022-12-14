using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnboundLib.Patches
{
    internal class MapLoaderPatch
    {
        [HarmonyPatch(typeof (MapManager), "OnLevelFinishedLoading")]
        private class Patch_OnLevelFinishedLoading
        {
            private static void Prefix(Scene scene, LoadSceneMode mode)
            {
                var sfPoly = Resources.FindObjectsOfTypeAll<SFPolygon>();
                foreach (var sf in sfPoly)
                {
                    // remove editor helpers
                    if (sf.name.Contains("EDITOR BORDER"))
                    {
                        sf.gameObject.SetActive(false);
                    }
                }
                if (sfPoly.Length == 0)
                {
                    UnityEngine.Debug.LogError("No ground found?");
                }
                foreach (var sf in sfPoly)
                {
                    if (sf.GetComponent<SpriteRenderer>() != null && !sf.name.Contains("Health"))
                    {
                        sf.GetComponent<SpriteRenderer>().material.shader = Shader.Find("Sprites/SFSoftShadowStencil");
                    }
                }
            }
        }
    }
}