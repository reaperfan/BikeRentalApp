using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class SeedBikesData : Migration
    {
        /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.InsertData(
                    table: "Bikes",
                    columns: new[] { "Name", "Type", "DailyPrice", "ImageUrl", "IsAvailable" },
                    values: new object[,]
                    {
                        { "Trek Marlin 7", 0, 8000m, "/images/bikes/BikeRental_Trek_Martin_7.jpg", true },
                        { "Cube Aim Pro", 0, 7500m, "/images/bikes/BikeRental_Cube_Aim_Pro.jpg", true },
                        { "Merida Big Nine 200", 0, 8500m, "/images/bikes/BikeRental_Merida_Big_Nine_200.jpg", true },
                        { "Trek Dual Sport 3", 1, 7000m, "/images/bikes/BikeRental_Trek_Dual_Sport_3.jpg", true },
                        { "Merida Crossway 20", 1, 6500m, "/images/bikes/BikeRental_Merida_Crossway_20.jpg", true },
                        { "Cube Nature Pro", 1, 7500m, "/images/bikes/BikeRental_Cube_Nature_Pro.jpg", true },
                        { "Csepel Royal", 2, 4500m, "/images/bikes/BikeRental_Csepel_Royal.jpg", true },
                        { "Neuzer City Life", 2, 5000m, "/images/bikes/BikeRental_Neuzer_City_Life.jpg", true },
                        { "Kellys Classic Urban", 2, 5500m, "/images/bikes/BikeRental_Kellys_Classic_Urban.jpg", true },
                        { "Cube Kathmandu Hybrid", 3, 14000m, "/images/bikes/BikeRental_Cube_Kathmandu_Hybrid.jpg", true },
                        { "Specialized Turbo Vado", 3, 15000m, "/images/bikes/BikeRental_Specialized_Turbo_Vado.jpg", true },
                        { "Haibike Trekking 4", 3, 14500m, "/images/bikes/BikeRental_Haibike_Trekking_4.jpg", true },
                        { "Cube Kid 200", 4, 3000m, "/images/bikes/BikeRental_Cube_Kid_200.jpg", true },
                        { "Merida Matts J24", 4, 3200m, "/images/bikes/BikeRental_Merida_Matts_J24.jpg", true },
                        { "Gepida Junior 24", 4, 2800m, "/images/bikes/BikeRental_Gepida_Junior_24.jpg", true }
                    });
            }

        /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DeleteData(
                    table: "Bikes",
                    keyColumn: "Id",
                    keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
            }
    }
}
