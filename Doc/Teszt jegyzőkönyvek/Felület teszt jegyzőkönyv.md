# BikeRental rendszer teszt jegyzőkönyv

Tesztelést végezte: Mester Ottó (Claude Code segítségével)  
Operációs rendszer: Windows 11 Pro  
Böngésző: Chromium alapú beépített böngésző (Claude asztali alkalmazás)  
Képernyőméret: kb. 750 px széles panel, valamint 375 × 812 px mobilnézet  
Dátum: 2026.11.11.  
Talált hibák száma: 4 (mind javítva, újratesztelve)  

## Adminisztrátori felület és reszponzivitás tesztelése

A teszt a fejlesztői backenden (5006-os port) és a Vite fejlesztői szerveren (5173-as port) futott, a fejlesztői
admin fiókkal bejelentkezve.

### Navigáció és jogosultság

1. Adminisztrátorként bejelentkezve a fejlécben megjelent az *Admin* menüpont, az `/admin` oldal betöltött.
2. Az *Áttekintés* fül mutatói: Biciklik 15 (15 elérhető), Aktív kölcsönzés 0, Bevétel 0 Ft, Felhasználók 5 (1 admin),
   a helyi adatbázis tartalmának megfelelően.
3. A *Biciklik* fülön mind a 15 kerékpár megjelent képpel, típussal, napidíjjal, „Elérhető” állapottal és a
   *Szerkesztés*, *Letiltás*, *Törlés* gombokkal.
4. A *Felhasználók* fülön a saját fióknál „(te)” jelölés jelent meg, és nem volt szerepkör-módosító gomb.
   A többi felhasználónál az *Admin jog megadása* gomb jelent meg.
5. A *Szerviz* fül az alapértelmezett 30 napos időszakra betöltött („Ebben az időszakban nincs szervizfoglalás.”).
6. A böngésző konzolján nem jelent meg hiba.
7. Nem admin felhasználó és látogató átirányítását az automatizált frontendtesztek ellenőrzik
   (`Admin.test.jsx`: „redirects non-admin users to the home page”, „redirects anonymous users to login”).

### Képfeltöltés

1. A *Biciklik* fülön egy meglévő JPG kerékpárképet választottam ki a *Kép tallózása…* gombbal.
2. Megjelent a „teszt-feltoltes.jpg feltöltve.” üzenet, a kép útvonala mezőbe a `/uploads/bikes/<azonosító>.jpg`
   érték került, és betöltött az előnézet (1286 px széles kép).
3. A feltöltött kép a `/uploads/...` címen 200-as válasszal elérhető volt.
4. A tesztkép a teszt után törlésre került.

### Reszponzivitás

1. **Hiba:** kb. 750 px szélességnél vízszintes görgetősáv jelent meg, mert a fejléc menüje kilógott
   (a dokumentum szélessége 862 px volt 733 px helyett). **Javítás:** 900 px alatt a fejléc és a menü több sorba
   tördel. **Újrateszt:** nincs túllógás.
2. **Hiba:** 375 px szélességen a *Biciklik* fülön 481 px-es dokumentumszélesség, mert a táblázat rejtett
   (csak képernyőolvasónak szóló) fejléc-szövege kilógott a görgethető táblázatkeretből. **Javítás:** a táblázatkeret
   pozicionált lett. **Újrateszt:** a *Biciklik*, *Felhasználók* és *Áttekintés* fülön a dokumentum szélessége 375 px.
3. **Hiba:** mobilon az admin fülek egy sorban, vízszintesen görgethetően jelentek meg, így a *Felhasználók* és
   *Szerviz* fül nem látszott. **Javítás:** 680 px alatt a fülek tördelnek. **Újrateszt:** mind az öt fül látható.
4. 375 px szélességen az összesítő kártyák egy oszlopba rendeződtek, az űrlapmezők teljes szélességűek.
5. **Hiba (kozmetikai):** a *Kép tallózása…* gomb felirata nem volt függőlegesen középen, mert az űrlap címkéinek
   oszlopos elrendezése ráhatott. **Javítás:** a gomb sorirányú elrendezést kapott. **Újrateszt:** a felirat középen van.

### Nem tesztelt területek

- Más böngészők (Firefox, Safari) és valódi mobileszközök.
- Az ügyféloldali oldalak (katalógus, kosár, szerviz, értékelések) böngészős végigkattintása; ezek viselkedését a
  HTTP-szintű jegyzőkönyvek és az automatizált frontendtesztek fedik le.
