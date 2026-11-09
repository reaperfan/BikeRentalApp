using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReviewPerAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId");
        }
    }
}
