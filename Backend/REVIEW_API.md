# Értékelések backend API

Az értékelés a meglévő `Review` modellen keresztül egy szervizfoglaláshoz tartozik.
A hozzá kapcsolódó frontend a `/ertekelesek` oldalon érhető el; használata és
ellenőrzése a [frontend leírásában](../Frontend/REVIEWS.md) található.

## Üzleti szabályok

- Csak a bejelentkezett ügyfél **saját, `Completed` állapotú** szervizfoglalása értékelhető.
- Foglalásonként legfeljebb egy értékelés. Az alkalmazás tranzakciója és az adatbázis
  egyedi indexe is megakadályozza a dupla értékelést, párhuzamos kéréseknél is.
- Pontszám: 1–5 közötti egész szám. A szöveg opcionális, legfeljebb 2000 karakter.
  A szélső szóközök eltűnnek, az üres vélemény `null` lesz.
- A szerző módosíthatja/törölheti a saját értékelését. Az admin más értékelését
  törölheti, átírni nem jogosult. Törlés után a foglalás ismét értékelhető.
- A felhasználóazonosító és létrehozási idő szerveroldali, a JWT és a rendszeróra alapján.
  Módosításkor az eredeti létrehozási dátum megmarad; a foglalás nem cserélhető ki.
- A nyilvános válasz csak az értékelés azonosítóját, a szerző felhasználónevét,
  pontszámát, véleményét és dátumát adja ki; e-mailt és foglalási adatokat nem.

## Végpontok

| Metódus | Útvonal | Jogosultság és eredmény |
| --- | --- | --- |
| GET | `/api/reviews?page=1&pageSize=10&rating=5` | Nyilvános, lapozott lista; a `rating` szűrő opcionális |
| GET | `/api/reviews/{id}` | Egy nyilvános értékelés |
| GET | `/api/reviews/summary` | Nyilvános átlag és csillagonkénti darabszám |
| GET | `/api/reviews/mine` | Bejelentkezett felhasználó saját értékelései |
| GET | `/api/reviews/reviewable-appointments` | Saját befejezett, még nem értékelt szervizek |
| POST | `/api/reviews` | Saját szerviz értékelése; 201 és saját értékelés válasza |
| PUT | `/api/reviews/{id}` | Saját értékelés cseréje; 204 |
| DELETE | `/api/reviews/{id}` | Saját értékelés törlése; 204 |
| DELETE | `/api/reviews/admin/{id}` | `Admin` szerepkörrel moderációs törlés; 204 |

A védett kérések fejléce: `Authorization: Bearer <accessToken>`.
Az alkalmazás meglévő `api()` frontend-segédfüggvénye használható majd hozzájuk.
A nyilvános lista és részletek bejelentkezés nélkül is elérhetők.

### Létrehozás

```json
{
  "serviceAppointmentId": 12,
  "rating": 5,
  "comment": "Pontos, segítőkész szerviz."
}
```

Válasz (a `Location` fejléc a nyilvános részletekre mutat):

```json
{
  "id": 7,
  "serviceAppointmentId": 12,
  "rating": 5,
  "comment": "Pontos, segítőkész szerviz.",
  "createdAt": "2026-11-09T12:00:00+00:00"
}
```

Ugyanezt a struktúrát adja a `/mine` tömbje. A módosítás törzse csak `rating`
és opcionális `comment`; PUT esetén a hiányzó megjegyzés törli a korábbi szöveget.
Az időpontok UTC-ben, explicit eltolással érkeznek.

### Naptárból/űrlapból értékelhető szervizek

A `/reviewable-appointments` válasza tömb: `id`, `appointmentDate`, `bikeType`,
`serviceType`. Az űrlap ezekből kínálhat választást. A listából az értékelés után
eltűnik a foglalás. A backend POST-nál is ellenőrzi a jogosultságot és a státuszt.

### Lista és összesítés

A lista válasza: `{ page, pageSize, totalCount, items }`.
Az `items` elemei: `{ id, authorName, rating, comment, createdAt }`.
Legújabb értékelés elöl, azonos dátumnál azonosító szerint csökkenő sorrend.
Alapértelmezett oldalméret 10, maximum 100, az oldal 1–1000000 között lehet.
A `totalCount` a szűrésnek megfelelő darabszám. A határon túli oldal üres tömböt ad.

A `/summary` válasza:

```json
{
  "totalCount": 2,
  "averageRating": 4.5,
  "oneStar": 0,
  "twoStars": 0,
  "threeStars": 0,
  "fourStars": 1,
  "fiveStars": 1
}
```

Üres adatbázisban az átlag `null`, a darabszámok 0-k. Az átlagot a backend nem
kerekíti; a frontend formázza a kívánt tizedesjegyre. Módosítás és törlés után az
összesítés a jelenlegi adatokat mutatja. A vélemény sima szöveg: a frontend
szövegként jelenítse meg, HTML-ként ne értelmezze.

## Hibák

- 400: hibás pontszám, túl hosszú szöveg, hibás listázási paraméterek.
- 401: hiányzó/érvénytelen hitelesítés a védett útvonalakon.
- 403: adminjog nélküli moderáció.
- 404: nem létező vagy más ügyfélhez tartozó módosítandó foglalás/értékelés.
- 409: nem befejezett szerviz vagy már létező értékelés.

Az üzleti hibaválasz `{ "message": "…" }`. A hibás JSON vagy nem konvertálható
query-paraméter keretrendszerszintű 400-as választ adhat. A frontend 409-nél
frissítse az értékelhető foglalásokat és a saját értékeléseket.

## Migráció és ellenőrzés

A `UniqueReviewPerAppointment` migráció a meglévő `Reviews.ServiceAppointmentId`
indexet egyedivé alakítja; táblát és értékeléseket nem töröl.

```sh
dotnet ef database update --project Backend
dotnet test BikeRentalApp.slnx
```

Ha korábban egy foglaláshoz több értékelés került az adatbázisba, az egyedi index
létrehozása meghiúsul. Ellenőrzés:

```sql
SELECT ServiceAppointmentId, COUNT(*)
FROM Reviews
GROUP BY ServiceAppointmentId
HAVING COUNT(*) > 1;
```

Ilyen esetben a megtartandó véleményről külön döntés szükséges; a migráció nem
választ önkényesen és nem töröl adatot. A tesztek izolált SQLite-adatbázisokat
használnak, beleértve az előző sémáról frissítést és az adatok megőrzését.
