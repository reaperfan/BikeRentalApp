using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.Dtos;

namespace Backend.Tests;

public class RentalTests : IClassFixture<AuthApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public RentalTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    // A foglaláshoz bejelentkezés kell, ezért minden teszt egy friss vevő fiók tokenjével fut
    public async Task InitializeAsync()
    {
        var email = $"rental-{Guid.NewGuid():N}@test.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto("Bérlő", email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto(email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateRental_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(4, "Anonymous", "anon@example.com", now.AddDays(60), now.AddDays(61));

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetRentals_ReturnsSeededRentals()
    {
        var res = await _client.GetAsync("/api/rentals");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var rentals = await res.Content.ReadFromJsonAsync<List<RentalDtoResponse>>();
        Assert.NotNull(rentals);
        Assert.NotEmpty(rentals);
        Assert.True(rentals.Count >= 3);  // Legalább 3 seeded
    }

    [Fact]
    public async Task GetRental_ById_ReturnsRental()
    {
        // Legalább 1 kölcsönzés van a seedben
        var listRes = await _client.GetAsync("/api/rentals");
        var rentals = await listRes.Content.ReadFromJsonAsync<List<RentalDtoResponse>>();
        var firstRentalId = rentals!.First().Id;

        var res = await _client.GetAsync($"/api/rentals/{firstRentalId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var rental = await res.Content.ReadFromJsonAsync<RentalDtoResponse>();
        Assert.NotNull(rental);
        Assert.Equal(firstRentalId, rental.Id);
        Assert.True(!string.IsNullOrEmpty(rental.CustomerName));
    }

    [Fact]
    public async Task GetRental_NotFound_Returns404()
    {
        var res = await _client.GetAsync("/api/rentals/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetRentalsByBike_ReturnsRentalsForBike()
    {
        var res = await _client.GetAsync("/api/rentals/bike/1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var rentals = await res.Content.ReadFromJsonAsync<List<RentalDtoResponse>>();
        Assert.NotNull(rentals);
        // BikeId 1-hez van kölcsönzés a seedben
        Assert.NotEmpty(rentals);
        Assert.All(rentals, r => Assert.Equal(1, r.BikeId));
    }

    [Fact]
    public async Task CreateRental_ValidData_ReturnsCreated()
    {
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(
            BikeId: 4,  // Trek Dual Sport 3 (Trekking)
            CustomerName: "New Customer",
            CustomerEmail: "newcustomer@example.com",
            StartDate: now.AddDays(20),
            EndDate: now.AddDays(22)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var created = await res.Content.ReadFromJsonAsync<RentalDtoResponse>();
        Assert.NotNull(created);
        Assert.Equal("New Customer", created.CustomerName);
        Assert.Equal("newcustomer@example.com", created.CustomerEmail);
        Assert.True(created.Id > 0);  // ID generálódott
    }

    [Fact]
    public async Task CreateRental_Successful_IsSavedInDatabase()
    {
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(
            BikeId: 9,
            CustomerName: "Save Test Customer",
            CustomerEmail: "savetest@example.com",
            StartDate: now.AddDays(35),
            EndDate: now.AddDays(37)
        );

        var createRes = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<RentalDtoResponse>();

        // Ellenőrzünk, hogy az adatbázisban tényleg ott van
        var getRes = await _client.GetAsync($"/api/rentals/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        var fetched = await getRes.Content.ReadFromJsonAsync<RentalDtoResponse>();
        Assert.NotNull(fetched);
        Assert.Equal("Save Test Customer", fetched.CustomerName);
        Assert.Equal("savetest@example.com", fetched.CustomerEmail);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task CreateRental_BikeNotFound_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(
            BikeId: 99999,
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            StartDate: now.AddDays(10),
            EndDate: now.AddDays(12)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var error = await res.Content.ReadAsStringAsync();
        Assert.Contains("bicikli", error.ToLower());
    }

    [Fact]
    public async Task CreateRental_InvalidDates_EndBeforeStart_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(
            BikeId: 6,
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            StartDate: now.AddDays(12),
            EndDate: now.AddDays(10)  // EndDate előbb van
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var error = await res.Content.ReadAsStringAsync();
        Assert.Contains("kezdési", error.ToLower());
    }

    [Fact]
    public async Task CreateRental_InvalidDates_SameDates_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var sameDate = now.AddDays(15);
        var rental = new RentalDtoCreate(
            BikeId: 6,
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            StartDate: sameDate,
            EndDate: sameDate  // Ugyanaz a dátum
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateRental_PastDate_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var rental = new RentalDtoCreate(
            BikeId: 7,
            CustomerName: "Test Customer",
            CustomerEmail: "test@example.com",
            StartDate: now.AddDays(-5),  // Múltban
            EndDate: now.AddDays(-2)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var error = await res.Content.ReadAsStringAsync();
        Assert.Contains("múlt", error.ToLower());
    }

    [Fact]
    public async Task CreateRental_MissingCustomerName_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var rentalJson = new { bikeId = 5, customerName = "", customerEmail = "test@example.com", startDate = now.AddDays(10), endDate = now.AddDays(12) };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/rentals");
        req.Content = JsonContent.Create(rentalJson);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateRental_InvalidEmail_ReturnsBadRequest()
    {
        var now = DateTime.UtcNow;
        var rentalJson = new { bikeId = 5, customerName = "Test", customerEmail = "not-an-email", startDate = now.AddDays(10).ToString("O"), endDate = now.AddDays(12).ToString("O") };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/rentals");
        req.Content = JsonContent.Create(rentalJson);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var error = await res.Content.ReadAsStringAsync();
        Assert.Contains("email", error.ToLower());
    }

    [Fact]
    public async Task CreateRental_DateConflict_WithExistingRental_ReturnsBadRequest()
    {
        // Egy biciklihez, aminek van meglévő kölcsönzése, próbálunk átfedő foglalást létrehozni
        // A seed adatok szerint: BikeId 1 foglalt 1-3 napig
        var now = DateTime.UtcNow;

        // Próbáljunk átfedő foglalást létrehozni (2-4 nap = ütközik 1-3-mal)
        var conflictRental = new RentalDtoCreate(
            BikeId: 1,  // BikeId 1-nek van kölcsönzése (1-3. nap)
            CustomerName: "Conflict Customer",
            CustomerEmail: "conflict@example.com",
            StartDate: now.AddDays(2),  // Ütközik a meglévő 1-3 napi foglalással
            EndDate: now.AddDays(4)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", conflictRental);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var error = await res.Content.ReadAsStringAsync();
        Assert.Contains("foglalt", error.ToLower());
    }

    [Fact]
    public async Task CreateRental_DateAfterExisting_SucceedsWithoutConflict()
    {
        // BikeId 1-nek van kölcsönzése: 1-3. nap (now + 1 - now + 3)
        // De ha a 4. naptól kezdünk, nincs ütközés
        var now = DateTime.UtcNow;

        var rental = new RentalDtoCreate(
            BikeId: 1,
            CustomerName: "No Conflict Customer",
            CustomerEmail: "noconflict@example.com",
            StartDate: now.AddDays(4),  // Közvetlenül a meglévő után
            EndDate: now.AddDays(6)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        // Ez sikeres kell legyen, mert nem ütközik (4-6 és 1-3 között nincs ütközés)
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task CreateRental_NoConflict_SucceedsWithAdjustedDates()
    {
        var now = DateTime.UtcNow;
        // BikeId 3-nak nincsen foglalása - biztonságosan létrehozható
        var rental = new RentalDtoCreate(
            BikeId: 3,
            CustomerName: "Safe Customer",
            CustomerEmail: "safe@example.com",
            StartDate: now.AddDays(15),
            EndDate: now.AddDays(17)
        );

        var res = await _client.PostAsJsonAsync("/api/rentals", rental);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var created = await res.Content.ReadFromJsonAsync<RentalDtoResponse>();
        Assert.NotNull(created);
        Assert.Equal(3, created.BikeId);
    }

    [Fact]
    public async Task UpdateRental_ValidData_ReturnsOk()
    {
        // Lekérdezzük az első kölcsönzést, majd frissítjük
        var listRes = await _client.GetAsync("/api/rentals");
        var rentals = await listRes.Content.ReadFromJsonAsync<List<RentalDtoResponse>>();
        var rentalId = rentals!.Last().Id;  // Az utolsót választjuk (legkevésbé valószínű konfliktus)

        var now = DateTime.UtcNow;
        var update = new RentalDtoUpdate(
            StartDate: now.AddDays(25),
            EndDate: now.AddDays(27)
        );

        var res = await _client.PutAsJsonAsync($"/api/rentals/{rentalId}", update);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var updated = await res.Content.ReadFromJsonAsync<RentalDtoResponse>();
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task UpdateRental_Successful_ChangesAreSaved()
    {
        // Létrehozunk egy új kölcsönzést, majd módosítjuk
        var now = DateTime.UtcNow;
        var newRental = new RentalDtoCreate(
            BikeId: 10,
            CustomerName: "Update Test",
            CustomerEmail: "update@example.com",
            StartDate: now.AddDays(40),
            EndDate: now.AddDays(42)
        );

        var createRes = await _client.PostAsJsonAsync("/api/rentals", newRental);
        var created = await createRes.Content.ReadFromJsonAsync<RentalDtoResponse>();
        var rentalId = created!.Id;

        // Módosítjuk a dátumokat
        var updateData = new RentalDtoUpdate(
            StartDate: now.AddDays(45),
            EndDate: now.AddDays(47)
        );

        var updateRes = await _client.PutAsJsonAsync($"/api/rentals/{rentalId}", updateData);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // Ellenőrzünk, hogy az adatbázisban tényleg módosult
        var getRes = await _client.GetAsync($"/api/rentals/{rentalId}");
        var fetched = await getRes.Content.ReadFromJsonAsync<RentalDtoResponse>();

        Assert.NotNull(fetched);
        Assert.True(fetched.StartDate > now.AddDays(44));
        Assert.True(fetched.EndDate > now.AddDays(46));
    }

    [Fact]
    public async Task UpdateRental_InvalidDates_ReturnsBadRequest()
    {
        var listRes = await _client.GetAsync("/api/rentals");
        var rentals = await listRes.Content.ReadFromJsonAsync<List<RentalDtoResponse>>();
        var rentalId = rentals!.First().Id;

        var now = DateTime.UtcNow;
        var update = new RentalDtoUpdate(
            StartDate: now.AddDays(12),
            EndDate: now.AddDays(10)  // EndDate előbb
        );

        var res = await _client.PutAsJsonAsync($"/api/rentals/{rentalId}", update);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateRental_NotFound_Returns404()
    {
        var now = DateTime.UtcNow;
        var update = new RentalDtoUpdate(
            StartDate: now.AddDays(10),
            EndDate: now.AddDays(12)
        );

        var res = await _client.PutAsJsonAsync("/api/rentals/99999", update);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task DeleteRental_ValidId_ReturnsNoContent()
    {
        // Létrehozunk egy új kölcsönzést, majd törlünk
        var now = DateTime.UtcNow;
        var newRental = new RentalDtoCreate(
            BikeId: 8,
            CustomerName: "To Delete",
            CustomerEmail: "delete@example.com",
            StartDate: now.AddDays(30),
            EndDate: now.AddDays(32)
        );

        var createRes = await _client.PostAsJsonAsync("/api/rentals", newRental);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<RentalDtoResponse>();
        var rentalIdToDelete = created!.Id;

        // Törlünk
        var deleteRes = await _client.DeleteAsync($"/api/rentals/{rentalIdToDelete}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Ellenőrzünk hogy már nem létezik
        var getAfterRes = await _client.GetAsync($"/api/rentals/{rentalIdToDelete}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterRes.StatusCode);
    }

    [Fact]
    public async Task DeleteRental_NotFound_Returns404()
    {
        var res = await _client.DeleteAsync("/api/rentals/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
