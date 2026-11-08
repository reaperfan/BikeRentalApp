using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Rental
    {
        public int Id { get; set; }

        [Required] public int BikeId { get; set; }
        [ForeignKey("BikeId")] public Bike? Bike { get; set; }

        [Required] public string CustomerName { get; set; } = string.Empty;
        [Required] public string CustomerEmail { get; set; } = string.Empty;

        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
