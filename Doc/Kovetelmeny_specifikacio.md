# Követelményspecifikáció

## 1. Vezetői összefoglaló

Egy városi kerékpárkölcsönző és -szerviz vállalkozás a forgalma növekedésével elérte azt a pontot, ahol a
telefonos és személyes foglalásra, valamint papíralapú nyilvántartásra épülő működés már nem fenntartható.
A tulajdonos célja, hogy a bérlés és a szervizidőpont-foglalás online, a nap bármely szakában elérhető legyen,
a flotta és a foglalások nyilvántartása pedig egy központi rendszerben történjen.
Feladatunk tárgya a **BikeRental** webalkalmazás elkészítése, amely a kerékpárok online bérlését,
a szervizidőpontok foglalását, az ügyfélértékelések gyűjtését és a teljes adminisztrációt egy helyen kezeli.

## 2. Jelenlegi helyzet leírása

- A kölcsönző jelenleg 15 kerékpárral rendelkezik öt kategóriában (MTB, trekking, városi, elektromos, gyerek).
  Minden kerékpárnak saját napidíja van.
- A bérlési igények telefonon, e-mailben vagy személyesen érkeznek. A foglalásokat egy füzetben és egy
  táblázatkezelőben rögzítik. Gyakori, hogy ugyanarra a kerékpárra két, egymást átfedő foglalás kerül,
  mert a két nyilvántartás nem mindig egyezik.
- Az ügyfél nem látja, mely kerékpár mikor szabad, ezért több körös egyeztetés szükséges egy foglaláshoz.
- A szerviz időpontjait egy fali naptárban vezetik. Munkanapokon 9 és 17 óra között egyórás sávokban fogadnak
  javítást. A lemondások és módosítások gyakran nem kerülnek át a naptárba, így üresen maradó vagy
  dupla foglalású sávok keletkeznek.
- Az ügyfelek véleményét nem gyűjtik rendszerezetten, így a tulajdonos nem kap visszajelzést a szolgáltatás
  minőségéről.
- A bevételről és a kihasználtságról csak hónap végén, kézi összesítéssel készül kimutatás.
- A kerékpárok adatai (név, kategória, ár, fénykép) egy statikus weboldalon szerepelnek, amelyet csak külső
  fejlesztő tud módosítani.

## 3. Vágyálomrendszer leírása

A cél a kölcsönző papíralapú és telefonos folyamatainak kiváltása egy webes rendszerrel, amely az ügyfeleknek
önkiszolgáló foglalást, a személyzetnek egyszerű adminisztrációt nyújt.

A rendszer nyilvános része bejelentkezés nélkül is mutassa a kerékpárkínálatot (kép, kategória, napidíj) és
az ügyfélértékeléseket. Regisztráció és bejelentkezés után az ügyfél egy naptárban válasszon bérlési időszakot;
a rendszer csak az adott időszakban szabad kerékpárokat kínálja fel, a kiválasztottakat kosárba tegye, és a kosár
tartalmát egy lépésben foglalja le. A foglaláskor a rendszer újra ellenőrizze az ütközést, hogy két ügyfél
ugyanarra az időszakra ne foglalhassa ugyanazt a kerékpárt.

Az ügyfél a szerviznaptárban lássa a szabad időpontokat, foglaljon, módosítsa vagy mondja le a saját foglalását.
Bejelentkezett ügyfél értékelést írhasson (1–5 csillag és szöveges vélemény), amelyet később módosíthat vagy törölhet.

Az adminisztrátor egy vezérlőpulton lássa a legfontosabb mutatókat (flotta, aktív és közelgő bérlések, bevétel,
függő szervizfoglalások, értékelések). Vehessen fel új kerékpárt fényképpel együtt, módosíthassa az árát, tilthassa
le átmenetileg, illetve törölhesse, ha még nem volt bérelve. Kezelje a bérléseket, a szervizfoglalások státuszát,
a felhasználók jogosultságát, és moderálhassa az értékeléseket.

