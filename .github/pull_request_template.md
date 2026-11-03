## Mit csinál ez a PR?

<!-- 1-2 mondat: mi változott és miért. -->

Closes #

## Változás típusa

- [ ] Új funkció (`feat`)
- [ ] Hibajavítás (`fix`)
- [ ] Refaktor / takarítás (`refactor` / `chore`)
- [ ] Dokumentáció (`docs`)

## Érintett rész

- [ ] Backend
- [ ] Frontend
- [ ] Adatbázis (új migráció)

## Hogyan teszteltem?

<!-- Milyen lépésekkel ellenőrizted? Képernyőkép frontend változásnál. -->

## Ellenőrzőlista

- [ ] A branch a friss `dev`-ből indult / rebase-elve van rá
- [ ] `dotnet test` zöld
- [ ] `npm run lint` és `npm run build` hibátlan a `Frontend` mappában
- [ ] Nincs commitolt titok (JWT kulcs, jelszó), `.db` fájl vagy `*.csproj.user`
- [ ] Új migrációnál a név csak ASCII betűkből áll
