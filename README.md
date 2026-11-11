# BikeRentalApp

Kerékpárkölcsönző webalkalmazás csoportmunkában.

- **Backend:** ASP.NET Core 10 Minimal API, EF Core + SQLite, JWT hitelesítés
- **Frontend:** React 19 + Vite, React Router

## Követelmények

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 vagy újabb
- EF Core CLI (migrációkhoz): `dotnet tool install --global dotnet-ef`

## Első indítás

### 1. JWT kulcs beállítása

A kulcs **nincs a repóban**. Hozd létre a `Backend/appsettings.Development.json` fájlt (gitignore-olt):

```json
{
  "Jwt": {
    "Key": "ide-egy-legalabb-32-karakteres-veletlen-szoveg"
  },
  "Admin": {
    "Email": "admin@bikerental.local",
    "Username": "Admin",
    "Password": "valassz-egy-jelszot"
  }
}
```

Az `Admin` rész opcionális: fejlesztői indításkor ebből jön létre az admin fiók, mert regisztrációval csak `Customer` szerepkörű felhasználó készül. Ha az e-mail cím már regisztrálva van, a fiók Admin szerepkört kap, a jelszava nem változik.

Véletlen kulcs generálása PowerShellben:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### 2. Adatbázis

```bash
cd Backend
dotnet ef database update
```

Ez létrehozza a `bikerental.db` SQLite fájlt.

### 3. Backend futtatása

```bash
cd Backend
dotnet run --launch-profile http
```

API: `http://localhost:5006`, Swagger: `http://localhost:5006/swagger`

### 4. Frontend futtatása

```bash
cd Frontend
npm install
npm run dev
```

A Vite dev szerver az `/api` kéréseket a backendre (`localhost:5006`) továbbítja.

## Admin API

Minden végponthoz Admin szerepkörű JWT kell.

| Végpont | Leírás |
|---------|--------|
| `GET /api/admin/stats` | Összesítő: biciklik, felhasználók, kölcsönzések, bevétel, függő szervizek, értékelések |
| `GET /api/admin/users` | Felhasználók listája |
| `PATCH /api/admin/users/{id}/role` | Szerepkör módosítása (`{ "role": "Admin" \| "Customer" }`), a saját admin jog nem vehető el |
| `GET /api/admin/rentals?bikeId=&from=&to=` | Kölcsönzések napszámmal és végösszeggel |
| `DELETE /api/admin/rentals/{id}` | Kölcsönzés törlése |
| `POST /api/bikes`, `PUT /api/bikes/{id}`, `DELETE /api/bikes/{id}` | Bicikli felvétele, módosítása, törlése (kölcsönzéssel rendelkező bicikli nem törölhető, 409) |
| `GET /api/service-appointments/admin?from=&to=` | Szervizfoglalások |
| `PATCH /api/service-appointments/admin/{id}/status` | Szerviz státusz (`Confirmed`, `Completed`, `Cancelled`) |
| `DELETE /api/reviews/admin/{id}` | Értékelés moderálása |

A szerepkör-változás a felhasználó következő bejelentkezésekor lép életbe.

## Tesztek

```bash
dotnet test BikeRentalApp.slnx
```

Frontend ellenőrzés:

```bash
cd Frontend
npm run lint
npm run build
```

A CI (GitHub Actions) minden PR-nél lefuttatja mindkettőt.

## Projekt szerkezet

```
Backend/          ASP.NET Core API
  Auth/           JWT beállítás, token szolgáltatás
  Data/           DbContext
  Dtos/           Kérés/válasz objektumok
  Endpoints/      Minimal API végpontok
  Migrations/     EF Core migrációk
  Model/          Entitások
  Services/       Üzleti logika
Backend.Tests/    xUnit integrációs tesztek
Frontend/         React + Vite kliens
```
