using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Keeps content clear of notches and system bars: every .eg-aman screen gets the design padding
    // (28 top, 16 sides, 24 bottom, from the LayarAwal preview) plus the screen's unsafe inset in panel units.
    [RequireComponent(typeof(UIDocument))]
    public class SafeArea : MonoBehaviour
    {
        private const float Top = 28f, Side = 16f, Bottom = 24f;
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
            float top = Top + (Screen.height - safe.yMax) * k, bottom = Bottom + safe.yMin * k;
            float left = Side + safe.xMin * k, right = Side + (Screen.width - safe.xMax) * k;
            root.Query(className: "eg-aman").ForEach(e =>
            {
                e.style.paddingTop = top; e.style.paddingBottom = bottom;
                e.style.paddingLeft = left; e.style.paddingRight = right;
            });
        }
    }
}
