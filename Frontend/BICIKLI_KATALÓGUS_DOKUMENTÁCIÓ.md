# BikeRental Frontend - Bicikli Katalógus és Foglalás Oldal

## Megvalósított Funkciók

### 1. **Bicikli Katalógus** (`/catalog`)
- **Oldal**: `Frontend/src/pages/BikeCatalog.jsx`
- **Funkció**:
  - Összes elérhető bicikli megjelenítése
  - **Szűrés típus szerint** (MTB, Trekking, City, E-Bike, Gyerek)
  - **Szűrés elérhetőség szerint** (csak az elérhető biciklyek)
  - **Kombinált szűrés** (típus + elérhetőség egyszerre)
  - Reszponzív rácsrendszer
  - Betöltési és hibakezelési állapotok

### 2. **Bicikli Kártya Komponens** (`BikeCard.jsx`)
- Bicikli egyedi megjelenítése
- Kép, név, típus, napi ár
- "Nem elérhető" címke nem elérhető bicikliknél
- Kép betöltési hiba kezelése
- "Kölcsönzés" gomb → Átnavigál a foglalási oldalra

### 3. **Foglalási Oldal** (`/rental/:bikeId`)
- **Oldal**: `Frontend/src/pages/BikeRental.jsx`
- **Funkcionalitás**:
  - Kiválasztott bicikli adatainak megjelenítése
  - Foglalási űrlap mezőkkel:
	- **Teljes név** (szükséges)
	- **Email cím** (szükséges)
	- **Kezdési dátum** (nem lehet múltbeli, minimum holnap)
	- **Befejezési dátum** (később kell legyen a kezdési dátuumnál)
  - **Automatikus kalkuláció**:
	- Kölcsönzési napok száma
	- Összesen fizetendő összeg (napi ár × napok száma)
  - **Validáció**:
	- Teljesen kitöltött mezők
	- Helyes dátum sorrend
	- Backend által ellenőrzött ütközésdetekció
  - **Hibaüzenetek** (backend validáció)
  - **Sikeres foglalás** után visszanavigál a katalógushoz

### 4. **API Service** (`Frontend/src/api/bikeService.js`)
Összes szükséges API hívás:
- `getBikes(filters)` - Biciklilista szűrési lehetőséggel
- `getBikeById(id)` - Egy bicikli részletei
- `createRental(rentalData)` - Új foglalás létrehozása
- `getRentals()` - Összes foglalás
- `getRentalById(id)` - Egy foglalás adatai
- `getRentalsByBike(bikeId)` - Bicikli foglalásai
- `updateRental(id, data)` - Foglalás módosítása
- `deleteRental(id)` - Foglalás törlése

### 5. **Navigáció Frissítés**
- **AppHeader.jsx** kiegészítve a **"Bicikliek"** linkkel
- Bejelentkezett felhasználók számára elérhető
- Route: `/catalog`

## Fájlstruktúra

```
Frontend/
├── src/
│   ├── pages/
│   │   ├── BikeCatalog.jsx          (Új - katalógus oldal)
│   │   ├── BikeRental.jsx            (Új - foglalási oldal)
│   │   └── ...meglévő oldalak
│   ├── components/
│   │   ├── BikeCard.jsx              (Új - bicikli kártya)
│   │   └── AppHeader.jsx             (Frissítve - új link)
│   ├── api/
│   │   ├── bikeService.js            (Új - API service)
│   │   └── client.js                 (Meglévő - fetch wrapper)
│   ├── styles/
│   │   ├── BikeCatalog.css           (Új - katalógus stílusok)
│   │   └── BikeRental.css            (Új - foglalási stílusok)
│   ├── App.jsx                       (Frissítve - új route-ok)
│   └── ...többi fájl
└── public/
	└── images/bikes/                 (Meglévő - 15 bicikli kép)
```

## Backend Integráció

### Használt API Endpoint-ok

1. **GET /api/bikes** (nyilvános)
   - Query paraméterek: `type`, `available`
   - Válasz: Biciklilista

2. **GET /api/bikes/{id}** (nyilvános)
   - Egy bicikli adatai

3. **POST /api/rentals** (nyilvános)
   - Body:
	 ```json
	 {
	   "bikeId": 1,
	   "customerName": "Nagy János",
	   "customerEmail": "nagy@example.com",
	   "startDate": "2025-01-15",
	   "endDate": "2025-01-17"
	 }
	 ```
   - Validációk a Backend által:
	 - Bicikli létezésének ellenőrzése
	 - Dátum sorrendje (StartDate < EndDate)
	 - Múltbeli kezdési dátum elutasítása
	 - Ütközés detekció (átfedő foglalások)

## Felhasználói Élmény

### Katalógus Folyamat
1. Bejelentkezett felhasználó → Kattint a **"Bicikliek"** menüre
2. Megtekinti az összes biciklit rácsos elrendezésben
3. **Szűrhet** típus és elérhetőség szerint
4. Rákattint egy biciklire → **Foglalási oldal**

### Foglalási Folyamat
1. Kitölti a foglalási formát
   - Név, email, dátumok
   - Automatikus összeget kalkuláció
2. Ellenőrzés:
   - Frontend: Mezőkitöltés, dátum sorrend
   - Backend: Bicikli létezése, ütközés, dátumok
3. Sikeres foglalás után → Vissza a katalógushoz
4. Hibaüzenet: Részletes feedback

## Stílusozás & Design

- **BikeCatalog.css**: Grid layout, filterek, kártya hoverkálhatósága
- **BikeRental.css**: Kétoszlopos layout (mobil-reszponzív), formástílusok
- Egységes szín- és tipográfiaséma (meglévő design-nal összhangban)
- **Mobiloptimalizálás**: @media breakpoints 768px és 480px

## Tesztelési Javaslatok

1. **Frontend dev szerver**: `npm run build && npm run dev` @ `http://localhost:5173`
2. **Backend szerver**: Visual Studio-ból vagy `dotnet run` @ `http://localhost:5000`
3. **Teszt lépések**:
   - Bejelentkezés
   - Katalógus megtekintése
   - Szűrés tesztelése
   - Foglalás form tesztelése
   - Sikeres és hibás foglalások

## Következő Lehetséges Fejlesztések

- Foglalási előzmények megjelenítése a Profil oldalon
- Foglalás törlése / módosítása
- Bicikli értékelések és megjegyzések
- Foglalás visszaigazolás email-ben
- Adminisztrációs bicikli kezelés felület
