using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnboundLib.Patches
{
    internal class cardBarPatch
    {

        [HarmonyPatch(typeof (MapManager), "OnLevelFinishedLoading")]
        private class Patch_Jump
        {
            private static void Prefix(Scene scene, LoadSceneMode mode)
            {
                var sfPoly = Resources.FindObjectsOfTypeAll<SFPolygon>();
                var sfFilter = new List<SFPolygon>();
                foreach (var sf in sfPoly)
                {
                    if (sf.name.Contains("Ground"))
                    {
                        sfFilter.Add(sf);
                    }

                    if (sf.name.Contains("EDITOR BORDER"))
                    {
                        sf.gameObject.SetActive(false);
                    }
                }
                if (sfFilter.Count == 0)
                {
                    UnityEngine.Debug.LogError("No ground found?");
                }
                foreach (var sf in sfFilter)
                {
                    if (sf.GetComponent<SpriteRenderer>() != null)
                    {
                        sf.GetComponent<SpriteRenderer>().material.shader = Shader.Find("Sprites/SFSoftShadowStencil");
                    }
                }
            }
        }
        
        

        
    }
}