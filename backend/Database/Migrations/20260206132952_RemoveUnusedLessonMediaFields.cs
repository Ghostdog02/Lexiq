using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedLessonMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LessonMediaUrl",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonTextUrl",
                table: "Lessons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LessonMediaUrl",
                table: "Lessons",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonTextUrl",
                table: "Lessons",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
