/*

This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/

namespace  IText.IO.Font.Otf {
    /// <summary>Constants corresponding to language tags in the OTF specification.</summary>
    /// <remarks>
    /// Constants corresponding to language tags in the OTF specification.
    /// Extracted from the specification, as published by Microsoft
    /// <a href="https://docs.microsoft.com/en-us/typography/opentype/spec/languagetags">here</a>.
    /// Note that tags in OTF always consist of exactly 4 bytes. Shorter
    /// identifiers are padded with spaces as necessary.
    /// </remarks>
    /// <author><a href="mailto:matthias.valvekens@itextpdf.com">Matthias Valvekens</a></author>
    public sealed class LanguageTags {
        public const string ABAZA = "ABA ";

        public const string ABKHAZIAN = "ABK ";

        public const string ACHOLI = "ACH ";

        public const string ACHI = "ACR ";

        public const string ADYGHE = "ADY ";

        public const string AFRIKAANS = "AFK ";

        public const string AFAR = "AFR ";

        public const string AGAW = "AGW ";

        public const string AITON = "AIO ";

        public const string AKAN = "AKA ";

        public const string ALSATIAN = "ALS ";

        public const string ALTAI = "ALT ";

        public const string AMHARIC = "AMH ";

        public const string ANGLO_SAXON = "ANG ";

        public const string PHONETIC_AMERICANIST = "APPH";

        public const string ARABIC = "ARA ";

        public const string ARAGONESE = "ARG ";

        public const string AARI = "ARI ";

        public const string RAKHINE = "ARK ";

        public const string ASSAMESE = "ASM ";

        public const string ASTURIAN = "AST ";

        public const string ATHAPASKAN = "ATH ";

        public const string AVAR = "AVR ";

        public const string AWADHI = "AWA ";

        public const string AYMARA = "AYM ";

        public const string TORKI = "AZB ";

        public const string AZERBAIJANI = "AZE ";

        public const string BADAGA = "BAD ";

        public const string BANDA = "BAD0";

        public const string BAGHELKHANDI = "BAG ";

        public const string BALKAR = "BAL ";

        public const string BALINESE = "BAN ";

        public const string BAVARIAN = "BAR ";

        public const string BAULE = "BAU ";

        public const string BATAK_TOBA = "BBC ";

        public const string BERBER = "BBR ";

        public const string BENCH = "BCH ";

        public const string BIBLE_CREE = "BCR ";

        public const string BANDJALANG = "BDY ";

        public const string BELARUSSIAN = "BEL ";

        public const string BEMBA = "BEM ";

        public const string BENGALI = "BEN ";

        public const string HARYANVI = "BGC ";

        public const string BAGRI = "BGQ ";

        public const string BULGARIAN = "BGR ";

        public const string BHILI = "BHI ";

        public const string BHOJPURI = "BHO ";

        public const string BIKOL = "BIK ";

        public const string BILEN = "BIL ";

        public const string BISLAMA = "BIS ";

        public const string KANAUJI = "BJJ ";

        public const string BLACKFOOT = "BKF ";

        public const string BALUCHI = "BLI ";

        public const string PAO_KAREN = "BLK ";

        public const string BALANTE = "BLN ";

        public const string BALTI = "BLT ";

        public const string BAMBARA = "BMB ";

        public const string BAMILEKE = "BML ";

        public const string BOSNIAN = "BOS ";

        public const string BISHNUPRIYA_MANIPURI = "BPY ";

        public const string BRETON = "BRE ";

        public const string BRAHUI = "BRH ";

        public const string BRAJ_BHASHA = "BRI ";

        public const string BURMESE = "BRM ";

        public const string BODO = "BRX ";

        public const string BASHKIR = "BSH ";

        public const string BURUSHASKI = "BSK ";

        public const string BETI = "BTI ";

        public const string BATAK_SIMALUNGUN = "BTS ";

        public const string BUGIS = "BUG ";

        public const string MEDUMBA = "BYV ";

        public const string KAQCHIKEL = "CAK ";

        public const string CATALAN = "CAT ";

        public const string ZAMBOANGA_CHAVACANO = "CBK ";

        public const string CHINANTEC = "CCHN";

        public const string CEBUANO = "CEB ";

        public const string CHECHEN = "CHE ";

        public const string CHAHA_GURAGE = "CHG ";

        public const string CHATTISGARHI = "CHH ";

        public const string CHICHEWA = "CHI ";

        public const string CHUKCHI = "CHK ";

        public const string CHUUKESE = "CHK0";

        public const string CHOCTAW = "CHO ";

        public const string CHIPEWYAN = "CHP ";

        public const string CHEROKEE = "CHR ";

        public const string CHAMORRO = "CHA ";

        public const string CHUVASH = "CHU ";

        public const string CHEYENNE = "CHY ";

        public const string CHIGA = "CGG ";

        public const string WESTERN_CHAM = "CJA ";

        public const string EASTERN_CHAM = "CJM ";

        public const string COMORIAN = "CMR ";

        public const string COPTIC = "COP ";

        public const string CORNISH = "COR ";

        public const string CORSICAN = "COS ";

        public const string CREOLES = "CPP ";

        public const string CREE = "CRE ";

        public const string CARRIER = "CRR ";

        public const string CRIMEAN_TATAR = "CRT ";

        public const string KASHUBIAN = "CSB ";

        public const string CHURCH_SLAVONIC = "CSL ";

        public const string CZECH = "CSY ";

        public const string CHITTAGONIAN = "CTG ";

        public const string SAN_BLAS_KUNA = "CUK ";

        public const string DANISH = "DAN ";

        public const string DARGWA = "DAR ";

        public const string DAYI = "DAX ";

        public const string WOODS_CREE = "DCR ";

        public const string GERMAN = "DEU ";

        public const string DOGRI = "DGO ";

        public const string DOGRI2 = "DGR ";

        public const string DHANGU = "DHG ";

        public const string DHIVEHI = "DHV ";

        public const string DIMLI = "DIQ ";

        public const string DIVEHI = "DIV ";

        public const string ZARMA = "DJR ";

        public const string DJAMBARRPUYNGU = "DJR0";

        public const string DANGME = "DNG ";

        public const string DAN = "DNJ ";

        public const string DINKA = "DNK ";

        public const string DARI = "DRI ";

        public const string DHUWAL = "DUJ ";

        public const string DUNGAN = "DUN ";

        public const string DZONGKHA = "DZN ";

        public const string EBIRA = "EBI ";

        public const string EASTERN_CREE = "ECR ";

        public const string EDO = "EDO ";

        public const string EFIK = "EFI ";

        public const string GREEK = "ELL ";

        public const string EASTERN_MANINKAKAN = "EMK ";

        public const string ENGLISH = "ENG ";

        public const string ERZYA = "ERZ ";

        public const string SPANISH = "ESP ";

        public const string CENTRAL_YUPIK = "ESU ";

        public const string ESTONIAN = "ETI ";

        public const string BASQUE = "EUQ ";

        public const string EVENKI = "EVK ";

        public const string EVEN = "EVN ";

        public const string EWE = "EWE ";

        public const string FRENCH_ANTILLEAN = "FAN ";

        public const string FANG = "FAN0";

        public const string PERSIAN = "FAR ";

        public const string FANTI = "FAT ";

        public const string FINNISH = "FIN ";

        public const string FIJIAN = "FJI ";

        public const string DUTCH_FLEMISH = "FLE ";

        public const string FEFE = "FMP ";

        public const string FOREST_NENETS = "FNE ";

        public const string FON = "FON ";

        public const string FAROESE = "FOS ";

        public const string FRENCH = "FRA ";

        public const string CAJUN_FRENCH = "FRC ";

        public const string FRISIAN = "FRI ";

        public const string FRIULIAN = "FRL ";

        public const string ARPITAN = "FRP ";

        public const string FUTA = "FTA ";

        public const string FULAH = "FUL ";

        public const string NIGERIAN_FULFULDE = "FUV ";

        public const string GA = "GAD ";

        public const string SCOTTISH_GAELIC = "GAE ";

        public const string GAGAUZ = "GAG ";

        public const string GALICIAN = "GAL ";

        public const string GARSHUNI = "GAR ";

        public const string GARHWALI = "GAW ";

        public const string GEEZ = "GEZ ";

        public const string GITHABUL = "GIH ";

        public const string GILYAK = "GIL ";

        public const string KIRIBATI_GILBERTESE = "GIL0";

        public const string KPELLE_GUINEA = "GKP ";

        public const string GILAKI = "GLK ";

        public const string GUMUZ = "GMZ ";

        public const string GUMATJ = "GNN ";

        public const string GOGO = "GOG ";

        public const string GONDI = "GON ";

        public const string GREENLANDIC = "GRN ";

        public const string GARO = "GRO ";

        public const string GUARANI = "GUA ";

        public const string WAYUU = "GUC ";

        public const string GUPAPUYNGU = "GUF ";

        public const string GUJARATI = "GUJ ";

        public const string GUSII = "GUZ ";

        public const string HAITIAN_CREOLE = "HAI ";

        public const string HALAM_FALAM_CHIN = "HAL ";

        public const string HARAUTI = "HAR ";

        public const string HAUSA = "HAU ";

        public const string HAWAIIAN = "HAW ";

        public const string HAYA = "HAY ";

        public const string HAZARAGI = "HAZ ";

        public const string HAMMER_BANNA = "HBN ";

        public const string HERERO = "HER ";

        public const string HILIGAYNON = "HIL ";

        public const string HINDI = "HIN ";

        public const string HIGH_MARI = "HMA ";

        public const string HMONG = "HMN ";

        public const string HIRI_MOTU = "HMO ";

        public const string HINDKO = "HND ";

        public const string HO = "HO  ";

        public const string HARARI = "HRI ";

        public const string CROATIAN = "HRV ";

        public const string HUNGARIAN = "HUN ";

        public const string ARMENIAN = "HYE ";

        public const string ARMENIAN_EAST = "HYE0";

        public const string IBAN = "IBA ";

        public const string IBIBIO = "IBB ";

        public const string IGBO = "IBO ";

        public const string IJO_LANGUAGES = "IJO ";

        public const string IDO = "IDO ";

        public const string INTERLINGUE = "ILE ";

        public const string ILOKANO = "ILO ";

        public const string INTERLINGUA = "INA ";

        public const string INDONESIAN = "IND ";

        public const string INGUSH = "ING ";

        public const string INUKTITUT = "INU ";

        public const string INUPIAT = "IPK ";

        public const string PHONETIC_TRANSCRIPTION_IPA = "IPPH";

        public const string IRISH = "IRI ";

        public const string IRISH_TRADITIONAL = "IRT ";

        public const string ICELANDIC = "ISL ";

        public const string INARI_SAMI = "ISM ";

        public const string ITALIAN = "ITA ";

        public const string HEBREW = "IWR ";

        public const string JAMAICAN_CREOLE = "JAM ";

        public const string JAPANESE = "JAN ";

        public const string JAVANESE = "JAV ";

        public const string LOJBAN = "JBO ";

        public const string KRYMCHAK = "JCT ";

        public const string YIDDISH = "JII ";

        public const string LADINO = "JUD ";

        public const string JULA = "JUL ";

        public const string KABARDIAN = "KAB ";

        public const string KABYLE = "KAB0";

        public const string KACHCHI = "KAC ";

        public const string KALENJIN = "KAL ";

        public const string KANNADA = "KAN ";

        public const string KARACHAY = "KAR ";

        public const string GEORGIAN = "KAT ";

        public const string KAZAKH = "KAZ ";

        public const string MAKONDE = "KDE ";

        public const string KABUVERDIANU_CRIOULO = "KEA ";

        public const string KEBENA = "KEB ";

        public const string KEKCHI = "KEK ";

        public const string KHUTSURI_GEORGIAN = "KGE ";

        public const string KHAKASS = "KHA ";

        public const string KHANTY_KAZIM = "KHK ";

        public const string KHMER = "KHM ";

        public const string KHANTY_SHURISHKAR = "KHS ";

        public const string KHAMTI_SHAN = "KHT ";

        public const string KHANTY_VAKHI = "KHV ";

        public const string KHOWAR = "KHW ";

        public const string KIKUYU = "KIK ";

        public const string KIRGHIZ = "KIR ";

        public const string KISII = "KIS ";

        public const string KIRMANJKI = "KIU ";

        public const string SOUTHERN_KIWAI = "KJD ";

        public const string EASTERN_PWO_KAREN = "KJP ";

        public const string BUMTHANGKHA = "KJZ ";

        public const string KOKNI = "KKN ";

        public const string KALMYK = "KLM ";

        public const string KAMBA = "KMB ";

        public const string KUMAONI = "KMN ";

        public const string KOMO = "KMO ";

        public const string KOMSO = "KMS ";

        public const string KHORASANI_TURKIC = "KMZ ";

        public const string KANURI = "KNR ";

        public const string KODAGU = "KOD ";

        public const string KOREAN_OLD_HANGUL = "KOH ";

        public const string KONKANI = "KOK ";

        public const string KIKONGO = "KON ";

        public const string KOMI = "KOM ";

        public const string KONGO = "KON0";

        public const string KOMI_PERMYAK = "KOP ";

        public const string KOREAN = "KOR ";

        public const string KOSRAEAN = "KOS ";

        public const string KOMI_ZYRIAN = "KOZ ";

        public const string KPELLE = "KPL ";

        public const string KRIO = "KRI ";

        public const string KARAKALPAK = "KRK ";

        public const string KARELIAN = "KRL ";

        public const string KARAIM = "KRM ";

        public const string KAREN = "KRN ";

        public const string KOORETE = "KRT ";

        public const string KASHMIRI = "KSH ";

        public const string RIPUARIAN = "KSH0";

        public const string KHASI = "KSI ";

        public const string KILDIN_SAMI = "KSM ";

        public const string SGAW_KAREN = "KSW ";

        public const string KUANYAMA = "KUA ";

        public const string KUI = "KUI ";

        public const string KULVI = "KUL ";

        public const string KUMYK = "KUM ";

        public const string KURDISH = "KUR ";

        public const string KURUKH = "KUU ";

        public const string KUY = "KUY ";

        public const string KORYAK = "KYK ";

        public const string WESTERN_KAYAH = "KYU ";

        public const string LADIN = "LAD ";

        public const string LAHULI = "LAH ";

        public const string LAK = "LAK ";

        public const string LAMBANI = "LAM ";

        public const string LAO = "LAO ";

        public const string LATIN = "LAT ";

        public const string LAZ = "LAZ ";

        public const string L_CREE = "LCR ";

        public const string LADAKHI = "LDK ";

        public const string LEZGI = "LEZ ";

        public const string LIGURIAN = "LIJ ";

        public const string LIMBURGISH = "LIM ";

        public const string LINGALA = "LIN ";

        public const string LISU = "LIS ";

        public const string LAMPUNG = "LJP ";

        public const string LAKI = "LKI ";

        public const string LOW_MARI = "LMA ";

        public const string LIMBU = "LMB ";

        public const string LOMBARD = "LMO ";

        public const string LOMWE = "LMW ";

        public const string LOMA = "LOM ";

        public const string LURI = "LRC ";

        public const string LOWER_SORBIAN = "LSB ";

        public const string LULE_SAMI = "LSM ";

        public const string LITHUANIAN = "LTH ";

        public const string LUXEMBOURGISH = "LTZ ";

        public const string LUBA_LULUA = "LUA ";

        public const string LUBA_KATANGA = "LUB ";

        public const string GANDA = "LUG ";

        public const string LUYIA = "LUH ";

        public const string LUO = "LUO ";

        public const string LATVIAN = "LVI ";

        public const string MADURA = "MAD ";

        public const string MAGAHI = "MAG ";

        public const string MARSHALLESE = "MAH ";

        public const string MAJANG = "MAJ ";

        public const string MAKHUWA = "MAK ";

        public const string MALAYALAM = "MAL ";

        public const string MAM = "MAM ";

        public const string MANSI = "MAN ";

        public const string MAPUDUNGUN = "MAP ";

        public const string MARATHI = "MAR ";

        public const string MARWARI = "MAW ";

        public const string MBUNDU = "MBN ";

        public const string MBO = "MBO ";

        public const string MANCHU = "MCH ";

        public const string MOOSE_CREE = "MCR ";

        public const string MENDE = "MDE ";

        public const string MANDAR = "MDR ";

        public const string MEEN = "MEN ";

        public const string MERU = "MER ";

        public const string PATTANI_MALAY = "MFA ";

        public const string MORISYEN = "MFE ";

        public const string MINANGKABAU = "MIN ";

        public const string MIZO = "MIZ ";

        public const string MACEDONIAN = "MKD ";

        public const string MAKASAR = "MKR ";

        public const string KITUBA = "MKW ";

        public const string MALE = "MLE ";

        public const string MALAGASY = "MLG ";

        public const string MALINKE = "MLN ";

        public const string MALAYALAM_REFORMED = "MLR ";

        public const string MALAY = "MLY ";

        public const string MANDINKA = "MND ";

        public const string MONGOLIAN = "MNG ";

        public const string MANIPURI = "MNI ";

        public const string MANINKA = "MNK ";

        public const string MANX = "MNX ";

        public const string MOHAWK = "MOH ";

        public const string MOKSHA = "MOK ";

        public const string MOLDAVIAN = "MOL ";

        public const string MON = "MON ";

        public const string MOROCCAN = "MOR ";

        public const string MOSSI = "MOS ";

        public const string MAORI = "MRI ";

        public const string MAITHILI = "MTH ";

        public const string MALTESE = "MTS ";

        public const string MUNDARI = "MUN ";

        public const string MUSCOGEE = "MUS ";

        public const string MIRANDESE = "MWL ";

        public const string HMONG_DAW = "MWW ";

        public const string MAYAN = "MYN ";

        public const string MAZANDERANI = "MZN ";

        public const string NAGA_ASSAMESE = "NAG ";

        public const string NAHUATL = "NAH ";

        public const string NANAI = "NAN ";

        public const string NEAPOLITAN = "NAP ";

        public const string NASKAPI = "NAS ";

        public const string NAURUAN = "NAU ";

        public const string NAVAJO = "NAV ";

        public const string N_CREE = "NCR ";

        public const string NDEBELE = "NDB ";

        public const string NDAU = "NDC ";

        public const string NDONGA = "NDG ";

        public const string LOW_SAXON = "NDS ";

        public const string NEPALI = "NEP ";

        public const string NEWARI = "NEW ";

        public const string NGBAKA = "NGA ";

        public const string NAGARI = "NGR ";

        public const string NORWAY_HOUSE_CREE = "NHC ";

        public const string NISI = "NIS ";

        public const string NIUEAN = "NIU ";

        public const string NYANKOLE = "NKL ";

        public const string NKO = "NKO ";

        public const string DUTCH = "NLD ";

        public const string NIMADI = "NOE ";

        public const string NOGAI = "NOG ";

        public const string NORWEGIAN = "NOR ";

        public const string NOVIAL = "NOV ";

        public const string NORTHERN_SAMI = "NSM ";

        public const string SOTHO_NORTHERN = "NSO ";

        public const string NORTHERN_TAI = "NTA ";

        public const string ESPERANTO = "NTO ";

        public const string NYAMWEZI = "NYM ";

        public const string NORWEGIAN_NYNORSK = "NYN ";

        public const string MBEMBE_TIGON = "NZA ";

        public const string OCCITAN = "OCI ";

        public const string OJI_CREE = "OCR ";

        public const string OJIBWAY = "OJB ";

        public const string ODIA_ORIYA = "ORI ";

        public const string OROMO = "ORO ";

        public const string OSSETIAN = "OSS ";

        public const string PALESTINIAN_ARAMAIC = "PAA ";

        public const string PANGASINAN = "PAG ";

        public const string PALI = "PAL ";

        public const string PAMPANGAN = "PAM ";

        public const string PUNJABI = "PAN ";

        public const string PALPA = "PAP ";

        public const string PAPIAMENTU = "PAP0";

        public const string PASHTO = "PAS ";

        public const string PALAUAN = "PAU ";

        public const string BOUYEI = "PCC ";

        public const string PICARD = "PCD ";

        public const string PENNSYLVANIA_GERMAN = "PDC ";

        public const string POLYTONIC_GREEK = "PGR ";

        public const string PHAKE = "PHK ";

        public const string NORFOLK = "PIH ";

        public const string FILIPINO = "PIL ";

        public const string PALAUNG = "PLG ";

        public const string POLISH = "PLK ";

        public const string PIEMONTESE = "PMS ";

        public const string WESTERN_PANJABI = "PNB ";

        public const string POCOMCHI = "POH ";

        public const string POHNPEIAN = "PON ";

        public const string PROVENCAL = "PRO ";

        public const string PORTUGUESE = "PTG ";

        public const string WESTERN_PWO_KAREN = "PWO ";

        public const string CHIN = "QIN ";

        public const string KICHE = "QUC ";

        public const string QUECHUA_BOLIVIA = "QUH ";

        public const string QUECHUA = "QUZ ";

        public const string QUECHUA_ECUADOR = "QVI ";

        public const string QUECHUA_PERU = "QWH ";

        public const string RAJASTHANI = "RAJ ";

        public const string RAROTONGAN = "RAR ";

        public const string RUSSIAN_BURIAT = "RBU ";

        public const string R_CREE = "RCR ";

        public const string REJANG = "REJ ";

        public const string RIANG = "RIA ";

        public const string TARIFIT = "RIF ";

        public const string RITARUNGO = "RIT ";

        public const string ARAKWAL = "RKW ";

        public const string ROMANSH = "RMS ";

        public const string VLAX_ROMANI = "RMY ";

        public const string ROMANIAN = "ROM ";

        public const string ROMANY = "ROY ";

        public const string RUSYN = "RSY ";

        public const string ROTUMAN = "RTM ";

        public const string KINYARWANDA = "RUA ";

        public const string RUNDI = "RUN ";

        public const string AROMANIAN = "RUP ";

        public const string RUSSIAN = "RUS ";

        public const string SADRI = "SAD ";

        public const string SANSKRIT = "SAN ";

        public const string SASAK = "SAS ";

        public const string SANTALI = "SAT ";

        public const string SAYISI = "SAY ";

        public const string SICILIAN = "SCN ";

        public const string SCOTS = "SCO ";

        public const string SEKOTA = "SEK ";

        public const string SELKUP = "SEL ";

        public const string OLD_IRISH = "SGA ";

        public const string SANGO = "SGO ";

        public const string SAMOGITIAN = "SGS ";

        public const string TACHELHIT = "SHI ";

        public const string SHAN = "SHN ";

        public const string SIBE = "SIB ";

        public const string SIDAMO = "SID ";

        public const string SILTE_GURAGE = "SIG ";

        public const string SKOLT_SAMI = "SKS ";

        public const string SLOVAK = "SKY ";

        public const string NORTH_SLAVEY = "SCS ";

        public const string SLAVEY = "SLA ";

        public const string SLOVENIAN = "SLV ";

        public const string SOMALI = "SML ";

        public const string SAMOAN = "SMO ";

        public const string SENA = "SNA ";

        public const string SHONA = "SNA0";

        public const string SINDHI = "SND ";

        public const string SINHALA = "SNH ";

        public const string SONINKE = "SNK ";

        public const string SODO_GURAGE = "SOG ";

        public const string SONGE = "SOP ";

        public const string SOTHO_SOUTHERN = "SOT ";

        public const string ALBANIAN = "SQI ";

        public const string SERBIAN = "SRB ";

        public const string SARDINIAN = "SRD ";

        public const string SARAIKI = "SRK ";

        public const string SERER = "SRR ";

        public const string SOUTH_SLAVEY = "SSL ";

        public const string SOUTHERN_SAMI = "SSM ";

        public const string SATERLAND_FRISIAN = "STQ ";

        public const string SUKUMA = "SUK ";

        public const string SUNDANESE = "SUN ";

        public const string SURI = "SUR ";

        public const string SVAN = "SVA ";

        public const string SWEDISH = "SVE ";

        public const string SWADAYA_ARAMAIC = "SWA ";

        public const string SWAHILI = "SWK ";

        public const string SWATI = "SWZ ";

        public const string SUTU = "SXT ";

        public const string UPPER_SAXON = "SXU ";

        public const string SYLHETI = "SYL ";

        public const string SYRIAC = "SYR ";

        public const string SYRIAC_ESTRANGELA = "SYRE";

        public const string SYRIAC_WESTERN = "SYRJ";

        public const string SYRIAC_EASTERN = "SYRN";

        public const string SILESIAN = "SZL ";

        public const string TABASARAN = "TAB ";

        public const string TAJIKI = "TAJ ";

        public const string TAMIL = "TAM ";

        public const string TATAR = "TAT ";

        public const string TH_CREE = "TCR ";

        public const string DEHONG_DAI = "TDD ";

        public const string TELUGU = "TEL ";

        public const string TETUM = "TET ";

        public const string TAGALOG = "TGL ";

        public const string TONGAN = "TGN ";

        public const string TIGRE = "TGR ";

        public const string TIGRINYA = "TGY ";

        public const string THAI = "THA ";

        public const string TAHITIAN = "THT ";

        public const string TIBETAN = "TIB ";

        public const string TIV = "TIV ";

        public const string TURKMEN = "TKM ";

        public const string TAMASHEK = "TMH ";

        public const string TEMNE = "TMN ";

        public const string TSWANA = "TNA ";

        public const string TUNDRA_NENETS = "TNE ";

        public const string TONGA = "TNG ";

        public const string TODO = "TOD ";

        public const string TOMA = "TOD0";

        public const string TOK_PISIN = "TPI ";

        public const string TURKISH = "TRK ";

        public const string TSONGA = "TSG ";

        public const string TSHANGLA = "TSJ ";

        public const string TUROYO_ARAMAIC = "TUA ";

        public const string TULU = "TUM ";

        public const string TUMBUKA = "TUL ";

        public const string TUVIN = "TUV ";

        public const string TUVALU = "TVL ";

        public const string TWI = "TWI ";

        public const string TAY = "TYZ ";

        public const string TAMAZIGHT = "TZM ";

        public const string TZOTZIL = "TZO ";

        public const string UDMURT = "UDM ";

        public const string UKRAINIAN = "UKR ";

        public const string UMBUNDU = "UMB ";

        public const string URDU = "URD ";

        public const string UPPER_SORBIAN = "USB ";

        public const string UYGHUR = "UYG ";

        public const string UZBEK = "UZB ";

        public const string VENETIAN = "VEC ";

        public const string VENDA = "VEN ";

        public const string VIETNAMESE = "VIT ";

        public const string VOLAPUK = "VOL ";

        public const string VORO = "VRO ";

        public const string WA = "WA  ";

        public const string WAGDI = "WAG ";

        public const string WARAY_WARAY = "WAR ";

        public const string WEST_CREE = "WCR ";

        public const string WELSH = "WEL ";

        public const string WALLOON = "WLN ";

        public const string WOLOF = "WLF ";

        public const string MEWATI = "WTM ";

        public const string LU = "XBD ";

        public const string KHENGKHA = "XKF ";

        public const string XHOSA = "XHS ";

        public const string MINJANGBAL = "XJB ";

        public const string SOGA = "XOG ";

        public const string KPELLE_LIBERIA = "XPE ";

        public const string SAKHA = "YAK ";

        public const string YAO = "YAO ";

        public const string YAPESE = "YAP ";

        public const string YORUBA = "YBA ";

        public const string Y_CREE = "YCR ";

        public const string YI_CLASSIC = "YIC ";

        public const string YI_MODERN = "YIM ";

        public const string ZEALANDIC = "ZEA ";

        public const string STANDARD_MOROCCAN_TAMAZIGHT = "ZGH ";

        public const string ZHUANG = "ZHA ";

        public const string CHINESE_HONG_KONG = "ZHH ";

        public const string CHINESE_PHONETIC = "ZHP ";

        public const string CHINESE_SIMPLIFIED = "ZHS ";

        public const string CHINESE_TRADITIONAL = "ZHT ";

        public const string ZANDE = "ZND ";

        public const string ZULU = "ZUL ";

        public const string ZAZAKI = "ZZA ";

        private LanguageTags() {
        }
    }
}
