using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    public partial class LobbyView
    {
        // Full pictures load from Resources only when shown, so the lobby does not hold them in memory.
        // A picture that does not exist yet falls back to a flat colour, so the screens work before the art is in.
        public static void SetGambar(VisualElement target, string resourcePath, Color fallback)
        {
            if (target.userData as string == resourcePath) return;
            target.userData = resourcePath;
            var texture = Resources.Load<Texture2D>(resourcePath);
            target.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
            target.style.backgroundColor = texture != null ? new StyleColor(Color.clear) : new StyleColor(fallback);
        }

        // For small moving pieces: hidden when null or when the picture does not exist yet.
        private static void SetOptionalGambar(VisualElement target, string resourcePath)
        {
            if (target.userData as string == resourcePath && resourcePath != null) return;
            var texture = resourcePath == null ? null : Resources.Load<Texture2D>(resourcePath);
            target.userData = texture != null ? resourcePath : null;
            target.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
            target.style.display = texture != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Lets Resources.UnloadUnusedAssets free the picture.
        private static void ClearGambar(VisualElement target)
        {
            target.userData = null;
            target.style.backgroundImage = new StyleBackground(StyleKeyword.None);
        }
    }
}
