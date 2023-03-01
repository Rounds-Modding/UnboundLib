using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace UnboundLib.Patches
{
    internal class MapLoaderPatch
    {
        [HarmonyPatch(typeof(MapManager), "OnLevelFinishedLoading")]
        private class Patch_OnLevelFinishedLoading
        {
            private static GameObject _mapInfoPrefab;
            private static GameObject MapInfoPrefab
            {
                get
                {
                    if (_mapInfoPrefab == null)
                    {
                        _mapInfoPrefab = Unbound.mapInfoAssets.LoadAsset<GameObject>("Map Info Canvas");
                    }
                    return _mapInfoPrefab;
                }
            }
            private static Canvas mapDetailsCanvas;

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

            private static void Postfix(Scene scene, LoadSceneMode mode)
            {
                // disable canvas in last map
                if (mapDetailsCanvas) mapDetailsCanvas.gameObject.SetActive(false);

                // create canvas for new map
                mapDetailsCanvas = GameObject.Instantiate(MapInfoPrefab).GetComponent<Canvas>();
                SceneManager.MoveGameObjectToScene(mapDetailsCanvas.gameObject, scene);

                // set level text
                var mapName = MapManager.instance.levels[MapManager.instance.currentLevelID];
                mapDetailsCanvas.GetComponentInChildren<TextMeshProUGUI>().text = mapName;
            }
        }
    }
}