# BikeRental rendszer teszt jegyzőkönyv

Tesztelést végezte: Mester Ottó (Claude Code segítségével)  
Operációs rendszer: Windows 11 Pro  
Tesztkörnyezet: izolált backend példány friss SQLite adatbázissal (Development mód, .NET SDK 10.0.403), HTTP kliens: Node.js 26  
Dátum: 2026.11.11.  
Talált hibák száma: 0  

## Szervizidőpont-foglalás tesztelése

Szabad sávok, foglalás, ütközés, nyitvatartás, lemondás.

Ssz.|Teszteset|Elvárt eredmény|Tényleges eredmény|Megfelelt
----|---------|---------------|------------------|---------
1|Szabad időpontok lekérése (14 nap)|200, munkanapokon 9–17 óra, 60 perces sávok|200 (80 sáv, időzóna: Europe/Budapest)|igen
2|Szabad időpontok bejelentkezés nélkül|401|401|igen
3|Időpontfoglalás szabad sávra|201, Pending státusz|201 (státusz: Pending)|igen
4|Ugyanarra a sávra második foglalás|409, foglalt üzenet|409 – „Ez az időpont már foglalt. Válassz másikat.”|igen
5|Nyitvatartáson kívüli időpont|400|400 – „Válassz egy jövőbeli, nyitvatartáson belüli szabad időpontot.”|igen
6|Hiányzó kerékpártípus|400|400 – „A kerékpár és a szerviz típusa kötelező (legfeljebb 100 karakter), a leírás legfeljebb 1000 karakter.”|igen
7|Saját foglalások listája|200, a foglalás szerepel|200|igen
8|Saját foglalás lemondása|204|204|igen
9|Már lemondott foglalás ismételt lemondása|409|409 – „Ez a foglalás már nem mondható le.”|igen

Összesen 9 teszteset, ebből 9 megfelelt, 0 eltérés.
