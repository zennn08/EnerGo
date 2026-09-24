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
        public const int QuestionsPerCapture = 3;

        private const string Solar = "https://www.eia.gov/energyexplained/solar/";
        private const string SolarPv = "https://www.eia.gov/energyexplained/solar/photovoltaics-and-electricity.php";
        private const string SolarWhere = "https://www.eia.gov/energyexplained/solar/where-solar-is-found.php";
        private const string SolarThermal = "https://www.eia.gov/energyexplained/solar/solar-thermal-power-plants.php";
        private const string SolarEnvironment = "https://www.eia.gov/energyexplained/solar/solar-energy-and-the-environment.php";
        private const string Storage = "https://www.eia.gov/energyexplained/electricity/energy-storage-for-electricity-generation.php";
        private const string Biomass = "https://www.eia.gov/energyexplained/biomass/";
        private const string SunFacts = "https://science.nasa.gov/sun/facts/";

        private const string CoalBasics = "https://www.eia.gov/energyexplained/coal/";
        private const string Coal = "https://www.eia.gov/energyexplained/coal/use-of-coal.php";
        private const string CoalEnvironment = "https://www.eia.gov/energyexplained/coal/coal-and-the-environment.php";
        private const string CoalMining = "https://www.eia.gov/energyexplained/coal/mining-and-transportation.php";
        private const string CoalIndonesia = "https://www.eia.gov/international/analysis/country/IDN";
        private const string CoalWorld = "https://www.iea.org/energy-system/fossil-fuels/coal";

        private const string Gold = "https://www.usgs.gov/centers/national-minerals-information-center/gold-statistics-and-information";
        private const string GoldProspecting = "https://pubs.usgs.gov/gip/prospect1/goldgip.html";
        private const string GoldElement = "https://www.rsc.org/periodic-table/element/79/gold";
        private const string GoldAsia = "https://www.usgs.gov/centers/national-minerals-information-center/asia-and-pacific";
        private const string Mercury = "https://www.who.int/news-room/fact-sheets/detail/mercury-and-health";

        private static QuestionDefinition Q(string id, string resourceId, string statement, bool answer, string explanation, string source) =>
            new QuestionDefinition(id, resourceId, statement, answer, explanation, source);

        // Ids are stored in saves: never renumber or reuse an id, only append new ones.
        public static readonly QuestionDefinition[] Questions =
        {
            // ── Matahari ──────────────────────────────────────────────
            Q("sun-01", ResourceIds.Sun, "Panel surya fotovoltaik mengubah cahaya matahari menjadi listrik.", true, "Sel fotovoltaik mengubah cahaya matahari langsung menjadi listrik.", Solar),
            Q("sun-02", ResourceIds.Sun, "Panel surya tetap menerima cahaya matahari pada malam hari.", false, "Pada malam hari tidak ada cahaya matahari langsung. Penyimpanan energi adalah sistem terpisah.", Solar),
            Q("sun-03", ResourceIds.Sun, "Energi surya dapat dimanfaatkan untuk panas maupun listrik.", true, "Teknologi surya dapat menangkap panas atau mengubah cahaya menjadi listrik.", Solar),
            Q("sun-04", ResourceIds.Sun, "Energi surya termasuk sumber energi terbarukan.", true, "Cahaya matahari terus tersedia setiap hari, sehingga energi surya tergolong terbarukan.", Solar),
            Q("sun-05", ResourceIds.Sun, "Panel surya langsung menghasilkan listrik arus bolak-balik (AC).", false, "Sel surya menghasilkan listrik arus searah (DC). Inverter mengubahnya menjadi AC.", SolarPv),
            Q("sun-06", ResourceIds.Sun, "Inverter dipakai untuk mengubah listrik DC dari panel surya menjadi AC.", true, "Peralatan rumah dan jaringan listrik memakai AC, jadi listrik DC dari panel diubah oleh inverter.", SolarPv),
            Q("sun-07", ResourceIds.Sun, "Sel surya umumnya dibuat dari bahan semikonduktor seperti silikon.", true, "Sebagian besar sel surya memakai silikon, sebuah bahan semikonduktor.", SolarPv),
            Q("sun-08", ResourceIds.Sun, "Panel surya menghasilkan listrik lebih banyak saat mendung tebal dibanding saat cerah.", false, "Awan menghalangi sebagian cahaya matahari, sehingga listrik yang dihasilkan berkurang.", SolarWhere),
            Q("sun-09", ResourceIds.Sun, "Panel surya tidak mengeluarkan gas buang saat menghasilkan listrik.", true, "Sistem fotovoltaik tidak menghasilkan emisi udara saat beroperasi.", SolarEnvironment),
            Q("sun-10", ResourceIds.Sun, "Energi matahari berasal dari reaksi fusi nuklir di inti matahari.", true, "Di inti matahari, hidrogen bergabung menjadi helium dan melepaskan energi yang sangat besar.", SunFacts),
            Q("sun-11", ResourceIds.Sun, "Cahaya matahari membutuhkan sekitar 8 menit untuk sampai ke Bumi.", true, "Cahaya menempuh jarak Matahari ke Bumi dalam kurang lebih 8 menit.", SunFacts),
            Q("sun-12", ResourceIds.Sun, "Matahari adalah sebuah planet.", false, "Matahari adalah bintang, pusat tata surya kita.", SunFacts),
            Q("sun-13", ResourceIds.Sun, "Pemanas air tenaga surya memakai panas matahari untuk menghangatkan air.", true, "Kolektor surya menyerap panas matahari lalu memanaskan air di dalamnya.", Solar),
            Q("sun-14", ResourceIds.Sun, "Satu sel surya tunggal biasanya cukup untuk menyalakan seluruh rumah.", false, "Satu sel hanya menghasilkan sedikit listrik. Banyak sel dirangkai menjadi modul dan array.", SolarPv),
            Q("sun-15", ResourceIds.Sun, "Beberapa panel surya dirangkai menjadi array untuk menghasilkan listrik lebih besar.", true, "Modul-modul surya digabung menjadi array agar daya listriknya bertambah.", SolarPv),
            Q("sun-16", ResourceIds.Sun, "Baterai dapat menyimpan listrik dari panel surya untuk dipakai malam hari.", true, "Sistem penyimpanan seperti baterai menyimpan kelebihan listrik untuk dipakai saat tidak ada cahaya.", Storage),
            Q("sun-17", ResourceIds.Sun, "Daerah dekat khatulistiwa seperti Indonesia menerima sinar matahari cukup kuat sepanjang tahun.", true, "Di dekat khatulistiwa matahari berada tinggi di langit hampir sepanjang tahun.", SolarWhere),
            Q("sun-18", ResourceIds.Sun, "Jumlah cahaya matahari yang diterima suatu tempat tidak dipengaruhi cuaca.", false, "Awan, polusi udara, dan cuaca memengaruhi banyaknya cahaya matahari yang sampai ke permukaan.", SolarWhere),
            Q("sun-19", ResourceIds.Sun, "Pembangkit listrik tenaga surya termal memakai cermin untuk memusatkan cahaya matahari.", true, "Cermin memusatkan cahaya untuk memanaskan fluida, lalu panasnya dipakai membangkitkan listrik.", SolarThermal),
            Q("sun-20", ResourceIds.Sun, "PLTS adalah singkatan dari Pembangkit Listrik Tenaga Surya.", true, "PLTS membangkitkan listrik dari energi cahaya matahari.", Solar),
            Q("sun-21", ResourceIds.Sun, "Panel surya harus dibakar agar dapat menghasilkan listrik.", false, "Panel surya tidak membakar apa pun. Listrik muncul saat cahaya mengenai sel surya.", SolarPv),
            Q("sun-22", ResourceIds.Sun, "Arah dan kemiringan panel surya memengaruhi banyaknya cahaya yang diterima.", true, "Panel yang menghadap matahari dengan sudut tepat menerima cahaya lebih banyak.", SolarWhere),
            Q("sun-23", ResourceIds.Sun, "Energi surya akan habis dalam beberapa tahun jika terus dipakai.", false, "Matahari akan terus bersinar selama miliaran tahun. Memakai cahayanya tidak menghabiskannya.", SunFacts),
            Q("sun-24", ResourceIds.Sun, "Panel surya hanya bisa dipasang di atap rumah.", false, "Panel surya juga dipasang di lahan terbuka sebagai pembangkit besar, bahkan di atas air.", Solar),
            Q("sun-25", ResourceIds.Sun, "Tumbuhan memakai energi cahaya matahari untuk fotosintesis.", true, "Lewat fotosintesis, tumbuhan menyimpan energi matahari sebagai energi kimia.", Biomass),
            Q("sun-26", ResourceIds.Sun, "Energi dalam batu bara awalnya berasal dari energi matahari yang ditangkap tumbuhan purba.", true, "Tumbuhan purba menyimpan energi matahari. Sisa tumbuhan itu lama-lama menjadi batu bara.", CoalBasics),
            Q("sun-27", ResourceIds.Sun, "Panel surya membutuhkan bahan bakar agar dapat bekerja.", false, "Panel surya hanya butuh cahaya matahari; tidak ada bahan bakar yang dipakai.", SolarPv),
            Q("sun-28", ResourceIds.Sun, "Energi surya hanya bisa dimanfaatkan di daerah gurun.", false, "Energi surya dapat dimanfaatkan di banyak tempat, walau hasilnya berbeda menurut lokasi dan cuaca.", SolarWhere),
            Q("sun-29", ResourceIds.Sun, "Semakin panas suhu panel surya, semakin besar listrik yang dihasilkannya.", false, "Sel surya justru bekerja kurang efisien saat suhunya terlalu panas.", SolarPv),
            Q("sun-30", ResourceIds.Sun, "Matahari berukuran lebih kecil daripada Bumi.", false, "Matahari jauh lebih besar; diameternya sekitar 109 kali diameter Bumi.", SunFacts),
            Q("sun-31", ResourceIds.Sun, "Listrik dari panel surya tidak bisa dialirkan ke jaringan listrik.", false, "Sistem surya dapat terhubung ke jaringan dan mengirim kelebihan listriknya.", SolarPv),
            Q("sun-32", ResourceIds.Sun, "Pemanas air tenaga surya mengeluarkan asap saat bekerja.", false, "Pemanas air surya hanya menyerap panas matahari, tidak membakar apa pun.", SolarEnvironment),
            Q("sun-33", ResourceIds.Sun, "Inverter berfungsi menyimpan listrik surya untuk dipakai malam hari.", false, "Inverter mengubah DC menjadi AC. Yang menyimpan listrik adalah baterai.", Storage),

            // ── Batu Bara ─────────────────────────────────────────────
            Q("coal-01", ResourceIds.Coal, "PLTU batu bara membakar batu bara untuk membuat uap penggerak turbin.", true, "Panas pembakaran membuat uap, lalu uap memutar turbin pembangkit.", Coal),
            Q("coal-02", ResourceIds.Coal, "Batu bara termasuk sumber energi yang terbarukan dalam waktu singkat.", false, "Batu bara terbentuk dari tumbuhan purba selama waktu geologis yang sangat panjang.", CoalBasics),
            Q("coal-03", ResourceIds.Coal, "Pembakaran batu bara dapat menghasilkan emisi karbon dioksida.", true, "Pembakaran batu bara menghasilkan CO2 serta polutan lain; besarnya bergantung pada proses dan pengendalian emisi.", CoalEnvironment),
            Q("coal-04", ResourceIds.Coal, "Batu bara terbentuk dari sisa tumbuhan yang tertimbun selama jutaan tahun.", true, "Sisa tumbuhan di rawa purba tertimbun, tertekan, dan terpanaskan hingga menjadi batu bara.", CoalBasics),
            Q("coal-05", ResourceIds.Coal, "Batu bara termasuk bahan bakar fosil.", true, "Batu bara, minyak bumi, dan gas alam adalah bahan bakar fosil dari sisa makhluk hidup purba.", CoalBasics),
            Q("coal-06", ResourceIds.Coal, "Antrasit adalah jenis batu bara dengan kandungan karbon paling tinggi.", true, "Antrasit mengandung sekitar 86 sampai 97 persen karbon, tertinggi di antara jenis batu bara.", CoalBasics),
            Q("coal-07", ResourceIds.Coal, "Lignit (batu bara muda) mengandung karbon lebih banyak daripada antrasit.", false, "Lignit adalah batu bara peringkat terendah dengan kandungan karbon paling sedikit.", CoalBasics),
            Q("coal-08", ResourceIds.Coal, "Batu bara dapat ditambang di permukaan maupun di bawah tanah.", true, "Tambang permukaan dipakai untuk lapisan dangkal, tambang bawah tanah untuk lapisan yang dalam.", CoalMining),
            Q("coal-09", ResourceIds.Coal, "Pembakaran batu bara tidak menyisakan abu.", false, "Pembakaran batu bara menyisakan abu terbang dan abu dasar yang harus dikelola.", CoalEnvironment),
            Q("coal-10", ResourceIds.Coal, "Sulfur dioksida dari pembakaran batu bara dapat menyebabkan hujan asam.", true, "Sulfur dioksida di udara dapat membentuk asam yang turun bersama hujan.", CoalEnvironment),
            Q("coal-11", ResourceIds.Coal, "Karbon dioksida termasuk gas rumah kaca.", true, "Karbon dioksida menahan panas di atmosfer sehingga termasuk gas rumah kaca.", CoalEnvironment),
            Q("coal-12", ResourceIds.Coal, "Batu bara juga dipakai industri baja dalam bentuk kokas.", true, "Kokas dari batu bara dipakai untuk melebur bijih besi menjadi baja.", Coal),
            Q("coal-13", ResourceIds.Coal, "Indonesia termasuk salah satu pengekspor batu bara terbesar di dunia.", true, "Indonesia adalah salah satu produsen dan pengekspor batu bara terbesar dunia.", CoalIndonesia),
            Q("coal-14", ResourceIds.Coal, "Kalimantan dan Sumatra adalah daerah penghasil batu bara utama di Indonesia.", true, "Sebagian besar cadangan dan produksi batu bara Indonesia berada di Kalimantan dan Sumatra.", CoalIndonesia),
            Q("coal-15", ResourceIds.Coal, "PLTU adalah singkatan dari Pembangkit Listrik Tenaga Uap.", true, "Di PLTU, uap panas memutar turbin untuk membangkitkan listrik.", Coal),
            Q("coal-16", ResourceIds.Coal, "Turbin di PLTU terhubung ke generator yang menghasilkan listrik.", true, "Turbin yang diputar uap menggerakkan generator, dan generator menghasilkan listrik.", Coal),
            Q("coal-17", ResourceIds.Coal, "Batu bara yang sudah dibakar dapat terbentuk kembali dalam beberapa hari.", false, "Pembentukan batu bara butuh jutaan tahun, jadi batu bara yang terbakar tidak cepat tergantikan.", CoalBasics),
            Q("coal-18", ResourceIds.Coal, "Tambang batu bara bawah tanah dapat melepaskan gas metana.", true, "Metana terperangkap di lapisan batu bara dan terlepas saat batu bara ditambang.", CoalEnvironment),
            Q("coal-19", ResourceIds.Coal, "Gas metana di tambang batu bara mudah terbakar sehingga berbahaya.", true, "Metana mudah terbakar, sehingga tambang perlu ventilasi untuk mencegah ledakan.", CoalEnvironment),
            Q("coal-20", ResourceIds.Coal, "Tambang permukaan dapat merusak bentang alam jika tidak direklamasi.", true, "Tambang permukaan memindahkan tanah dan batuan, sehingga lahan perlu dipulihkan setelahnya.", CoalEnvironment),
            Q("coal-21", ResourceIds.Coal, "Reklamasi adalah upaya memulihkan lahan bekas tambang.", true, "Reklamasi mengembalikan tanah, menanam ulang, dan memperbaiki lahan bekas tambang.", CoalEnvironment),
            Q("coal-22", ResourceIds.Coal, "Batu bara termasuk logam.", false, "Batu bara adalah batuan sedimen yang mudah terbakar, bukan logam.", CoalBasics),
            Q("coal-23", ResourceIds.Coal, "Batu bara umumnya berwarna hitam atau cokelat kehitaman.", true, "Batu bara adalah batuan sedimen hitam atau cokelat kehitaman yang dapat dibakar.", CoalBasics),
            Q("coal-24", ResourceIds.Coal, "Batu bara biasa diangkut dengan kereta api, kapal tongkang, atau truk.", true, "Batu bara dikirim dari tambang ke pembangkit atau pelabuhan dengan kereta, tongkang, dan truk.", CoalMining),
            Q("coal-25", ResourceIds.Coal, "Sebagian besar batu bara di dunia dipakai untuk membangkitkan listrik.", true, "Sektor pembangkit listrik adalah pemakai batu bara terbesar di dunia.", CoalWorld),
            Q("coal-26", ResourceIds.Coal, "Emisi PLTU dapat dikurangi dengan alat pengendali seperti scrubber.", true, "Scrubber menangkap sulfur dioksida dari gas buang sebelum keluar dari cerobong.", CoalEnvironment),
            Q("coal-27", ResourceIds.Coal, "Batu bara terbentuk dari sisa hewan laut purba.", false, "Batu bara terbentuk dari sisa tumbuhan purba di rawa-rawa, bukan dari hewan laut.", CoalBasics),
            Q("coal-28", ResourceIds.Coal, "PLTU dapat menghasilkan listrik tanpa memerlukan air.", false, "PLTU membutuhkan air untuk membuat uap dan untuk pendinginan.", CoalEnvironment),
            Q("coal-29", ResourceIds.Coal, "Batu bara adalah sumber energi yang tidak akan pernah habis.", false, "Batu bara tidak terbarukan; cadangannya berkurang setiap kali ditambang.", CoalBasics),
            Q("coal-30", ResourceIds.Coal, "Pembakaran batu bara sama sekali tidak mencemari udara.", false, "Pembakaran batu bara melepaskan CO2, sulfur dioksida, nitrogen oksida, dan partikel debu.", CoalEnvironment),
            Q("coal-31", ResourceIds.Coal, "Batu bara berwujud cair seperti minyak bumi.", false, "Batu bara berwujud padat, berupa batuan sedimen.", CoalBasics),
            Q("coal-32", ResourceIds.Coal, "Antrasit adalah batu bara dengan peringkat paling rendah.", false, "Antrasit justru peringkat tertinggi. Peringkat terendah adalah lignit.", CoalBasics),
            Q("coal-33", ResourceIds.Coal, "Semua batu bara di Indonesia ditambang di Pulau Jawa.", false, "Produksi batu bara Indonesia terutama berasal dari Kalimantan dan Sumatra.", CoalIndonesia),
            Q("coal-34", ResourceIds.Coal, "Karbon dioksida dari PLTU tidak berpengaruh pada pemanasan global.", false, "CO2 adalah gas rumah kaca yang menambah pemanasan global.", CoalEnvironment),
            Q("coal-35", ResourceIds.Coal, "Lahan bekas tambang batu bara tidak perlu dipulihkan.", false, "Lahan bekas tambang perlu direklamasi agar tanah dan lingkungannya pulih.", CoalEnvironment),
            Q("coal-36", ResourceIds.Coal, "Batu bara tidak mengandung karbon.", false, "Unsur utama batu bara adalah karbon; itulah yang terbakar menghasilkan energi.", CoalBasics),
            Q("coal-37", ResourceIds.Coal, "Batu bara tidak pernah dipakai untuk membangkitkan listrik.", false, "Pembangkitan listrik justru pemakaian batu bara yang terbesar.", Coal),

            // ── Emas ──────────────────────────────────────────────────
            Q("gold-01", ResourceIds.Gold, "Emas dimanfaatkan pada sebagian komponen elektronik.", true, "Emas dipakai dalam elektronik karena menghantar listrik dan tahan korosi.", Gold),
            Q("gold-02", ResourceIds.Gold, "Emas adalah bahan bakar yang dibakar PLTU batu bara.", false, "Emas adalah mineral logam. PLTU batu bara memakai batu bara sebagai bahan bakar.", Gold),
            Q("gold-03", ResourceIds.Gold, "Emas memiliki sifat tahan korosi.", true, "Ketahanan korosi membantu pemakaian emas pada kontak elektronik tertentu.", Gold),
            Q("gold-04", ResourceIds.Gold, "Simbol kimia emas adalah Au.", true, "Au berasal dari kata Latin aurum yang berarti emas.", GoldElement),
            Q("gold-05", ResourceIds.Gold, "Simbol kimia emas adalah Ag.", false, "Ag adalah simbol perak. Simbol emas adalah Au.", GoldElement),
            Q("gold-06", ResourceIds.Gold, "Emas lebih berat daripada besi untuk ukuran yang sama.", true, "Massa jenis emas sekitar 19,3 g/cm3, jauh lebih besar daripada besi yang sekitar 7,9 g/cm3.", GoldElement),
            Q("gold-07", ResourceIds.Gold, "Mendulang memanfaatkan emas yang berat sehingga mengendap di dasar dulang.", true, "Saat dulang digoyang dalam air, pasir ringan terbuang dan emas yang berat tertinggal.", GoldProspecting),
            Q("gold-08", ResourceIds.Gold, "Emas mudah berkarat jika terkena air.", false, "Emas tidak berkarat dan tetap berkilau meski terkena air dan udara.", GoldElement),
            Q("gold-09", ResourceIds.Gold, "Emas dapat ditempa menjadi lembaran yang sangat tipis.", true, "Emas adalah logam yang paling mudah ditempa; bisa dibuat menjadi lembaran sangat tipis.", GoldElement),
            Q("gold-10", ResourceIds.Gold, "Emas murni disebut emas 24 karat.", true, "Karat menunjukkan kadar emas; 24 karat berarti emas murni.", GoldElement),
            Q("gold-11", ResourceIds.Gold, "Emas 18 karat lebih murni daripada emas 24 karat.", false, "Emas 18 karat hanya sekitar 75 persen emas, sedangkan 24 karat adalah emas murni.", GoldElement),
            Q("gold-12", ResourceIds.Gold, "Emas di pasir dan kerikil sungai disebut endapan plaser.", true, "Endapan plaser terbentuk saat emas terkikis dari batuan lalu terkumpul di sungai.", GoldProspecting),
            Q("gold-13", ResourceIds.Gold, "Emas dapat ditemukan dalam urat kuarsa di batuan.", true, "Endapan emas primer sering berupa urat kuarsa di dalam batuan.", GoldProspecting),
            Q("gold-14", ResourceIds.Gold, "Tambang Grasberg di Papua adalah salah satu tambang emas dan tembaga terbesar di dunia.", true, "Grasberg di Papua termasuk tambang emas dan tembaga terbesar di dunia.", GoldAsia),
            Q("gold-15", ResourceIds.Gold, "Merkuri yang dipakai penambang emas skala kecil berbahaya bagi kesehatan.", true, "Uap merkuri dapat merusak saraf, ginjal, dan paru-paru, serta mencemari lingkungan.", Mercury),
            Q("gold-16", ResourceIds.Gold, "Emas termasuk sumber daya mineral, bukan sumber energi.", true, "Emas tidak dibakar untuk energi; emas dipakai sebagai logam untuk perhiasan dan industri.", Gold),
            Q("gold-17", ResourceIds.Gold, "Emas adalah penghantar listrik yang baik.", true, "Emas menghantar listrik dengan baik dan tidak berkarat, cocok untuk konektor elektronik.", GoldElement),
            Q("gold-18", ResourceIds.Gold, "Emas dapat ditarik dengan magnet.", false, "Emas tidak tertarik magnet. Magnet justru dipakai untuk memisahkan pasir besi dari emas.", GoldProspecting),
            Q("gold-19", ResourceIds.Gold, "Pasir hitam di dulang yang tertarik magnet biasanya magnetit, bukan emas.", true, "Pasir hitam sering berisi magnetit yang berat dan magnetis, sering ditemukan bersama emas.", GoldProspecting),
            Q("gold-20", ResourceIds.Gold, "Emas dipakai sebagai perhiasan karena berkilau dan tidak mudah pudar.", true, "Emas tidak bereaksi dengan udara dan air, sehingga kilaunya bertahan lama.", GoldElement),
            Q("gold-21", ResourceIds.Gold, "Emas dapat didaur ulang dari barang elektronik bekas.", true, "Emas dari perangkat bekas dan perhiasan lama dapat diambil kembali dan dipakai ulang.", Gold),
            Q("gold-22", ResourceIds.Gold, "Emas berwujud cair pada suhu kamar.", false, "Emas baru melebur pada suhu sekitar 1.064 derajat Celsius.", GoldElement),
            Q("gold-23", ResourceIds.Gold, "Nomor atom emas adalah 79.", true, "Setiap atom emas memiliki 79 proton.", GoldElement),
            Q("gold-24", ResourceIds.Gold, "Pirit sering disebut emas palsu karena warnanya mirip emas.", true, "Pirit berwarna kuning mengilap seperti emas, tetapi lebih keras dan lebih ringan.", GoldProspecting),
            Q("gold-25", ResourceIds.Gold, "Emas murni sangat keras sehingga tidak bisa digores.", false, "Emas murni termasuk logam lunak, sehingga sering dicampur logam lain agar lebih kuat.", GoldElement),
            Q("gold-26", ResourceIds.Gold, "Emas juga dipakai di bidang kedokteran gigi.", true, "Paduan emas dipakai untuk tambalan dan mahkota gigi karena kuat dan tidak berkarat.", GoldElement),
            Q("gold-27", ResourceIds.Gold, "Emas lebih ringan daripada air sehingga mengapung.", false, "Emas sekitar 19 kali lebih berat daripada air dengan volume yang sama, jadi emas tenggelam.", GoldElement),
            Q("gold-28", ResourceIds.Gold, "Mendulang emas dilakukan dengan membakar pasir sungai.", false, "Mendulang memakai air dan goyangan dulang, bukan api.", GoldProspecting),
            Q("gold-29", ResourceIds.Gold, "Emas mudah hancur berkeping-keping jika dipukul.", false, "Emas sangat mudah ditempa: dipukul akan memipih, bukan hancur.", GoldElement),
            Q("gold-30", ResourceIds.Gold, "Uap merkuri aman dihirup saat mengolah emas.", false, "Uap merkuri beracun dan dapat merusak saraf, paru-paru, dan ginjal.", Mercury),
            Q("gold-31", ResourceIds.Gold, "Emas terbentuk dari sisa tumbuhan purba seperti batu bara.", false, "Emas adalah unsur kimia logam; emas tidak berasal dari sisa tumbuhan.", GoldElement),
            Q("gold-32", ResourceIds.Gold, "Pirit yang berwarna kuning adalah emas asli.", false, "Pirit adalah mineral besi sulfida yang hanya mirip emas, sehingga disebut emas palsu.", GoldProspecting),
            Q("gold-33", ResourceIds.Gold, "Emas tidak dapat menghantarkan listrik.", false, "Emas adalah penghantar listrik yang baik, sehingga dipakai di konektor elektronik.", GoldElement),
            Q("gold-34", ResourceIds.Gold, "Saat mendulang, emas lebih mudah terbuang daripada pasir karena lebih ringan.", false, "Emas lebih berat daripada pasir, sehingga mengendap di dasar dan pasir yang terbuang.", GoldProspecting),
            Q("gold-35", ResourceIds.Gold, "Emas 24 karat adalah emas yang dicampur separuh dengan logam lain.", false, "Emas 24 karat adalah emas murni. Emas campuran memiliki karat lebih rendah.", GoldElement)
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

        // `count` distinct random question ids for one capture (fewer if the bank is smaller).
        public static string[] PickForCapture(string resourceId, int count)
        {
            var pool = new List<QuestionDefinition>(ForResource(resourceId));
            var ids = new List<string>();
            while (ids.Count < count && pool.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, pool.Count);
                ids.Add(pool[i].id);
                pool.RemoveAt(i);
            }
            return ids.ToArray();
        }
    }
}
