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
    public async Task GetBikes_ReturnsEmptyList_WhenNoBikesExist()
    {
        var res = await _client.GetAsync("/api/bikes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var bikes = await res.Content.ReadFromJsonAsync<List<BikeDtoResponse>>();
        Assert.NotNull(bikes);
        Assert.Empty(bikes);
    }

    [Fact]
    public async Task GetBike_ReturnsNotFound_WhenBikeDoesNotExist()
    {
        var res = await _client.GetAsync("/api/bikes/999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task BikeEndpoints_AllowAnonymous_NoTokenRequired()
    {
        var res = await _client.GetAsync("/api/bikes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
