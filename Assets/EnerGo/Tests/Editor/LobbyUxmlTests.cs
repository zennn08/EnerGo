using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace EnerGo.Tests
{
    // LobbyView looks elements up by name; a renamed or missing element would silently leave a button dead.
    public class LobbyUxmlTests
    {
        static VisualElement Clone(string path)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(tree, path);
            return tree.Instantiate();
        }

        static void AssertNames(VisualElement root, params string[] names)
        {
            foreach (var n in names) Assert.IsNotNull(root.Q(n), n);
        }

        [Test]
        public void LobbyHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Lobby.uxml"),
                "total-unit", "judul", "tile-sun", "tile-coal", "tile-gold", "tile-oil", "tile-uranium",
                "mode-kamera", "btn-berburu", "btn-panduan", "btn-latihan", "btn-pengaturan");

        [Test]
        public void SceneRootHasEveryLayer() =>
            AssertNames(Clone("Assets/UI/Screens/LobbyScene.uxml"),
                "lapis-splash", "lapis-lobby", "lapis-sheet", "lapis-panduan", "lapis-transisi");

        [Test]
        public void SheetsHaveEveryNamedElement()
        {
            AssertNames(Clone("Assets/UI/Screens/LobbyScene.uxml"), "tirai");
            AssertNames(Clone("Assets/UI/Screens/SheetPengaturan.uxml"),
                "sheet-pengaturan", "atur-ar", "atur-ar-sub", "atur-volume", "atur-volume-persen", "atur-getar", "atur-getar-sub",
                "btn-ke-latihan", "btn-reset", "reset-bar", "btn-selesai");
            AssertNames(Clone("Assets/UI/Screens/SheetLatihan.uxml"),
                "sheet-latihan", "latihan-sun", "latihan-coal", "latihan-gold", "latihan-oil", "latihan-uranium", "latihan-random",
                "btn-mulai-latihan");
        }
    }
}
