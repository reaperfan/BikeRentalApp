# BikeRental rendszer teszt jegyzőkönyv

 
Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: izolált backend példány friss SQLite adatbázissal (Development mód, .NET SDK 10.0.403), HTTP kliens: Node.js 26  
Dátum: 2026.11.11.  
Talált hibák száma: 0  

## Értékelések tesztelése

Összesítés, beküldés, validáció, módosítás, szűrés. A moderálás az adminisztrátori jegyzőkönyvben szerepel.

Ssz.|Teszteset|Elvárt eredmény|Tényleges eredmény|Megfelelt
----|---------|---------------|------------------|---------
1|Összesítés bejelentkezés nélkül|200|200 (értékelések: 0)|igen
2|Értékelés bejelentkezés nélkül|401|401|igen
3|Értékelés 5 csillaggal, szöveggel|201, szöveg szélső szóközök nélkül|201|igen
4|Érvénytelen pontszám (6)|400|400 – „A pontszám 1 és 5 közötti egész szám, a vélemény legfeljebb 2000 karakter lehet.”|igen
5|2001 karakteres vélemény|400|400 – „A pontszám 1 és 5 közötti egész szám, a vélemény legfeljebb 2000 karakter lehet.”|igen
6|Saját értékelés módosítása|204|204|igen
7|Lista szűrése 4 csillagra|200, a módosított értékelés szerepel|200 (szerző: tesztelek)|igen

Összesen 7 teszteset, ebből 7 megfelelt, 0 eltérés.
