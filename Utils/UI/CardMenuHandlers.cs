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
    internal class CardToggleMenuHandler : MonoBehaviour
    {
        private GameObject _togglePrefab;
        private GameObject TogglePrefab
        {
            get
            {
                if (_togglePrefab == null)
                {
                    _togglePrefab = Unbound.UIAssets.LoadAsset<GameObject>("Card Detail");
                }
                return _togglePrefab;
            }
        }

        public static CardToggleMenuHandler Instance { get; private set; }

        private Animator animator
        {
            get { return GetComponent<Animator>(); }
        }
        private Transform content
        {
            get { return transform.Find("Viewport/Content"); }
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Show()
        {
            animator.Play("Fade-in");
        }
        public void Hide()
        {
            animator.Play("Fade-out");
        }
        public void AddCardToggle(CardInfo info)
        {
            Instantiate(TogglePrefab, content).AddComponent<CardToggleHandler>()
                .SetName(info.cardName)
                .SetActions(
                () =>
                {
                    if (Unbound.activeCards.Contains(info))
                    {
                        Unbound.activeCards.Remove(info);
                    }
                    if (!Unbound.inactiveCards.Contains(info))
                    {
                        Unbound.inactiveCards.Add(info);
                    }
                },
                () =>
                {
                    if (!Unbound.activeCards.Contains(info))
                    {
                        Unbound.activeCards.Add(info);
                    }
                    if (Unbound.inactiveCards.Contains(info))
                    {
                        Unbound.inactiveCards.Remove(info);
                    }
                });
        }
    }

    internal class CardToggleHandler : MonoBehaviour
    {
        private TextMeshProUGUI cardName
        {
            get { return transform.Find("Name").GetComponent<TextMeshProUGUI>();  }
        }
        private Button onButton
        {
            get { return transform.Find("Material/On").GetComponent<Button>(); }
        }
        private Button offButton
        {
            get { return transform.Find("Material/Off").GetComponent<Button>(); }
        }
        private Animator animator
        {
            get { return transform.Find("Material").GetComponent<Animator>(); }
        }

        void Awake()
        {
            onButton.onClick.AddListener(() =>
            {
                animator.Play("Switch Off");
                cardName.alpha = 0.5f;
            });
            offButton.onClick.AddListener(() =>
            {
                animator.Play("Switch On");
                cardName.alpha = 1f;
            });
        }

        public CardToggleHandler SetName(string name)
        {
            cardName.text = name;
            return this;
        }
        public CardToggleHandler SetActions(UnityAction disable, UnityAction enable)
        {
            onButton.onClick.AddListener(disable);
            offButton.onClick.AddListener(enable);
            return this;
        }
    }
}
