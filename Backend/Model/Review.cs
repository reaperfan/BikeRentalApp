using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Review
{
    public int Id { get; set; }

    // Az értékelést író felhasználó
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
