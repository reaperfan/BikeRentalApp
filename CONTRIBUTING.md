# Közös munka szabályai

## Ágak

| Ág | Szerepe |
|----|---------|
| `main` | Stabil, leadható állapot. Csak `dev`-ből merge-elünk ide, beadás / mérföldkő előtt. |
| `dev` | Integrációs ág. Ide mennek a PR-ek. |
| `feature/<issue>-<rovid-nev>` | Új funkció, pl. `feature/12-bike-list` |
| `fix/<issue>-<rovid-nev>` | Hibajavítás, pl. `fix/15-login-redirect` |

`main`-re és `dev`-re **senki nem pushol közvetlenül**, minden PR-en keresztül megy.

## Munkafolyamat

1. Vegyél fel egy issue-t (vagy hozz létre egyet), és rendeld magadhoz.
2. Friss `dev`-ből indíts branchet:
   ```bash
   git checkout dev
   git pull
   git checkout -b feature/12-bike-list
   ```
3. Dolgozz, kis commitokkal (lásd lent).
4. Push, majd nyiss PR-t a `dev` felé. A leírásba írd: `Closes #12`.
5. Legalább egy csapattárs nézze át és hagyja jóvá. A CI-nak zöldnek kell lennie.
6. **Squash and merge**, utána töröld a branchet.

Ha közben a `dev` változott, húzd be a branchedre, mielőtt merge-elsz:

```bash
git fetch
git rebase origin/dev
git push --force-with-lease
```

## Commit üzenetek

[Conventional Commits](https://www.conventionalcommits.org/) formátum, angolul:

```
<típus>(<rész>): <rövid leírás>
```

- típus: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`
- rész: `backend`, `frontend`, `db`, `ci`

Példák:

```
feat(frontend): add bike list page
fix(backend): reject overlapping service appointments
chore(db): add Reviews migration
```

Egy commit = egy logikai változás. Backend és frontend változást lehetőleg külön commitba tegyél.

## Elnevezések

- Kódban (osztály, fájl, változó, route) **angol** nevek, ékezet nélkül.
- Felhasználói felületen megjelenő szöveg lehet magyar.
- EF migráció neve csak ASCII betű legyen: `dotnet ef migrations add AddReviews`.

## Amit soha ne commitolj

- JWT kulcsot vagy bármilyen jelszót. A kulcs a `Backend/appsettings.Development.json`-ba kerül, ami gitignore-olt.
- `*.db` fájlt, `bin/`, `obj/`, `.vs/`, `node_modules/`, `*.csproj.user`.
