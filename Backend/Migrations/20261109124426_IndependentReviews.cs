using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class IndependentReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ServiceAppointmentId",
                table: "Reviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_CommentLength",
                table: "Reviews",
                sql: "\"Comment\" IS NULL OR length(\"Comment\") <= 2000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews",
                sql: "\"Rating\" BETWEEN 1 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_CommentLength",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "ServiceAppointmentId",
                table: "Reviews",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ServiceAppointments_ServiceAppointmentId",
                table: "Reviews",
                column: "ServiceAppointmentId",
                principalTable: "ServiceAppointments",
                principalColumn: "Id");
        }
    }
}