A felület legyen modern, mobilon is jól használható, magyar nyelvű. A rendszer költséghatékonyan üzemeltethető,
nyílt forráskódú, platformfüggetlen technológiákra épüljön: ASP.NET Core backend, React kliens, SQLite adatbázis,
böngészőből elérhető felület.

## 4. A rendszerre vonatkozó pályázat, törvények, rendeletek, szabványok és ajánlások felsorolása

**Európai uniós rendeletek, irányelvek**

*AZ EURÓPAI PARLAMENT ÉS A TANÁCS (EU) 2016/679 RENDELETE (2016. április 27.)* a természetes személyeknek a személyes
adatok kezelése tekintetében történő védelméről és az ilyen adatok szabad áramlásáról (GDPR). A rendszer
felhasználónevet, e-mail-címet, bérlési és szervizadatokat kezel.

*AZ EURÓPAI PARLAMENT ÉS A TANÁCS (EU) 2019/882 IRÁNYELVE (2019. április 17.)* a termékekre és szolgáltatásokra
vonatkozó akadálymentességi követelményekről (European Accessibility Act).

*2011/83/EU irányelv* a fogyasztók jogairól (távollévők között kötött szerződések).

**Törvények**

*2011. évi CXII. törvény* az információs önrendelkezési jogról és az információszabadságról (Infotv.)

*2013. évi V. törvény* a Polgári Törvénykönyvről (bérleti szerződés, vállalkozási szerződés)

*1997. évi CLV. törvény* a fogyasztóvédelemről

*2001. évi CVIII. törvény* az elektronikus kereskedelmi szolgáltatások, valamint az információs társadalommal összefüggő
szolgáltatások egyes kérdéseiről

**Kormány- és miniszteri rendeletek**

*45/2014. (II. 26.) Korm. rendelet* a fogyasztó és a vállalkozás közötti szerződések részletes szabályairól

*1/1975. (II. 5.) KPM–BM együttes rendelet* a közúti közlekedés szabályairól (KRESZ), a kerékpárok kötelező
felszereltségére vonatkozó előírások miatt

**Szabványok, ajánlások**

*WCAG 2.1* (Web Content Accessibility Guidelines), AA szint

*ISO/IEC 25010* szoftvertermék-minőségi modell

*ISO/IEC/IEEE 12207* szoftveréletciklus-folyamatok

*OWASP Top 10* és *OWASP ASVS* webalkalmazás-biztonsági ajánlások

*RFC 7519* JSON Web Token (JWT)

## 5. Jelenlegi üzleti folyamatok modellje

Üzleti szereplők:
- ügyfél (bérlő, szervizügyfél)

Üzleti munkatársak:
- pultos (kölcsönzés, foglalások felvétele)
- szerelő (szerviz)
- tulajdonos (árazás, flotta, kimutatások)

Üzleti entitások:
- kerékpár
- foglalási füzet és táblázat
- szerviznaptár
- átadás-átvételi lap
- havi kimutatás

Üzleti folyamatok:
- Bérlési igény felvétele: ügyfél telefonon vagy személyesen jelzi az igényét => pultos a füzetben és a táblázatban
  megnézi, szabad-e a kerékpár => ha szabad, rögzíti a foglalást a füzetbe, majd később a táblázatba => szóban
  vagy e-mailben visszaigazolja.
- Kerékpár kiadása: ügyfél megérkezik => pultos megkeresi a foglalást => kitölti az átadás-átvételi lapot =>
  átadja a kerékpárt.
- Kerékpár visszavétele: ügyfél visszahozza a kerékpárt => pultos ellenőrzi az állapotát => kiszámolja a
  napok száma és a napidíj alapján a díjat => rögzíti a visszavételt.
- Szervizidőpont foglalása: ügyfél telefonál => pultos a fali naptárban szabad sávot keres => beírja az ügyfél
  nevét, a kerékpár típusát és a hibát.
