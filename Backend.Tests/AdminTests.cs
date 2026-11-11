using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class AdminTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public AdminTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    private async Task<string> Login(string email, string password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto(email, password));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken;
    }

    private Task<string> AdminToken() => Login("admin@test.com", "AdminPassword123!");

    private async Task<(string Token, int Id)> CustomerToken()
    {
        var email = $"customer-{Guid.NewGuid():N}@test.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto("Vevő", email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var token = await Login(email, "Password123!");
        var users = await Send<List<AdminUserResponse>>(HttpMethod.Get, "/api/admin/users", await AdminToken());
        return (token, users.Single(u => u.Email == email).Id);
    }

    private async Task<HttpResponseMessage> Request(HttpMethod method, string url, string? token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) req.Content = JsonContent.Create(body);
        return await _client.SendAsync(req);
    }

    private async Task<T> Send<T>(HttpMethod method, string url, string token)
    {
        var res = await Request(method, url, token);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<int> AddRental(int bikeId, int startInDays, int days)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BikeDbContext>();
        var start = DateTime.UtcNow.Date.AddDays(startInDays);
        var rental = new Rental { BikeId = bikeId, CustomerName = "Teszt Elek", CustomerEmail = "elek@test.com",
            StartDate = start, EndDate = start.AddDays(days) };
        db.Rentals.Add(rental);
        await db.SaveChangesAsync();
        return rental.Id;
    }

    [Theory]
    [InlineData("/api/admin/stats")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/rentals")]
    public async Task AdminEndpoints_WithoutToken_Return401(string url)
    {
        var res = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/stats")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/rentals")]
    public async Task AdminEndpoints_AsCustomer_Return403(string url)
    {
        var (token, _) = await CustomerToken();
        var res = await Request(HttpMethod.Get, url, token);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Stats_AsAdmin_ReturnsCounts()
    {
        var stats = await Send<AdminStatsResponse>(HttpMethod.Get, "/api/admin/stats", await AdminToken());

        Assert.True(stats.BikeCount >= 15);
        Assert.True(stats.AdminCount >= 1);
        Assert.True(stats.RentalCount >= 3);
        Assert.True(stats.UpcomingRentalCount >= 3);
        Assert.True(stats.TotalRevenue > 0);
    }

    [Fact]
    public async Task Users_AsAdmin_ListsAdmin()
    {
        var users = await Send<List<AdminUserResponse>>(HttpMethod.Get, "/api/admin/users", await AdminToken());
        Assert.Contains(users, u => u.Email == "admin@test.com" && u.Role == "Admin");
    }

    [Fact]
    public async Task UpdateRole_PromotesCustomer_AndNewTokenHasAdminAccess()
    {
        var (_, id) = await CustomerToken();
        var res = await Request(HttpMethod.Patch, $"/api/admin/users/{id}/role", await AdminToken(), new UserRoleRequest("Admin"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var user = await res.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.Equal("Admin", user!.Role);

        var token = await Login(user.Email, "Password123!");
        Assert.Equal(HttpStatusCode.OK, (await Request(HttpMethod.Get, "/api/admin/stats", token)).StatusCode);
    }

    [Fact]
    public async Task UpdateRole_InvalidRole_Returns400()
    {
        var (_, id) = await CustomerToken();
        var res = await Request(HttpMethod.Patch, $"/api/admin/users/{id}/role", await AdminToken(), new UserRoleRequest("SuperUser"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_DemoteSelf_Returns400()
    {
        var token = await AdminToken();
        var users = await Send<List<AdminUserResponse>>(HttpMethod.Get, "/api/admin/users", token);
        var self = users.Single(u => u.Email == "admin@test.com");

        var res = await Request(HttpMethod.Patch, $"/api/admin/users/{self.Id}/role", token, new UserRoleRequest("Customer"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_NonExistent_Returns404()
    {
        var res = await Request(HttpMethod.Patch, "/api/admin/users/99999/role", await AdminToken(), new UserRoleRequest("Admin"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Rentals_AsAdmin_IncludeTotalPrice()
    {
        // Bike 7 = Csepel Royal, 4500 Ft/nap
        var id = await AddRental(7, 40, 3);
        var rentals = await Send<List<AdminRentalResponse>>(HttpMethod.Get, "/api/admin/rentals?bikeId=7", await AdminToken());

        var rental = rentals.Single(r => r.Id == id);
        Assert.Equal(3, rental.Days);
        Assert.Equal(13500m, rental.TotalPrice);
        Assert.All(rentals, r => Assert.Equal(7, r.BikeId));
    }

    [Fact]
    public async Task Rentals_FilterByPeriod_ExcludesOutsideRentals()
    {
        var inside = await AddRental(8, 100, 2);
        var outside = await AddRental(8, 200, 2);
        var from = DateTime.UtcNow.Date.AddDays(99).ToString("O");
        var to = DateTime.UtcNow.Date.AddDays(110).ToString("O");

        var rentals = await Send<List<AdminRentalResponse>>(HttpMethod.Get,
            $"/api/admin/rentals?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}", await AdminToken());

        Assert.Contains(rentals, r => r.Id == inside);
        Assert.DoesNotContain(rentals, r => r.Id == outside);
    }

    [Fact]
    public async Task DeleteRental_AsAdmin_RemovesIt()
    {
        var id = await AddRental(9, 50, 1);
        var token = await AdminToken();

        Assert.Equal(HttpStatusCode.NoContent, (await Request(HttpMethod.Delete, $"/api/admin/rentals/{id}", token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Request(HttpMethod.Delete, $"/api/admin/rentals/{id}", token)).StatusCode);
    }

    [Fact]
    public async Task DeleteBike_WithRentals_Returns409()
    {
        // Bike 1-hez van kölcsönzés a seedben
        var res = await Request(HttpMethod.Delete, "/api/bikes/1", await AdminToken());
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/bikes/1")).StatusCode);
    }

    [Theory]
    [InlineData("", 5000)]
    [InlineData("Teszt bringa", 0)]
    [InlineData("Teszt bringa", -100)]
    public async Task CreateBike_InvalidData_Returns400(string name, decimal price)
    {
        var res = await Request(HttpMethod.Post, "/api/bikes", await AdminToken(),
            new BikeDtoCreateUpdate(name, BikeType.City, price, "/images/bikes/test.jpg"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateBike_InvalidPrice_Returns400()
    {
        var res = await Request(HttpMethod.Put, "/api/bikes/2", await AdminToken(),
            new BikeDtoUpdateFull("Cube Aim Pro", BikeType.MTB, -1m, "/images/bikes/BikeRental_Cube_Aim_Pro.jpg", true));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AdminSeeder_CreatesAdminFromConfig_AndPromotesExistingUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BikeDbContext>();
        var email = $"seed-{Guid.NewGuid():N}@test.com";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Email"] = email, ["Admin:Password"] = "SeedPassword123!", ["Admin:Username"] = "SeedAdmin"
        }).Build();

        await AdminSeeder.SeedAsync(db, config);
        Assert.Equal("Admin", db.Users.Single(u => u.Email == email).Role);
        await Login(email, "SeedPassword123!");

        var (_, customerId) = await CustomerToken();
        var customer = db.Users.Single(u => u.Id == customerId);
        config["Admin:Email"] = customer.Email;
        await AdminSeeder.SeedAsync(db, config);
        await db.Entry(customer).ReloadAsync();
        Assert.Equal("Admin", customer.Role);
    }

    [Fact]
    public async Task AdminSeeder_WithoutConfig_DoesNothing()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BikeDbContext>();
        var before = db.Users.Count();

        await AdminSeeder.SeedAsync(db, new ConfigurationBuilder().Build());
        Assert.Equal(before, db.Users.Count());
    }
}
