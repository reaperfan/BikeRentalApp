using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos
{
    public record RentalDtoResponse(
        int Id,
        int BikeId,
        string BikeName,
        string CustomerName,
        string CustomerEmail,
        DateTime StartDate,
        DateTime EndDate,
        DateTime CreatedAt
    );

    public record RentalDtoCreate(
        [Required] int BikeId,
        [Required] string CustomerName,
        [Required] [EmailAddress] string CustomerEmail,
        [Required] DateTime StartDate,
        [Required] DateTime EndDate
    );

    public record RentalDtoUpdate(
        [Required] DateTime StartDate,
        [Required] DateTime EndDate
    );
}
