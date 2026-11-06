using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Draws LobbyScene with UI Toolkit (Jurnal Ekspedisi). Holds no game state: it reads LobbyController,
    // re-reads every shown value when the controller reports a change, and forwards taps to its actions.
    [RequireComponent(typeof(UIDocument))]
    public partial class LobbyView : MonoBehaviour
    {
        [SerializeField] private LobbyController controller;

        private VisualElement root, lapisSheet, lapisPanduan, lapisSplash, lapisTransisi, lapisProlog, lapisDesa;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            lapisSheet = root.Q("lapis-sheet");
            lapisPanduan = root.Q("lapis-panduan");
            lapisSplash = root.Q("lapis-splash");
            lapisTransisi = root.Q("lapis-transisi");
            lapisProlog = root.Q("lapis-prolog");
            lapisDesa = root.Q("lapis-desa");

            root.Q<Button>("btn-berburu").clicked += () => controller.StartHunt();
            root.Q<Button>("btn-panduan").clicked += () => controller.OpenSheet(LobbyController.ScreenState.Guides);
            root.Q<Button>("btn-latihan").clicked += () => controller.OpenSheet(LobbyController.ScreenState.Practice);
            root.Q<Button>("btn-pengaturan").clicked += () => controller.OpenSheet(LobbyController.ScreenState.Settings);

            BindSheets();
            BindGuide();
            BindProlog();
            BindDesa();
            controller.StateChanged += OnStateChanged;
            OnStateChanged();
        }

        private void OnDisable()
        {
            if (controller != null) controller.StateChanged -= OnStateChanged;
            if (controller != null) controller.FirstFill -= StartSorak;
        }

        private void Update()
        {
            AnimateSplashAndTransition();
            UpdateSheets();
            UpdateGuide();
            UpdateProlog();
            UpdateDesa();
        }

        private void OnStateChanged()
        {
            Refresh();
            ShowLayers();
        }

        // Every value on the lobby, read again from its source (progress, settings).
        public void Refresh()
        {
            RefreshSheets();
            RefreshDesa();
        }

        // Specimen tile: catalogue number, icon, name, unit badge (count < 0 hides it), locked state.
        public static void FillTile(VisualElement holder, string id, int count, bool locked, bool ringkas)
        {
            var tile = holder.ClassListContains("eg-tile") ? holder : holder.Q(className: "eg-tile");
            foreach (var other in new[] { "sun", "coal", "gold", "oil", "uranium", "random" })
                tile.RemoveFromClassList("eg-muda--" + other);
            tile.AddToClassList("eg-muda--" + id);
            tile.EnableInClassList("eg-tile--ringkas", ringkas);
            tile.EnableInClassList("is-terkunci", locked);
            tile.Q<Label>(className: "eg-tile__no").text = LobbyText.CatalogNo(id);
            var icon = tile.Q(className: "eg-tile__ikon");
            foreach (var other in new[] { "sun", "coal", "gold", "oil", "uranium", "random" })
                icon.RemoveFromClassList("eg-ikon--" + other);
            icon.AddToClassList("eg-ikon--" + id);
            tile.Q<Label>(className: "eg-tile__nama").text = id == LobbyController.RandomResource ? "Acak" : ResourceIds.Name(id);
            var badge = tile.Q<Label>(className: "eg-badge");
            badge.style.display = count < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (count >= 0) badge.text = LobbyText.Badge(count);
        }

        private static void Show(VisualElement layer, bool visible)
        {
            layer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            layer.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        // One layer takes taps at a time: the village sits under the sheets, splash and transition cover everything.
        private void ShowLayers()
        {
            var s = controller.State;
            bool sheet = s == LobbyController.ScreenState.Settings || s == LobbyController.ScreenState.Practice || s == LobbyController.ScreenState.Guides;
            Show(lapisProlog, s == LobbyController.ScreenState.Prolog);
            Show(lapisDesa, controller.HomeVisible);
            lapisProlog.pickingMode = PickingMode.Ignore;
            lapisDesa.pickingMode = PickingMode.Ignore;
            ShowProlog(s);
            Show(lapisSheet, sheet);
            Show(lapisPanduan, controller.OpenGuideId != null);
            Show(lapisSplash, s == LobbyController.ScreenState.Splash);
            Show(lapisTransisi, s == LobbyController.ScreenState.TransitioningToAR);
            ShowSheet(s);
        }

        partial void BindSheets();
        partial void RefreshSheets();
        partial void UpdateSheets();
        partial void ShowSheet(LobbyController.ScreenState state);
        partial void BindGuide();
        partial void UpdateGuide();
        partial void AnimateSplashAndTransition();
        partial void BindProlog();
        partial void UpdateProlog();
        partial void ShowProlog(LobbyController.ScreenState state);
        partial void BindDesa();
        partial void UpdateDesa();
        partial void RefreshDesa();
    }
}
