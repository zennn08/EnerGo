using System;
using System.Collections.Generic;

namespace EnerGo
{
    [Serializable]
    public class QuestionDefinition
    {
        public string id;
        public string resourceId;
        public string statement;
        public bool correctAnswer;
        public string explanation;
        public string sourceUrl;
        public bool reviewed;

        public QuestionDefinition(string id, string resourceId, string statement, bool correctAnswer, string explanation, string sourceUrl)
        {
            this.id = id; this.resourceId = resourceId; this.statement = statement;
            this.correctAnswer = correctAnswer; this.explanation = explanation;
            this.sourceUrl = sourceUrl; reviewed = true;
        }
    }

    public static class QuizBank
    {
        private const string Solar = "https://www.eia.gov/energyexplained/solar/";
        private const string Coal = "https://www.eia.gov/energyexplained/coal/use-of-coal.php";
        private const string CoalEnvironment = "https://www.eia.gov/energyexplained/coal/coal-and-the-environment.php";
        private const string Gold = "https://www.usgs.gov/centers/national-minerals-information-center/gold-statistics-and-information";

        public static readonly QuestionDefinition[] Questions =
        {
            new QuestionDefinition("sun-01", ResourceIds.Sun, "Panel surya fotovoltaik mengubah cahaya matahari menjadi listrik.", true, "Sel fotovoltaik mengubah cahaya matahari langsung menjadi listrik.", Solar),
            new QuestionDefinition("sun-02", ResourceIds.Sun, "Panel surya tetap menerima cahaya matahari pada malam hari.", false, "Pada malam hari tidak ada cahaya matahari langsung. Penyimpanan energi adalah sistem terpisah.", Solar),
            new QuestionDefinition("sun-03", ResourceIds.Sun, "Energi surya dapat dimanfaatkan untuk panas maupun listrik.", true, "Teknologi surya dapat menangkap panas atau mengubah cahaya menjadi listrik.", Solar),
            new QuestionDefinition("coal-01", ResourceIds.Coal, "PLTU batu bara membakar batu bara untuk membuat uap penggerak turbin.", true, "Panas pembakaran membuat uap, lalu uap memutar turbin pembangkit.", Coal),
            new QuestionDefinition("coal-02", ResourceIds.Coal, "Batu bara termasuk sumber energi yang terbarukan dalam waktu singkat.", false, "Batu bara terbentuk dari tumbuhan purba selama waktu geologis yang sangat panjang.", "https://www.eia.gov/energyexplained/coal/"),
            new QuestionDefinition("coal-03", ResourceIds.Coal, "Pembakaran batu bara dapat menghasilkan emisi karbon dioksida.", true, "Pembakaran batu bara menghasilkan CO2 serta polutan lain; besarnya bergantung pada proses dan pengendalian emisi.", CoalEnvironment),
            new QuestionDefinition("gold-01", ResourceIds.Gold, "Emas dimanfaatkan pada sebagian komponen elektronik.", true, "Emas dipakai dalam elektronik karena menghantar listrik dan tahan korosi.", Gold),
            new QuestionDefinition("gold-02", ResourceIds.Gold, "Emas adalah bahan bakar yang dibakar PLTU batu bara.", false, "Emas adalah mineral logam. PLTU batu bara memakai batu bara sebagai bahan bakar.", Gold),
            new QuestionDefinition("gold-03", ResourceIds.Gold, "Emas memiliki sifat tahan korosi.", true, "Ketahanan korosi membantu pemakaian emas pada kontak elektronik tertentu.", Gold)
        };

        public static QuestionDefinition[] ForResource(string id)
        {
            var result = new List<QuestionDefinition>();
            foreach (var question in Questions)
                if (question.resourceId == id && question.reviewed) result.Add(question);
            return result.ToArray();
        }

        public static QuestionDefinition Find(string questionId, string resourceId)
        {
            foreach (var question in Questions)
                if (question.id == questionId && question.resourceId == resourceId && question.reviewed) return question;
            return null;
        }
    }
}
