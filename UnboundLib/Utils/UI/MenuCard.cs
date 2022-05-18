using UnityEngine;

namespace UnboundLib.Utils.UI
{
    internal class MenuCard : MonoBehaviour
    {
        private ScaleShake scaleShake;

        private void Start()
        {
            scaleShake = GetComponentInChildren<ScaleShake>();
        }

        private void Update()
        {
            if (scaleShake == null)
            {
                scaleShake = GetComponentInChildren<ScaleShake>();
                return;
            }
            if (scaleShake.targetScale == 1f) return;
            scaleShake.targetScale = 1f;
        }
    }
}
