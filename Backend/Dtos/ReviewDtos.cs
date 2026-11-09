namespace Backend.Dtos;

public sealed record CreateReviewRequest(int Rating, string? Comment);
public sealed record UpdateReviewRequest(int Rating, string? Comment);
public sealed record PublicReviewResponse(int Id, string AuthorName, int Rating, string? Comment, DateTimeOffset CreatedAt);
public sealed record OwnReviewResponse(int Id, int Rating, string? Comment, DateTimeOffset CreatedAt);
public sealed record ReviewPage(int Page, int PageSize, int TotalCount, IReadOnlyList<PublicReviewResponse> Items);
public sealed record ReviewSummary(int TotalCount, double? AverageRating, int OneStar, int TwoStars, int ThreeStars, int FourStars, int FiveStars);
