# Értékelési felület

Útvonal: `/ertekelesek`, elérhető a fejléc **Értékelések** menüpontjából.
A meglévő React/Vite alkalmazás és az `/api/reviews` backend használatával működik,
új frontendfüggőség nélkül.

## Használat

Bejelentkezés nélkül is olvasható a lapozott lista, pontszám szerint szűrhető,
és megtekinthető az átlag és a csillagonkénti megoszlás. Az űrlaphoz a bejelentkezési
link visszahoz az értékelések oldalára.

Bejelentkezve a saját, befejezett és még nem értékelt szervizekből lehet választani.
A csillagok rádiógombok, billentyűzettel is használhatók. Szöveges vélemény nem
kötelező, legfeljebb 2000 karakter. Az értékelés beküldése után a szerviz eltűnik a
választóból. Saját értékelés módosítható vagy megerősítés után törölhető.
Törlés után a foglalás újra értékelhető. Admin szerepkörrel a nyilvános kártyákon
külön moderációs törlés érhető el.

Mentés/törlés után a saját lista, a nyilvános lista és az összesítés frissül.
A felület kezeli a betöltést, az üres listákat, a hálózati hibák újrapróbálását,
a 409-es foglalási ütközést és a lejárt bejelentkezést. A véleményeket szövegként
jeleníti meg, HTML-t nem értelmez. A dátumok budapesti helyi idő szerint láthatók.

## Indítás

Először alkalmazd a backend `UniqueReviewPerAppointment` migrációját a kiválasztott
fejlesztői adatbázison. Az API-t a meglévő HTTP-profillal indítsd a repository gyökeréből:

```sh
dotnet ef database update --project Backend
dotnet run --project Backend --launch-profile http
```

A `Frontend` könyvtárból:

```sh
npm ci
npm run dev
```

A Vite az `/api` kéréseket a backend 5006-os HTTP-portjára továbbítja.
[Backend API-leírás](../Backend/REVIEW_API.md).

## Ellenőrzés

`npm run build` és `npm run lint`. Az új értékelési fájlok nem adnak
lintfigyelmeztetést; az AuthContext, BikeCatalog és BikeRental meglévő figyelmeztetései
nem ehhez a változtatáshoz tartoznak.

Chromiumban, működő backenddel és külön ideiglenes SQLite-adatbázissal ellenőrizve:
nyilvános üres lista, bejelentkezés utáni visszairányítás, csak saját befejezett
szervizek kiválasztása, kötelező csillagválasztás, létrehozás, módosítás, megerősített
törlés, szűrés, lapozás, adminisztrátori moderáció, ütközés utáni frissítés,
hálózati hiba utáni újrapróbálás, HTML biztonságos szöveges megjelenítése és
375 px-es mobilnézet vízszintes túlcsordulás nélkül.
