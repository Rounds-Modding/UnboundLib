using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UnboundLib
{
    [RequireComponent(typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup))]
    public class InfoPopup : Image
    {
        Text text;
        CanvasGroup group;

        protected override void Awake()
        {
            base.Start();
            color = new Color(0, 0, 0, 125f / 255f);

            // text
            text = new GameObject().AddComponent<Text>();
            text.rectTransform.SetParent(rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;

            // layout group
            var layout = GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = layout.childControlWidth = layout.childForceExpandHeight = layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(10, 10, 5, 5);

            // content fitter
            var fitter = GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            group = GetComponent<CanvasGroup>();
        }
        public void Build(string message)
        {
            text.text = message;
            StartCoroutine(DisplayPopup());
        }
        private IEnumerator DisplayPopup()
        {
            float time = 0;
            float damp = 0;
            float val = 0;

            rectTransform.position = new Vector2(Screen.width / 2, Screen.height / 2);

            while (time < 3f)
            {
                rectTransform.anchoredPosition += Vector2.up;
                val = Mathf.SmoothDamp(val, 1.25f, ref damp, 2f);
                group.alpha = 1 - val;
                time += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
