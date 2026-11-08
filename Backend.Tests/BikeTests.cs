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
}
