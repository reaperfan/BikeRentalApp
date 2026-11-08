using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Tests;

public class BikeTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiFactory _factory;

    public BikeTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [Fact]
    public async Task GetBikes_ReturnsAllSeededBikes()
    {
        var res = await _client.GetAsync("/api/bikes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(15, bikes.Count);

        // Ellenőrzünk néhány konkrét biciklit
        var trek = bikes.FirstOrDefault(b => b.Name == "Trek Marlin 7");
        Assert.NotNull(trek);
        Assert.Equal(BikeType.MTB, trek.Type);
        Assert.Equal(8000m, trek.DailyPrice);

        var cube = bikes.FirstOrDefault(b => b.Name == "Cube Kathmandu Hybrid");
        Assert.NotNull(cube);
        Assert.Equal(BikeType.EBike, cube.Type);
        Assert.Equal(14000m, cube.DailyPrice);
    }

    [Fact]
    public async Task GetBike_ReturnsBikeById()
    {
        // ID 1 = Trek Marlin 7
        var res = await _client.GetAsync("/api/bikes/1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bike = await res.Content.ReadFromJsonAsync<BikeDtoResponse>();
        Assert.NotNull(bike);
        Assert.Equal(1, bike.Id);
        Assert.Equal("Trek Marlin 7", bike.Name);
        Assert.Equal(BikeType.MTB, bike.Type);
        Assert.Equal(8000m, bike.DailyPrice);
        Assert.True(bike.IsAvailable);
    }

    [Fact]
    public async Task BikeEndpoints_AllowAnonymous_NoTokenRequired()
    {
        var res = await _client.GetAsync("/api/bikes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetBikes_ContainsAllBikeTypes()
    {
        var res = await _client.GetAsync("/api/bikes");
        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();

        Assert.Contains(bikes, b => b.Type == BikeType.MTB);
        Assert.Contains(bikes, b => b.Type == BikeType.Trekking);
        Assert.Contains(bikes, b => b.Type == BikeType.City);
        Assert.Contains(bikes, b => b.Type == BikeType.EBike);
        Assert.Contains(bikes, b => b.Type == BikeType.Kid);
    }

    [Fact]
    public async Task GetBike_ReturnsNotFound_WhenBikeDoesNotExist()
    {
        var res = await _client.GetAsync("/api/bikes/999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetBikes_FilterByType_MTB_ReturnsMTBBikesOnly()
    {
        var res = await _client.GetAsync("/api/bikes?type=0");  // 0 = MTB
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.NotEmpty(bikes);
        Assert.All(bikes, b => Assert.Equal(BikeType.MTB, b.Type));
        Assert.Equal(3, bikes.Count);  // 3 MTB bicikli van a seedben
    }

    [Fact]
    public async Task GetBikes_FilterByType_Trekking_ReturnsTrekkingBikesOnly()
    {
        var res = await _client.GetAsync("/api/bikes?type=1");  // 1 = Trekking
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b => Assert.Equal(BikeType.Trekking, b.Type));
    }

    [Fact]
    public async Task GetBikes_FilterByType_City_ReturnsCityBikesOnly()
    {
        var res = await _client.GetAsync("/api/bikes?type=2");  // 2 = City
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b => Assert.Equal(BikeType.City, b.Type));
    }

    [Fact]
    public async Task GetBikes_FilterByType_EBike_ReturnsEBikesOnly()
    {
        var res = await _client.GetAsync("/api/bikes?type=3");  // 3 = EBike
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b => Assert.Equal(BikeType.EBike, b.Type));
    }

    [Fact]
    public async Task GetBikes_FilterByType_Kid_ReturnsKidBikesOnly()
    {
        var res = await _client.GetAsync("/api/bikes?type=4");  // 4 = Kid
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b => Assert.Equal(BikeType.Kid, b.Type));
    }

    [Fact]
    public async Task GetBikes_FilterByAvailable_True_ReturnsOnlyAvailableBikes()
    {
        var res = await _client.GetAsync("/api/bikes?available=true");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.NotEmpty(bikes);
        Assert.All(bikes, b => Assert.True(b.IsAvailable));
        Assert.Equal(15, bikes.Count);  // Minden bicikli elérhető a seedben
    }

    [Fact]
    public async Task GetBikes_FilterByAvailable_False_ReturnsOnlyUnavailableBikes()
    {
        var res = await _client.GetAsync("/api/bikes?available=false");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Empty(bikes);  // Nincsenek nem elérhető biciklil a seedben
    }

    [Fact]
    public async Task GetBikes_FilterCombined_TypeAndAvailable_ReturnFilteredResult()
    {
        // MTB típusú és elérhető biciklil
        var res = await _client.GetAsync("/api/bikes?type=0&available=true");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.NotEmpty(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b => 
        {
            Assert.Equal(BikeType.MTB, b.Type);
            Assert.True(b.IsAvailable);
        });
    }

    [Fact]
    public async Task GetBikes_FilterCombined_CityAndUnavailable_ReturnsEmpty()
    {
        // City típusú és nem elérhető biciklil (nincs ilyen)
        var res = await _client.GetAsync("/api/bikes?type=2&available=false");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Empty(bikes);
    }

    [Fact]
    public async Task GetBikes_FilterCombined_EBikeAndAvailable_ReturnAllAvailableEBikes()
    {
        // EBike típusú és elérhető biciklil
        var res = await _client.GetAsync("/api/bikes?type=3&available=true");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Equal(3, bikes.Count);
        Assert.All(bikes, b =>
        {
            Assert.Equal(BikeType.EBike, b.Type);
            Assert.True(b.IsAvailable);
        });
    }
}
