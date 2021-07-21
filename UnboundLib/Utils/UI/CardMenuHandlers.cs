using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnboundLib.Networking;
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
        private Transform modCardsContent
        {
            get { return transform.Find("Mod Cards/Content"); }
        }
        private Transform defaultCardsContent
        {
            get { return transform.Find("Default Cards/Content"); }
        }

        private ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "UnboundLib.cfg"), true);
        private bool toggleAll;
        public List<CardToggleHandler> cardToggleHandlers = new List<CardToggleHandler>();

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

            var toggleAllButton = Instantiate(transform.Find("Title/Modded Button").gameObject, transform.Find("Title"));
            toggleAllButton.name = "Toggle all Button";
            toggleAllButton.transform.position += new Vector3(100, 0, 0);
            toggleAllButton.GetComponent<TextMeshProUGUI>().text = "Toggle all";
            var button = toggleAllButton.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                var _toggleAll = true;
                foreach (var toggle in cardToggleHandlers)
                {
                    if (toggleAll)
                    {
                        toggle.SetValue(true);
                        _toggleAll = false;
                    }
                    else
                    {
                        toggle.SetValue(false);
                        _toggleAll = true;
                    }
                }
                
                toggleAll = _toggleAll;
            });
        }

        public void Show()
        {
            animator.Play("Fade-in");
        }
        public void Hide()
        {
            animator.Play("Fade-out");
        }
        public void AddCardToggle(CardInfo info, bool isModded = true)
        {
            var toggle = Instantiate(TogglePrefab, isModded ? modCardsContent : defaultCardsContent).AddComponent<CardToggleHandler>()
                .Register(info)
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
                            Unbound.inactiveCards.Sort((x, y) => string.Compare(x.cardName, y.cardName));
                        }
                    }, 
                    () =>
                    {
                        if (!Unbound.activeCards.Contains(info))
                        {
                            Unbound.activeCards.Add(info);
                            Unbound.activeCards.Sort((x, y) => string.Compare(x.cardName, y.cardName));
                        }
                        if (Unbound.inactiveCards.Contains(info))
                        {
                            Unbound.inactiveCards.Remove(info);
                        }
                    });
            toggle.isEnabled = isModded ? config.Bind("Toggle Modded cards:", info.cardName, true) : config.Bind("Toggle default cards", info.cardName, true);
            cardToggleHandlers.Add(toggle);
        }
    }

    internal class CardToggleHandler : MonoBehaviour
    {
        internal static List<CardToggleHandler> toggles = new List<CardToggleHandler>();

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

        public CardInfo info;
        private ConfigEntry<bool> _isEnabled;
        public ConfigEntry<bool> isEnabled
        {
            set
            {
                if (value.Value)
                {
                    animator.Play("Switch On");
                    cardName.alpha = 1f;
                } else
                {
                    animator.Play("Switch Off");
                    cardName.alpha = 0.25f;
                }

                _isEnabled = value;
            }
            get => _isEnabled;
        }

        void Awake()
        {
            onButton.onClick.AddListener(() =>
            {
                // animator.Play("Switch Off");
                // cardName.alpha = 0.25f;
                _isEnabled.Value = false;
                isEnabled = _isEnabled;
            });
            offButton.onClick.AddListener(() =>
            {
                // animator.Play("Switch On");
                // cardName.alpha = 1f;
                _isEnabled.Value = true;
                isEnabled = _isEnabled;
            });

            toggles.Add(this);
        }

        public CardToggleHandler Register(CardInfo info)
        {
            this.info = info;
            return this;
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

        public void SetValue(bool enabled)
        {
            NetworkingManager.RPC(typeof(CardToggleHandler), nameof(Toggle), info.cardName, enabled);
        }
        private void SetValue(string name, bool enabled)
        {
            if (enabled)
            {
                offButton.onClick?.Invoke();
            }
            else
            {
                onButton.onClick?.Invoke();
            }

            CardChoice.instance.cards = Unbound.activeCards.ToArray();
        }
        
        [UnboundRPC]
        private static void Toggle(string name, bool enabled)
        {
            var picker = toggles.FirstOrDefault(c => c.info.cardName.ToUpper() == name.ToUpper());

            if (picker == null && enabled)
            {
                NetworkingManager.RPC_Others(typeof(CardToggleHandler), nameof(RejectToggle), name, !enabled);
            }
            else
            {
                picker.SetValue(name, enabled);
            }
        }
        [UnboundRPC]
        private static void RejectToggle(string name, bool enabled)
        {
            var picker = toggles.FirstOrDefault(c => c.info.cardName.ToUpper() == name.ToUpper());

            if (picker != null)
            {
                picker.SetValue(name, enabled);
            }
        }
    }
}
