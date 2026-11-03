using UnityEngine;

namespace EnerGo
{
    // Page state of the read-mode guide card in the lobby (LANJUT ... TUTUP), kept free of UI calls.
    public class GuidePager
    {
        private readonly int count;

        public GuidePager(int pageCount) { count = Mathf.Max(1, pageCount); }

        public int Page { get; private set; }
        public int Count => count;
        public bool IsLast => Page >= count - 1;
        public string PrimaryLabel => IsLast ? "TUTUP" : "LANJUT";
        public string StepLabel => "LANGKAH " + (Page + 1) + "/" + count;

        // Advances one page; false on the last page (the caller closes the card).
        public bool Next()
        {
            if (IsLast) return false;
            Page++;
            return true;
        }

        public void Prev() { if (Page > 0) Page--; }
    }
}
