# BikeRental rendszer teszt jegyzőkönyv

Tesztelést végezte: Mester Ottó (Claude Code segítségével)  
Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: izolált backend példány friss SQLite adatbázissal (Development mód, .NET SDK 10.0.403), HTTP kliens: Node.js 26  
Dátum: 2026.11.11.  
Talált hibák száma: 3  

## Kerékpárkatalógus és bérlés tesztelése

A kerékpárlista, a szűrők és a bérlés szerveroldali ellenőrzései. Az időpontok a teszt napjához képest relatívak (pl. +10..+12 nap).

Ssz.|Teszteset|Elvárt eredmény|Tényleges eredmény|Megfelelt
----|---------|---------------|------------------|---------
1|Biciklik listázása bejelentkezés nélkül|200, 15 seed bicikli|200 (15 db)|igen
2|Szűrés típusra (E-Bike)|200, csak E-Bike|200 (3 db)|igen
3|Szűrés elérhetőségre|200, csak elérhető|200 (15 db)|igen
4|Bicikli adatlap|200, név, típus, napidíj|200 (Trek Marlin 7, 8000 Ft/nap)|igen
5|Nem létező bicikli|404, hibaüzenet|404 – „Bicikli nem található.”|igen
6|Foglalás bejelentkezés nélkül|401|401|igen
7|Foglalás helyes adatokkal (bike 1, +10..+12 nap)|201, foglalás létrejön|201 (azonosító: 1)|igen
8|Átfedő foglalás ugyanarra a biciklire|400, foglalt üzenet|400 – „Az adott bicikli már foglalt ebben az időszakban.”|igen
9|Közvetlenül csatlakozó foglalás (+12..+14)|201|201|igen
10|Azonos kezdő és záró dátum|400|400 – „A kezdési dátum előbb kell legyen, mint a befejezési dátum.”|igen
11|Záró dátum a kezdő előtt|400|400 – „A kezdési dátum előbb kell legyen, mint a befejezési dátum.”|igen
12|Múltbeli kezdő dátum|400|400 – „A kezdési dátum nem lehet a múltban.”|igen
13|Nem létező bicikli foglalása|400|400 – „A megadott bicikli nem létezik.”|igen
14|Érvénytelen e-mail cím a foglalásban|400|201 (a backend nem ellenőrzi az e-mail formátumát)|**nem**
15|Foglalások listája bejelentkezés nélkül|csak a foglalt időszakok, személyes adat nélkül|200 (a válasz tartalmazza az ügyfelek nevét és e-mail címét)|**nem**
16|Foglalás törlése bejelentkezés nélkül|401|204 (bárki törölheti a foglalást)|**nem**

Összesen 16 teszteset, ebből 13 megfelelt, 3 eltérés.

### Talált hibák

- **Érvénytelen e-mail cím a foglalásban**: elvárt 400, tényleges 201 (a backend nem ellenőrzi az e-mail formátumát). Javaslat: A `RentalDtoCreate` `[EmailAddress]` attribútumát a Minimal API nem ellenőrzi; a végpontban explicit ellenőrzés kell.
- **Foglalások listája bejelentkezés nélkül**: elvárt csak a foglalt időszakok, személyes adat nélkül, tényleges 200 (a válasz tartalmazza az ügyfelek nevét és e-mail címét). Javaslat: A katalógusnak csak a foglalt időszakok kellenek; külön, név és e-mail nélküli végpont kell, a teljes lista adminisztrátori legyen.
- **Foglalás törlése bejelentkezés nélkül**: elvárt 401, tényleges 204 (bárki törölheti a foglalást). Javaslat: A `PUT` és `DELETE /api/rentals/{id}` `AllowAnonymous`; adminisztrátori jogosultsághoz kell kötni (az admin felület már a `/api/admin/rentals` végpontot használja).

### Kódátnézés során talált további hiba

- A `POST /api/rentals` nem ellenőrzi a kerékpár `IsAvailable` mezőjét, így egy adminisztrátor által letiltott
  kerékpár közvetlen API-hívással lefoglalható. A felület a letiltott kerékpárnál nem engedi a kosárba tételt,
  ezért a hiba csak közvetlen API-hívással jön elő. A hibát tesztfuttatással nem ellenőriztük.

A kosár működését (több tétel, végösszeg, tétel törlése, részleges hiba kezelése) és a naptár foglalt napjainak
kezelését az *Automatizált tesztek jegyzőkönyv* frontendtesztjei fedik le.
