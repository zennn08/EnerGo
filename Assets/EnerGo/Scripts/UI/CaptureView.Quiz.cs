using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The quiz, its feedback card and the final result. Swipes and the BENAR/SALAH buttons are read by the controller
    // (one answer path), so those buttons ignore the pointer here and only show where to press.
    public partial class CaptureView
    {
        private VisualElement lapisKuis, penanda, kuisSoal, kartuSoal, kartuBalik, kuisHasil, soalWaktuIsi, soalIkon, balikIkon, balikJawabIkon, hasilIkon, hasilBintang, hasilRincian, pendarKiri, pendarKanan, lanjutIsi;
        private Label pilSoal, soalDetik, soalTeks, cap, balikSelotip, balikJawab, balikTeks, balikUnit, hasilJudul, hasilSkor, hasilPlus, hasilMutu, hasilPesan, kuisError;
        private Button btnBenar, btnSalah, btnLanjut, btnSelesai;
        private readonly VisualElement[] marks = new VisualElement[8];
        private string quizIconShown, resultShownFor;

        partial void BindQuiz()
        {
            lapisKuis = root.Q("lapis-kuis");
            penanda = root.Q("kuis-penanda");
            kuisSoal = root.Q("kuis-soal");
            kartuSoal = root.Q("kartu-soal");
            kartuBalik = root.Q("kartu-balik");
            kuisHasil = root.Q("kuis-hasil");
            soalWaktuIsi = root.Q("soal-waktu").Q(className: "eg-bar__isi");
            soalIkon = root.Q("soal-ikon");
            balikIkon = root.Q("balik-ikon");
            balikJawabIkon = root.Q("balik-jawab-ikon");
            hasilIkon = root.Q("hasil-ikon");
            hasilBintang = root.Q("hasil-bintang");
            hasilRincian = root.Q("hasil-rincian");
            pendarKiri = root.Q("kuis-pendar-kiri");
            pendarKanan = root.Q("kuis-pendar-kanan");
            lanjutIsi = root.Q("lanjut-isi");
            pilSoal = root.Q<Label>("pil-soal");
            soalDetik = root.Q<Label>("soal-detik");
            soalTeks = root.Q<Label>("soal-teks");
            cap = root.Q<Label>("cap-jawab");
            balikSelotip = root.Q<Label>("balik-selotip");
            balikJawab = root.Q<Label>("balik-jawab");
            balikTeks = root.Q<Label>("balik-teks");
            balikUnit = root.Q<Label>("balik-unit");
            hasilJudul = root.Q<Label>("hasil-judul");
            hasilSkor = root.Q<Label>("hasil-skor");
            hasilPlus = root.Q<Label>("hasil-plus");
            hasilMutu = root.Q<Label>("hasil-mutu");
            hasilPesan = root.Q<Label>("hasil-pesan");
            kuisError = root.Q<Label>("kuis-error");
            btnBenar = root.Q<Button>("btn-benar");
            btnSalah = root.Q<Button>("btn-salah");
            btnLanjut = root.Q<Button>("btn-lanjut");
            btnSelesai = root.Q<Button>("btn-selesai-hasil");

            penanda.Clear();
            hasilBintang.Clear();
            for (int i = 0; i < marks.Length; i++)
            {
                marks[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                marks[i].AddToClassList("kuis__tanda");
                penanda.Add(marks[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                var star = new VisualElement { pickingMode = PickingMode.Ignore };
                star.AddToClassList("kuis__bintang-satu");
                if (i == 1) star.AddToClassList("kuis__bintang-satu--besar");
                hasilBintang.Add(star);
            }
            btnLanjut.clicked += () => controller.NextQuizQuestion();
            btnSelesai.clicked += () => controller.FinishQuizResult();
        }

        partial void UpdateQuiz(CaptureScreen screen)
        {
            bool quizLayer = screen != CaptureScreen.Play;
            Show(lapisKuis, quizLayer);
            lapisKuis.pickingMode = quizLayer ? PickingMode.Position : PickingMode.Ignore;
            if (!quizLayer) return;

            var session = controller.QuizSession;
            var question = controller.QuizQuestion;
            if (session == null || question == null) return;

            Show(kuisSoal, screen == CaptureScreen.Quiz);
            Show(btnBenar, screen == CaptureScreen.Quiz);
            Show(btnSalah, screen == CaptureScreen.Quiz);
            Show(kartuBalik, screen == CaptureScreen.Feedback);
            Show(kuisHasil, screen == CaptureScreen.Result);

            var marked = controller.ReadQuizMarks();
            for (int i = 0; i < marks.Length; i++)
            {
                bool used = i < session.QuestionCount;
                Show(marks[i], used);
                if (!used) continue;
                bool? m = marked[i];
                marks[i].EnableInClassList("is-benar", m == true);
                marks[i].EnableInClassList("is-salah", m == false);
                marks[i].EnableInClassList("is-kini", m == null && i == session.questionIndex && screen == CaptureScreen.Quiz);
            }
            pilSoal.text = "SOAL " + Mathf.Min(session.questionIndex + 1, session.QuestionCount) + "/" + session.QuestionCount;

            if (quizIconShown != session.resourceId)
            {
                foreach (var e in new[] { soalIkon, balikIkon, hasilIkon })
                {
                    if (quizIconShown != null) e.RemoveFromClassList("eg-ikon--" + quizIconShown);
                    e.AddToClassList("eg-ikon--" + session.resourceId);
                }
                quizIconShown = session.resourceId;
            }

            bool error = !string.IsNullOrEmpty(EnerGoProgress.Error);
            Show(kuisError, error);
            if (error) kuisError.text = EnerGoProgress.Error;

            if (screen == CaptureScreen.Quiz) ShowQuestion(question);
            else if (screen == CaptureScreen.Feedback) ShowFeedback(session, question);
            else ShowResult(session);
        }

        private void ShowQuestion(QuestionDefinition question)
        {
            soalTeks.text = question.statement;
            Place(btnBenar, controller.AnswerButtonRect(true));
            Place(btnSalah, controller.AnswerButtonRect(false));
            btnBenar.EnableInClassList("is-tekan", controller.AnswerButtonHeld(true));
            btnSalah.EnableInClassList("is-tekan", controller.AnswerButtonHeld(false));

            float left = controller.QuizTimeLeft, seconds = controller.QuizSeconds;
            bool hurry = left <= 5f;
            soalWaktuIsi.style.width = Length.Percent(100f * left / seconds);
            soalWaktuIsi.EnableInClassList("eg-isi--stempel", hurry);
            soalDetik.text = Mathf.CeilToInt(left).ToString();
            soalDetik.EnableInClassList("kuis__detik--buru", hurry);

            // The card follows the finger and tips over; past the threshold the stamp and edge glow are full.
            Vector2 offset = controller.CardOffset;
            kartuSoal.style.translate = new Translate(offset.x, offset.y);
            kartuSoal.style.rotate = new Rotate(CaptureScreens.CardRotation(offset.x));
            float ratio = Mathf.Clamp01(Mathf.Abs(offset.x) / CaptureScreens.SwipeThreshold);
            bool moved = Mathf.Abs(offset.x) > 4f, right = offset.x > 0f;
            Show(cap, moved);
            kartuSoal.EnableInClassList("kuis__kartu--benar", moved && right);
            kartuSoal.EnableInClassList("kuis__kartu--salah", moved && !right);
            pendarKanan.style.opacity = moved && right ? 0.35f * ratio : 0f;
            pendarKiri.style.opacity = moved && !right ? 0.35f * ratio : 0f;
            if (!moved) return;
            cap.text = right ? "BENAR" : "SALAH";
            cap.style.opacity = ratio;
            cap.style.rotate = new Rotate(right ? -14f : 14f);
            cap.EnableInClassList("kuis__cap--benar", right);
            cap.EnableInClassList("kuis__cap--salah", !right);
            cap.EnableInClassList("kuis__cap--kiri", right);
            cap.EnableInClassList("kuis__cap--kanan", !right);
        }

        private void ShowFeedback(CaptureSession session, QuestionDefinition question)
        {
            pendarKiri.style.opacity = 0f;
            pendarKanan.style.opacity = 0f;
            kartuBalik.EnableInClassList("eg-kartu--benar", session.correct);
            kartuBalik.EnableInClassList("eg-kartu--salah", !session.correct);
            balikSelotip.text = session.correct ? "BENAR!" : session.timedOut ? "WAKTU HABIS" : "SALAH";
            balikSelotip.EnableInClassList("eg-selotip--hijau", session.correct);
            balikSelotip.EnableInClassList("eg-selotip--merah", !session.correct);
            balikJawab.text = CaptureScreens.AnswerChip(question.correctAnswer);
            balikJawabIkon.EnableInClassList("kuis__chip-ikon--benar", question.correctAnswer);
            balikJawabIkon.EnableInClassList("kuis__chip-ikon--salah", !question.correctAnswer);
            balikJawab.EnableInClassList("kuis__chip--benar", question.correctAnswer);
            balikJawab.EnableInClassList("kuis__chip--salah", !question.correctAnswer);
            balikTeks.text = question.explanation;
            Show(balikUnit.parent, session.correct);

            // A wrong answer holds the button briefly so the explanation actually gets read.
            float held = CaptureScreens.ReadHold(session.correct, controller.SecondsSinceFeedback);
            bool ready = held >= 1f;
            btnLanjut.SetEnabled(ready);
            btnLanjut.EnableInClassList("eg-btn--utama", ready);
            btnLanjut.EnableInClassList("eg-btn--nonaktif", !ready);
            btnLanjut.text = ready ? (session.IsLastQuestion ? "LIHAT HASIL" : "SOAL BERIKUTNYA") : "BACA DULU...";
            Show(lanjutIsi, !ready);
            lanjutIsi.style.width = Length.Percent(100f * held);
        }

        private void ShowResult(CaptureSession session)
        {
            pendarKiri.style.opacity = 0f;
            pendarKanan.style.opacity = 0f;
            if (resultShownFor == session.sessionId) return;
            resultShownFor = session.sessionId;

            int count = session.QuestionCount;
            // Saves from the 1-question version have no correctCount; their single answer is in `correct`.
            int right = session.questionIds != null && session.questionIds.Length > 0 ? session.correctCount : (session.correct ? 1 : 0);
            bool pure = right == count;
            hasilJudul.text = UiLayout.ResultTitle(right, count);
            int stars = UiLayout.Stars(right);
            for (int i = 0; i < 3; i++) hasilBintang[i].EnableInClassList("is-kosong", i >= stars);
            hasilSkor.text = right + "/" + count + " BENAR";
            hasilPlus.text = "+" + session.rewardQuantity;
            hasilMutu.text = pure ? "Murni" : "Normal";
            hasilPesan.text = CaptureScreens.ResultMessage(right, count);
            btnSelesai.text = string.IsNullOrEmpty(session.spawnId) ? "LANJUT KE LOBBY" : "LANJUT BERBURU";
            hasilRincian.Clear();
            foreach (var chip in controller.ResultChips(session, right))
            {
                var l = new Label(chip) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("kuis__chip");
                hasilRincian.Add(l);
            }
        }

        // Puts an element exactly on a rect the controller also uses for input (GUI units).
        private void Place(VisualElement e, Rect gui)
        {
            float k = controller.GuiToPanel(root.layout.width);
            e.style.left = gui.x * k;
            e.style.top = gui.y * k;
            e.style.width = gui.width * k;
            e.style.height = gui.height * k;
        }
    }
}
