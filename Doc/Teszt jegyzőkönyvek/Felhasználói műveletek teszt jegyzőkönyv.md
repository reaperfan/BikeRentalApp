# BikeRental rendszer teszt jegyzőkönyv

Tesztelést végezte: Mester Ottó (Claude Code segítségével)  
Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: izolált backend példány friss SQLite adatbázissal (Development mód, .NET SDK 10.0.403), HTTP kliens: Node.js 26  
Dátum: 2026.11.11.  
Talált hibák száma: 0  

## Felhasználói műveletek tesztelése

Regisztráció, bejelentkezés és a saját adatok lekérése (Profil oldal adatforrása).

Ssz.|Teszteset|Elvárt eredmény|Tényleges eredmény|Megfelelt
----|---------|---------------|------------------|---------
1|Regisztráció helyes adatokkal|200, sikeres üzenet|200 – „Sikeres regisztráció!”|igen
2|Regisztráció már létező e-mail címmel|400, hibaüzenet|400 – „Ez az email cím már regisztrálva van.”|igen
3|Belépés hibás jelszóval|401, hibaüzenet|401 – „Hibás email vagy jelszó.”|igen
4|Belépés nem regisztrált e-mail címmel|401, hibaüzenet|401 – „Hibás email vagy jelszó.”|igen
5|Belépés helyes adatokkal|200, token, Customer szerepkör|200 (szerepkör: Customer)|igen
6|Saját adatok lekérése (Profil)|200, felhasználónév és e-mail|200 (felhasználónév: tesztelek, szerepkör: Customer)|igen
7|Saját adatok lekérése token nélkül|401|401|igen
8|Saját adatok lekérése módosított tokennel|401|401|igen

Összesen 8 teszteset, ebből 8 megfelelt, 0 eltérés.
