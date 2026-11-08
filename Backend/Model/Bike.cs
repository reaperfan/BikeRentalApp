using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Bike
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public BikeType Type { get; set; }
        [Required] public decimal DailyPrice { get; set; }
        [Required] public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }
}
