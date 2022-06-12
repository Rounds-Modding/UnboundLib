using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnboundLib
{
    public class ModalHandler : MonoBehaviour
    {
        private Text title => transform.Find("Foreground/Title Bar/Title").GetComponent<Text>();
        private TextMeshProUGUI content => transform.Find("Foreground/Content/Text").GetComponent<TextMeshProUGUI>();
        private GameObject confirmButton => transform.Find("Foreground/Buttons/Got It").gameObject;
        private GameObject cancelButton => transform.Find("Foreground/Buttons/Whatever").gameObject;

        private void Start()
        {
            // automatically destroy the modal 1 second after it's been dismissed
            confirmButton.GetComponent<Button>().onClick.AddListener(() => Destroy(gameObject, 1f));
            cancelButton.GetComponent<Button>().onClick.AddListener(() => Destroy(gameObject, 1f));
        }

        public ModalHandler Title(string text)
        {
            title.text = text;
            return this;
        }
        public ModalHandler Message(string text)
        {
            content.text = text;
            return this;
        }
        public ModalHandler ConfirmButton(string text, UnityAction action)
        {
            SetupButton(confirmButton, text, action);
            return this;
        }
        public ModalHandler CancelButton(string text, UnityAction action)
        {
            SetupButton(cancelButton, text, action);
            return this;
        }
        private static void SetupButton(GameObject root, string text, UnityAction action)
        {
            root.GetComponent<Button>().onClick.AddListener(action);
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>())
            {
                t.text = text;
            }
        }
        public void Show()
        {
            GetComponent<Animator>().Play("Fade-in");
        }
    }
}
