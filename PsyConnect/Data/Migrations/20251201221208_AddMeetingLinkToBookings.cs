using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsyConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingLinkToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "Bookings");
        }
    }
}
