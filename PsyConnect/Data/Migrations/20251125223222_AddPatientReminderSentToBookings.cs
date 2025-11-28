using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientReminderSentToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PatientReminderSent",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientReminderSent",
                table: "Bookings");
        }
    }
}
