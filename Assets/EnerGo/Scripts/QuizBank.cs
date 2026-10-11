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

        private const string OilBasics = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/";
        private const string OilWhere = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/where-our-oil-comes-from.php";
        private const string OilRefining = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/refining-crude-oil.php";
        private const string OilUse = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/use-of-oil.php";
        private const string OilEnvironment = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/oil-and-the-environment.php";
        private const string IndonesiaEnergy = "https://www.eia.gov/international/analysis/country/IDN";

        private const string Nuclear = "https://www.eia.gov/energyexplained/nuclear/";
        private const string NuclearPlants = "https://www.eia.gov/energyexplained/nuclear/nuclear-power-plants.php";
        private const string NuclearFuelCycle = "https://www.eia.gov/energyexplained/nuclear/the-nuclear-fuel-cycle.php";
        private const string NuclearEnvironment = "https://www.eia.gov/energyexplained/nuclear/nuclear-power-and-the-environment.php";
        private const string WhatIsUranium = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/introduction/what-is-uranium-how-does-it-work";
        private const string Reactors = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/nuclear-power-reactors/nuclear-power-reactors";
        private const string RadiationLife = "https://world-nuclear.org/information-library/safety-and-security/radiation-and-health/radiation-and-life";
        private const string Alara = "https://www.cdc.gov/radiation-health/safety/alara.html";
        private const string UraniumMining = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/mining-of-uranium/world-uranium-mining-production";
        private const string NuclearIndonesia = "https://world-nuclear.org/information-library/country-profiles/countries-g-n/indonesia";

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
            Q("gold-35", ResourceIds.Gold, "Emas 24 karat adalah emas yang dicampur separuh dengan logam lain.", false, "Emas 24 karat adalah emas murni. Emas campuran memiliki karat lebih rendah.", GoldElement),

            // ── Minyak Bumi ───────────────────────────────────────────
            Q("oil-01", ResourceIds.Oil, "Minyak bumi terbentuk dari sisa organisme laut kecil seperti plankton yang tertimbun jutaan tahun.", true, "Sisa tumbuhan dan hewan laut kecil tertimbun lumpur, lalu panas dan tekanan mengubahnya menjadi minyak.", OilBasics),
            Q("oil-02", ResourceIds.Oil, "Minyak bumi termasuk sumber energi terbarukan.", false, "Minyak bumi adalah bahan bakar fosil yang butuh jutaan tahun untuk terbentuk, jadi tidak terbarukan.", OilBasics),
            Q("oil-03", ResourceIds.Oil, "Minyak mentah diolah di kilang menjadi bensin, solar, dan avtur.", true, "Kilang memisahkan dan mengolah minyak mentah menjadi berbagai bahan bakar.", OilRefining),
            Q("oil-04", ResourceIds.Oil, "Minyak bumi sering terperangkap di bawah lapisan batuan kedap yang disebut batuan penutup.", true, "Batuan penutup yang kedap menahan minyak agar tidak naik ke permukaan, sehingga minyak terkumpul di reservoir.", OilWhere),
            Q("oil-05", ResourceIds.Oil, "Di dalam reservoir, gas alam biasanya berada di atas minyak, dan air di bawahnya.", true, "Gas paling ringan sehingga naik ke atas, minyak di tengah, dan air yang paling berat di bawah.", OilWhere),
            Q("oil-06", ResourceIds.Oil, "Minyak lebih berat daripada air sehingga langsung tenggelam saat tumpah di laut.", false, "Sebagian besar minyak lebih ringan daripada air, sehingga tumpahan minyak mengapung dan menyebar di permukaan laut.", OilEnvironment),
            Q("oil-07", ResourceIds.Oil, "Pompa angguk dipakai untuk mengangkat minyak ketika tekanan alami sumur sudah melemah.", true, "Saat tekanan reservoir turun, pompa membantu mengangkat minyak dari dalam sumur ke permukaan.", OilWhere),
            Q("oil-08", ResourceIds.Oil, "Satu barel minyak setara dengan sekitar 159 liter.", true, "Satu barel berisi 42 galon AS, atau sekitar 159 liter.", OilBasics),
            Q("oil-09", ResourceIds.Oil, "Pembakaran bensin di kendaraan tidak menghasilkan karbon dioksida.", false, "Membakar bensin dan solar menghasilkan karbon dioksida, gas rumah kaca utama.", OilEnvironment),
            Q("oil-10", ResourceIds.Oil, "Sektor transportasi adalah pemakai produk minyak bumi terbesar.", true, "Sebagian besar minyak dipakai sebagai bensin, solar, dan avtur untuk kendaraan, kapal, dan pesawat.", OilUse),
            Q("oil-11", ResourceIds.Oil, "Plastik dapat dibuat dari bahan hasil olahan minyak bumi.", true, "Produk petrokimia dari minyak bumi menjadi bahan baku plastik dan banyak barang lain.", OilUse),
            Q("oil-12", ResourceIds.Oil, "Kilang memisahkan minyak mentah dengan cara membekukannya.", false, "Kilang memanaskan minyak mentah lalu memisahkannya lewat distilasi, bukan membekukannya.", OilRefining),
            Q("oil-13", ResourceIds.Oil, "Distilasi di kilang memisahkan minyak mentah berdasarkan perbedaan titik didih.", true, "Tiap bagian minyak menguap pada suhu berbeda, sehingga bisa dipisahkan di menara distilasi.", OilRefining),
            Q("oil-14", ResourceIds.Oil, "Tumpahan minyak di laut dapat membahayakan burung dan ikan.", true, "Minyak melapisi bulu burung dan meracuni makhluk laut, sehingga tumpahan sangat merusak.", OilEnvironment),
            Q("oil-15", ResourceIds.Oil, "Pengeboran minyak juga dilakukan di lepas pantai memakai anjungan.", true, "Banyak cadangan minyak berada di bawah dasar laut dan dibor dari anjungan lepas pantai.", OilWhere),
            Q("oil-16", ResourceIds.Oil, "Minyak bumi selalu ditemukan dekat permukaan tanah, tidak pernah di kedalaman ribuan meter.", false, "Banyak sumur minyak dibor hingga ribuan meter di bawah permukaan.", OilWhere),
            Q("oil-17", ResourceIds.Oil, "PLTD memakai bahan bakar solar, yang merupakan produk minyak bumi.", true, "Pembangkit Listrik Tenaga Diesel membakar solar hasil olahan minyak bumi.", OilUse),
            Q("oil-18", ResourceIds.Oil, "Riau termasuk daerah penghasil minyak bumi utama di Indonesia.", true, "Lapangan-lapangan minyak besar seperti di Blok Rokan berada di Riau.", IndonesiaEnergy),
            Q("oil-19", ResourceIds.Oil, "Indonesia saat ini mengekspor jauh lebih banyak minyak daripada yang diimpornya.", false, "Sejak awal 2000-an konsumsi Indonesia melebihi produksinya, sehingga Indonesia menjadi pengimpor bersih minyak.", IndonesiaEnergy),
            Q("oil-20", ResourceIds.Oil, "Pertamina adalah perusahaan energi milik negara Indonesia.", true, "Pertamina adalah BUMN yang mengelola minyak dan gas bumi Indonesia.", IndonesiaEnergy),
            Q("oil-21", ResourceIds.Oil, "Minyak mentah umumnya berwarna hitam atau cokelat gelap.", true, "Minyak mentah biasanya cairan kental berwarna gelap, walau warnanya bisa berbeda tiap lapangan.", OilBasics),
            Q("oil-22", ResourceIds.Oil, "Minyak bumi terbentuk hanya dalam waktu beberapa tahun.", false, "Pembentukan minyak bumi butuh jutaan tahun di bawah panas dan tekanan.", OilBasics),
            Q("oil-23", ResourceIds.Oil, "Aspal untuk jalan berasal dari bagian berat hasil pengolahan minyak bumi.", true, "Bagian paling berat dari minyak mentah di kilang dipakai sebagai aspal.", OilRefining),
            Q("oil-24", ResourceIds.Oil, "Mengebor kantong gas bertekanan tinggi tidak memiliki risiko apa pun.", false, "Gas bertekanan tinggi bisa menyembur liar (blowout) jika pengeboran tidak dikendalikan.", OilEnvironment),
            Q("oil-25", ResourceIds.Oil, "Sumur minyak modern dilengkapi alat pencegah semburan liar (blowout preventer).", true, "Blowout preventer menutup sumur saat tekanan melonjak untuk mencegah semburan liar.", OilEnvironment),
            Q("oil-26", ResourceIds.Oil, "Avtur adalah bahan bakar khusus untuk kapal laut.", false, "Avtur adalah bahan bakar pesawat jet, salah satu produk kilang minyak.", OilUse),
            Q("oil-27", ResourceIds.Oil, "Gas buang kendaraan berbahan bakar minyak dapat menyebabkan polusi udara di kota.", true, "Pembakaran bensin dan solar melepaskan polutan yang menurunkan kualitas udara.", OilEnvironment),
            Q("oil-28", ResourceIds.Oil, "Minyak yang sudah disedot dari reservoir akan terisi kembali dengan cepat.", false, "Reservoir terisi selama jutaan tahun. Minyak yang sudah diambil tidak cepat tergantikan.", OilBasics),
            Q("oil-29", ResourceIds.Oil, "Selain bahan bakar, minyak bumi juga dipakai untuk membuat pelumas.", true, "Oli dan pelumas mesin dibuat dari hasil olahan minyak bumi.", OilUse),
            Q("oil-30", ResourceIds.Oil, "Minyak bumi dan minyak goreng berasal dari sumber yang sama.", false, "Minyak goreng dibuat dari tumbuhan seperti kelapa sawit, sedangkan minyak bumi berasal dari dalam perut bumi.", OilBasics),

            // ── Uranium ───────────────────────────────────────────────
            Q("uranium-01", ResourceIds.Uranium, "Uranium adalah logam radioaktif yang ditemukan di batuan kerak Bumi.", true, "Uranium terdapat dalam batuan, tanah, bahkan air laut dalam jumlah kecil.", WhatIsUranium),
            Q("uranium-02", ResourceIds.Uranium, "Uranium termasuk sumber energi terbarukan.", false, "Uranium adalah bahan tambang yang jumlahnya terbatas, jadi tidak terbarukan.", Nuclear),
            Q("uranium-03", ResourceIds.Uranium, "PLTN memakai panas dari fisi uranium untuk membuat uap penggerak turbin.", true, "Panas dari reaksi fisi mendidihkan air menjadi uap, lalu uap memutar turbin pembangkit.", NuclearPlants),
            Q("uranium-04", ResourceIds.Uranium, "Fisi adalah proses pembelahan inti atom yang melepaskan energi.", true, "Saat inti uranium terbelah, sejumlah besar energi dilepaskan sebagai panas.", Nuclear),
            Q("uranium-05", ResourceIds.Uranium, "Matahari menghasilkan energi dengan reaksi fisi uranium.", false, "Matahari memakai fusi, yaitu penggabungan inti hidrogen, bukan fisi uranium.", Nuclear),
            Q("uranium-06", ResourceIds.Uranium, "Batang kendali di reaktor menyerap neutron untuk mengatur laju reaksi berantai.", true, "Memasukkan batang kendali lebih dalam menyerap lebih banyak neutron, sehingga reaksi melambat.", Reactors),
            Q("uranium-07", ResourceIds.Uranium, "Uranium-235 lebih mudah mengalami fisi daripada uranium-238.", true, "Uranium-235 adalah isotop yang mudah dibelah dan menjadi bahan bakar utama reaktor.", WhatIsUranium),
            Q("uranium-08", ResourceIds.Uranium, "Sebagian besar uranium alam berupa uranium-235.", false, "Uranium alam hampir seluruhnya uranium-238. Uranium-235 hanya sekitar 0,7 persen.", WhatIsUranium),
            Q("uranium-09", ResourceIds.Uranium, "Pengayaan uranium menaikkan kadar uranium-235 dalam bahan bakar.", true, "Pengayaan menaikkan kadar uranium-235 agar reaksi berantai di reaktor dapat berlangsung.", NuclearFuelCycle),
            Q("uranium-10", ResourceIds.Uranium, "Pencacah Geiger dipakai untuk mendeteksi radiasi.", true, "Pencacah Geiger berbunyi klik setiap kali menangkap partikel radiasi.", RadiationLife),
            Q("uranium-11", ResourceIds.Uranium, "Radiasi nuklir dapat dilihat dengan mata telanjang sebagai cahaya hijau.", false, "Radiasi pengion tidak terlihat oleh mata, sehingga perlu alat seperti pencacah Geiger.", RadiationLife),
            Q("uranium-12", ResourceIds.Uranium, "Timbal dan beton tebal dapat menahan radiasi.", true, "Bahan padat seperti timbal dan beton tebal dipakai sebagai pelindung radiasi.", RadiationLife),
            Q("uranium-13", ResourceIds.Uranium, "PLTN tidak mengeluarkan karbon dioksida saat membangkitkan listrik.", true, "Reaktor nuklir tidak membakar bahan bakar, sehingga tidak melepaskan CO2 saat beroperasi.", NuclearEnvironment),
            Q("uranium-14", ResourceIds.Uranium, "Bahan bakar bekas dari reaktor tetap radioaktif dan harus disimpan dengan aman.", true, "Bahan bakar bekas sangat radioaktif dan disimpan di kolam atau wadah khusus.", NuclearEnvironment),
            Q("uranium-15", ResourceIds.Uranium, "Limbah nuklir boleh dibuang ke tempat sampah biasa.", false, "Limbah radioaktif diatur ketat dan disimpan di fasilitas khusus agar tidak mencemari lingkungan.", NuclearEnvironment),
            Q("uranium-16", ResourceIds.Uranium, "Bahan bakar reaktor dibentuk menjadi pelet kecil yang disusun dalam batang bahan bakar.", true, "Pelet uranium dimasukkan ke dalam tabung logam yang disebut batang bahan bakar.", NuclearFuelCycle),
            Q("uranium-17", ResourceIds.Uranium, "Uranium menyimpan energi jauh lebih padat daripada batu bara.", true, "Sedikit uranium menghasilkan energi yang setara dengan batu bara dalam jumlah sangat besar.", WhatIsUranium),
            Q("uranium-18", ResourceIds.Uranium, "Indonesia sudah memiliki banyak PLTN komersial yang beroperasi.", false, "Indonesia belum memiliki PLTN komersial. Saat ini hanya ada reaktor riset.", NuclearIndonesia),
            Q("uranium-19", ResourceIds.Uranium, "Indonesia memiliki reaktor riset, salah satunya di Serpong.", true, "Reaktor riset di Serpong, Bandung, dan Yogyakarta dipakai untuk penelitian dan produksi radioisotop.", NuclearIndonesia),
            Q("uranium-20", ResourceIds.Uranium, "Kalimantan Barat memiliki endapan uranium yang sudah diteliti, misalnya di daerah Kalan.", true, "Eksplorasi di Kalan, Kalimantan Barat, menemukan endapan uranium.", NuclearIndonesia),
            Q("uranium-21", ResourceIds.Uranium, "Uranium yang baru ditambang bisa langsung dimasukkan ke reaktor tanpa diolah.", false, "Bijih uranium harus diolah, dikonversi, diperkaya, lalu dibentuk menjadi bahan bakar.", NuclearFuelCycle),
            Q("uranium-22", ResourceIds.Uranium, "Yellowcake adalah konsentrat uranium hasil pengolahan bijih.", true, "Bijih uranium digerus dan diolah menjadi bubuk kuning yang disebut yellowcake.", NuclearFuelCycle),
            Q("uranium-23", ResourceIds.Uranium, "Semakin lama berada dekat sumber radiasi, semakin besar dosis yang diterima tubuh.", true, "Dosis radiasi menumpuk seiring waktu, jadi waktu paparan harus dibatasi.", RadiationLife),
            Q("uranium-24", ResourceIds.Uranium, "Menjauh dari sumber radiasi mengurangi dosis yang diterima.", true, "Waktu, jarak, dan pelindung adalah tiga cara utama mengurangi dosis radiasi.", RadiationLife),
            Q("uranium-25", ResourceIds.Uranium, "Kita sama sekali tidak pernah terkena radiasi dalam kehidupan sehari-hari.", false, "Ada radiasi latar alami dari batuan, tanah, dan luar angkasa yang kita terima setiap hari.", RadiationLife),
            Q("uranium-26", ResourceIds.Uranium, "Reaksi berantai terjadi ketika neutron dari satu fisi memicu fisi inti atom lainnya.", true, "Setiap fisi melepaskan neutron baru yang dapat membelah inti uranium berikutnya.", Reactors),
            Q("uranium-27", ResourceIds.Uranium, "Meltdown berarti reaktor sedang bekerja paling efisien.", false, "Meltdown adalah kecelakaan saat inti reaktor terlalu panas hingga bahan bakarnya meleleh.", Reactors),
            Q("uranium-28", ResourceIds.Uranium, "Banyak reaktor nuklir memakai air sebagai pendingin.", true, "Air mengambil panas dari inti reaktor dan sekaligus dipakai untuk membuat uap.", Reactors),
            Q("uranium-29", ResourceIds.Uranium, "Uranium hanya ditemukan di luar angkasa, tidak di Bumi.", false, "Uranium ditambang dari batuan di Bumi, misalnya di Kazakhstan, Kanada, dan Australia.", WhatIsUranium),
            Q("uranium-30", ResourceIds.Uranium, "Kazakhstan termasuk produsen uranium terbesar di dunia.", true, "Kazakhstan menambang uranium lebih banyak daripada negara lain mana pun.", UraniumMining),
            Q("uranium-31", ResourceIds.Uranium, "Tiga cara melindungi diri dari radiasi adalah membatasi waktu, menjaga jarak, dan memakai perisai.", true, "Prinsip ALARA: sesingkat mungkin di dekat sumber, sejauh mungkin darinya, dan pasang penghalang seperti timbal.", Alara),
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
