using System;
using System.IO;
using UnityEngine;

namespace EnerGo
{
    public static class ResourceIds
    {
        public const string Sun = "sun";
        public const string Coal = "coal";
        public const string Gold = "gold";
        public static string Selected = Gold;
        public static string Name(string id) => id == Sun ? "Matahari" : id == Coal ? "Batu Bara" : "Emas";
    }

    [Serializable]
    public class InventoryEntry
    {
        public string resourceId;
        public int normalQuantity;
        public int pureQuantity;
        public int Total => normalQuantity + pureQuantity;
    }

    [Serializable]
    public class CaptureSession
    {
        public string sessionId;
        public string resourceId;
        public string questionId;    // the question currently on screen
        public string state;         // awaiting-analysis (answering), feedback (after an answer), result (final)
        public bool selectedAnswer;
        public bool correct;         // was the latest answer right
        public int rewardQuantity;
        public string[] questionIds; // all questions of this capture; empty in saves from the 1-question version
        public int questionIndex;
        public int correctCount;

        public int QuestionCount => questionIds != null && questionIds.Length > 0 ? questionIds.Length : 1;
        public bool IsLastQuestion => questionIndex >= QuestionCount - 1;
        public bool Pure => correctCount == QuestionCount; // every answer right
    }

    [Serializable]
    public class SaveGame
    {
        public int schemaVersion = 1;
        public string rulesetId = "demo-v1";
        public InventoryEntry[] inventory = {
            new InventoryEntry { resourceId = ResourceIds.Sun },
            new InventoryEntry { resourceId = ResourceIds.Coal },
            new InventoryEntry { resourceId = ResourceIds.Gold }
        };
        public CaptureSession pendingCapture;
        public string lastCommittedCaptureId = "";
        public bool legacyGoldImported;

        public InventoryEntry Get(string id)
        {
            foreach (var entry in inventory)
                if (entry != null && entry.resourceId == id) return entry;
            throw new ArgumentException("Unknown resource: " + id);
        }
    }

    public static class EnerGoProgress
    {
        private const string FileName = "energo-demo-v1.json";
        private static SaveGame current;
        private static bool recoveryRequired;
        public static string Error { get; private set; }
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string BackupPath => SavePath + ".bak";

        public static SaveGame Current
        {
            get
            {
                if (current == null) Load();
                return current;
            }
        }

        public static void Load()
        {
            Error = null;
            recoveryRequired = false;
            if (TryRead(SavePath, out current)) return;
            if (TryRead(BackupPath, out current))
            {
                Error = "Save utama rusak. Cadangan berhasil dipulihkan.";
                return;
            }
            if (File.Exists(SavePath) || File.Exists(BackupPath))
            {
                current = new SaveGame();
                recoveryRequired = true;
                Error = "Save tidak dapat dibaca. Data lama tidak dihapus; periksa berkas sebelum reset.";
                return;
            }
            current = new SaveGame();
            int oldGold = Mathf.Max(0, PlayerPrefs.GetInt("EnerGo.Prototype.Gold", 0));
            current.Get(ResourceIds.Gold).normalQuantity = oldGold;
            current.legacyGoldImported = true;
            if (!Commit(current)) Error = "Gagal membuat save. Periksa ruang penyimpanan.";
        }

        private static bool TryRead(string path, out SaveGame data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                var value = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
                // JsonUtility materializes a null nested class as an empty object.
                if (value != null && value.pendingCapture != null && string.IsNullOrEmpty(value.pendingCapture.sessionId)) value.pendingCapture = null;
                if (!Valid(value)) return false;
                data = value;
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("EnerGo save read: " + ex.Message); return false; }
        }

        private static bool Valid(SaveGame value)
        {
            if (value == null || value.schemaVersion != 1 || value.rulesetId != "demo-v1" || value.inventory == null || value.inventory.Length != 3 || !value.legacyGoldImported) return false;
            foreach (string id in new[] { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold })
            {
                int matches = 0;
                foreach (var entry in value.inventory)
                    if (entry != null && entry.resourceId == id && entry.normalQuantity >= 0 && entry.pureQuantity >= 0) matches++;
                if (matches != 1) return false;
            }
            var session = value.pendingCapture;
            if (session == null) return true;
            if (string.IsNullOrEmpty(session.sessionId) || QuizBank.Find(session.questionId, session.resourceId) == null) return false;
            if (session.state != "awaiting-analysis" && session.state != "feedback" && session.state != "result") return false;
            if (session.questionIds != null)
                foreach (var id in session.questionIds)
                    if (QuizBank.Find(id, session.resourceId) == null) return false;
            return session.questionIndex >= 0 && session.questionIndex < session.QuestionCount &&
                   session.correctCount >= 0 && session.correctCount <= session.QuestionCount &&
                   session.rewardQuantity >= 0 && session.rewardQuantity <= 1 + QuizBank.QuestionsPerCapture; // old 1-question saves stored up to 3
        }

