using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserHeartsAndTimesOnTop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTimesOnTopAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesOnTop",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Upgrade existing users from 3 hearts (old default) to 5 hearts (new max)
            migrationBuilder.Sql("UPDATE [Users] SET [Hearts] = 5 WHERE [Hearts] = 3");

            // Remove progression chain: all lessons are now accessible when user has hearts
            migrationBuilder.Sql("UPDATE [Lessons] SET [IsLocked] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimesOnTopAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimesOnTop",
                table: "Users");
        }
    }
}
