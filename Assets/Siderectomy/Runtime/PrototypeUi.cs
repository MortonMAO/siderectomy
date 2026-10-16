using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Siderectomy
{
    public static class PrototypeUi
    {
        public static readonly Color Ink = new Color32(222, 234, 248, 255);
        public static readonly Color Muted = new Color32(140, 163, 191, 255);
        public static readonly Color Accent = new Color32(80, 218, 219, 255);
        public static readonly Color Panel = new Color32(17, 29, 48, 245);
        private static Font font;
        public static RectTransform CanvasRoot()
        {
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new [] { "Microsoft YaHei", "SimHei", "Arial" }, 22);
            var go = new GameObject("PrototypeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            if (Object.FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return go.GetComponent<RectTransform>();
        }
        public static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
        }
        public static Image Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        public static Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size = 22, Color? color = null)
        {
            var t = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color ?? Ink; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.alignment = TextAnchor.MiddleLeft; t.supportRichText = false; return t;
        }
        public static Button Button(Transform parent, string name, string value, float x, float y, float width, float height, UnityAction callback)
        {
            var box = Box(parent, name, x, y, width, height, new Color32(32, 54, 77, 255)); box.raycastTarget = true;
            var b = box.gameObject.AddComponent<Button>(); b.targetGraphic = box;
            var colors = b.colors; colors.highlightedColor = new Color(0.7f, 1, 1); colors.pressedColor = new Color(0.4f, 0.8f, 0.85f); colors.disabledColor = new Color(0.42f, 0.47f, 0.55f); b.colors = colors;
            var label = Label(box.transform, "Label", value, 16, 4, width - 32, height - 8, 21); label.alignment = TextAnchor.MiddleCenter;
            b.onClick.AddListener(callback); return b;
        }
        public static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f); r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
