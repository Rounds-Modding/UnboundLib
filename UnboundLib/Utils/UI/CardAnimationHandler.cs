using UnityEngine;
using UnityEngine.EventSystems;

namespace UnboundLib.Utils.UI
{
    internal class CardAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool toggled;

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
                if (animatorComponent.enabled == value) continue;
                animatorComponent.enabled = value;
            }
            foreach (PositionNoise positionComponent in gameObject.GetComponentsInChildren<PositionNoise>())
            {
                if (positionComponent.enabled == value) continue;
                positionComponent.enabled = value;
            }

            toggled = value;
        }

        private void Update()
        {
            ToggleAnimation(toggled);
        }
    }
}