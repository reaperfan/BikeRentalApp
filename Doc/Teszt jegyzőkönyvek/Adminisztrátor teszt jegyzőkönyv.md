# BikeRental rendszer teszt jegyzőkönyv

Tesztelést végezte: Mester Ottó, Hajtun Atilla, Rácz Roland 
Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: izolált backend példány friss SQLite adatbázissal (Development mód, .NET SDK 10.0.403), HTTP kliens: Node.js 26  
Dátum: 2026.11.11.  
Talált hibák száma: 0  

## Adminisztrátori funkciók tesztelése

A fejlesztői admin fiókkal (`admin@bikerental.local`) végzett műveletek, a vevő fiókkal végzett jogosultsági ellenőrzésekkel együtt.

Ssz.|Teszteset|Elvárt eredmény|Tényleges eredmény|Megfelelt
----|---------|---------------|------------------|---------
1|Belépés a fejlesztői admin fiókkal|200, Admin szerepkör|200|igen
2|Admin összesítő vevő fiókkal|403|403|igen
3|Admin összesítő|200, darabszámok és bevétel|200 (biciklik: 15, kölcsönzések: 2, bevétel: 33000 Ft)|igen
4|Új bicikli felvétele|201|201 (azonosító: 16)|igen
5|Új bicikli negatív napidíjjal|400|400 – „A név (max. 100 karakter), a típus, a pozitív napidíj és a kép elérési útja kötelező.”|igen
6|Bicikli felvétele vevő fiókkal|403|403|igen
7|Bicikli módosítása (ár, letiltás)|200, új ár, nem elérhető|200|igen
8|Kölcsönzéssel rendelkező bicikli törlése|409, hibaüzenet|409 – „A biciklihez kölcsönzés tartozik, nem törölhető. Állítsd nem elérhetőre.”|igen
9|Kölcsönzés nélküli bicikli törlése|204|204|igen
10|Képfeltöltés (PNG)|201, /uploads/bikes/… URL|201 (/uploads/bikes/853c7caa215a402ba9a59cac9be13caf.png)|igen
11|Feltöltött kép letöltése bejelentkezés nélkül|200|200|igen
12|Képnek álcázott HTML fájl feltöltése|400|400 – „Csak JPG, PNG vagy WEBP kép tölthető fel.”|igen
13|Felhasználók listája|200, a regisztrált teszt felhasználó szerepel|200 (2 felhasználó)|igen
14|Saját admin jog elvétele|400|400 – „A saját admin jogosultságodat nem veheted el.”|igen
15|Vevő előléptetése adminná|200, Admin szerepkör|200|igen
16|Kölcsönzések listája végösszeggel|200, napok és végösszeg|200 (2 nap, 16000 Ft)|igen
17|Szervizfoglalások listája (admin)|200|200 (1 foglalás)|igen
18|Szerviz visszaigazolása|204|204|igen
19|Már visszaigazolt szerviz ismételt visszaigazolása|409|409 – „Nem engedélyezett státuszváltás.”|igen
20|Szerviz lezárása (Elkészült)|204|204|igen
21|Értékelés moderálása (törlés)|204|204|igen
22|Kölcsönzés törlése (admin)|204|204|igen

Összesen 22 teszteset, ebből 22 megfelelt, 0 eltérés.
