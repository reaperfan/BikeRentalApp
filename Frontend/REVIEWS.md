# Egyszerű értékelési felület

Útvonal: `/ertekelesek`, a fejléc **Értékelések** menüpontja.
Bejelentkezés után csillagot választasz, opcionálisan leírod a véleményed,
és elküldöd. Nincs szervizválasztás, foglalás vagy befejezett szerviz ellenőrzése.
A saját vélemények módosíthatók, megerősítés után törölhetők.

A lista és az átlag bejelentkezés nélkül is látható. A lista pontszám szerint
szűrhető és lapozható. Adminisztrátor a nyilvános kártyákról is törölhet.
A véleményeket szövegként jelenítjük meg, HTML-t nem értelmezünk.

Fejlesztői indításkor a backend automatikusan frissíti az adatbázist az
`IndependentReviews` sémára:

```sh
dotnet run --project Backend --launch-profile http
```

A `Frontend` könyvtárból `npm ci`, majd `npm run dev`. A meglévő Vite-proxy
az 5006-os HTTP-porton futó backendhez továbbítja az `/api` kéréseket.
Új frontendfüggőség nem szükséges. Az űrlap kizárólag `rating` és `comment`
mezőket küld; a saját értékelésekhez is csak az értékelés azonosítóját használja.

Ellenőrzés: `npm run build`, `npm run lint` és böngészős foglalás nélküli
beküldés, módosítás, törlés, valamint mobilnézet. A meglévő AuthContext,
BikeCatalog és BikeRental lintfigyelmeztetései nem ehhez a változtatáshoz tartoznak.

[API-leírás](../Backend/REVIEW_API.md).
