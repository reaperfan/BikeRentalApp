using Backend.Models;

namespace Backend.Dtos
{
    public record BikeDtoResponse(int Id, string Name, BikeType Type, decimal DailyPrice, string ImageUrl, bool IsAvailable);
}
