# Egyszerű értékelési API

Bejelentkezés után bárki írhat 1–5 csillagos értékelést és opcionális szöveges
véleményt. Nem kell hozzá szervizfoglalás vagy befejezett szerviz.
Egy felhasználó több véleményt is írhat. A szöveg legfeljebb 2000 karakter;
a szélső szóközök eltűnnek, az üres szöveg `null` lesz.

| Metódus | Útvonal | Működés |
| --- | --- | --- |
| GET | `/api/reviews?page=1&pageSize=10&rating=5` | Nyilvános, lapozott lista; a pontszámszűrő opcionális |
| GET | `/api/reviews/{id}` | Egy nyilvános értékelés |
| GET | `/api/reviews/summary` | Nyilvános átlag és csillagonkénti darabszám |
| GET | `/api/reviews/mine` | Saját értékelések, bejelentkezve |
| POST | `/api/reviews` | Értékelés beküldése, bejelentkezve; 201 |
| PUT | `/api/reviews/{id}` | Saját értékelés módosítása; 204 |
| DELETE | `/api/reviews/{id}` | Saját értékelés törlése; 204 |
| DELETE | `/api/reviews/admin/{id}` | Adminisztrátori törlés; 204 |

POST és PUT törzse:

```json
{"rating": 5, "comment": "Kedves, segítőkész csapat!"}
```

A saját értékelés válasza `{ id, rating, comment, createdAt }`; a nyilvános
válasz ehhez az `authorName` felhasználónevet adja. A nyilvános listában nincs
felhasználóazonosító, e-mail vagy foglalási adat. Az idők UTC-ben érkeznek.
A saját értékelés létrehozási dátuma módosításkor megmarad.

Lista: `{ page, pageSize, totalCount, items }`, legújabb elöl, azonos dátumnál
azonosító szerint csökkenő sorrendben. Alapoldalméret 10, maximum 100;
az oldalszám 1–1000000 között lehet. Az üres oldal üres tömböt ad.
Összesítés: `{ totalCount, averageRating, oneStar, twoStars, threeStars,
fourStars, fiveStars }`; vélemények nélkül az átlag `null`, a darabszámok 0-k.

A védett műveletekhez a meglévő login JWT-je kell:
`Authorization: Bearer <accessToken>`. Más véleményét átírni nem lehet;
az admin is csak törölheti a más által írt véleményt.
Hibák: 400 érvénytelen adat, 401 hiányzó/lejárt login, 403 hiányzó adminjog,
404 hiányzó vagy más ügyfélhez tartozó módosítandó értékelés.
Az üzleti hibák `{ "message": "…" }` alakúak.

## Adatbázis és ellenőrzés

A `Reviews` tábla végső szerkezete:

| Mező | Szerep |
| --- | --- |
| `Id` | Automatikus elsődleges kulcs |
| `UserId` | Kötelező szerző, idegen kulcs a `Users` táblára |
| `Rating` | Kötelező 1–5 pontszám |
| `Comment` | Opcionális, legfeljebb 2000 karakteres szöveg |
| `CreatedAt` | A létrehozás UTC-időpontja |

Az `IndependentReviews` migráció eltávolítja a régi szervizoszlopot, idegen kulcsát
és indexét. A szerző, pontszám, szöveg, dátum és az értékelések azonosítója megmarad.
A `Users`, `ServiceAppointments`, `Bikes` és `Rentals` táblák adatai változatlanok.
Szerviz törlése nincs hatással az értékelésekre. Felhasználó törlése csak az ő
értékeléseit törli, a többi szerző véleményét nem.

Adatbázisszintű CHECK-feltétel is védi az 1–5 pontszámot és a szöveg maximális
hosszát. A szerző szerinti kereséshez a `UserId` index megmarad.
A korábbi migrációfájlok történeti sémaleírások: az adatbázis frissítéséhez
szükségesek, a futó alkalmazás nem használja a régi kapcsolatot.

Fejlesztői (`Development`) módban a backend induláskor automatikusan alkalmazza
az adatbázis-migrációkat. Így egy korábbi, még kötelező szervizkapcsolatot használó
adatbázis sem okoz 500-as hibát értékeléskor. A két eredeti, láthatatlan
szóelválasztót tartalmazó migrációazonosítót is felismeri; táblákat nem hoz létre
újra a névváltozás miatt. A frissítés hibájánál az alkalmazás indulása megáll,
a hibát a backend naplója mutatja.

```sh
dotnet run --project Backend --launch-profile http
dotnet test BikeRentalApp.slnx
```

Éles környezetben a migráció továbbra is külön telepítési lépés:
`dotnet ef database update --project Backend`.

A tesztek friss adatbázison és három korábbi sémaverzióról frissítve is
ellenőrzik a táblát, az adatmegőrzést, a módosítást és törlést. A downgrade az
eltávolított kapcsolat eredeti értékeit nem tudja visszaállítani; a korábbi
rendszerre visszatéréshez az átállás előtti adatbázismentés szükséges.
A frontend a `/ertekelesek` oldalon található.
