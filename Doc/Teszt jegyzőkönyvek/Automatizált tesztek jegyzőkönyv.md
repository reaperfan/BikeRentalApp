# BikeRental rendszer teszt jegyzőkönyv


Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: .NET SDK 10.0.403, xUnit; Node.js 26, Vitest 5.1  
Dátum: 2026.11.11.  
Talált hibák száma: 1 (13 bukó teszt, egy közös okkal)  

## Automatizált tesztek futtatása

### Backend (xUnit)

Futtatás: `dotnet test BikeRentalApp.slnx`. Minden tesztosztály saját, ideiglenes SQLite adatbázissal futó tesztszervert
kap (`AuthApiFactory`), a feltöltött fájlok ideiglenes mappába kerülnek.

Tesztosztály|Terület|Sikeres|Sikertelen
-----------|-------|-------|----------
AuthTests|Regisztráció, bejelentkezés, token, saját adatok|9|0
BikeTests|Kerékpárlista, szűrés, adatlap|15|0
BikeAdminTests|Kerékpár felvétele, módosítása, törlése adminként|8|0
AdminTests|Admin összesítő, felhasználók, szerepkör, bérlések, kerékpár-validáció, képfeltöltés, admin seed|27|0
RentalTests|Bérlések listája, létrehozása, módosítása, törlése|8|**13**
ReviewTests|Értékelések végpontjai|9|0
ReviewSchemaTests|Értékelés tábla sémája, CHECK-feltételek|4|0
ReviewMigrationTests|Migráció korábbi sémaverziókról adatmegőrzéssel|3|0
ReviewStartupTests|Fejlesztői induláskori automatikus migráció|2|0
**Összesen**||**85**|**13**

### Frontend (Vitest)

Futtatás: `npm test` a `Frontend` mappában.

Tesztfájl|Terület|Sikeres|Sikertelen
---------|-------|-------|----------
`utils/rentalCart.test.js`|Kosár tárolása, hozzáadás, csere, törlés, ürítés|7|0
`utils/rentalDates.test.js`|Dátumformázás, napok száma, foglalt napok, szabad időszak|10|0
`components/DateRangeCalendar.test.jsx`|Naptár: múltbeli és foglalt napok tiltása, időszakválasztás, lapozás|7|0
`pages/BikeCatalog.test.jsx`|Katalógus: szűrés időszakra, kosárba tétel, hibaállapot|5|0
`pages/RentalCart.test.jsx`|Kosár: üres állapot, végösszeg, törlés, foglalás, részleges hiba|5|0
`pages/Admin.test.jsx`|Admin: átirányítás, összesítő, kerékpárfelvétel, képfeltöltés, letiltás, szerepkör, bérlések, szervizstátusz|11|0
**Összesen**||**45**|**0**

### Talált hibák

- **A 13 bukó `RentalTests` teszt** mind 401-et kap ott, ahol 201-et vagy 400-at vár. Ok: a `2e64299` commit a
  `POST /api/rentals` végpontot hitelesítéshez kötötte, a tesztek viszont token nélkül hívják. A végpont viselkedése
  helyes, a teszteket kell kiegészíteni bejelentkezéssel (az `AdminTests` és `BikeAdminTests` mintájára).
  A bukó tesztek:
  `CreateRental_Successful_IsSavedInDatabase`, `CreateRental_BikeNotFound_ReturnsBadRequest`,
  `CreateRental_InvalidDates_SameDates_ReturnsBadRequest`, `CreateRental_InvalidDates_EndBeforeStart_ReturnsBadRequest`,
  `CreateRental_PastDate_ReturnsBadRequest`, `CreateRental_DateConflict_WithExistingRental_ReturnsBadRequest`,
  `CreateRental_DateAfterExisting_SucceedsWithoutConflict`, `CreateRental_NoConflict_SucceedsWithAdjustedDates`,
  `CreateRental_MissingCustomerName_ReturnsBadRequest`, `CreateRental_InvalidEmail_ReturnsBadRequest`,
  `CreateRental_ValidData_ReturnsCreated`, `UpdateRental_Successful_ChangesAreSaved`,
  `DeleteRental_ValidId_ReturnsNoContent`. Az utóbbi kettő azért bukik, mert az előkészítő lépésként létrehozott
  bérlés is 401-et kap.

### Statikus ellenőrzések

- `npm run lint` (oxlint): hiba nincs, 5 figyelmeztetés a korábbi kódban (`AuthContext.jsx`, `BikeRental.jsx`).
- `npm run build`: sikeres, a kliens csomag kb. 330 kB (tömörítve kb. 100 kB).
- `dotnet build`: 0 hiba.
