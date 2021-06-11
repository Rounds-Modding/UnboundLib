using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace UnboundLib
{
    public class ModalHandler : MonoBehaviour
    {
        private Text title
        {
            get { return transform.Find("Foreground/Title Bar/Title").GetComponent<Text>(); }
        }
        private TextMeshProUGUI content
        {
            get { return transform.Find("Foreground/Content/Text").GetComponent<TextMeshProUGUI>(); }
        }
        private GameObject confirmButton
        {
            get { return transform.Find("Foreground/Buttons/Got It").gameObject; }
        }
        private GameObject cancelButton
        {
            get { return transform.Find("Foreground/Buttons/Whatever").gameObject; }
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
        private void SetupButton(GameObject root, string text, UnityAction action)
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
