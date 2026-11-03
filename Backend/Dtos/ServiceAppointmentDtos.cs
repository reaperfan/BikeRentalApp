namespace Backend.Dtos;

// A dátumhoz kötelező időzónaeltolást (pl. +02:00) vagy Z jelölést küldeni.
public sealed record ServiceAppointmentRequest(DateTimeOffset AppointmentDate, string BikeType,
    string ServiceType, string? ProblemDescription);
public sealed record ServiceStatusRequest(string Status);
public sealed record ServiceAppointmentResponse(int Id, DateTimeOffset AppointmentDate, string BikeType,
    string ServiceType, string? ProblemDescription, string Status, DateTimeOffset CreatedAt);
