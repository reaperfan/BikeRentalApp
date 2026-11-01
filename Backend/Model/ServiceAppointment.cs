using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class ServiceAppointment
{
    public int Id { get; set; }

    // A foglalást létrehozó felhasználó
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // A szerviz időpontja
    public DateTime AppointmentDate { get; set; }

    [Required, MaxLength(100)]
    public string BikeType { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ServiceType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ProblemDescription { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}