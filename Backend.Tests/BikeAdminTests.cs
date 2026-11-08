using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Tests;

public class BikeAdminTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public BikeAdminTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    private async Task<string> GetAdminToken()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", 
            new UserLoginDto("admin@test.com", "AdminPassword123!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDto>();
        return loginData!.AccessToken;
    }

    private async Task<HttpResponseMessage> AdminRequest(HttpMethod method, string url, object? body = null)
    {
        var token = await GetAdminToken();
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body != null)
            req.Content = JsonContent.Create(body);

        return await _client.SendAsync(req);
    }

    [Fact]
    public async Task CreateBike_AsAdmin_ReturnsCreated()
    {
        var newBike = new BikeDtoCreateUpdate(
            "Test Trek XYZ",
            BikeType.MTB,
            9999m,
            "/images/bikes/test.jpg"
        );

        var res = await AdminRequest(HttpMethod.Post, "/api/bikes", newBike);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<BikeDtoResponse>();
        Assert.NotNull(created);
        Assert.Equal("Test Trek XYZ", created.Name);
        Assert.Equal(BikeType.MTB, created.Type);
        Assert.Equal(9999m, created.DailyPrice);
        Assert.True(created.IsAvailable);
    }

    [Fact]
    public async Task CreateBike_WithoutAdmin_Returns403()
    {
        var newBike = new BikeDtoCreateUpdate(
            "Test Trek XYZ",
            BikeType.MTB,
            9999m,
            "/images/bikes/test.jpg"
        );

        var res = await _client.PostAsJsonAsync("/api/bikes", newBike);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UpdateBike_AsAdmin_ReturnsOk()
    {
        // ID 1 = Trek Marlin 7
        var updated = new BikeDtoUpdateFull(
            "Trek Marlin 7 Updated",
            BikeType.MTB,
            9500m,
            "/images/bikes/updated.jpg",
            false
        );

        var res = await AdminRequest(HttpMethod.Put, "/api/bikes/1", updated);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var result = await res.Content.ReadFromJsonAsync<BikeDtoResponse>();
        Assert.NotNull(result);
        Assert.Equal("Trek Marlin 7 Updated", result.Name);
        Assert.Equal(9500m, result.DailyPrice);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task UpdateBike_NonExistent_Returns404()
    {
        var updated = new BikeDtoUpdateFull(
            "Test",
            BikeType.MTB,
            5000m,
            "/images/bikes/test.jpg",
            true
        );

        var res = await AdminRequest(HttpMethod.Put, "/api/bikes/999", updated);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task DeleteBike_AsAdmin_ReturnsNoContent()
    {
        // Ellenőrzünk hogy létezik
        var getRes = await _client.GetAsync("/api/bikes/1");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // Törlünk
        var deleteRes = await AdminRequest(HttpMethod.Delete, "/api/bikes/1");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Ellenőrzünk hogy már nem létezik
        var getAfterRes = await _client.GetAsync("/api/bikes/1");
        Assert.Equal(HttpStatusCode.NotFound, getAfterRes.StatusCode);
    }

    [Fact]
    public async Task DeleteBike_NonExistent_Returns404()
    {
        var res = await AdminRequest(HttpMethod.Delete, "/api/bikes/999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreateBike_MissingRequiredField_Returns400()
    {
        var token = await GetAdminToken();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/bikes");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Direkt JSON-t küldünk hibás adatokkal (nincs Name)
        var invalidJson = new { Type = "MTB", DailyPrice = 5000, ImageUrl = "/test.jpg" };
        req.Content = JsonContent.Create(invalidJson);

        var res = await _client.SendAsync(req);
        // Model binding error miatt 400
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetBikes_StillWorksAfterUpdate()
    {
        // Listázunk
        var listRes = await _client.GetAsync("/api/bikes");
        var bikes = await listRes.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        var initialCount = bikes!.Count;

        // Létrehozunk egy újat
        var newBike = new BikeDtoCreateUpdate("New Test Bike", BikeType.City, 5500m, "/images/bikes/test.jpg");
        var createRes = await AdminRequest(HttpMethod.Post, "/api/bikes", newBike);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        // Újra listázunk
        var listRes2 = await _client.GetAsync("/api/bikes");
        var bikes2 = await listRes2.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();

        Assert.Equal(initialCount + 1, bikes2!.Count);
    }
}
