using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos
{
    public record BikeDtoResponse(int Id, string Name, BikeType Type, decimal DailyPrice, string ImageUrl, bool IsAvailable);

    public record BikeDtoCreateUpdate(
        [Required] string Name,
        [Required] BikeType Type,
        [Required] [Range(typeof(decimal), "0.01", "999999.99")] decimal DailyPrice,
        [Required] string ImageUrl
    );

    public record BikeDtoUpdateFull(
        [Required] string Name,
        [Required] BikeType Type,
        [Required] [Range(typeof(decimal), "0.01", "999999.99")] decimal DailyPrice,
        [Required] string ImageUrl,
        bool IsAvailable
    );
}
