using UnityEngine;
using UnityEngine.EventSystems;

namespace UnboundLib.Utils.UI
{
    internal class CardAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            ToggleAnimation(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ToggleAnimation(false);
        }

        public void ToggleAnimation(bool value)
        {
            foreach (Animator animatorComponent in gameObject.GetComponentsInChildren<Animator>())
            {
                animatorComponent.enabled = value;
            }
            foreach (PositionNoise positionComponent in gameObject.GetComponentsInChildren<PositionNoise>())
            {
                positionComponent.enabled = value;
            }
        }
    }
}
