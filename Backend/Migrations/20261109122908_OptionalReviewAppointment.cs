using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class OptionalReviewAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceAppointmentId",
                table: "Reviews",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId",
                principalTable: "ServiceAppointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceAppointmentId",
                table: "Reviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId",
                principalTable: "ServiceAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
