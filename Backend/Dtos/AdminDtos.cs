namespace Backend.Dtos;

public sealed record AdminStatsResponse(
    int BikeCount,
    int AvailableBikeCount,
    int UserCount,
    int AdminCount,
    int RentalCount,
    int ActiveRentalCount,
    int UpcomingRentalCount,
    decimal TotalRevenue,
    int PendingServiceAppointmentCount,
    int ReviewCount,
    double? AverageRating);

public sealed record AdminUserResponse(int Id, string Username, string Email, string Role, DateTime CreatedAt);
public sealed record UserRoleRequest(string Role);

public sealed record AdminRentalResponse(
    int Id,
    int BikeId,
    string BikeName,
    string CustomerName,
    string CustomerEmail,
    DateTime StartDate,
    DateTime EndDate,
    DateTime CreatedAt,
    int Days,
    decimal TotalPrice);
