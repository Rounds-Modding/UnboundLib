using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnboundLib.Patches
{
    [HarmonyPatch(typeof(MapManager), "OnLevelFinishedLoading")]
    class MapManager_Patch_OnLevelFinishedLoading
    {
        private static void Prefix(Scene scene, MapManager __instance)
        {
            foreach (Transform obj in scene.GetRootGameObjects()[0].GetComponentsInChildren<Transform>())
            {
                if (obj.name.IndexOf("EDITOR", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    obj.gameObject.SetActive(false);
                } 
                else if ( obj.name.IndexOf("NOT COL", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    // check if color was set to white in unity and change it back to white after 0.1 seconds because it changes somehow
                    if ( obj.GetComponent<SpriteRenderer>() && obj.GetComponent<SpriteRenderer>().color == Color.white)
                    {
                        __instance.ExecuteAfterSeconds(0.1f, () =>
                        {
                            obj.GetComponent<SpriteRenderer>().color = Color.white;
                        });
                    }
                    __instance.ExecuteAfterSeconds(0.1f, () =>
                    {
                        // check if it has the shader or if it doesn't have a SpriteRenderer
                        if ((obj.GetComponent<SpriteRenderer>() && obj.GetComponent<SpriteRenderer>().material.shader == Shader.Find("Sprites/SFSoftShadowStencil")) || !obj.GetComponent<SpriteRenderer>()) return;
                        // remove objects that adds art to it
                        Object.Destroy(obj.GetComponent<SpriteMask>());
                    });
                    continue;
                }

                if (obj.GetComponent<SpriteRenderer>() && obj.GetComponent<SpriteRenderer>().material.shader != Shader.Find("Sprites/SFSoftShadowStencil"))
                {
                    // add art shader
                    obj.GetComponent<SpriteRenderer>().material.shader = Shader.Find("Sprites/SFSoftShadowStencil");
                }
            }
        }
    }
}