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

A `serviceAppointmentId` nem része az API-nak. Az értékelhető foglalások korábbi
`/reviewable-appointments` végpontja megszűnt.

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

Az `OptionalReviewAppointment` migráció opcionálissá teszi a korábbi
szervizkapcsolatot. A régi vélemények és kapcsolataik megmaradnak;
az új értékelések `ServiceAppointmentId` mezője `null`. A korábbi egyedi index
csak a régi, nem null foglalási hivatkozásokat védi, szerviz nélküli értékelésből
több is tárolható. Az alkalmazás már nem használja ezt a kapcsolatot.

```sh
dotnet ef database update --project Backend
dotnet test BikeRentalApp.slnx
```

A korábbi kötelező kapcsolatot visszaállító downgrade előtt a szerviz nélküli
véleményeket külön kezelni kell; hozzájuk a rendszer nem talál ki foglalást.
A tesztek a szerviz nélküli beküldést és a régi adatok megőrzését is ellenőrzik.
A frontend a `/ertekelesek` oldalon található.
