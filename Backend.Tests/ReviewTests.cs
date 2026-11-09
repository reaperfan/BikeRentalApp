using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public sealed class ReviewTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();
    private HttpClient Client() => _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    private async Task<(HttpClient Client, int UserId)> Customer()
    {
        var client = Client();
        var email = $"review-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto("Értékelő", email, "Password123!"))).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/auth/login", new UserLoginDto(email, "Password123!"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        return (client, me.GetProperty("id").GetInt32());
    }
    private static async Task<OwnReviewResponse> Create(HttpClient client, int rating = 5, string? comment = "Köszönöm!")
    {
        var result = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(rating, comment));
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<OwnReviewResponse>())!;
    }

    [Fact]
    public async Task WithoutAnyService_CreateReadEditDelete()
    {
        var (client, userId) = await Customer(); using var owner = client; using var publicClient = Client();
        using (var scope = _factory.Services.CreateScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<BikeDbContext>().ServiceAppointments.AnyAsync(a => a.UserId == userId));
        var review = await Create(client, 5, "  Pontos munka.  ");
        Assert.Equal("Pontos munka.", review.Comment);
        Assert.Equal(TimeSpan.Zero, review.CreatedAt.Offset);
        Assert.Single((await client.GetFromJsonAsync<OwnReviewResponse[]>("/api/reviews/mine"))!);
        var publicReview = await publicClient.GetFromJsonAsync<JsonElement>($"/api/reviews/{review.Id}");
        Assert.Equal("Értékelő", publicReview.GetProperty("authorName").GetString());
        foreach (var hidden in new[] { "email", "userId", "serviceAppointmentId", "passwordHash", "serviceAppointment", "user" })
            Assert.False(publicReview.TryGetProperty(hidden, out _));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(4, "Javított vélemény"))).StatusCode);
        var edited = await publicClient.GetFromJsonAsync<PublicReviewResponse>($"/api/reviews/{review.Id}");
        Assert.Equal(4, edited!.Rating);
        Assert.Equal(review.CreatedAt, edited.CreatedAt);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/reviews/{review.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/api/reviews/{review.Id}")).StatusCode);
    }

    [Fact]
    public async Task OtherCustomersCannotChangeSomeoneElsesReview()
    {
        var (client, userId) = await Customer(); using var owner = client;
        var (other, _) = await Customer(); using var outsider = other;
        var review = await Create(client);
        Assert.Empty((await other.GetFromJsonAsync<OwnReviewResponse[]>("/api/reviews/mine"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(1, "Más nevében"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/reviews/{review.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/reviews/admin/{review.Id}")).StatusCode);
    }

    [Fact]
    public async Task MultipleReviewsCanBeSubmittedIndependently()
    {
        var (client, _) = await Customer(); using var owner = client;
        var results = await Task.WhenAll(
            client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(5, "Első")),
            client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(4, "Második")));
        Assert.All(results, result => Assert.Equal(HttpStatusCode.Created, result.StatusCode));
        Assert.Equal(2, (await client.GetFromJsonAsync<OwnReviewResponse[]>("/api/reviews/mine"))!.Length);
        using var scope = _factory.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<BikeDbContext>().Reviews.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task InvalidRatingsAreRejectedOnCreateAndEdit(int rating)
    {
        var (client, userId) = await Customer(); using var owner = client;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(rating, null))).StatusCode);
        var review = await Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(rating, null))).StatusCode);
    }

    [Fact]
    public async Task CommentLimitsAreEnforcedAndWhitespaceIsNormalized()
    {
        var (client, userId) = await Customer(); using var owner = client;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(5, new string('x', 2001)))).StatusCode);
        var review = await Create(client, 1, new string('x', 2000));
        Assert.Equal(2000, review.Comment!.Length);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(5, new string('x', 2001)))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(5, "  "))).StatusCode);
        Assert.Null((await client.GetFromJsonAsync<PublicReviewResponse>($"/api/reviews/{review.Id}"))!.Comment);
    }

    [Fact]
    public async Task PublicPaginationFiltersAndSummaryReflectEditsAndDeletion()
    {
        using var anonymous = Client();
        var summary = (await anonymous.GetFromJsonAsync<ReviewSummary>("/api/reviews/summary"))!;
        Assert.Equal(0, summary.TotalCount); Assert.Null(summary.AverageRating);
        Assert.Empty((await anonymous.GetFromJsonAsync<ReviewPage>("/api/reviews"))!.Items);
        var (client, userId) = await Customer(); using var owner = client;
        var first = await Create(client, 5);
        var second = await Create(client, 1);
        summary = (await anonymous.GetFromJsonAsync<ReviewSummary>("/api/reviews/summary"))!;
        Assert.Equal(2, summary.TotalCount); Assert.Equal(3d, summary.AverageRating);
        Assert.Equal(1, summary.OneStar); Assert.Equal(1, summary.FiveStars);
        var page = (await anonymous.GetFromJsonAsync<ReviewPage>("/api/reviews?page=1&pageSize=1"))!;
        Assert.Equal(2, page.TotalCount); Assert.Equal(second.Id, Assert.Single(page.Items).Id);
        page = (await anonymous.GetFromJsonAsync<ReviewPage>("/api/reviews?page=2&pageSize=1"))!;
        Assert.Equal(first.Id, Assert.Single(page.Items).Id);
        page = (await anonymous.GetFromJsonAsync<ReviewPage>("/api/reviews?rating=5"))!;
        Assert.Equal(1, page.TotalCount); Assert.Equal(first.Id, Assert.Single(page.Items).Id);
        (await client.PutAsJsonAsync($"/api/reviews/{second.Id}", new UpdateReviewRequest(3, null))).EnsureSuccessStatusCode();
        Assert.Equal(4d, (await anonymous.GetFromJsonAsync<ReviewSummary>("/api/reviews/summary"))!.AverageRating);
        (await client.DeleteAsync($"/api/reviews/{first.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(3d, (await anonymous.GetFromJsonAsync<ReviewSummary>("/api/reviews/summary"))!.AverageRating);
        foreach (var query in new[] { "page=0", "page=2147483647", "pageSize=0", "pageSize=101", "rating=6", "rating=0", "page=abc" })
            Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync($"/api/reviews?{query}")).StatusCode);
    }

    [Fact]
    public async Task AuthenticationIsRequiredForWritesAndPrivateLists()
    {
        using var anonymous = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/reviews/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(5, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/api/reviews/1", new UpdateReviewRequest(5, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync("/api/reviews/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync("/api/reviews/admin/1")).StatusCode);
    }

    [Fact]
    public async Task AdminCanModerateButCannotEditSomeoneElsesReview()
    {
        var (client, userId) = await Customer(); using var owner = client;
        var review = await Create(client);
        using var admin = Client();
        var login = await admin.PostAsJsonAsync("/api/auth/login", new UserLoginDto("admin@test.com", "AdminPassword123!"));
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/reviews/{review.Id}", new UpdateReviewRequest(1, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/reviews/admin/{review.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/reviews/admin/{review.Id}")).StatusCode);
    }
    public void Dispose() => _factory.Dispose();
}
