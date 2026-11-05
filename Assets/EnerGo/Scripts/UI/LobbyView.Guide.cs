using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Sheet Panduan and the read-mode guide card over it (GuideCard).
    public partial class LobbyView
    {
        private Label panduanNotice;
        private GuideCard guideCard;
        private string shownGuideId;

        partial void BindGuide()
        {
            sheetPanduan = root.Q("sheet-panduan");
            panduanNotice = root.Q<Label>("panduan-notice");
            foreach (var id in GuideBook.Ids)
            {
                string pick = id;
                FillTile(root.Q("panduan-" + id), id, -1, false, ringkas: false);
                root.Q("panduan-" + id).RegisterCallback<ClickEvent>(_ => controller.OpenGuide(pick));
            }
            root.Q<Button>("btn-reset-panduan").clicked += () => controller.ResetGuides();

            guideCard = new GuideCard(root, "TUTUP", () => controller.CloseGuide(), () => controller.CloseGuide());
        }

        // Opens or closes the card when the controller's guide changes.
        partial void UpdateGuide()
        {
            if (panduanNotice != null)
                panduanNotice.style.visibility = controller.GuidesResetNoticeVisible ? Visibility.Visible : Visibility.Hidden;
            if (controller.OpenGuideId == shownGuideId) return;
            shownGuideId = controller.OpenGuideId;
            if (shownGuideId == null) guideCard.Close(); else guideCard.Open(shownGuideId);
        }
    }
}
