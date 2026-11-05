using UnityEngine;

namespace EnerGo
{
    // Whether the pre-game guide should still show for each SDA ("Jangan ingatkan lagi").
    public static class GuideViewer
    {
        public static string SkipKey(string id) => "EnerGo.Guide.Skip." + id;
        public static bool ShouldShow(string id) => GuideBook.For(id).Length > 0 && PlayerPrefs.GetInt(SkipKey(id), 0) == 0;

        public static void ResetAll()
        {
            foreach (var id in GuideBook.Ids) PlayerPrefs.DeleteKey(SkipKey(id));
            PlayerPrefs.Save();
        }
    }
}
