using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Keeps content clear of notches and system bars: every .eg-aman screen gets the design padding
    // (28 top, 16 sides, 24 bottom, from the LayarAwal preview) plus the screen's unsafe inset in panel units.
    // Bottom-anchored absolute pieces sit outside that padding: sheets keep their edge and pad their content up, the splash prompt moves up.
    [RequireComponent(typeof(UIDocument))]
    public class SafeArea : MonoBehaviour
    {
        private const float Top = 28f, Side = 16f, Bottom = 24f;
        private const float SheetPadBottom = 40f, SplashPromptBottom = 64f; // Layar.uss .eg-sheet padding / .splash__bawah
        private VisualElement root;
        private Rect lastSafe;
        private float lastWidth;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.RegisterCallback<GeometryChangedEvent>(_ => Apply());
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || !Mathf.Approximately(root.layout.width, lastWidth)) Apply();
        }

        private void Apply()
        {
            if (root == null || float.IsNaN(root.layout.width) || root.layout.width <= 0f || Screen.width <= 0) return;
            lastSafe = Screen.safeArea;
            lastWidth = root.layout.width;
            float k = root.layout.width / Screen.width; // screen pixels -> panel units
            var safe = Screen.safeArea;
            float top = Top + (Screen.height - safe.yMax) * k, inset = UiLayout.PanelBottomInset(safe.yMin, Screen.width, root.layout.width), bottom = Bottom + inset;
            float left = Side + safe.xMin * k, right = Side + (Screen.width - safe.xMax) * k;
            root.Query(className: "eg-aman").ForEach(e =>
            {
                e.style.paddingTop = top; e.style.paddingBottom = bottom;
                e.style.paddingLeft = left; e.style.paddingRight = right;
            });
            root.Query(className: "eg-sheet").ForEach(e => e.style.paddingBottom = SheetPadBottom + inset);
            root.Query(className: "splash__bawah").ForEach(e => e.style.bottom = SplashPromptBottom + inset);
        }
    }
}