        public static bool Commit(SaveGame next)
        {
            if (recoveryRequired) { Error = "Save rusak. Reset terkonfirmasi diperlukan sebelum menyimpan lagi."; return false; }
            if (!Valid(next)) { Error = "Data progres tidak valid."; return false; }
            string temp = SavePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temp, JsonUtility.ToJson(next, true));
                if (!TryRead(temp, out _)) throw new IOException("Temporary save invalid");
                if (File.Exists(SavePath))
                {
                    // Replace preserves the last valid file as a backup on supported platforms.
                    try { File.Replace(temp, SavePath, BackupPath); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(SavePath, BackupPath, true);
                        File.Delete(SavePath);
                        File.Move(temp, SavePath);
                    }
                }
                else File.Move(temp, SavePath);
                current = next;
                Error = null;
                return true;
            }
            catch (Exception ex)
            {
                Error = "Gagal menyimpan progres: " + ex.Message;
                Debug.LogError("EnerGo save: " + ex);
                return false;
            }
        }

        private static SaveGame Copy() => JsonUtility.FromJson<SaveGame>(JsonUtility.ToJson(Current));

        public static bool BeginAnalysis(string resourceId)
        {
            if (Current.pendingCapture != null || QuizBank.ForResource(resourceId).Length == 0) return false;
            var next = Copy();
            var ids = QuizBank.PickForCapture(resourceId, QuizBank.QuestionsPerCapture);
            next.pendingCapture = new CaptureSession
            {
                sessionId = Guid.NewGuid().ToString("N"), resourceId = resourceId,
                questionIds = ids, questionIndex = 0, questionId = ids[0],
                state = "awaiting-analysis"
            };
            return Commit(next);
        }

        public static bool Answer(bool answer)
        {
            var pending = Current.pendingCapture;
            if (pending == null || pending.state != "awaiting-analysis" || Current.lastCommittedCaptureId == pending.sessionId) return false;
            var question = QuizBank.Find(pending.questionId, pending.resourceId);
            if (question == null) return false;
            var next = Copy();
            var session = next.pendingCapture;
            session.selectedAnswer = answer;
            session.correct = question.correctAnswer == answer;
            if (session.correct) session.correctCount++;
            session.state = "feedback";
            if (session.IsLastQuestion)
            {
                // Reward is granted together with the last answer: 1 unit + 1 per correct answer, "Murni" only if all correct.
                session.rewardQuantity = RewardFor(session.correctCount);
                var entry = next.Get(session.resourceId);
                if (session.Pure) entry.pureQuantity += session.rewardQuantity;
                else entry.normalQuantity += session.rewardQuantity;
                next.lastCommittedCaptureId = session.sessionId;
            }
            return Commit(next);
        }

        public static int RewardFor(int correctCount) => 1 + correctCount;

        // From the per-answer feedback card: show the next question, or the final result after the last one.
        public static bool NextQuestion()
        {
            var pending = Current.pendingCapture;
            if (pending == null || pending.state != "feedback") return false;
            var next = Copy();
            var session = next.pendingCapture;
            if (session.IsLastQuestion) session.state = "result";
            else
            {
                session.questionIndex++;
                session.questionId = session.questionIds[session.questionIndex];
                session.state = "awaiting-analysis";
            }
            return Commit(next);
        }

        public static bool ClearResult()
        {
            if (Current.pendingCapture == null || Current.pendingCapture.state != "result") return false;
            var next = Copy();
            next.pendingCapture = null;
            return Commit(next);
        }

        public static bool Reset()
        {
            var next = new SaveGame { legacyGoldImported = true };
            bool wasBlocked = recoveryRequired;
            recoveryRequired = false;
            if (!Commit(next)) { recoveryRequired = wasBlocked; return false; }
            PlayerPrefs.DeleteKey("EnerGo.Prototype.Gold");
            PlayerPrefs.Save();
            return true;
        }
    }
}