- Szerviz lemondása: ügyfél telefonál => pultos kihúzza a bejegyzést (ha eszébe jut).
- Új kerékpár felvétele a kínálatba: tulajdonos beszerzi a kerékpárt => lefényképezi => megkéri a külső fejlesztőt
  a weboldal frissítésére => bevezeti a táblázatba.
- Havi kimutatás: tulajdonos a füzet és a táblázat alapján kézzel összesíti a bérléseket és a bevételt.

## 6. Igényelt üzleti folyamatok modellje

Online folyamatok látogatók számára (bejelentkezés nélkül):
- Kínálat böngészése: Bérlés menü => kerékpárok listája kategória-szűréssel => kép, név, napidíj megjelenítése.
- Értékelések olvasása: Értékelések menü => átlag és csillagonkénti eloszlás => lapozható, pontszámra szűrhető lista.
- Regisztráció: Fiók létrehozása => felhasználónév, e-mail, jelszó (kétszer) megadása => véglegesítés.
- Bejelentkezés: Bejelentkezés menü => e-mail és jelszó => a szerepkörnek megfelelő menü megjelenik.

Online folyamatok bejelentkezett ügyfelek számára:
- Kerékpárbérlés: Bérlés menü => naptárban kezdő és záró nap kiválasztása => a rendszer csak az időszakban szabad
  kerékpárokat mutatja => Kosárba => Kosár menü => tételek és végösszeg ellenőrzése => Foglalás véglegesítése.
- Szervizidőpont foglalása: Szerviz menü => naptárban nap kiválasztása => szabad időpont kiválasztása =>
  kerékpártípus, kért szerviz, hibaleírás megadása => Időpont lefoglalása.
- Szervizfoglalás módosítása, lemondása: Szerviz menü => Saját foglalásaim => Módosítás / Lemondás => megerősítés.
- Értékelés írása, módosítása, törlése: Értékelések menü => csillagok és vélemény => Elküldés; a saját értékelések
  listájában módosítás vagy törlés.
- Profil megtekintése: Profil menü => felhasználónév, e-mail, szerepkör, regisztráció dátuma.

Online folyamatok adminisztrátorok számára (az ügyfélfunkciókon felül):
- Áttekintés: Admin menü => Áttekintés fül => mutatók kártyákon.
- Kerékpár felvétele: Admin menü => Biciklik fül => név, típus, napidíj, kép tallózása => Bicikli felvétele.
- Kerékpár módosítása, letiltása, törlése: Admin menü => Biciklik fül => Szerkesztés / Letiltás / Törlés => megerősítés.
- Bérlések kezelése: Admin menü => Kölcsönzések fül => szűrés kerékpárra és időszakra => végösszegek => törlés.
- Felhasználók jogosultságának kezelése: Admin menü => Felhasználók fül => Admin jog megadása / elvétele.
- Szervizfoglalások kezelése: Admin menü => Szerviz fül => időszak, státusz szűrés => Visszaigazolás / Elkészült / Lemondás.
- Értékelések moderálása: Értékelések menü => Moderálás: törlés => megerősítés.

## 7. Követelménylista

ID|Verzió|Név|Kifejtés
--|------|---|--------
K01|V1.0|Felhasználói fiókok kezelése|Regisztráció, bejelentkezés, kijelentkezés, profil megtekintése; szerepkörök (ügyfél, adminisztrátor) szerinti hozzáférés az igényelt üzleti folyamatokban leírtak szerint.
K02|V1.0|Nyilvános kerékpárkatalógus|A kerékpárok listája képpel, kategóriával és napidíjjal bejelentkezés nélkül is böngészhető, kategória és elérhetőség szerint szűrhető.
K03|V1.0|Online kerékpárbérlés|Időszak kiválasztása naptárban, csak szabad kerékpárok felkínálása, kosár, több kerékpár egy lépésben történő foglalása, ütközésmentes foglalás szerveroldali ellenőrzéssel.
K04|V1.0|Szervizidőpont-foglalás|Szabad szervizidőpontok megjelenítése munkanapokon 9–17 óra között egyórás sávokban, legfeljebb 90 napra előre; foglalás, módosítás, lemondás, egy sávra egy foglalás.
K05|V1.0|Ügyfélértékelések|1–5 csillagos értékelés legfeljebb 2000 karakteres véleménnyel; nyilvános lista és összesítés; saját értékelés módosítása, törlése; adminisztrátori moderálás.
K06|V1.0|Flotta adminisztrációja|Kerékpár felvétele képfeltöltéssel, adatainak módosítása, átmeneti letiltása, törlése (bérléssel rendelkező kerékpár nem törölhető).
K07|V1.0|Bérlések és szerviz adminisztrációja|Bérlések listázása végösszeggel, szűrés, törlés; szervizfoglalások státuszának kezelése (Függő → Visszaigazolt → Elkészült, illetve Lemondva).
K08|V1.0|Vezérlőpult, kimutatások|Flotta-, bérlés-, bevétel-, szerviz- és értékelésmutatók egy képernyőn.
K09|V1.0|Biztonság és adatvédelem|Jelszavak hash-elt tárolása, tokenalapú hitelesítés, szerepkör szerinti jogosultság minden védett végponton, feltöltött fájlok tartalmi ellenőrzése, személyes adatok védelme a GDPR szerint.
K10|V1.0|Egyszerű, reszponzív kezelőfelület|Magyar nyelvű, asztali gépen és mobiltelefonon is használható felület, elterjedt böngészőkben.
K11|V1.0|Platformfüggetlen, költséghatékony üzemeltetés|Nyílt forráskódú, platformfüggetlen technológiák (.NET, Node.js, SQLite); licencdíj nélküli működés.
K12|V1.0|Karbantarthatóság, bővíthetőség|Automatizált tesztek, verziókezelt adatbázis-migrációk, folyamatos integráció; új funkciók utólag hozzáadhatók.

## 8. Irányított és szabad szöveges riportok szövege

Az adminisztrátori vezérlőpult *Áttekintés* része a következő mutatókat jeleníti meg:

- Biciklik: összes kerékpár és ebből az elérhetők száma.
- Aktív kölcsönzés: a jelenleg futó bérlések száma, mellette a közelgő és az összes bérlés száma.
- Bevétel: az összes bérlés díjának összege (napok száma × napidíj).
- Felhasználók: regisztrált felhasználók száma, ebből az adminisztrátorok száma.
- Függő szerviz: visszaigazolásra váró szervizfoglalások száma.
- Értékelések: értékelések száma és átlaga.

A *Kölcsönzések* lista a szűrésnek megfelelő bérlések darabszámát és végösszegét is kiírja (pl. „3 db · 41 500 Ft”).

## 9. Fogalomszótár

- **Bérlés (kölcsönzés)**: egy kerékpár egy ügyfél részére egy kezdő és egy záró nap közötti időszakra. A záró nap
  a visszahozatal napja, azon a napon a kerékpár már újra foglalható.
- **Napidíj**: egy kerékpár egy napra eső bérleti díja forintban. A bérlés díja a napok száma × napidíj.
- **Kosár**: a böngészőben tárolt, még le nem foglalt bérlési tételek listája.
- **Szervizsáv**: egy 60 perces szervizidőpont munkanapokon 9 és 17 óra között, budapesti idő szerint.
- **Szervizstátusz**: *Függő* (visszaigazolásra vár), *Visszaigazolt*, *Elkészült*, *Lemondva*.
- **Adminisztrátor**: a kölcsönző munkatársa, aki a flottát, a bérléseket, a szervizt és a felhasználókat kezeli.
- **JWT (JSON Web Token)**: a bejelentkezéskor kiadott, aláírt hozzáférési token, amely 60 percig érvényes.
- **Elérhető kerékpár**: az adminisztrátor által nem letiltott kerékpár; letiltott kerékpár nem jelenik meg bérelhetőként.
